using LeFauxMods.Common.Integrations.BetterChests;

namespace LeFauxMods.ExpandedStorage;

internal sealed class ModConfig() : Dictionary<string, StorageOptions>(StringComparer.OrdinalIgnoreCase);
