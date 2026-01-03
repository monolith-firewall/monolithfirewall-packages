using Monolith.FireWall.Common.Interfaces;
using Monolith.FireWall.Common.Models;

namespace Monolith.Vpn;

public class PackageDefinition : IMonolithPackageDefinition
{
    public string Id => "monolith-vpn";
    public string Name => "Monolith VPN";
    public string Version => "1.0.0";
    public string Description => "VPN management package with IPsec, OpenVPN, and WireGuard modules";
    public string Author => "Monolith FireWall Team";
    public string[] Dependencies => Array.Empty<string>();

    public IEnumerable<IMonolithModule> GetModules()
    {
        var modules = new List<IMonolithModule>();
        
        // Add IPsec module
        modules.Add(new Modules.Ipsec.IpsecModule());
        
        // Add OpenVPN module
        modules.Add(new Modules.OpenVpn.OpenVpnModule());
        
        // Add WireGuard module
        modules.Add(new Modules.WireGuard.WireGuardModule());
        
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
    }

    public async Task OnUnloadAsync(IPackageContext context)
    {
        // Package cleanup logic
        await Task.CompletedTask;
    }
}
