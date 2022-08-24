global using Community.VisualStudio.Toolkit;
global using Task = System.Threading.Tasks.Task;
global using System;
global using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace RainbowBraces
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.RainbowBracesString)]
    public sealed class RainbowBracesPackage : ToolkitPackage
    {
    }
}