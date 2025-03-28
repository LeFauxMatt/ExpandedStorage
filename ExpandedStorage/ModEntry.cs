using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using LeFauxMods.ExpandedStorage.Services;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;

namespace LeFauxMods.ExpandedStorage;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(helper.Translation);
        ModState.Init(helper, this.ModManifest);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Apply();

        // Events
        helper.Events.Content.AssetRequested += OnAssetRequested;
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!ModState.Config.Any() || !e.NameWithoutLocale.IsEquivalentTo(ModConstants.BigCraftableData))
        {
            return;
        }

        e.Edit(static assetData =>
            {
                var data = assetData.AsDictionary<string, BigCraftableData>().Data;
                foreach (var (itemId, storageConfig) in ModState.Config)
                {
                    if (!data.TryGetValue(itemId, out var bigCraftableData) ||
                        storageConfig.GetData() is not { } dict)
                    {
                        continue;
                    }

                    bigCraftableData.CustomFields ??= [];
                    foreach (var (key, value) in dict)
                    {
                        bigCraftableData.CustomFields[key] = value;
                    }
                }
            },
            AssetEditPriority.Late);
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e) =>
        _ = this.Helper.GameContent.InvalidateCache(ModConstants.BigCraftableData);
}