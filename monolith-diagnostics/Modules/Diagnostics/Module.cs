using Monolith.FireWall.Common.Enums;
using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Diagnostics.Modules.Diagnostics;

public class DiagnosticsModule : IMonolithModule
{
    public string Id => "diagnostics";
    public string Name => "Diagnostics";
    public string PackageId => "monolith-diagnostics";

    public IEnumerable<RouteDefinition> GetRoutes()
    {
        return Array.Empty<RouteDefinition>();
    }

    public IEnumerable<MenuDefinition> GetMenuItems()
    {
        return new[]
        {
            new MenuDefinition(
                "diagnostics",
                "Diagnostics",
                "activity",
                10,
                new[] { "diagnostics.read" }
            )
        };
    }

    public IEnumerable<PageDefinition> GetPages()
    {
        return new[]
        {
            new PageDefinition(
                "/p/monolith-diagnostics/diagnostics",
                "/_content/Monolith.Diagnostics/Pages/Diagnostics/Config.cshtml",
                new[] { "diagnostics.read" }
            )
        };
    }

    public IEnumerable<WidgetDefinition> GetWidgets()
    {
        return Array.Empty<WidgetDefinition>();
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
            new AptDependency("traceroute", "Traceroute diagnostic tool"),
            new AptDependency("mtr", "MTR multi-traceroute tool"),
            new AptDependency("iputils-ping", "Ping diagnostic tool")
        };
    }

    public IEnumerable<PermissionDefinition> GetRequiredPermissions()
    {
        return new[]
        {
            new PermissionDefinition("diagnostics.read", "Run diagnostics tools", "diagnostics", "tools")
        };
    }

    public IEnumerable<SystemPermissionDefinition> GetSystemPermissions()
    {
        return new[]
        {
            new SystemPermissionDefinition(SystemPermissionType.CommandExec, "ping", "Run ping diagnostics"),
            new SystemPermissionDefinition(SystemPermissionType.CommandExec, "traceroute", "Run traceroute diagnostics"),
            new SystemPermissionDefinition(SystemPermissionType.CommandExec, "mtr", "Run MTR diagnostics")
        };
    }

    public IEnumerable<CronJobDefinition> GetCronJobs()
    {
        return Array.Empty<CronJobDefinition>();
    }

    public IEnumerable<ISetupWizardPage> GetSetupWizardPages()
    {
        // Diagnostics doesn't need setup wizard pages
        return Array.Empty<ISetupWizardPage>();
    }
}
