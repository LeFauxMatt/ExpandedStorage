namespace LeFauxMods.ExpandedStorage;

using Common.Integrations.BetterChests;
using Common.Integrations.ContentPatcher;
using Common.Integrations.ExpandedStorage;
using Common.Integrations.GenericModConfigMenu;
using Common.Models;
using Common.Utilities;
using Services;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;

internal sealed class ModEntry : Mod
{
    private readonly Dictionary<string, StorageData> data = new(StringComparer.OrdinalIgnoreCase);

    private BetterChestsIntegration bc = null!;
    private GenericModConfigMenuIntegration gmcm = null!;

    public delegate bool TryGetDataDelegate(string itemId, [NotNullWhen(true)] out StorageData? storageData);

    public override void Entry(IModHelper helper)
    {
        // Init
        Log.Init(this.Monitor);
        ModPatches.Init(this.TryGetData);
        this.bc = new BetterChestsIntegration(this.Helper.ModRegistry);
        this.gmcm = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry);

        // Events
        helper.Events.Content.AssetReady += this.OnAssetReady;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        var contentPatcherIntegration = new ContentPatcherIntegration(helper);
        if (contentPatcherIntegration.IsLoaded)
        {
            ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) => () =>
        Game1.bigCraftableData.TryGetValue(itemId, out var oneData) ? oneData.CustomFields : null;

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
                    var config = this.Helper.ReadConfig<Dictionary<string, Dictionary<string, string>>>();
                    var allData = asset.AsDictionary<string, BigCraftableData>().Data;
                    foreach (var (itemId, bigCraftableData) in allData)
                    {
                        if (bigCraftableData.CustomFields?.GetBool(Constants.ModEnabled) != true)
                        {
                            continue;
                        }

                        if (!config.TryGetValue(itemId, out var storageConfig))
                        {
                            continue;
                        }

                        // Load config options
                        var configModel = new DictionaryModel(() => storageConfig);
                        var sourceOptions = new StorageOptions(configModel);

                        // Load custom fields model
                        var customFields = new DictionaryModel(() => bigCraftableData.CustomFields);
                        var targetOptions = new StorageOptions(customFields);

                        // Copy config options to custom fields
                        sourceOptions.CopyTo(targetOptions);
                    }
                },
                AssetEditPriority.Late);
        }
    }

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e) => this.ReloadData();

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) => this.ReloadData();

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
        if (!this.bc.IsLoaded || !this.gmcm.IsLoaded)
        {
            return;
        }

        var config = this.Helper.ReadConfig<Dictionary<string, Dictionary<string, string>>>();
        this.gmcm.Register(Reset, Save);

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

            if (!config.TryGetValue(itemId, out var storageConfig))
            {
                storageConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var configModel = new DictionaryModel(() => storageConfig);
            var storageOptions = new StorageOptions(configModel);
            if (!config.ContainsKey(itemId))
            {
                // Load custom fields model
                var customFields = new DictionaryModel(() => bigCraftableData.CustomFields);
                var sourceOptions = new StorageOptions(customFields);

                // Copy config options to custom fields
                sourceOptions.CopyTo(storageOptions);
                config[itemId] = storageConfig;
            }

            this.bc.Api.AddConfigOptions(
                this.ModManifest,
                itemId,
                () => TokenParser.ParseText(bigCraftableData.DisplayName),
                storageOptions);
        }

        void Reset()
        {
            foreach (var (_, storageConfig) in config)
            {
                storageConfig.Clear();
            }
        }

        void Save()
        {
            this.Helper.WriteConfig(config);
            this.Helper.GameContent.InvalidateCache(Constants.BigCraftableData);
        }
    }

    /// <summary>Attempts to retrieve storage data for an item type based on the item id.</summary>
    /// <param name="itemId">The id for the item from which to retrieve the storage data.</param>
    /// <param name="storageData">When this method returns, contains the storage data; otherwise, null.</param>
    /// <returns><c>true</c> if the storage data was successfully retrieved; otherwise, <c>false</c>.</returns>
    private bool TryGetData(string itemId, [NotNullWhen(true)] out StorageData? storageData) =>
        this.data.TryGetValue(itemId, out storageData);
}
