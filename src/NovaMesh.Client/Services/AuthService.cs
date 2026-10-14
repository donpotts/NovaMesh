using System.Net.Http.Headers;
using System.Net.Http.Json;
using NovaMesh.Client.Models;

namespace NovaMesh.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    public LoginResponse? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient http) => _http = http;

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest { UserName = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                CurrentUser = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (CurrentUser != null)
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentUser.Token);
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> RegisterAsync(string username, string email, string password, string fullName)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", new RegisterRequest { UserName = username, Email = email, Password = password, FullName = fullName });
            if (response.IsSuccessStatusCode)
            {
                CurrentUser = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (CurrentUser != null)
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentUser.Token);
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public void Logout()
    {
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        OnAuthStateChanged?.Invoke();
    }
}