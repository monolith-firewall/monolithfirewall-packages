using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Vpn.Modules.OpenVpn;

public class OpenVpnModule : IMonolithModule
{
    public string Id => "openvpn";
    public string Name => "OpenVPN";
    public string Description => "OpenVPN server and client management";

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "vpn-openvpn",
                "OpenVPN",
                "shield",
                20,
                new[] { "vpn.openvpn.read" },
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
                        var manager = new OpenVpnManager(null);
                        var settings = await manager.GetSettingsAsync();
                        return new ApiResponse(true, settings, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.read" }
            ),
            new RouteDefinition(
                "update-settings",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var result = await manager.UpdateSettingsAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update settings");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "get-servers",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var servers = await manager.GetServersAsync();
                        return new ApiResponse(true, servers, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.read" }
            ),
            new RouteDefinition(
                "get-clients",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var clients = await manager.GetClientsAsync();
                        return new ApiResponse(true, clients, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.read" }
            ),
            new RouteDefinition(
                "update-server",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var result = await manager.UpdateServerAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update server");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "update-client",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var result = await manager.UpdateClientAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update client");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "delete-server",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var serverId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.DeleteServerAsync(serverId);
                        return new ApiResponse(result, null, result ? null : "Failed to delete server");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "delete-client",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var clientId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.DeleteClientAsync(clientId);
                        return new ApiResponse(result, null, result ? null : "Failed to delete client");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "start-server",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var serverId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StartServerAsync(serverId);
                        return new ApiResponse(result, null, result ? null : "Failed to start server");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "stop-server",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var serverId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StopServerAsync(serverId);
                        return new ApiResponse(result, null, result ? null : "Failed to stop server");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "start-client",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var clientId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StartClientAsync(clientId);
                        return new ApiResponse(result, null, result ? null : "Failed to start client");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            ),
            new RouteDefinition(
                "stop-client",
                async (request) =>
                {
                    try
                    {
                        var manager = new OpenVpnManager(null);
                        var clientId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StopClientAsync(clientId);
                        return new ApiResponse(result, null, result ? null : "Failed to stop client");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.openvpn.write" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-vpn/openvpn",
                "/_content/Monolith.Vpn/Pages/OpenVpn/Config.cshtml",
                new[] { "vpn.openvpn.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return new[]
        {
            new WidgetDefinition(
                "vpn.openvpn.status",
                "OpenVPN Status",
                "monolith-vpn",
                "openvpn",
                "OpenVPN servers and clients status",
                "shield",
                4,
                2,
                21,
                new[] { "vpn.openvpn.read" }
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
            new AptDependency("openvpn", "OpenVPN VPN daemon")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("vpn.openvpn.read", "Read OpenVPN configuration", "vpn", "openvpn"),
            new PermissionDefinition("vpn.openvpn.write", "Modify OpenVPN configuration", "vpn", "openvpn")
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
