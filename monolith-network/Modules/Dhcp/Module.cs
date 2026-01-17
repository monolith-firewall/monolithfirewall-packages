using Monolith.FireWall.Common.Enums;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;
using Monolith.Network.Modules.Dhcp;
using CodeLogic;
using CL.SQLite.Services;

namespace Monolith.Network.Modules.Dhcp;

public class DhcpModule : IMonolithModule, IMonolithModuleLifecycle, IModuleConfigGenerator
{
    private IModuleContext? _context;
    private readonly DhcpConfigGenerator _configGenerator = new();

    public string Id => "dhcp";
    public string Name => "DHCP Server";
    public string Description => "Dynamic Host Configuration Protocol server management";

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "network-dhcp",
                "DHCP Server",
                "network",
                10,
                new[] { "network.dhcp.read" },
                null  // Single page with tabs - no submenu
            )
        };
    }

    public IEnumerable<RouteDefinition> GetRoutes()
    {
        return new[]
        {
            new RouteDefinition(
                "get-config",
                async (request) =>
                {
                    // Get DHCP configuration
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var config = await manager.GetConfigAsync();
                        return new ApiResponse(true, config, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.read" }
            ),
            new RouteDefinition(
                "update-config",
                async (request) =>
                {
                    // Update DHCP configuration
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var result = await manager.UpdateConfigAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update configuration");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.write" }
            ),
            new RouteDefinition(
                "get-settings",
                async (request) =>
                {
                    // Get DHCP settings
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var settings = await manager.GetSettingsAsync();
                        return new ApiResponse(true, settings, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.read" }
            ),
            new RouteDefinition(
                "update-settings",
                async (request) =>
                {
                    // Update DHCP settings
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var result = await manager.UpdateSettingsAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update settings");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.write" }
            ),
            new RouteDefinition(
                "get-interfaces",
                async (request) =>
                {
                    // Get network interfaces with DHCP configuration
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var interfaces = await manager.GetInterfacesAsync(request.User);
                        return new ApiResponse(true, interfaces, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.read" }
            ),
            new RouteDefinition(
                "update-interface",
                async (request) =>
                {
                    // Update DHCP configuration for an interface
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var result = await manager.UpdateInterfaceAsync(request.Body ?? "{}");
                        return new ApiResponse(result, null, result ? null : "Failed to update interface configuration");
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.write" }
            ),
            new RouteDefinition(
                "get-leases",
                async (request) =>
                {
                    // List DHCP leases
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var leases = await manager.ListLeasesAsync();
                        return new ApiResponse(true, leases, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.read" }
            ),
            new RouteDefinition(
                "list-leases",
                async (request) =>
                {
                    // List DHCP leases (alias)
                    try
                    {
                        var manager = new DhcpManager(_context);
                        var leases = await manager.ListLeasesAsync();
                        return new ApiResponse(true, leases, null);
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse(false, null, ex.Message);
                    }
                },
                new[] { "network.dhcp.read" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-network/dhcp",
                "/_content/Monolith.Network/Pages/Dhcp/Config.cshtml",
                new[] { "network.dhcp.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return new[]
        {
            new WidgetDefinition(
                "network.dhcp.status",
                "DHCP Status",
                "monolith-network",
                "dhcp",
                "DHCP server status and active leases",
                "network",
                4,
                2,
                15,
                new[] { "network.dhcp.read" }
            )
        };
    }

    public IEnumerable<TemplateDefinition> GetTemplates()
    {
        return Array.Empty<TemplateDefinition>();
    }

    public IEnumerable<ServiceDefinition> GetServices()
    {
        return new[]
        {
            new ServiceDefinition(
                "isc-dhcp-server",
                "isc-dhcp-server.service",
                Array.Empty<string>() // No special capabilities required
            )
        };
    }

    public IEnumerable<AptDependency> GetAptDependencies()
    {
        return new[]
        {
            new AptDependency("isc-dhcp-server", "DHCP server daemon")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("network.dhcp.read", "Read DHCP configuration", "network", "dhcp"),
            new PermissionDefinition("network.dhcp.write", "Modify DHCP configuration", "network", "dhcp")
        };
    }

    public IEnumerable<SystemPermissionDefinition> GetSystemPermissions()
    {
        return new[]
        {
            new SystemPermissionDefinition(
                SystemPermissionType.NetworkControl,
                "read",
                "Read network interfaces for DHCP configuration"),
            new SystemPermissionDefinition(
                SystemPermissionType.FileRead,
                "/etc/dhcp/dhcpd.conf",
                "Read DHCP server configuration"),
            new SystemPermissionDefinition(
                SystemPermissionType.FileWrite,
                "/etc/dhcp/dhcpd.conf",
                "Write DHCP server configuration")
        };
    }

    public IEnumerable<CronJobDefinition> GetCronJobs()
    {
        return Array.Empty<CronJobDefinition>();
    }

    public IEnumerable<ISetupWizardPage> GetSetupWizardPages()
    {
        // DHCP setup page - configure DHCP server during initial setup
        return new[]
        {
            new SetupWizardPage
            {
                Id = "dhcp",
                Title = "DHCP Server Configuration",
                Description = "Configure the DHCP server to provide IP addresses to devices on your network",
                Order = 10,
                Route = "/setup/package/monolith-network/dhcp",
                IsRequired = false,
                IsComplete = false,
                PackageId = "monolith-network",
                ModuleId = "dhcp"
            }
        };
    }

    public string PackageId => "monolith-network";

    public Task OnStartAsync(IModuleContext context)
    {
        _context = context;
        
        // Note: Database tables are synced during package installation.
        // OnStartAsync is kept for other initialization tasks if needed.
        
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

    // IModuleConfigGenerator implementation
    public bool RequiresServiceRestart => _configGenerator.RequiresServiceRestart;

    public IEnumerable<string> GetConfigFilePaths() => _configGenerator.GetConfigFilePaths();

    public Task<ModuleConfigGenerationResult> GenerateConfigAsync(
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        return _configGenerator.GenerateConfigAsync(context, cancellationToken);
    }
}
