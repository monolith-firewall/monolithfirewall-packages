using System.Text.Json;
using System.Text.RegularExpressions;
using CL.SQLite.Services;
using CodeLogic;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;
using Monolith.FireWall.Core.Models;
using Monolith.FireWall.Core.Services;
using Monolith.FireWall.Platform;
using Monolith.FireWall.Platform.Validation;
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
                
                // Ensure tables are created by attempting a sync
                // CL.SQLite will create tables automatically, but we can force it
                try
                {
                    // Create repositories for all DHCP entities to ensure tables exist
                    var _ = sqlite.CreateRepository<DhcpInterfaceEntity>();
                    var __ = sqlite.CreateRepository<DhcpSettingsEntity>();
                }
                catch
                {
                    // Tables will be created on first insert
                }
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
            var config = JsonSerializer.Deserialize<DhcpConfig>(
                configJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            if (config == null)
                return false;

            if (!ValidateDhcpConfig(config, out var error))
            {
                Console.WriteLine($"DhcpManager.UpdateConfigAsync: validation failed: {error}");
                return false;
            }

            // For now, just validate (storage/config generation lives elsewhere)
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
        try
        {
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                return new DhcpSettings
                {
                    Enabled = false,
                    DefaultLeaseTime = 7200,
                    MaxLeaseTime = 86400,
                    DnsRegistration = false,
                    LogLevel = "info"
                };
            }

            var queryBuilder = sqlite.CreateQueryBuilder<DhcpSettingsEntity>();
            var result = await queryBuilder.FirstOrDefaultAsync();

            if (result.IsSuccess && result.Data != null)
            {
                var entity = result.Data;
                return new DhcpSettings
                {
                    Enabled = entity.Enabled,
                    DefaultLeaseTime = entity.DefaultLeaseTime,
                    MaxLeaseTime = entity.MaxLeaseTime,
                    DnsRegistration = entity.DnsRegistration,
                    LogLevel = entity.LogLevel
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        // Return defaults if no settings found
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
            var settings = JsonSerializer.Deserialize<DhcpSettings>(
                settingsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            if (settings == null)
                return false;

            // Get SQLite library
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                Console.WriteLine("SQLite library not available");
                return false;
            }

            var repository = sqlite.CreateRepository<DhcpSettingsEntity>();
            var queryBuilder = sqlite.CreateQueryBuilder<DhcpSettingsEntity>();

            // Get existing settings or create new
            var existingResult = await queryBuilder.FirstOrDefaultAsync();
            DhcpSettingsEntity entity;

            if (existingResult.IsSuccess && existingResult.Data != null)
            {
                entity = existingResult.Data;
            }
            else
            {
                entity = new DhcpSettingsEntity
                {
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Update entity from settings (note: enabled is now controlled per-interface)
            entity.DefaultLeaseTime = settings.DefaultLeaseTime;
            entity.MaxLeaseTime = settings.MaxLeaseTime;
            entity.DnsRegistration = settings.DnsRegistration;
            entity.LogLevel = settings.LogLevel;
            entity.UpdatedAt = DateTime.UtcNow;

            // Save to database
            bool saveSuccess;
            if (entity.Id == 0)
            {
                var insertResult = await repository.InsertAsync(entity);
                saveSuccess = insertResult.IsSuccess;
            }
            else
            {
                var updateResult = await repository.UpdateAsync(entity);
                saveSuccess = updateResult.IsSuccess;
            }

            if (saveSuccess)
            {
                Console.WriteLine("DHCP global settings saved");
            }

            return saveSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating settings: {ex.Message}");
            return false;
        }
    }

    public async Task<List<DhcpInterface>> GetInterfacesAsync(UserContext? user = null)
    {
        // First, get available interfaces from platform/filesystem
        var platformInterfaces = await GetInterfacesFromPlatformAsync(user);
        if (platformInterfaces.Count == 0)
        {
            platformInterfaces = await GetInterfacesFromFilesystemAsync();
        }

        // Load saved configurations from database
        var savedConfigs = await LoadInterfaceConfigsFromDatabaseAsync();

        // Merge: use saved config if exists, otherwise use platform data
        var mergedInterfaces = new List<DhcpInterface>();
        foreach (var platformIface in platformInterfaces)
        {
            var savedConfig = savedConfigs.FirstOrDefault(s => s.InterfaceName == platformIface.Name);
            if (savedConfig != null)
            {
                // Use saved configuration
                mergedInterfaces.Add(new DhcpInterface
                {
                    Name = savedConfig.InterfaceName,
                    Enabled = savedConfig.Enabled,
                    Subnet = savedConfig.Subnet ?? platformIface.Subnet,
                    ClientPolicy = savedConfig.ClientPolicy,
                    PoolStart = savedConfig.PoolStart ?? "",
                    PoolEnd = savedConfig.PoolEnd ?? "",
                    Dns1 = ExtractDnsServer(savedConfig.DnsServers, 0),
                    Dns2 = ExtractDnsServer(savedConfig.DnsServers, 1),
                    Dns3 = ExtractDnsServer(savedConfig.DnsServers, 2),
                    Dns4 = ExtractDnsServer(savedConfig.DnsServers, 3),
                    Gateway = savedConfig.Gateway ?? "",
                    Domain = savedConfig.Domain ?? "",
                    LeaseTime = savedConfig.LeaseTime,
                    MaxLeaseTime = savedConfig.MaxLeaseTime,
                    StaticArp = savedConfig.StaticArp
                });
            }
            else
            {
                // Use platform data (no saved config)
                mergedInterfaces.Add(platformIface);
            }
        }

        return mergedInterfaces;
    }

    private async Task<List<DhcpInterfaceEntity>> LoadInterfaceConfigsFromDatabaseAsync()
    {
        try
        {
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                return new List<DhcpInterfaceEntity>();
            }

            var repository = sqlite.CreateRepository<DhcpInterfaceEntity>();
            var result = await repository.GetAllAsync();
            return result.IsSuccess && result.Data != null
                ? result.Data.ToList()
                : new List<DhcpInterfaceEntity>();
        }
        catch
        {
            return new List<DhcpInterfaceEntity>();
        }
    }

    private string ExtractDnsServer(string? dnsServersJson, int index)
    {
        if (string.IsNullOrEmpty(dnsServersJson))
        {
            return "";
        }

        try
        {
            var servers = JsonSerializer.Deserialize<List<string>>(dnsServersJson);
            if (servers != null && index < servers.Count)
            {
                return servers[index];
            }
        }
        catch
        {
            // Best effort
        }

        return "";
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
            Console.WriteLine($"UpdateInterfaceAsync called with: {configJson}");
            var config = JsonSerializer.Deserialize<DhcpInterfaceConfig>(
                configJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );

            // Defensive parsing fallback: we've seen cases where System.Text.Json doesn't populate
            // properties as expected in this dynamically loaded module environment.
            if (config == null || string.IsNullOrEmpty(config.Interface))
            {
                if (TryParseDhcpInterfaceConfig(configJson, out var parsed))
                {
                    config = parsed;
                    Console.WriteLine("Parsed DHCP interface config via JSON fallback parser");
                }
            }

            if (config == null || string.IsNullOrEmpty(config.Interface))
            {
                Console.WriteLine("Invalid config: config is null or interface is empty");
                return false;
            }

            Console.WriteLine($"Updating interface: {config.Interface}");

            // Get SQLite library
            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                Console.WriteLine("SQLite library not available");
                return false;
            }

            Console.WriteLine("SQLite library obtained");

            // Ensure tables are synchronized first - this will create them if they don't exist
            try
            {
                Console.WriteLine("Syncing DHCP tables...");
                await sqlite.TableSyncService.SyncTableAsync<DhcpInterfaceEntity>(CancellationToken.None);
                Console.WriteLine("dhcp_interfaces table synced");
                await sqlite.TableSyncService.SyncTableAsync<DhcpSettingsEntity>(CancellationToken.None);
                Console.WriteLine("dhcp_settings table synced");
                await sqlite.TableSyncService.SyncTableAsync<DhcpLeaseEntity>(CancellationToken.None);
                Console.WriteLine("dhcp_leases table synced");
                Console.WriteLine("All DHCP tables synchronized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Table sync failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Continue anyway - table might already exist or will be created on insert
            }

            var repository = sqlite.CreateRepository<DhcpInterfaceEntity>();
            var queryBuilder = sqlite.CreateQueryBuilder<DhcpInterfaceEntity>();

            // Check if interface config already exists
            var existingResult = await queryBuilder
                .Where(e => e.InterfaceName == config.Interface)
                .FirstOrDefaultAsync();

            DhcpInterfaceEntity entity;
            if (existingResult.IsSuccess && existingResult.Data != null)
            {
                entity = existingResult.Data;
            }
            else
            {
                entity = new DhcpInterfaceEntity
                {
                    InterfaceName = config.Interface,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Update entity from config
            entity.Enabled = config.Enabled;
            entity.ClientPolicy = config.ClientPolicy;
            entity.PoolStart = config.PoolStart;
            entity.PoolEnd = config.PoolEnd;
            
            // Combine DNS servers into JSON array
            var dnsServers = new List<string>();
            if (!string.IsNullOrEmpty(config.Dns1)) dnsServers.Add(config.Dns1);
            if (!string.IsNullOrEmpty(config.Dns2)) dnsServers.Add(config.Dns2);
            if (!string.IsNullOrEmpty(config.Dns3)) dnsServers.Add(config.Dns3);
            if (!string.IsNullOrEmpty(config.Dns4)) dnsServers.Add(config.Dns4);
            entity.DnsServers = dnsServers.Count > 0 ? JsonSerializer.Serialize(dnsServers) : null;

            entity.Gateway = config.Gateway;
            entity.Domain = config.Domain;
            entity.LeaseTime = config.LeaseTime;
            entity.MaxLeaseTime = config.MaxLeaseTime;
            entity.StaticArp = config.StaticArp;
            entity.UpdatedAt = DateTime.UtcNow;

            // Calculate subnet from interface assignment if not set
            if (string.IsNullOrEmpty(entity.Subnet))
            {
                try
                {
                    // Try to get subnet from platform or filesystem
                    var interfaces = await GetInterfacesFromFilesystemAsync();
                    var iface = interfaces.FirstOrDefault(i => i.Name == config.Interface);
                    if (iface != null && !string.IsNullOrEmpty(iface.Subnet) && iface.Subnet != "Not configured")
                    {
                        entity.Subnet = iface.Subnet;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating subnet: {ex.Message}");
                    // Best effort - continue without subnet
                }
            }

            // Save to database
            bool saveSuccess = false;
            string? errorMessage = null;
            
            try
            {
                if (entity.Id == 0)
                {
                    var insertResult = await repository.InsertAsync(entity);
                    saveSuccess = insertResult.IsSuccess;
                    if (!saveSuccess)
                    {
                        errorMessage = "Insert failed";
                        Console.WriteLine($"Failed to insert DHCP interface config");
                    }
                }
                else
                {
                    var updateResult = await repository.UpdateAsync(entity);
                    saveSuccess = updateResult.IsSuccess;
                    if (!saveSuccess)
                    {
                        errorMessage = "Update failed";
                        Console.WriteLine($"Failed to update DHCP interface config");
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Console.WriteLine($"Exception saving DHCP interface config: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            if (saveSuccess)
            {
                Console.WriteLine($"DHCP interface configuration saved for {config.Interface}");
                
                // Trigger config file generation after successful save
                try
                {
                    if (_context != null)
                    {
                        var configGenerator = new DhcpConfigGenerator();
                        var genResult = await configGenerator.GenerateConfigAsync(_context, CancellationToken.None);
                        if (genResult.Success)
                        {
                            Console.WriteLine($"DHCP config file generated successfully");
                        }
                        else
                        {
                            Console.WriteLine($"DHCP config file generation failed: {genResult.Error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating DHCP config file: {ex.Message}");
                    // Don't fail the save operation if config generation fails
                }
            }
            else
            {
                // Log detailed error for debugging
                Console.WriteLine($"Save failed. Error: {errorMessage}");
            }

            return saveSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating interface: {ex.Message}");
            return false;
        }
    }

    private static bool TryParseDhcpInterfaceConfig(string json, out DhcpInterfaceConfig config)
    {
        config = new DhcpInterfaceConfig();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var name = prop.Name.Trim().ToLowerInvariant();
                try
                {
                    switch (name)
                    {
                        case "interface":
                            config.Interface = prop.Value.GetString() ?? "";
                            break;
                        case "enabled":
                            config.Enabled = prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False
                                ? prop.Value.GetBoolean()
                                : config.Enabled;
                            break;
                        case "clientpolicy":
                            config.ClientPolicy = prop.Value.GetString() ?? config.ClientPolicy;
                            break;
                        case "poolstart":
                            config.PoolStart = prop.Value.GetString() ?? config.PoolStart;
                            break;
                        case "poolend":
                            config.PoolEnd = prop.Value.GetString() ?? config.PoolEnd;
                            break;
                        case "dns1":
                            config.Dns1 = prop.Value.GetString() ?? config.Dns1;
                            break;
                        case "dns2":
                            config.Dns2 = prop.Value.GetString() ?? config.Dns2;
                            break;
                        case "dns3":
                            config.Dns3 = prop.Value.GetString() ?? config.Dns3;
                            break;
                        case "dns4":
                            config.Dns4 = prop.Value.GetString() ?? config.Dns4;
                            break;
                        case "gateway":
                            config.Gateway = prop.Value.GetString() ?? config.Gateway;
                            break;
                        case "domain":
                            config.Domain = prop.Value.GetString() ?? config.Domain;
                            break;
                        case "leasetime":
                            if (prop.Value.TryGetInt32(out var leaseTime))
                            {
                                config.LeaseTime = leaseTime;
                            }
                            break;
                        case "maxleasetime":
                            if (prop.Value.TryGetInt32(out var maxLeaseTime))
                            {
                                config.MaxLeaseTime = maxLeaseTime;
                            }
                            break;
                        case "staticarp":
                            if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            {
                                config.StaticArp = prop.Value.GetBoolean();
                            }
                            break;
                    }
                }
                catch
                {
                    // Ignore malformed fields, best-effort parsing.
                }
            }

            return !string.IsNullOrWhiteSpace(config.Interface);
        }
        catch
        {
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

    /// <summary>
    /// Start the DHCP server service
    /// </summary>
    public async Task<ServiceControlResult> StartServiceAsync()
    {
        if (_context == null)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = "Module context not available"
            };
        }

        try
        {
            // Get SystemdServiceManager from context
            var serviceManager = _context.GetService<SystemdServiceManager>();
            if (serviceManager == null)
            {
                return new ServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.StartServiceAsync("isc-dhcp-server");
            return new ServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "Service started successfully"
            };
        }
        catch (Exception ex)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = $"Error starting service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Stop the DHCP server service
    /// </summary>
    public async Task<ServiceControlResult> StopServiceAsync()
    {
        if (_context == null)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = "Module context not available"
            };
        }

        try
        {
            var serviceManager = _context.GetService<SystemdServiceManager>();
            if (serviceManager == null)
            {
                return new ServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.StopServiceAsync("isc-dhcp-server");
            return new ServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "Service stopped successfully"
            };
        }
        catch (Exception ex)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = $"Error stopping service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Restart the DHCP server service
    /// </summary>
    public async Task<ServiceControlResult> RestartServiceAsync()
    {
        if (_context == null)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = "Module context not available"
            };
        }

        try
        {
            var serviceManager = _context.GetService<SystemdServiceManager>();
            if (serviceManager == null)
            {
                return new ServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.RestartServiceAsync("isc-dhcp-server");
            return new ServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "Service restarted successfully"
            };
        }
        catch (Exception ex)
        {
            return new ServiceControlResult
            {
                Success = false,
                Message = $"Error restarting service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get the status of the DHCP server service
    /// </summary>
    public async Task<DhcpServiceStatus> GetServiceStatusAsync()
    {
        if (_context == null)
        {
            return new DhcpServiceStatus
            {
                IsRunning = false,
                IsEnabled = false,
                Status = "Unknown",
                Message = "Module context not available"
            };
        }

        try
        {
            var serviceManager = _context.GetService<SystemdServiceManager>();
            if (serviceManager == null)
            {
                return new DhcpServiceStatus
                {
                    IsRunning = false,
                    IsEnabled = false,
                    Status = "Unknown",
                    Message = "SystemdServiceManager not available"
                };
            }

            var status = await serviceManager.GetServiceStatusAsync("isc-dhcp-server");
            return new DhcpServiceStatus
            {
                IsRunning = status.IsRunning,
                IsEnabled = status.IsEnabled,
                Status = status.ActiveState.ToString(),
                Message = $"{status.RawActiveState} / {status.RawEnabledState}"
            };
        }
        catch (Exception ex)
        {
            return new DhcpServiceStatus
            {
                IsRunning = false,
                IsEnabled = false,
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Parse DHCP leases from ISC-DHCP-Server lease file and update database
    /// </summary>
    public async Task<LeaseParseResult> ParseAndUpdateLeasesAsync()
    {
        const string leaseFilePath = "/var/lib/dhcp/dhcpd.leases";
        var result = new LeaseParseResult
        {
            Success = false,
            ParsedCount = 0,
            UpdatedCount = 0
        };

        try
        {
            if (!File.Exists(leaseFilePath))
            {
                result.Message = $"Lease file not found: {leaseFilePath}";
                return result;
            }

            var leaseContent = await File.ReadAllTextAsync(leaseFilePath);
            var leases = ParseDhcpdLeases(leaseContent);
            result.ParsedCount = leases.Count;

            if (_repository == null)
            {
                result.Message = "Database repository not available";
                return result;
            }

            // Update leases in database
            foreach (var lease in leases)
            {
                try
                {
                    // Check if lease exists
                    var existingResult = await _queryBuilder!
                        .Where(l => l.MacAddress == lease.MacAddress)
                        .FirstOrDefaultAsync();

                    DhcpLeaseEntity entity;
                    if (existingResult.IsSuccess && existingResult.Data != null)
                    {
                        entity = existingResult.Data;
                    }
                    else
                    {
                        entity = new DhcpLeaseEntity
                        {
                            MacAddress = lease.MacAddress
                        };
                    }

                    // Update entity
                    entity.IpAddress = lease.IpAddress;
                    entity.Hostname = lease.Hostname;
                    entity.LeaseStart = lease.LeaseStart;
                    entity.LeaseEnd = lease.LeaseEnd;
                    entity.State = lease.State;
                    entity.UpdatedAt = DateTime.UtcNow;

                    // Save to database
                    if (entity.Id == 0)
                    {
                        var insertResult = await _repository.InsertAsync(entity);
                        if (insertResult.IsSuccess)
                        {
                            result.UpdatedCount++;
                        }
                    }
                    else
                    {
                        var updateResult = await _repository.UpdateAsync(entity);
                        if (updateResult.IsSuccess)
                        {
                            result.UpdatedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating lease for {lease.MacAddress}: {ex.Message}");
                }
            }

            result.Success = true;
            result.Message = $"Parsed {result.ParsedCount} leases, updated {result.UpdatedCount} in database";
        }
        catch (Exception ex)
        {
            result.Message = $"Error parsing leases: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Parse ISC-DHCP-Server lease file format
    /// </summary>
    private List<DhcpLease> ParseDhcpdLeases(string content)
    {
        var leases = new List<DhcpLease>();

        // Regex to match lease blocks
        var leaseRegex = new Regex(
            @"lease\s+([\d\.]+)\s+\{([^\}]+)\}",
            RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

        var matches = leaseRegex.Matches(content);
        foreach (Match match in matches)
        {
            if (match.Groups.Count < 3)
                continue;

            var ipAddress = match.Groups[1].Value.Trim();
            var leaseBlock = match.Groups[2].Value;

            var lease = new DhcpLease
            {
                IpAddress = ipAddress,
                State = "active"
            };

            // Extract MAC address
            var macMatch = Regex.Match(leaseBlock, @"hardware\s+ethernet\s+([\da-fA-F:]+)\s*;", RegexOptions.IgnoreCase);
            if (macMatch.Success)
            {
                lease.MacAddress = macMatch.Groups[1].Value.Trim().ToLowerInvariant();
            }

            // Extract hostname
            var hostnameMatch = Regex.Match(leaseBlock, @"client-hostname\s+""([^""]+)""\s*;", RegexOptions.IgnoreCase);
            if (hostnameMatch.Success)
            {
                lease.Hostname = hostnameMatch.Groups[1].Value.Trim();
            }

            // Extract lease start time
            var startsMatch = Regex.Match(leaseBlock, @"starts\s+\d+\s+([\d\/\s:]+)\s*;", RegexOptions.IgnoreCase);
            if (startsMatch.Success)
            {
                if (DateTime.TryParse(startsMatch.Groups[1].Value.Trim().Replace("/", "-"), out var startTime))
                {
                    lease.LeaseStart = startTime;
                }
            }

            // Extract lease end time
            var endsMatch = Regex.Match(leaseBlock, @"ends\s+\d+\s+([\d\/\s:]+)\s*;", RegexOptions.IgnoreCase);
            if (endsMatch.Success)
            {
                if (DateTime.TryParse(endsMatch.Groups[1].Value.Trim().Replace("/", "-"), out var endTime))
                {
                    lease.LeaseEnd = endTime;
                }
            }

            // Determine state
            if (lease.LeaseEnd != default && lease.LeaseEnd < DateTime.Now)
            {
                lease.State = "expired";
            }
            else if (leaseBlock.Contains("binding state active", StringComparison.OrdinalIgnoreCase))
            {
                lease.State = "active";
            }
            else if (leaseBlock.Contains("binding state free", StringComparison.OrdinalIgnoreCase))
            {
                lease.State = "free";
            }

            // Only add leases with MAC address
            if (!string.IsNullOrEmpty(lease.MacAddress))
            {
                leases.Add(lease);
            }
        }

        return leases;
    }

    private static bool ValidateDhcpConfig(DhcpConfig config, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(config.Interface) || !PlatformValidators.IsValidInterfaceName(config.Interface))
        {
            error = "Interface is required and must be a valid name";
            return false;
        }

        if (!PlatformValidators.IsValidIpv4(config.StartAddress) ||
            !PlatformValidators.IsValidIpv4(config.EndAddress))
        {
            error = "Start and end addresses must be valid IPv4 addresses";
            return false;
        }

        if (!PlatformValidators.IsValidIpv4(config.Gateway))
        {
            error = "Gateway must be a valid IPv4 address";
            return false;
        }

        if (!PlatformValidators.IsValidIpv4(config.SubnetMask))
        {
            error = "Subnet mask must be a valid IPv4 address";
            return false;
        }

        if (config.DnsServers != null && config.DnsServers.Length > 0)
        {
            if (!PlatformValidators.AreValidDnsServers(config.DnsServers))
            {
                error = "One or more DNS servers are invalid";
                return false;
            }
        }

        if (config.LeaseTime <= 0)
        {
            error = "Lease time must be greater than zero";
            return false;
        }

        return true;
    }
}

// Service control result model
public class ServiceControlResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// DHCP service status model
public class DhcpServiceStatus
{
    public bool IsRunning { get; set; }
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// Lease parse result model
public class LeaseParseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ParsedCount { get; set; }
    public int UpdatedCount { get; set;
}
}
