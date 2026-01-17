using System.Text.Json;
using CodeLogic;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Core.Services;
using Monolith.FireWall.Platform.Validation;

namespace Monolith.Network.Modules.Dns;

public class DnsManager
{
    private readonly IModuleContext? _context;

    public DnsManager(IModuleContext? context)
    {
        _context = context;
    }

    public async Task<DnsSettings> GetSettingsAsync()
    {
        var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
        if (sqlite == null)
        {
            return GetDefaultSettings();
        }

        var repo = sqlite.CreateRepository<DnsSettingsEntity>();
        var all = await repo.GetAllAsync();
        var entity = all.IsSuccess && all.Data != null && all.Data.Any()
            ? all.Data.First()
            : null;

        // Create defaults on first run so config generation has something to read later.
        if (entity == null)
        {
            entity = new DnsSettingsEntity
            {
                Enabled = false,
                Recursion = true,
                Forwarding = false,
                Forwarders = JsonSerializer.Serialize(new[] { "8.8.8.8", "8.8.4.4" }),
                LogLevel = "info",
                DnssecValidation = true,
                UpdatedAt = DateTime.UtcNow
            };
            _ = await repo.InsertAsync(entity);
        }

        var forwarders = new List<string>();
        if (!string.IsNullOrEmpty(entity.Forwarders))
        {
            try
            {
                forwarders = JsonSerializer.Deserialize<List<string>>(entity.Forwarders) ?? new List<string>();
            }
            catch
            {
                forwarders = entity.Forwarders.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }

        return new DnsSettings
        {
            Enabled = entity.Enabled,
            Recursion = entity.Recursion,
            Forwarding = entity.Forwarding,
            Forwarders = forwarders.ToArray(),
            LogLevel = entity.LogLevel,
            DnssecValidation = entity.DnssecValidation
        };
    }

    public async Task<bool> UpdateSettingsAsync(string settingsJson)
    {
        try
        {
            Console.WriteLine($"DnsManager.UpdateSettingsAsync called with: {settingsJson}");
            var settings = JsonSerializer.Deserialize<DnsSettings>(
                settingsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            if (settings == null)
            {
                Console.WriteLine("DnsManager.UpdateSettingsAsync: settings deserialized null");
                return false;
            }

            if (!ValidateDnsSettings(settings, out var error))
            {
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: validation failed: {error}");
                return false;
            }

            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                Console.WriteLine("DnsManager.UpdateSettingsAsync: SQLite library not available");
                return false;
            }

            // Ensure table exists (and attempt to sync schema) before writing.
            try
            {
                if (sqlite.TableSyncService != null)
                {
                    await sqlite.TableSyncService.SyncTableAsync<DnsSettingsEntity>(CancellationToken.None);
                    Console.WriteLine("DnsManager.UpdateSettingsAsync: dns_settings table synced");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: table sync failed: {ex.Message}");
            }

            var repo = sqlite.CreateRepository<DnsSettingsEntity>();
            var all = await repo.GetAllAsync();
            var entity = all.IsSuccess && all.Data != null && all.Data.Any()
                ? all.Data.First()
                : new DnsSettingsEntity();

            entity.Enabled = settings.Enabled;
            entity.Recursion = settings.Recursion;
            entity.Forwarding = settings.Forwarding;
            entity.Forwarders = settings.Forwarders.Length > 0 ? JsonSerializer.Serialize(settings.Forwarders) : null;
            entity.LogLevel = settings.LogLevel ?? "info";
            entity.DnssecValidation = settings.DnssecValidation;
            entity.UpdatedAt = DateTime.UtcNow;

            bool ok;
            if (entity.Id == 0)
            {
                var insert = await repo.InsertAsync(entity);
                ok = insert.IsSuccess;
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: insert success={insert.IsSuccess}");
            }
            else
            {
                var update = await repo.UpdateAsync(entity);
                ok = update.IsSuccess;
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: update success={update.IsSuccess}");
            }

            if (!ok)
            {
                Console.WriteLine("DnsManager.UpdateSettingsAsync: save failed (IsSuccess=false)");
                return false;
            }

            // Sanity check: re-load to ensure it actually persisted.
            try
            {
                var after = await repo.GetAllAsync();
                var count = after.IsSuccess && after.Data != null ? after.Data.Count : -1;
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: dns_settings row count after save = {count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DnsManager.UpdateSettingsAsync: post-save read failed: {ex.Message}");
            }

            // Trigger config generation (dnsmasq) after successful save
            try
            {
                if (_context != null)
                {
                    var generator = new DnsConfigGenerator();
                    var genResult = await generator.GenerateConfigAsync(_context, CancellationToken.None);
                    if (_context.Logger != null)
                    {
                        if (genResult.Success)
                        {
                            _context.Logger.LogInformation("DNS config file generated successfully");
                        }
                        else
                        {
                            _context.Logger.LogWarning($"DNS config generation failed: {genResult.Error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context?.Logger?.LogWarning($"Error generating DNS config: {ex.Message}");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateDnsSettings(DnsSettings settings, out string? error)
    {
        error = null;

        if (settings.Forwarders != null && settings.Forwarders.Length > 0)
        {
            foreach (var fwd in settings.Forwarders)
            {
                if (string.IsNullOrWhiteSpace(fwd) || !PlatformValidators.IsValidIp(fwd))
                {
                    error = $"Invalid forwarder address: {fwd}";
                    return false;
                }
            }
        }

        return true;
    }

    public async Task<List<DnsZone>> GetZonesAsync()
    {
        var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
        if (sqlite == null)
        {
            return new List<DnsZone>();
        }

        var repo = sqlite.CreateRepository<DnsZoneEntity>();
        var all = await repo.GetAllAsync();
        if (!all.IsSuccess || all.Data == null)
        {
            return new List<DnsZone>();
        }

        return all.Data.Select(z => new DnsZone
        {
            Name = z.ZoneName,
            Type = z.ZoneType,
            Enabled = z.Enabled,
            File = z.ZoneFile ?? "",
            Masters = DeserializeStringArray(z.Masters),
            AllowTransfer = z.AllowTransfer,
            AllowTransferTo = DeserializeStringArray(z.AllowTransferTo),
            Ttl = z.Ttl,
            SoaEmail = z.SoaEmail ?? "admin@example.com",
            Refresh = z.Refresh,
            Retry = z.Retry,
            Expire = z.Expire,
            NegativeTtl = z.NegativeTtl
        }).ToList();
    }

    public async Task<List<DnsRecord>> GetRecordsAsync(string zoneName = "")
    {
        var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
        if (sqlite == null)
        {
            return new List<DnsRecord>();
        }

        var repo = sqlite.CreateRepository<DnsRecordEntity>();
        var all = await repo.GetAllAsync();
        if (!all.IsSuccess || all.Data == null)
        {
            return new List<DnsRecord>();
        }

        var filtered = string.IsNullOrWhiteSpace(zoneName)
            ? all.Data
            : all.Data.Where(r => string.Equals(r.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase)).ToList();

        return filtered.Select(r => new DnsRecord
        {
            Id = r.Id.ToString(),
            Zone = r.ZoneName,
            Name = r.RecordName,
            Type = r.RecordType,
            Data = r.RecordData,
            Ttl = r.Ttl,
            Priority = r.Priority,
            Enabled = r.Enabled
        }).ToList();
    }

    public async Task<bool> UpdateZoneAsync(string zoneJson)
    {
        try
        {
            var zone = JsonSerializer.Deserialize<DnsZoneConfig>(
                zoneJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            if (zone == null || string.IsNullOrWhiteSpace(zone.Zone))
            {
                return false;
            }

            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                return false;
            }

            var repo = sqlite.CreateRepository<DnsZoneEntity>();
            var all = await repo.GetAllAsync();
            var entity = all.IsSuccess && all.Data != null
                ? all.Data.FirstOrDefault(z => string.Equals(z.ZoneName, zone.Zone, StringComparison.OrdinalIgnoreCase))
                : null;

            entity ??= new DnsZoneEntity { ZoneName = zone.Zone };

            entity.Enabled = zone.Enabled;
            entity.ZoneType = zone.Type;
            entity.ZoneFile = zone.File;
            entity.Masters = zone.Masters.Length > 0 ? JsonSerializer.Serialize(zone.Masters) : null;
            entity.AllowTransfer = zone.AllowTransfer;
            entity.AllowTransferTo = zone.AllowTransferTo.Length > 0 ? JsonSerializer.Serialize(zone.AllowTransferTo) : null;
            entity.Ttl = zone.Ttl;
            entity.SoaEmail = zone.SoaEmail;
            entity.Refresh = zone.Refresh;
            entity.Retry = zone.Retry;
            entity.Expire = zone.Expire;
            entity.NegativeTtl = zone.NegativeTtl;
            entity.UpdatedAt = DateTime.UtcNow;

            return entity.Id == 0
                ? (await repo.InsertAsync(entity)).IsSuccess
                : (await repo.UpdateAsync(entity)).IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateRecordAsync(string recordJson)
    {
        try
        {
            var record = JsonSerializer.Deserialize<DnsRecordConfig>(
                recordJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            if (record == null || string.IsNullOrWhiteSpace(record.Zone) || string.IsNullOrWhiteSpace(record.Name))
            {
                return false;
            }

            var sqlite = CodeLogic.Libs.Get<CL.SQLite.SQLiteLibrary>();
            if (sqlite == null)
            {
                return false;
            }

            var repo = sqlite.CreateRepository<DnsRecordEntity>();

            DnsRecordEntity entity;
            if (int.TryParse(record.Id, out var id) && id > 0)
            {
                var all = await repo.GetAllAsync();
                entity = all.IsSuccess && all.Data != null ? all.Data.FirstOrDefault(r => r.Id == id) ?? new DnsRecordEntity() : new DnsRecordEntity();
                entity.Id = id;
            }
            else
            {
                entity = new DnsRecordEntity();
            }

            entity.ZoneName = record.Zone;
            entity.RecordName = record.Name;
            entity.RecordType = record.Type;
            entity.RecordData = record.Data;
            entity.Ttl = record.Ttl;
            entity.Priority = record.Priority;
            entity.Enabled = record.Enabled;
            entity.UpdatedAt = DateTime.UtcNow;

            return entity.Id == 0
                ? (await repo.InsertAsync(entity)).IsSuccess
                : (await repo.UpdateAsync(entity)).IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    private static DnsSettings GetDefaultSettings() =>
        new()
        {
            Enabled = false,
            Recursion = true,
            Forwarding = false,
            Forwarders = new[] { "8.8.8.8", "8.8.4.4" },
            LogLevel = "info",
            DnssecValidation = true
        };

    private static string[] DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch
        {
            return json.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
    }

    /// <summary>
    /// Start the DNS server service (dnsmasq)
    /// </summary>
    public async Task<DnsServiceControlResult> StartServiceAsync()
    {
        if (_context == null)
        {
            return new DnsServiceControlResult
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
                return new DnsServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.StartServiceAsync("dnsmasq");
            return new DnsServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "DNS service started successfully"
            };
        }
        catch (Exception ex)
        {
            return new DnsServiceControlResult
            {
                Success = false,
                Message = $"Error starting DNS service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Stop the DNS server service (dnsmasq)
    /// </summary>
    public async Task<DnsServiceControlResult> StopServiceAsync()
    {
        if (_context == null)
        {
            return new DnsServiceControlResult
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
                return new DnsServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.StopServiceAsync("dnsmasq");
            return new DnsServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "DNS service stopped successfully"
            };
        }
        catch (Exception ex)
        {
            return new DnsServiceControlResult
            {
                Success = false,
                Message = $"Error stopping DNS service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Restart the DNS server service (dnsmasq)
    /// </summary>
    public async Task<DnsServiceControlResult> RestartServiceAsync()
    {
        if (_context == null)
        {
            return new DnsServiceControlResult
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
                return new DnsServiceControlResult
                {
                    Success = false,
                    Message = "SystemdServiceManager not available"
                };
            }

            var result = await serviceManager.RestartServiceAsync("dnsmasq");
            return new DnsServiceControlResult
            {
                Success = result.Success,
                Message = result.ErrorMessage ?? "DNS service restarted successfully"
            };
        }
        catch (Exception ex)
        {
            return new DnsServiceControlResult
            {
                Success = false,
                Message = $"Error restarting DNS service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get the status of the DNS server service (dnsmasq)
    /// </summary>
    public async Task<DnsServiceStatus> GetServiceStatusAsync()
    {
        if (_context == null)
        {
            return new DnsServiceStatus
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
                return new DnsServiceStatus
                {
                    IsRunning = false,
                    IsEnabled = false,
                    Status = "Unknown",
                    Message = "SystemdServiceManager not available"
                };
            }

            var status = await serviceManager.GetServiceStatusAsync("dnsmasq");
            return new DnsServiceStatus
            {
                IsRunning = status.IsRunning,
                IsEnabled = status.IsEnabled,
                Status = status.ActiveState.ToString(),
                Message = $"{status.RawActiveState} / {status.RawEnabledState}"
            };
        }
        catch (Exception ex)
        {
            return new DnsServiceStatus
            {
                IsRunning = false,
                IsEnabled = false,
                Status = "Error",
                Message = ex.Message
            };
        }
    }
}

// DNS service control result model
public class DnsServiceControlResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// DNS service status model
public class DnsServiceStatus
{
    public bool IsRunning { get; set; }
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
