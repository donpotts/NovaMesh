using System.Net.Http.Json;
using Microsoft.JSInterop;
using NovaMesh.Client.Models;

namespace NovaMesh.Client.Services;

public class TenantStateService
{
    private readonly IJSRuntime? _js;
    public TenantDto? CurrentTenant { get; private set; }
    public List<TenantDto> Tenants { get; private set; } = new();
    public event Action? OnTenantChanged;

    public TenantStateService(IJSRuntime? js = null)
    {
        _js = js;
    }

    public async Task InitializeAsync(HttpClient http, ThemeService theme)
    {
        try
        {
            var fetched = await http.GetFromJsonAsync<List<TenantDto>>("/api/tenants");
            if (fetched != null && fetched.Count > 0)
            {
                Tenants = fetched;
            }
        }
        catch
        {
            // Offline or initial fallback
            Tenants = new List<TenantDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Acme Global", NormalizedName = "ACME", EditionName = "Enterprise", PrimaryColor = "#4F46E5", SecondaryColor = "#06B6D4", AccentColor = "#F59E0B", CustomDomain = "acme.platform.local", Slogan = "Next-Generation Global Solutions" },
                new() { Id = Guid.NewGuid(), Name = "Stark Logistics", NormalizedName = "STARK", EditionName = "Professional", PrimaryColor = "#DC2626", SecondaryColor = "#EA580C", AccentColor = "#FBBF24", CustomDomain = "stark.platform.local", Slogan = "Powering Autonomous Supply Chains" },
                new() { Id = Guid.NewGuid(), Name = "Apex Cloud", NormalizedName = "APEX", EditionName = "Startup", PrimaryColor = "#059669", SecondaryColor = "#10B981", AccentColor = "#34D399", CustomDomain = "apex.platform.local", Slogan = "Cloud Native Infrastructure" }
            };
        }

        string? savedTenantId = null;
        if (_js != null)
        {
            try { savedTenantId = await _js.InvokeAsync<string?>("localStorage.getItem", "forge_selected_tenant_id"); }
            catch { }
        }

        if (!string.IsNullOrEmpty(savedTenantId) && Guid.TryParse(savedTenantId, out var tid))
        {
            CurrentTenant = Tenants.FirstOrDefault(t => t.Id == tid);
        }

        if (CurrentTenant == null && Tenants.Count > 0)
        {
            CurrentTenant = Tenants[0];
        }

        ApplyTheme(theme);
    }

    public async Task SelectTenantAsync(TenantDto? tenant, ThemeService theme)
    {
        CurrentTenant = tenant;
        if (_js != null)
        {
            try
            {
                if (tenant != null) await _js.InvokeVoidAsync("localStorage.setItem", "forge_selected_tenant_id", tenant.Id.ToString());
                else await _js.InvokeVoidAsync("localStorage.removeItem", "forge_selected_tenant_id");
            }
            catch { }
        }
        ApplyTheme(theme);
        OnTenantChanged?.Invoke();
    }

    private void ApplyTheme(ThemeService theme)
    {
        if (CurrentTenant != null)
        {
            theme.SetColors(CurrentTenant.PrimaryColor ?? "#4F46E5", CurrentTenant.SecondaryColor ?? "#06B6D4");
        }
    }
}