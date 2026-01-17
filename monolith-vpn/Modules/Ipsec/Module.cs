using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Vpn.Modules.Ipsec;

public class IpsecModule : IMonolithModule
{
    public string Id => "ipsec";
    public string Name => "IPsec";
    public string Description => "IPsec VPN tunnel management";

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "vpn-ipsec",
                "IPsec",
                "shield",
                10,
                new[] { "vpn.ipsec.read" },
                null
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
                    try
                    {
                        var manager = new IpsecManager(null);
                        var settings = await manager.GetSettingsAsync();
                        return new ApiResponse(true, settings, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.read" }
            ),
            new RouteDefinition(
                "update-settings",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var result = await manager.UpdateSettingsAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update settings");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.write" }
            ),
            new RouteDefinition(
                "get-connections",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var connections = await manager.GetConnectionsAsync();
                        return new ApiResponse(true, connections, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.read" }
            ),
            new RouteDefinition(
                "update-connection",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var result = await manager.UpdateConnectionAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update connection");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.write" }
            ),
            new RouteDefinition(
                "delete-connection",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var connectionId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.DeleteConnectionAsync(connectionId);
                        return new ApiResponse(result, null, result ? null : "Failed to delete connection");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.write" }
            ),
            new RouteDefinition(
                "start-connection",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var connectionId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StartConnectionAsync(connectionId);
                        return new ApiResponse(result, null, result ? null : "Failed to start connection");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.write" }
            ),
            new RouteDefinition(
                "stop-connection",
                async (request) =>
                {
                    try
                    {
                        var manager = new IpsecManager(null);
                        var connectionId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StopConnectionAsync(connectionId);
                        return new ApiResponse(result, null, result ? null : "Failed to stop connection");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.ipsec.write" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-vpn/ipsec",
                "/_content/Monolith.Vpn/Pages/Ipsec/Config.cshtml",
                new[] { "vpn.ipsec.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return new[]
        {
            new WidgetDefinition(
                "vpn.ipsec.status",
                "IPsec Status",
                "monolith-vpn",
                "ipsec",
                "IPsec connections status",
                "shield",
                4,
                2,
                20,
                new[] { "vpn.ipsec.read" }
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
            new AptDependency("strongswan", "IPsec VPN daemon")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("vpn.ipsec.read", "Read IPsec configuration", "vpn", "ipsec"),
            new PermissionDefinition("vpn.ipsec.write", "Modify IPsec configuration", "vpn", "ipsec")
        };
    }

    public IEnumerable<SystemPermissionDefinition> GetSystemPermissions()
    {
        return Array.Empty<SystemPermissionDefinition>();
    }

    public IEnumerable<CronJobDefinition> GetCronJobs()
    {
        return Array.Empty<CronJobDefinition>();
    }

    public IEnumerable<ISetupWizardPage> GetSetupWizardPages()
    {
        // VPN modules don't need setup wizard pages (optional configuration)
        return Array.Empty<ISetupWizardPage>();
    }

    public string PackageId => "monolith-vpn";
}
