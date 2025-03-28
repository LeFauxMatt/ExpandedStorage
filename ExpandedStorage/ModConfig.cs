using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;
using LeFauxMods.ExpandedStorage.Models;

namespace LeFauxMods.ExpandedStorage;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig() : Dictionary<string, StorageConfig>(StringComparer.OrdinalIgnoreCase),
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
        foreach (var (itemId, storageConfig) in this)
        {
            sb.AppendLine(itemId)
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageConfig.ColorfulChests),25}: {storageConfig.ColorfulChests}")
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageConfig.UnlimitedStorage),25}: {storageConfig.UnlimitedStorage}");
        }

        return sb.ToString();
    }
}