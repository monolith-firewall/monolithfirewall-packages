using System.Text.Json;
using CL.SQLite.Services;
using CodeLogic;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;
using Monolith.FireWall.Platform;
using Monolith.FireWall.Platform.Models;

namespace Monolith.Network.Modules.Dhcp;

public class DhcpManager
{
    private readonly IModuleContext? _context;
    private readonly IPlatformClient? _platformClient;
    private Repository<DhcpLeaseEntity>? _repository;
    private QueryBuilder<DhcpLeaseEntity>? _queryBuilder;

    public DhcpManager(IModuleContext? context)
    {
        _context = context;
        _platformClient = GetPlatformClient(context);
        InitializeRepository();
    }

    private static IPlatformClient? GetPlatformClient(IModuleContext? context)
    {
        if (context == null)
        {
            return null;
        }

        try
        {
            return context.GetService<IPlatformClient>();
        }
        catch
        {
            return null;
        }
    }

    private void InitializeRepository()
    {
        try
        {
            // Get SQLite from CodeLogic service locator
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite != null)
            {
                _repository = sqlite.CreateRepository<DhcpLeaseEntity>();
                _queryBuilder = sqlite.CreateQueryBuilder<DhcpLeaseEntity>();
                
                // Table will be created automatically on first use
            }
        }
        catch (Exception ex)
        {
            // Log error - repository will be null
            Console.WriteLine($"Failed to initialize DHCP repository: {ex.Message}");
        }
    }

    public async Task<DhcpConfig> GetConfigAsync()
    {
        // For now, return default config
        // In future, load from database or config file
        return new DhcpConfig
        {
            Enabled = true,
            Interface = "eth0",
            StartAddress = "192.168.1.100",
            EndAddress = "192.168.1.200",
            SubnetMask = "255.255.255.0",
            Gateway = "192.168.1.1",
            DnsServers = new[] { "8.8.8.8", "8.8.4.4" },
            LeaseTime = 3600
        };
    }

    public async Task<bool> UpdateConfigAsync(string configJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<DhcpConfig>(configJson);
            if (config == null)
                return false;

            // For now, just validate
            // In future, save to database and update dnsmasq config
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<DhcpLease>> ListLeasesAsync()
    {
        var leases = new List<DhcpLease>();

        if (_repository == null)
        {
            return leases;
        }

        try
        {
            var result = await _repository.GetAllAsync();
            
            // If table doesn't exist yet, just return empty list
            if (!result.IsSuccess)
            {
                return leases;
            }
            
            var entities = result.Data ?? new List<DhcpLeaseEntity>();
            foreach (var entity in entities)
            {
                leases.Add(new DhcpLease
                {
                    MacAddress = entity.MacAddress,
                    IpAddress = entity.IpAddress,
                    Hostname = entity.Hostname ?? "",
                    LeaseStart = entity.LeaseStart,
                    LeaseEnd = entity.LeaseEnd,
                    State = entity.State,
                    Interface = "",
                    Status = entity.State
                });
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - just return empty list
            Console.WriteLine($"Error listing DHCP leases: {ex.Message}");
        }

        return leases;
    }

    public async Task<DhcpLease?> GetLeaseByMacAsync(string macAddress)
    {
        if (_queryBuilder == null)
            return null;

        try
        {
            var result = await _queryBuilder
                .Where(l => l.MacAddress == macAddress)
                .FirstOrDefaultAsync();

            if (!result.IsSuccess || result.Data == null)
                return null;

            var entity = result.Data;

            return new DhcpLease
            {
                MacAddress = entity.MacAddress,
                IpAddress = entity.IpAddress,
                Hostname = entity.Hostname ?? "",
                LeaseStart = entity.LeaseStart,
                LeaseEnd = entity.LeaseEnd,
                State = entity.State
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<DhcpSettings> GetSettingsAsync()
    {
        // Return default settings
        // In future, load from database
        return new DhcpSettings
        {
            Enabled = false,
            DefaultLeaseTime = 7200,
            MaxLeaseTime = 86400,
            DnsRegistration = false,
            LogLevel = "info"
        };
    }

    public async Task<bool> UpdateSettingsAsync(string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<DhcpSettings>(settingsJson);
            if (settings == null)
                return false;

            // For now, just validate
            // In future, save to database
            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<DhcpInterface>> GetInterfacesAsync(UserContext? user = null)
    {
        var platformInterfaces = await GetInterfacesFromPlatformAsync(user);
        if (platformInterfaces.Count > 0)
        {
            return platformInterfaces;
        }

        return await GetInterfacesFromFilesystemAsync();
    }

    private async Task<List<DhcpInterface>> GetInterfacesFromPlatformAsync(UserContext? user)
    {
        if (_platformClient == null)
        {
            return new List<DhcpInterface>();
        }

        try
        {
            var platformContext = BuildPlatformContext(user);
            var interfaceResponse = await _platformClient.ExecuteAsync(
                "platform.network.interfaces.list",
                null,
                platformContext);
            if (!interfaceResponse.Success || interfaceResponse.Data == null)
            {
                return new List<DhcpInterface>();
            }

            var interfaceInfos = ExtractList<InterfaceInfo>(interfaceResponse.Data);
            if (interfaceInfos.Count == 0)
            {
                return new List<DhcpInterface>();
            }

            var addressResponse = await _platformClient.ExecuteAsync(
                "platform.network.addresses.list",
                null,
                platformContext);
            var addresses = addressResponse.Success
                ? ExtractList<AddressInfo>(addressResponse.Data)
                : new List<AddressInfo>();

            var ipv4ByInterface = addresses
                .Where(a => string.Equals(a.Family, "inet", StringComparison.OrdinalIgnoreCase))
                .GroupBy(a => a.Interface)
                .ToDictionary(group => group.Key, group => group.First());

            var interfaces = new List<DhcpInterface>();
            foreach (var iface in interfaceInfos)
            {
                if (string.Equals(iface.Name, "lo", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var subnet = "Not configured";
                if (ipv4ByInterface.TryGetValue(iface.Name, out var ipv4))
                {
                    subnet = $"{ipv4.Address}/{ipv4.PrefixLength}";
                }

                interfaces.Add(new DhcpInterface
                {
                    Name = iface.Name,
                    Enabled = iface.IsUp,
                    Subnet = subnet,
                    ClientPolicy = "allow-all",
                    PoolStart = "",
                    PoolEnd = "",
                    Dns1 = "8.8.8.8",
                    Dns2 = "8.8.4.4",
                    Dns3 = "",
                    Dns4 = "",
                    Gateway = "",
                    Domain = "local.domain",
                    LeaseTime = 7200,
                    MaxLeaseTime = 86400,
                    StaticArp = false
                });
            }

            return interfaces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting interfaces from platform: {ex.Message}");
            return new List<DhcpInterface>();
        }
    }

    private async Task<List<DhcpInterface>> GetInterfacesFromFilesystemAsync()
    {
        var interfaces = new List<DhcpInterface>();

        try
        {
            var netPath = "/sys/class/net";
            if (Directory.Exists(netPath))
            {
                foreach (var dir in Directory.GetDirectories(netPath))
                {
                    var ifaceName = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(ifaceName))
                    {
                        continue;
                    }

                    if (ifaceName == "lo")
                    {
                        continue;
                    }

                    var operstatePath = Path.Combine(dir, "operstate");
                    bool isUp = false;
                    if (File.Exists(operstatePath))
                    {
                        var operstate = (await File.ReadAllTextAsync(operstatePath)).Trim();
                        isUp = operstate == "up";
                    }

                    string? subnet = null;
                    try
                    {
                        var result = await RunCommandAsync("ip", $"-4 addr show {ifaceName}");
                        var lines = result.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains("inet "))
                            {
                                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    subnet = parts[1];
                                }
                                break;
                            }
                        }
                    }
                    catch
                    {
                    }

                    interfaces.Add(new DhcpInterface
                    {
                        Name = ifaceName,
                        Enabled = isUp,
                        Subnet = subnet ?? "Not configured",
                        ClientPolicy = "allow-all",
                        PoolStart = "",
                        PoolEnd = "",
                        Dns1 = "8.8.8.8",
                        Dns2 = "8.8.4.4",
                        Dns3 = "",
                        Dns4 = "",
                        Gateway = "",
                        Domain = "local.domain",
                        LeaseTime = 7200,
                        MaxLeaseTime = 86400,
                        StaticArp = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting interfaces: {ex.Message}");
        }

        return interfaces;
    }

    public async Task<bool> UpdateInterfaceAsync(string configJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<DhcpInterfaceConfig>(configJson);
            if (config == null)
                return false;

            // For now, just validate
            // In future, save to database and update DHCP server config
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating interface: {ex.Message}");
            return false;
        }
    }

    private static PlatformContext BuildPlatformContext(UserContext? user)
    {
        var context = new PlatformContext();
        if (user != null)
        {
            context.UserId = user.UserId;
            context.Permissions = user.Permissions;
        }

        return context;
    }

    private static List<T> ExtractList<T>(object? data)
    {
        if (data == null)
        {
            return new List<T>();
        }

        if (data is IEnumerable<T> items)
        {
            return items.ToList();
        }

        if (data is JsonElement element)
        {
            try
            {
                var result = JsonSerializer.Deserialize<List<T>>(element.GetRawText());
                return result ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        return new List<T>();
    }

    private async Task<string> RunCommandAsync(string command, string arguments)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }
        catch
        {
            return "";
        }
    }
}
