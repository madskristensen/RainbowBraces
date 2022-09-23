namespace RainbowBraces.Commands
{
    [Command(PackageIds.Toggle)]
    public class ToggleCommand : BaseCommand<ToggleCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Checked = General.Instance.Enabled;
        }
        
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            General settings = await General.GetLiveInstanceAsync();
            settings.Enabled = !settings.Enabled;
            await settings.SaveAsync();
        }
    }
}
