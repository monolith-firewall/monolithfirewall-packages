using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Vpn.Modules.WireGuard;

public class WireGuardModule : IMonolithModule
{
    public string Id => "wireguard";
    public string Name => "WireGuard";
    public string Description => "WireGuard VPN management";

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "vpn-wireguard",
                "WireGuard",
                "shield",
                30,
                new[] { "vpn.wireguard.read" },
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
                        var manager = new WireGuardManager(null);
                        var settings = await manager.GetSettingsAsync();
                        return new ApiResponse(true, settings, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.read" }
            ),
            new RouteDefinition(
                "update-settings",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var result = await manager.UpdateSettingsAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update settings");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "get-interfaces",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var interfaces = await manager.GetInterfacesAsync();
                        return new ApiResponse(true, interfaces, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.read" }
            ),
            new RouteDefinition(
                "get-peers",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var interfaceId = request.Query?.GetValueOrDefault("interface") ?? null;
                        var peers = await manager.GetPeersAsync(interfaceId);
                        return new ApiResponse(true, peers, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.read" }
            ),
            new RouteDefinition(
                "update-interface",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var result = await manager.UpdateInterfaceAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update interface");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "update-peer",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var result = await manager.UpdatePeerAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update peer");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "delete-interface",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var interfaceId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.DeleteInterfaceAsync(interfaceId);
                        return new ApiResponse(result, null, result ? null : "Failed to delete interface");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "delete-peer",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var peerId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.DeletePeerAsync(peerId);
                        return new ApiResponse(result, null, result ? null : "Failed to delete peer");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "start-interface",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var interfaceId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StartInterfaceAsync(interfaceId);
                        return new ApiResponse(result, null, result ? null : "Failed to start interface");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "stop-interface",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var interfaceId = request.Query?.GetValueOrDefault("id") ?? "";
                        var result = await manager.StopInterfaceAsync(interfaceId);
                        return new ApiResponse(result, null, result ? null : "Failed to stop interface");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            ),
            new RouteDefinition(
                "generate-keypair",
                async (request) =>
                {
                    try
                    {
                        var manager = new WireGuardManager(null);
                        var keyPair = await manager.GenerateKeyPairAsync();
                        return new ApiResponse(true, keyPair, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "vpn.wireguard.write" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-vpn/wireguard",
                "/_content/Monolith.Vpn/Pages/WireGuard/Config.cshtml",
                new[] { "vpn.wireguard.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return new[]
        {
            new WidgetDefinition(
                "vpn.wireguard.status",
                "WireGuard Status",
                "monolith-vpn",
                "wireguard",
                "WireGuard interfaces and peers status",
                "shield",
                4,
                2,
                22,
                new[] { "vpn.wireguard.read" }
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
            new AptDependency("wireguard", "WireGuard VPN daemon")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("vpn.wireguard.read", "Read WireGuard configuration", "vpn", "wireguard"),
            new PermissionDefinition("vpn.wireguard.write", "Modify WireGuard configuration", "vpn", "wireguard")
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
