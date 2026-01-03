using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Network;

public class PackageDefinition : IMonolithPackageDefinition
{
    public string Id => "monolith-network";
    public string Name => "Monolith Network";
    public string Version => "1.0.0";
    public string Description => "Network management package with DHCP, DNS, and firewall modules";
    public string Author => "Monolith FireWall Team";
    public string[] Dependencies => Array.Empty<string>();

    public IEnumerable<IMonolithModule> GetModules()
    {
        // Create modules list
        var modules = new List<IMonolithModule>();
        
        // Add DHCP module
        modules.Add(new Modules.Dhcp.DhcpModule());
        
        // Add DNS module - if this throws, the list will only have DHCP
        modules.Add(new Modules.Dns.DnsModule());
        
        return modules;
    }
}

public class Package : IMonolithPackage
{
    public async Task OnLoadAsync(IPackageContext context)
    {
        // Package initialization logic
        await Task.CompletedTask;
    }

    public void RegisterLocalizations(CodeLogic.Localization.ILocalizationManager localizationManager)
    {
        // Register package localizations
        // For now, empty - will be populated in future phases
    }

    public async Task OnUnloadAsync(IPackageContext context)
    {
        // Package cleanup logic
        await Task.CompletedTask;
    }
}
