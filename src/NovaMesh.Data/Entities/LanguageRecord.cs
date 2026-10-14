namespace NovaMesh.Data.Entities;

public class LanguageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CultureCode { get; set; } = "en-US";
    public string DisplayName { get; set; } = "English (US)";
    public string FlagEmoji { get; set; } = "🇺🇸";
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int TranslationProgressPercent { get; set; } = 100;
}