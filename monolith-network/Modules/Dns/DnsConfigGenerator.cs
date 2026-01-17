using System.Net;
using System.Text;
using System.Text.Json;
using CL.SQLite.Services;
using CodeLogic;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Core.Models;
using Monolith.FireWall.Core.Services;

namespace Monolith.Network.Modules.Dns;

/// <summary>
/// Config generator for DNS module.
/// Generates dnsmasq configuration from database settings.
/// Uses dnsmasq for simplicity and DHCP integration.
/// </summary>
public class DnsConfigGenerator : IModuleConfigGenerator
{
    public bool RequiresServiceRestart => true;

    public IEnumerable<string> GetConfigFilePaths()
    {
        return new[]
        {
            "/etc/dnsmasq.conf",
            "/etc/dnsmasq.d/monolith.conf"
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

            // Get DNS settings
            var settingsRepo = sqlite.CreateRepository<DnsSettingsEntity>();
            var settingsResult = await settingsRepo.GetAllAsync();
            var dnsSettings = settingsResult.IsSuccess && settingsResult.Data != null && settingsResult.Data.Any()
                ? settingsResult.Data.First()
                : null;

            // If no settings, create defaults
            if (dnsSettings == null)
            {
                logger?.LogInformation("No DNS settings found, creating defaults");
                await CreateDefaultDnsConfigAsync(sqlite, logger, cancellationToken);
                
                // Re-fetch settings
                settingsResult = await settingsRepo.GetAllAsync();
                dnsSettings = settingsResult.IsSuccess && settingsResult.Data != null && settingsResult.Data.Any()
                    ? settingsResult.Data.First()
                    : null;
            }

            // If DNS is not enabled, skip config generation
            if (dnsSettings == null || !dnsSettings.Enabled)
            {
                logger?.LogInformation("DNS is not enabled, skipping config generation");
                result.Success = true;
                result.Metadata["skipped"] = true;
                result.Metadata["reason"] = "DNS not enabled";
                return result;
            }

            // Generate dnsmasq config
            var dnsmasqConfPath = "/etc/dnsmasq.d/monolith.conf";
            var dnsmasqConf = GenerateDnsmasqConf(dnsSettings, logger);
            await WriteConfigFileAsync(dnsmasqConfPath, dnsmasqConf, cancellationToken);
            result.GeneratedFiles.Add(dnsmasqConfPath);

            result.Success = true;
            result.RequiresRestart = true;
            result.Metadata["enabled"] = true;
            result.Metadata["forwarding"] = dnsSettings.Forwarding;

            logger?.LogInformation("✓ Generated DNS config");
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            context.Logger?.LogError(ex, "Error generating DNS config");
        }

        return result;
    }

    private async Task CreateDefaultDnsConfigAsync(
        CL.SQLite.SQLiteLibrary sqlite,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get system DNS servers
            var systemSettingsRepo = sqlite.CreateRepository<SystemSettingsEntity>();
            var systemSettingsResult = await systemSettingsRepo.GetAllAsync();
            var systemSettings = systemSettingsResult.IsSuccess && systemSettingsResult.Data != null && systemSettingsResult.Data.Any()
                ? systemSettingsResult.Data.First()
                : null;

            var forwarders = new List<string>();
            if (systemSettings?.DnsServers != null)
            {
                try
                {
                    forwarders = JsonSerializer.Deserialize<List<string>>(systemSettings.DnsServers) ?? new List<string>();
                }
                catch
                {
                    // Fallback to parsing comma-separated
                    forwarders = systemSettings.DnsServers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            // Default forwarders if none configured
            if (forwarders.Count == 0)
            {
                forwarders.AddRange(new[] { "8.8.8.8", "8.8.4.4" });
            }

            // Get LAN interfaces
            var interfaceStore = new InterfaceAssignmentStore();
            var assignments = await interfaceStore.GetAssignmentsAsync();
            var lanInterfaces = assignments
                .Where(a => a.Role == InterfaceRole.Lan)
                .Select(a => a.InterfaceName)
                .ToList();

            // Create default DNS settings
            var settingsRepo = sqlite.CreateRepository<DnsSettingsEntity>();
            var defaultSettings = new DnsSettingsEntity
            {
                Enabled = true,
                Recursion = true,
                Forwarding = true,
                Forwarders = JsonSerializer.Serialize(forwarders),
                LogLevel = "info",
                DnssecValidation = true,
                LocalDomain = systemSettings?.Domain ?? "local",
                ListenInterfaces = lanInterfaces.Count > 0 
                    ? JsonSerializer.Serialize(lanInterfaces)
                    : null,
                UpdatedAt = DateTime.UtcNow
            };

            var insertResult = await settingsRepo.InsertAsync(defaultSettings);
            if (insertResult.IsSuccess)
            {
                logger?.LogInformation("Created default DNS settings");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error creating default DNS config");
        }
    }

    private string GenerateDnsmasqConf(DnsSettingsEntity settings, ILogger? logger)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("# Generated by Monolith FireWall");
        sb.AppendLine("# Do not edit this file manually - changes will be overwritten");
        sb.AppendLine();
        
        // Listen interfaces
        if (!string.IsNullOrEmpty(settings.ListenInterfaces))
        {
            try
            {
                var interfaces = JsonSerializer.Deserialize<List<string>>(settings.ListenInterfaces);
                if (interfaces != null && interfaces.Count > 0)
                {
                    foreach (var iface in interfaces)
                    {
                        sb.AppendLine($"interface={iface}");
                    }
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to parse listen interfaces: {ex.Message}");
            }
        }
        else
        {
            // Listen on all interfaces if not specified
            sb.AppendLine("# Listen on all interfaces");
            sb.AppendLine();
        }

        // Local domain
        if (!string.IsNullOrEmpty(settings.LocalDomain))
        {
            sb.AppendLine($"domain={settings.LocalDomain}");
            sb.AppendLine($"local=/{settings.LocalDomain}/");
            sb.AppendLine();
        }

        // Forwarders
        if (settings.Forwarding && !string.IsNullOrEmpty(settings.Forwarders))
        {
            try
            {
                var forwarders = JsonSerializer.Deserialize<List<string>>(settings.Forwarders);
                if (forwarders != null && forwarders.Count > 0)
                {
                    foreach (var forwarder in forwarders)
                    {
                        sb.AppendLine($"server={forwarder}");
                    }
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to parse forwarders: {ex.Message}");
            }
        }

        // Recursion
        if (!settings.Recursion)
        {
            sb.AppendLine("# Recursion disabled");
            sb.AppendLine();
        }

        // DNSSEC validation
        if (settings.DnssecValidation)
        {
            sb.AppendLine("dnssec");
            sb.AppendLine("trust-anchor=.,20326,8,2,E06D44B80B8F1D39A95C0B0D7C65D08458E880409BBC683457104237C7F8EC8D");
            sb.AppendLine();
        }

        // DHCP integration (read DHCP leases for hostname resolution)
        sb.AppendLine("# Read DHCP leases for hostname resolution");
        sb.AppendLine("dhcp-leasefile=/var/lib/dhcp/dhcpd.leases");
        sb.AppendLine("read-ethers");
        sb.AppendLine();

        // Logging
        if (settings.LogLevel.Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("log-queries");
            sb.AppendLine("log-dhcp");
        }

        // Cache size
        sb.AppendLine("cache-size=1000");
        sb.AppendLine();

        // Don't read /etc/resolv.conf (we manage forwarders)
        sb.AppendLine("no-resolv");
        sb.AppendLine();

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

        // Set permissions (readable by dnsmasq)
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
