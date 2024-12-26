using LeFauxMods.Common.Integrations.BetterChests;
using LeFauxMods.Common.Integrations.ColorfulChests;
using LeFauxMods.Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.ExpandedStorage.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.ExpandedStorage;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static readonly ModConfig DefaultConfig = [];

    private readonly Dictionary<string, StorageData> data = new(StringComparer.OrdinalIgnoreCase);

    private BetterChestsIntegration bc = null!;
    private ModConfig config = null!;
    private ConfigHelper<ModConfig> configHelper = null!;
    private GenericModConfigMenuIntegration gmcm = null!;

    public delegate bool TryGetDataDelegate(string itemId, [NotNullWhen(true)] out StorageData? storageData);

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        this.configHelper = new ConfigHelper<ModConfig>(this.Helper);
        this.config = this.configHelper.Load();

        Log.Init(this.Monitor);
        ModPatches.Init(this.TryGetData);

        this.bc = new BetterChestsIntegration(this.Helper.ModRegistry);
        this.gmcm = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry);

        // Events
        helper.Events.Content.AssetReady += this.OnAssetReady;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);

        var contentPatcherIntegration = new ContentPatcherIntegration(helper);
        if (contentPatcherIntegration.IsLoaded)
        {
            ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) =>
        () =>
            Game1.bigCraftableData.TryGetValue(itemId, out var oneData) ? oneData.CustomFields : null;

    private bool GetPalette(Chest chest, [NotNullWhen(true)] out Color[]? palette)
    {
        palette = null;
        if (!this.TryGetData(chest.ItemId, out var storage))
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

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            this.ReloadData();
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            // Add config options to the data
            e.Edit(asset =>
                {
                    var allData = asset.AsDictionary<string, BigCraftableData>().Data;
                    foreach (var (itemId, bigCraftableData) in allData)
                    {
                        if (bigCraftableData.CustomFields?.GetBool(Constants.ModEnabled) != true)
                        {
                            continue;
                        }

                        // Load custom fields model
                        var customFields = new DictionaryModel(() => bigCraftableData.CustomFields);
                        var baseOptions = new StorageOptions(customFields);

                        if (!DefaultConfig.TryGetValue(itemId, out var defaultOptions))
                        {
                            defaultOptions = new StorageOptions();
                            DefaultConfig[itemId] = defaultOptions;
                        }

                        baseOptions.CopyTo(defaultOptions);

                        if (!this.config.TryGetValue(itemId, out var configOptions))
                        {
                            continue;
                        }

                        // Copy config options to custom fields
                        configOptions.CopyTo(baseOptions);
                    }
                },
                AssetEditPriority.Late);
        }
    }

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e) => this.ReloadData();

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.config.Clear();
        foreach (var (itemId, newOptions) in e.Config)
        {
            this.config[itemId] = new StorageOptions();
            newOptions.CopyTo(this.config[itemId]);
        }

        this.Helper.GameContent.InvalidateCache(Constants.BigCraftableData);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ReloadData();
        var colorfulChestsIntegration = new ColorfulChestsIntegration(this.Helper.ModRegistry);
        if (colorfulChestsIntegration.IsLoaded)
        {
            colorfulChestsIntegration.Api.AddHandler(this.GetPalette);
        }
    }

    private void ReloadData()
    {
        if (!Context.IsGameLaunched)
        {
            return;
        }

        this.data.Clear();
        foreach (var (itemId, bigCraftableData) in Game1.bigCraftableData)
        {
            if (bigCraftableData.CustomFields?.GetBool(Constants.ModEnabled) != true)
            {
                continue;
            }

            var customFields = new DictionaryModel(GetCustomFields(itemId));
            this.data[itemId] = new StorageData(customFields);
        }

        this.SetupConfig();
    }

    private void SetupConfig()
    {
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        var tempConfig = this.configHelper.Load();
        foreach (var (itemId, defaultOptions) in DefaultConfig)
        {
            if (tempConfig.ContainsKey(itemId))
            {
                continue;
            }

            tempConfig[itemId] = new StorageOptions();
            defaultOptions.CopyTo(tempConfig[itemId]);
        }

        this.gmcm.Register(
            () =>
            {
                tempConfig.Clear();
                foreach (var (itemId, defaultOptions) in DefaultConfig)
                {
                    tempConfig[itemId] = new StorageOptions();
                    defaultOptions.CopyTo(tempConfig[itemId]);
                }
            },
            () => this.configHelper.Save(tempConfig));

        if (!this.bc.IsLoaded)
        {
            return;
        }

        // Add page links
        foreach (var (itemId, _) in this.data)
        {
            if (!Game1.bigCraftableData.TryGetValue(itemId, out var bigCraftableData))
            {
                continue;
            }

            this.gmcm.Api.AddPageLink(
                this.ModManifest,
                itemId,
                () => TokenParser.ParseText(bigCraftableData.DisplayName),
                () => TokenParser.ParseText(bigCraftableData.Description));
        }

        // Add config options
        foreach (var (itemId, _) in this.data)
        {
            if (!Game1.bigCraftableData.TryGetValue(itemId, out var bigCraftableData))
            {
                continue;
            }

            if (!tempConfig.TryGetValue(itemId, out var storageOptions))
            {
                continue;
            }

            this.bc.Api.AddConfigOptions(
                this.ModManifest,
                itemId,
                () => TokenParser.ParseText(bigCraftableData.DisplayName),
                storageOptions);
        }
    }

    /// <summary>Attempts to retrieve storage data for an item type based on the item id.</summary>
    /// <param name="itemId">The id for the item from which to retrieve the storage data.</param>
    /// <param name="storageData">When this method returns, contains the storage data; otherwise, null.</param>
    /// <returns><c>true</c> if the storage data was successfully retrieved; otherwise, <c>false</c>.</returns>
    private bool TryGetData(string itemId, [NotNullWhen(true)] out StorageData? storageData) =>
        this.data.TryGetValue(itemId, out storageData);
}
