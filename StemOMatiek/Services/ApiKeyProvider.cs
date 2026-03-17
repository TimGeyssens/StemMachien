namespace StemOMatiek.Services;

/// <summary>
/// Per-circuit (per user session) API key storage.
/// Elke burger draagt zijn eigen sleutel bij zich.
/// </summary>
public class ApiKeyProvider
{
    private string? _apiKey;

    public string? ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;
            OnKeyChanged?.Invoke();
        }
    }

    public bool HasKey => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Fired when the API key changes, so components can react</summary>
    public event Action? OnKeyChanged;
}
