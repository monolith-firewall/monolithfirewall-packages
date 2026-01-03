using Monolith.FireWall.Common.Interfaces;

namespace Monolith.Diagnostics;

public class PackageDefinition : IMonolithPackageDefinition
{
    public string Id => "monolith-diagnostics";
    public string Name => "Monolith Diagnostics";
    public string Version => "1.0.0";
    public string Description => "Diagnostics tools package with ping, traceroute, and MTR";
    public string Author => "Monolith FireWall Team";
    public string[] Dependencies => Array.Empty<string>();

    public IEnumerable<IMonolithModule> GetModules()
    {
        return new IMonolithModule[]
        {
            new Modules.Diagnostics.DiagnosticsModule()
        };
    }
}

public class Package : IMonolithPackage
{
    public Task OnLoadAsync(IPackageContext context)
    {
        return Task.CompletedTask;
    }

    public void RegisterLocalizations(CodeLogic.Localization.ILocalizationManager localizationManager)
    {
    }

    public Task OnUnloadAsync(IPackageContext context)
    {
        return Task.CompletedTask;
    }
}
