using Monolith.FireWall.Common.Enums;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Network.Modules.Dns;

public class DnsModule : IMonolithModule, IMonolithModuleLifecycle
{
    private IModuleContext? _context;

    public string Id => "dns";
    public string Name => "DNS Server";
    public string Description => "Domain Name System server management";

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "network-dns",
                "DNS Server",
                "network",
                20,
                new[] { "network.dns.read" },
                null  // Single page with tabs - no submenu
            )
        };
    }

    public IEnumerable<RouteDefinition> GetRoutes()
    {
        return new[]
        {
            new RouteDefinition(
                "get-settings",
                async (request) =>
                {
                    // Get DNS settings
                    try
                    {
                        var manager = new DnsManager(_context);
                        var settings = await manager.GetSettingsAsync();
                        return new ApiResponse(true, settings, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.read" }
            ),
            new RouteDefinition(
                "update-settings",
                async (request) =>
                {
                    // Update DNS settings
                    try
                    {
                        var manager = new DnsManager(_context);
                        var result = await manager.UpdateSettingsAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update settings");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.write" }
            ),
            new RouteDefinition(
                "get-zones",
                async (request) =>
                {
                    // Get DNS zones
                    try
                    {
                        var manager = new DnsManager(_context);
                        var zones = await manager.GetZonesAsync();
                        return new ApiResponse(true, zones, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.read" }
            ),
            new RouteDefinition(
                "get-records",
                async (request) =>
                {
                    // Get DNS records for a zone
                    try
                    {
                        var manager = new DnsManager(_context);
                        var query = request.Query.ContainsKey("zone") ? request.Query["zone"] : "";
                        var records = await manager.GetRecordsAsync(query);
                        return new ApiResponse(true, records, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.read" }
            ),
            new RouteDefinition(
                "update-zone",
                async (request) =>
                {
                    // Update DNS zone configuration
                    try
                    {
                        var manager = new DnsManager(_context);
                        var result = await manager.UpdateZoneAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update zone");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.write" }
            ),
            new RouteDefinition(
                "update-record",
                async (request) =>
                {
                    // Update DNS record
                    try
                    {
                        var manager = new DnsManager(_context);
                        var result = await manager.UpdateRecordAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update record");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dns.write" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-network/dns",
                "/_content/Monolith.Network/Pages/Dns/Config.cshtml",
                new[] { "network.dns.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return new[]
        {
            new WidgetDefinition(
                "network.dns.status",
                "DNS Status",
                "monolith-network",
                "dns",
                "DNS server status and query statistics",
                "network",
                4,
                2,
                15,
                new[] { "network.dns.read" }
            )
        };
    }

    public IEnumerable<TemplateDefinition> GetTemplates()
    {
        return Array.Empty<TemplateDefinition>();
    }

    public IEnumerable<ServiceDefinition> GetServices()
    {
        return Array.Empty<ServiceDefinition>();
    }

    public IEnumerable<AptDependency> GetAptDependencies()
    {
        return new[]
        {
            new AptDependency("bind9", "BIND9 DNS server")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("network.dns.read", "Read DNS configuration", "network", "dns"),
            new PermissionDefinition("network.dns.write", "Modify DNS configuration", "network", "dns")
        };
    }

    public IEnumerable<SystemPermissionDefinition> GetSystemPermissions()
    {
        return new[]
        {
            new SystemPermissionDefinition(
                SystemPermissionType.NetworkControl,
                "read",
                "Read network resolver state for DNS configuration"),
            new SystemPermissionDefinition(
                SystemPermissionType.FileRead,
                "/etc/bind",
                "Read DNS server configuration and zone files"),
            new SystemPermissionDefinition(
                SystemPermissionType.FileWrite,
                "/etc/bind",
                "Write DNS server configuration and zone files")
        };
    }

    public IEnumerable<CronJobDefinition> GetCronJobs()
    {
        return Array.Empty<CronJobDefinition>();
    }

    public string PackageId => "monolith-network";

    public Task OnStartAsync(IModuleContext context)
    {
        _context = context;
        return Task.CompletedTask;
    }

    public Task OnStopAsync(IModuleContext context)
    {
        _context = null;
        return Task.CompletedTask;
    }

    public Task OnConfigChangedAsync(string key, string? oldValue, string? newValue)
    {
        return Task.CompletedTask;
    }
}
