using System.Globalization;
using System.Text;
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
    public string GetSummary()
    {
        var sb = new StringBuilder();
        foreach (var (itemId, values) in this)
        {
            sb.AppendLine(itemId);
            foreach (var (key, value) in values)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{key,25}: {value}");
            }
        }

        return sb.ToString();
    }
}