using LeFauxMods.Common.Integrations.ColorfulChests;
using LeFauxMods.Common.Integrations.ContentPatcher;
using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Objects;

namespace LeFauxMods.ExpandedStorage.Services;

/// <summary>Responsible for managing state.</summary>
internal sealed class ModState
{
    private static ModState? Instance;
    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly ConfigHelper<ModConfig> configHelper;
    private ConfigMenu? configMenu;
    private Dictionary<string, StorageData>? data;

    private ColorfulChestsIntegration? colorfulChests;

    private ModState(IModHelper helper, IManifest manifest)
    {
        // Init
        this.helper = helper;
        this.manifest = manifest;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        _ = new ContentPatcherIntegration(helper);

        // Events
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
        helper.Events.Content.AssetReady += this.OnAssetReady;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
    }

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Dictionary<string, StorageData> Data => Instance!.data ??= Instance.GetData();

    public static IColorfulChestsApi? ColorfulChests => Instance?.colorfulChests?.Api;

    public static void Init(IModHelper helper, IManifest manifest) => Instance ??= new ModState(helper, manifest);

    private static bool GetPalette(Chest chest, [NotNullWhen(true)] out Color[]? palette)
    {
        palette = null;
        if (!Data.TryGetValue(chest.ItemId, out var storage))
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

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) =>
        () => Game1.bigCraftableData.TryGetValue(itemId, out var bigCraftableData)
            ? bigCraftableData.CustomFields
            : null;

    private Dictionary<string, StorageData> GetData()
    {
        this.data ??= new Dictionary<string, StorageData>(StringComparer.OrdinalIgnoreCase);
        foreach (var (itemId, bigCraftableData) in Game1.bigCraftableData)
        {
            if (bigCraftableData.CustomFields?.GetBool(Constants.ModEnabled) != true)
            {
                continue;
            }

            var customFields = new DictionaryModel(GetCustomFields(itemId));
            this.data[itemId] = new StorageData(customFields);

            // Initialize Config
            ConfigHelper.Temp.TryAdd(itemId,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { Constants.ColorfulChestsEnabled, "true" }, { Constants.UnlimitedStorageEnabled, "true" }
                });
        }

        this.configMenu?.SetupMenu();
        return this.data;
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            return;
        }

        this.helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        this.helper.Events.Content.AssetReady -= this.OnAssetReady;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        _ = Data;
        this.helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.configMenu = new ConfigMenu(this.helper, this.manifest);
        this.colorfulChests = new ColorfulChestsIntegration(this.helper.ModRegistry);
        if (this.colorfulChests.IsLoaded)
        {
            this.colorfulChests.Api.AddHandler(GetPalette);
        }
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(static assetName => assetName.IsEquivalentTo(Constants.BigCraftableData)))
        {
            this.data = null;
        }
    }

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e)
    {
        this.data = null;
        this.helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }
}