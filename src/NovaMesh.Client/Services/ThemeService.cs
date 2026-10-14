using MudBlazor;

namespace NovaMesh.Client.Services;

public class ThemeService
{
    public bool IsDarkMode { get; set; } = true;
    public string CurrentPrimaryColor { get; set; } = "#4F46E5";
    public string CurrentSecondaryColor { get; set; } = "#06B6D4";
    public event Action? OnThemeChanged;

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        NotifyThemeChanged();
    }

    public void SetColors(string primary, string secondary)
    {
        CurrentPrimaryColor = primary;
        CurrentSecondaryColor = secondary;
        NotifyThemeChanged();
    }

    public void NotifyThemeChanged()
    {
        OnThemeChanged?.Invoke();
    }

    public MudTheme CreateCustomTheme()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = CurrentPrimaryColor,
                Secondary = CurrentSecondaryColor,
                AppbarBackground = "#FFFFFF",
                AppbarText = "#1E293B",
                Background = "#F8FAFC",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#1E293B",
                TextPrimary = "#0F172A",
                TextSecondary = "#475569"
            },
            PaletteDark = new PaletteDark
            {
                Primary = CurrentPrimaryColor,
                Secondary = CurrentSecondaryColor,
                AppbarBackground = "#151821",
                AppbarText = "#F1F5F9",
                Background = "#0F1117",
                Surface = "#1A1D27",
                DrawerBackground = "#13161F",
                DrawerText = "#F1F5F9",
                TextPrimary = "#F8FAFC",
                TextSecondary = "#94A3B8"
            }
        };
    }
}