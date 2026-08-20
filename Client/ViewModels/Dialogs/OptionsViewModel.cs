namespace Markwardt.AssetPipeline.Client.ViewModels.Dialogs;

/// <summary>
/// Backs the modal Options dialog for configuring access to the scenario.com and meshy.ai APIs.
/// </summary>
internal sealed partial class OptionsViewModel(ISettingsService settingsService) : ViewModelBase
{
    [ObservableProperty]
    private string scenarioApiKey = "";

    [ObservableProperty]
    private string scenarioApiSecret = "";

    [ObservableProperty]
    private string meshyApiKey = "";

    /// <summary>
    /// Raised when the dialog should close, after a successful save or a cancellation.
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// Loads the currently persisted API settings into this dialog's editable fields.
    /// </summary>
    public async Task InitializeAsync()
    {
        ApiSettings api = (await settingsService.LoadAsync()).Api;
        ScenarioApiKey = api.ScenarioApiKey;
        ScenarioApiSecret = api.ScenarioApiSecret;
        MeshyApiKey = api.MeshyApiKey;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        AppSettings settings = await settingsService.LoadAsync();
        AppSettings updated = settings with
        {
            Api = new ApiSettings
            {
                ScenarioApiKey = ScenarioApiKey,
                ScenarioApiSecret = ScenarioApiSecret,
                MeshyApiKey = MeshyApiKey,
            },
        };
        await settingsService.SaveAsync(updated);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
