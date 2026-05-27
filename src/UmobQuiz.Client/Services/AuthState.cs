namespace UmobQuiz.Client.Services;

public sealed class AuthState
{
    private const string TokenKey = "umobquiz_token";
    private const string UsernameKey = "umobquiz_username";

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public event Action? Changed;

    public void SetSession(string token, string username)
    {
        Token = token;
        Username = username;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Token = null;
        Username = null;
        Changed?.Invoke();
    }
}
