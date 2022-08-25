global using Community.VisualStudio.Toolkit;
global using Task = System.Threading.Tasks.Task;
global using System;
global using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace RainbowBraces
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Environment\\Fonts and Colors", Vsix.Name, 0, 0, true, SupportsProfiles = true, ProvidesLocalizedCategoryName = false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.RainbowBracesString)]
    public sealed class RainbowBracesPackage : ToolkitPackage
    {
    }
}