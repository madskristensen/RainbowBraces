namespace RainbowBraces.Commands
{
    [Command(PackageIds.Toggle)]
    public class ToggleCommand : BaseCommand<ToggleCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            General settings = await General.GetLiveInstanceAsync();
            settings.Enabled = !settings.Enabled;
            await settings.SaveAsync();
        }
    }
}
