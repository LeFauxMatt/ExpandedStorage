namespace LeFauxMods.ExpandedStorage;

using Common.Integrations.ContentPatcher;
using Common.Models;
using Common.Utilities;
using Models;
using Services;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;

internal sealed class ModEntry : Mod
{
    private readonly Dictionary<string, StorageData> data = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> exclude = new(StringComparer.OrdinalIgnoreCase);

    public delegate bool TryGetDataDelegate(string itemId, [NotNullWhen(true)] out StorageData? storageData);

    public override void Entry(IModHelper helper)
    {
        // Init
        Log.Init(this.Monitor);
        ModPatches.Init(this.TryGetData);

        // Events
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;

        var contentPatcherIntegration = new ContentPatcherIntegration(helper);
        if (contentPatcherIntegration.IsLoaded)
        {
            ModEvents.Subscribe<ConditionsApiReadyEventArgs>(this.OnConditionsApiReady);
        }
    }

    /// <summary>Attempts to retrieve storage data for an item type based on the item id.</summary>
    /// <param name="itemId">The id for the item from which to retrieve the storage data.</param>
    /// <param name="storageData">When this method returns, contains the storage data; otherwise, null.</param>
    /// <returns><c>true</c> if the storage data was successfully retrieved; otherwise, <c>false</c>.</returns>
    public bool TryGetData(string itemId, [NotNullWhen(true)] out StorageData? storageData)
    {
        if (this.data.TryGetValue(itemId, out storageData))
        {
            return true;
        }

        if (this.exclude.Contains(itemId))
        {
            return false;
        }

        if (!Game1.bigCraftableData.TryGetValue(itemId, out var oneData))
        {
            this.exclude.Add(itemId);
            return false;
        }

        if (oneData.CustomFields?.GetBool(Constants.ModEnabled) != true)
        {
            this.exclude.Add(itemId);
            return false;
        }

        var customFields = new DictionaryModel(GetCustomFields(itemId));
        storageData = new StorageData(customFields);
        this.data[itemId] = storageData;
        return true;
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) => () =>
        Game1.bigCraftableData.TryGetValue(itemId, out var oneData) ? oneData.CustomFields : null;

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            // Add config options to the data
            e.Edit(asset =>
                {
                    var allData = asset.AsDictionary<string, BigCraftableData>().Data;
                    foreach (var (itemId, oneData) in allData)
                    {
                        if (oneData.CustomFields?.GetBool(Constants.ModEnabled) != true)
                        {
                            continue;
                        }

                        // Copy storage options to data
                        var customFields = new DictionaryModel(() => oneData.CustomFields);
                        var dataModel = new StorageData(customFields);
                    }
                },
                AssetEditPriority.Late);
        }
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (!e.NamesWithoutLocale.Any(assetName => assetName.IsEquivalentTo(Constants.BigCraftableData)))
        {
            return;
        }

        this.data.Clear();
        this.exclude.Clear();
    }

    private void OnConditionsApiReady(ConditionsApiReadyEventArgs e)
    {
        this.data.Clear();
        this.exclude.Clear();
    }
}
