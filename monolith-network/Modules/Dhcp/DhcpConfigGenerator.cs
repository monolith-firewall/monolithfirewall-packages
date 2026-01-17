using System.Net;
using System.Text;
using System.Text.Json;
using CL.SQLite.Services;
using CodeLogic;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Core.Models;
using Monolith.FireWall.Core.Services;

namespace Monolith.Network.Modules.Dhcp;

/// <summary>
/// Config generator for DHCP module.
/// Generates /etc/dhcp/dhcpd.conf and /etc/default/isc-dhcp-server from database settings.
/// </summary>
public class DhcpConfigGenerator : IModuleConfigGenerator
{
    public bool RequiresServiceRestart => true;

    public IEnumerable<string> GetConfigFilePaths()
    {
        return new[]
        {
            "/etc/dhcp/dhcpd.conf",
            "/etc/default/isc-dhcp-server"
        };
    }

    public async Task<ModuleConfigGenerationResult> GenerateConfigAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        var result = new ModuleConfigGenerationResult
        {
            Success = false,
            GeneratedFiles = new List<string>()
        };

        try
        {
            // NOTE: Package modules run inside Core, but IModuleContext is not a full DI container.
            // Use the built-in context logger and write files directly (Core runs with privileges).
            var logger = context.Logger;

            // Get SQLite library
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                result.Error = "SQLite library not available";
                return result;
            }

            // Get DHCP settings
            var settingsRepo = sqlite.CreateRepository<DhcpSettingsEntity>();
            var settingsResult = await settingsRepo.GetAllAsync();
            var globalSettings = settingsResult.IsSuccess && settingsResult.Data != null && settingsResult.Data.Any()
                ? settingsResult.Data.First()
                : null;

            // Get DHCP interfaces
            var interfacesRepo = sqlite.CreateRepository<DhcpInterfaceEntity>();
            var interfacesResult = await interfacesRepo.GetAllAsync();
            var dhcpInterfaces = interfacesResult.IsSuccess && interfacesResult.Data != null
                ? interfacesResult.Data.Where(i => i.Enabled).ToList()
                : new List<DhcpInterfaceEntity>();

            // If no interfaces configured, try to create defaults
            if (dhcpInterfaces.Count == 0)
            {
                logger?.LogInformation("No DHCP interfaces configured, attempting to create defaults");
                await CreateDefaultDhcpConfigAsync(sqlite, logger, cancellationToken);
                
                // Re-fetch interfaces
                interfacesResult = await interfacesRepo.GetAllAsync();
                dhcpInterfaces = interfacesResult.IsSuccess && interfacesResult.Data != null
                    ? interfacesResult.Data.Where(i => i.Enabled).ToList()
                    : new List<DhcpInterfaceEntity>();
            }

            // If still no interfaces, DHCP is not configured
            if (dhcpInterfaces.Count == 0)
            {
                logger?.LogInformation("No DHCP interfaces enabled, skipping config generation");
                result.Success = true;
                result.Metadata["skipped"] = true;
                result.Metadata["reason"] = "No enabled DHCP interfaces";
                return result;
            }

            // Generate dhcpd.conf
            var dhcpdConfPath = "/etc/dhcp/dhcpd.conf";
            var dhcpdConf = GenerateDhcpdConf(dhcpInterfaces, globalSettings, logger);
            await WriteConfigFileAsync(dhcpdConfPath, dhcpdConf, cancellationToken);
            result.GeneratedFiles.Add(dhcpdConfPath);

            // Generate isc-dhcp-server defaults
            var iscDefaultsPath = "/etc/default/isc-dhcp-server";
            var iscDefaults = GenerateIscDefaults(dhcpInterfaces);
            await WriteConfigFileAsync(iscDefaultsPath, iscDefaults, cancellationToken);
            result.GeneratedFiles.Add(iscDefaultsPath);

            result.Success = true;
            result.RequiresRestart = true;
            result.Metadata["interfaces"] = dhcpInterfaces.Count;
            result.Metadata["enabled"] = globalSettings?.Enabled ?? false;

            logger?.LogInformation($"✓ Generated DHCP config for {dhcpInterfaces.Count} interface(s)");
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            context.Logger?.LogError(ex, "Error generating DHCP config");
        }

        return result;
    }

    private async Task CreateDefaultDhcpConfigAsync(
        CL.SQLite.SQLiteLibrary sqlite,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get interface assignments to find LAN interface
            var interfaceStore = new InterfaceAssignmentStore();
            var assignments = await interfaceStore.GetAssignmentsAsync();
            
            // Find LAN interface (role = Lan)
            var lanAssignment = assignments.FirstOrDefault(a => a.Role == InterfaceRole.Lan);

            if (lanAssignment == null)
            {
                logger?.LogWarning("No LAN interface found, cannot create default DHCP config");
                return;
            }

            // Get system DNS servers - we'll use a type name string to avoid direct dependency
            // Try to get system settings via reflection or direct SQL query
            var systemSettingsRepo = sqlite.CreateRepository<SystemSettingsEntity>();
            var systemSettingsResult = await systemSettingsRepo.GetAllAsync();
            var systemSettings = systemSettingsResult.IsSuccess && systemSettingsResult.Data != null && systemSettingsResult.Data.Any()
                ? systemSettingsResult.Data.First()
                : null;

            var dnsServers = new List<string>();
            if (systemSettings?.DnsServers != null)
            {
                try
                {
                    dnsServers = JsonSerializer.Deserialize<List<string>>(systemSettings.DnsServers) ?? new List<string>();
                }
                catch
                {
                    // Fallback to parsing comma-separated
                    dnsServers = systemSettings.DnsServers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            // Default DNS if none configured
            if (dnsServers.Count == 0)
            {
                dnsServers.AddRange(new[] { "8.8.8.8", "8.8.4.4" });
            }

            // Calculate subnet from interface IP
            var interfaceIp = lanAssignment.IpAddress;
            // Use prefix length to calculate netmask, default to /24 if not set
            var prefixLength = lanAssignment.PrefixLength ?? 24;
            var interfaceNetmask = CalculateNetmaskFromPrefix(prefixLength);
            
            if (string.IsNullOrEmpty(interfaceIp) || !IPAddress.TryParse(interfaceIp, out var ip))
            {
                // Default to 192.168.1.0/24 if interface IP not set
                interfaceIp = "192.168.1.1";
                interfaceNetmask = "255.255.255.0";
            }

            var subnet = CalculateSubnet(interfaceIp, interfaceNetmask);
            var gateway = interfaceIp;
            var poolStart = CalculatePoolStart(subnet);
            var poolEnd = CalculatePoolEnd(subnet);

            // Create default DHCP interface config
            var dhcpInterfaceRepo = sqlite.CreateRepository<DhcpInterfaceEntity>();
            var dhcpInterface = new DhcpInterfaceEntity
            {
                InterfaceName = lanAssignment.InterfaceName,
                Enabled = true,
                Subnet = subnet,
                PoolStart = poolStart,
                PoolEnd = poolEnd,
                Gateway = gateway,
                DnsServers = JsonSerializer.Serialize(dnsServers),
                Domain = systemSettings?.Domain ?? "local",
                LeaseTime = 7200, // 2 hours
                MaxLeaseTime = 86400, // 24 hours
                ClientPolicy = "allow-all",
                StaticArp = false,
                UpdatedAt = DateTime.UtcNow
            };

            var insertResult = await dhcpInterfaceRepo.InsertAsync(dhcpInterface);
            if (insertResult.IsSuccess)
            {
                logger?.LogInformation($"Created default DHCP config for interface {lanAssignment.InterfaceName}");
            }

            // Create default global settings if not exists
            var settingsRepo = sqlite.CreateRepository<DhcpSettingsEntity>();
            var settingsResult = await settingsRepo.GetAllAsync();
            if (!settingsResult.IsSuccess || settingsResult.Data == null || !settingsResult.Data.Any())
            {
                var defaultSettings = new DhcpSettingsEntity
                {
                    Enabled = true,
                    DefaultLeaseTime = 7200,
                    MaxLeaseTime = 86400,
                    DnsRegistration = false,
                    LogLevel = "info",
                    UpdatedAt = DateTime.UtcNow
                };
                await settingsRepo.InsertAsync(defaultSettings);
                logger?.LogInformation("Created default DHCP global settings");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error creating default DHCP config");
        }
    }

    private string CalculateNetmaskFromPrefix(int prefixLength)
    {
        // Convert CIDR prefix to netmask
        var mask = (uint)(0xFFFFFFFF << (32 - prefixLength));
        var bytes = BitConverter.GetBytes(mask);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return new IPAddress(bytes).ToString();
    }

    private string CalculateSubnet(string ipAddress, string netmask)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip) || !IPAddress.TryParse(netmask, out var mask))
        {
            return "192.168.1.0/24"; // Default
        }

        var ipBytes = ip.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();

        // Calculate network address
        var networkBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
        }

        // Calculate CIDR prefix
        int cidr = 0;
        foreach (var b in maskBytes)
        {
            cidr += System.Numerics.BitOperations.PopCount(b);
        }

        return $"{new IPAddress(networkBytes)}/{cidr}";
    }

    private string CalculatePoolStart(string subnet)
    {
        // Extract network and calculate pool start (network + 100)
        var parts = subnet.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var network))
        {
            return "192.168.1.100";
        }

        var bytes = network.GetAddressBytes();
        if (bytes.Length == 4)
        {
            bytes[3] = 100; // .100
            return new IPAddress(bytes).ToString();
        }

        return "192.168.1.100";
    }

    private string CalculatePoolEnd(string subnet)
    {
        // Extract network and calculate pool end (network + 200)
        var parts = subnet.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var network))
        {
            return "192.168.1.200";
        }

        var bytes = network.GetAddressBytes();
        if (bytes.Length == 4)
        {
            bytes[3] = 200; // .200
            return new IPAddress(bytes).ToString();
        }

        return "192.168.1.200";
    }

    private string GenerateDhcpdConf(
        List<DhcpInterfaceEntity> interfaces,
        DhcpSettingsEntity? globalSettings,
        ILogger? logger)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("# Generated by Monolith FireWall");
        sb.AppendLine("# Do not edit this file manually - changes will be overwritten");
        sb.AppendLine();
        
        // Global settings
        if (globalSettings != null)
        {
            sb.AppendLine("# Global DHCP settings");
            sb.AppendLine($"default-lease-time {globalSettings.DefaultLeaseTime};");
            sb.AppendLine($"max-lease-time {globalSettings.MaxLeaseTime};");
            sb.AppendLine();
        }

        // Interface configurations
        foreach (var iface in interfaces)
        {
            if (string.IsNullOrEmpty(iface.Subnet) || 
                string.IsNullOrEmpty(iface.PoolStart) || 
                string.IsNullOrEmpty(iface.PoolEnd))
            {
                logger?.LogWarning($"Skipping DHCP interface {iface.InterfaceName}: missing subnet/pool configuration");
                continue;
            }

            sb.AppendLine($"# DHCP configuration for {iface.InterfaceName}");
            sb.AppendLine($"subnet {iface.Subnet.Replace("/", " netmask ")} {{");
            
            // Range
            sb.AppendLine($"  range {iface.PoolStart} {iface.PoolEnd};");
            
            // Gateway
            if (!string.IsNullOrEmpty(iface.Gateway))
            {
                sb.AppendLine($"  option routers {iface.Gateway};");
            }
            
            // DNS servers
            if (!string.IsNullOrEmpty(iface.DnsServers))
            {
                try
                {
                    var dnsServers = JsonSerializer.Deserialize<List<string>>(iface.DnsServers);
                    if (dnsServers != null && dnsServers.Count > 0)
                    {
                        sb.AppendLine($"  option domain-name-servers {string.Join(", ", dnsServers)};");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"Failed to parse DNS servers for {iface.InterfaceName}: {ex.Message}");
                }
            }
            
            // Domain
            if (!string.IsNullOrEmpty(iface.Domain))
            {
                sb.AppendLine($"  option domain-name \"{iface.Domain}\";");
            }
            
            // Lease time
            sb.AppendLine($"  default-lease-time {iface.LeaseTime};");
            sb.AppendLine($"  max-lease-time {iface.MaxLeaseTime};");
            
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateIscDefaults(List<DhcpInterfaceEntity> interfaces)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Generated by Monolith FireWall");
        sb.AppendLine("# Do not edit this file manually - changes will be overwritten");
        sb.AppendLine();
        
        // Get list of interfaces
        var interfaceList = string.Join(" ", interfaces.Select(i => i.InterfaceName));
        
        sb.AppendLine($"INTERFACESv4=\"{interfaceList}\"");
        sb.AppendLine("INTERFACESv6=\"\"");
        
        return sb.ToString();
    }

    private async Task WriteConfigFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        // Ensure directory exists
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Write file
        await File.WriteAllTextAsync(path, content, cancellationToken);

        // Set permissions (readable by dhcp user)
        try
        {
            var chmodInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"644 {path}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(chmodInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch
        {
            // Best effort - file permissions are not critical
        }
    }
}
