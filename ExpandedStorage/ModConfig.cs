using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.ExpandedStorage;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig() : Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
    IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.Clear();
        foreach (var (key, value) in this)
        {
            other.Add(key, value);
        }
    }

    /// <inheritdoc />
    public string GetSummary() => string.Empty;
}