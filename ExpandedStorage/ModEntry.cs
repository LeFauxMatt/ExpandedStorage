using LeFauxMods.Common.Integrations.ColorfulChests;
using LeFauxMods.Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using LeFauxMods.ExpandedStorage.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Objects;

namespace LeFauxMods.ExpandedStorage;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Apply();

        // Events
        helper.Events.Content.AssetReady += OnAssetReady;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        _ = new ContentPatcherIntegration(helper);
        ModEvents.Subscribe<ConditionsApiReadyEventArgs>(OnConditionsApiReady);
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) =>
        () =>
            Game1.bigCraftableData.TryGetValue(itemId, out var oneData) ? oneData.CustomFields : null;

    private static bool GetPalette(Chest chest, [NotNullWhen(true)] out Color[]? palette)
    {
        palette = null;
        if (!ModState.Data.TryGetValue(chest.ItemId, out var storage))
        {
            return false;
        }

        if (storage.TintOverride.Length == 0)
        {
            return false;
        }

        palette = storage.TintOverride;
        return true;
    }

    private static void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            ReloadData();
        }
    }

    private static void ReloadData()
    {
        if (!Context.IsGameLaunched)
        {
            return;
        }

        ModState.Data.Clear();
        foreach (var (itemId, data) in Game1.bigCraftableData)
        {
            if (data.CustomFields?.GetBool(Constants.ModEnabled) != true)
            {
                continue;
            }

            var customFields = new DictionaryModel(GetCustomFields(itemId));
            ModState.Data[itemId] = new StorageData(customFields);
        }
    }

    private static void OnConditionsApiReady(ConditionsApiReadyEventArgs e) => ReloadData();

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e) =>
        _ = this.Helper.GameContent.InvalidateCache(Constants.BigCraftableData);

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _ = new ConfigMenu(this.Helper, this.ModManifest);

        ReloadData();
        var colorfulChestsIntegration = new ColorfulChestsIntegration(this.Helper.ModRegistry);
        if (colorfulChestsIntegration.IsLoaded)
        {
            colorfulChestsIntegration.Api.AddHandler(GetPalette);
        }
    }
}