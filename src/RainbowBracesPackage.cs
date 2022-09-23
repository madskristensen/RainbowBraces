global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;

namespace RainbowBraces
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Environment\\Fonts and Colors", Vsix.Name, 0, 0, true, SupportsProfiles = true, ProvidesLocalizedCategoryName = false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.RainbowBracesString)]
    [ProvideAutoLoad(VSConstants.VsEditorFactoryGuid.TextEditor_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class RainbowBracesPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
        }
    }
}