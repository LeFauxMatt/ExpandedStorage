using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.ExpandedStorage.Models;

/// <inheritdoc />
internal sealed class StorageConfig : DictionaryDataModel
{
    /// <inheritdoc />
    /// <param name="dictionaryModel">The backing dictionary.</param>
    public StorageConfig(IDictionaryModel? dictionaryModel = null)
        : base(dictionaryModel ?? new DictionaryModel())
    {
        if (this.GetData()?.Any() != false)
        {
            return;
        }

        this.ColorfulChests = true;
        this.UnlimitedStorage = true;
    }

    /// <summary>Gets or sets a value indicating whether colorful chests is enabled.</summary>
    public bool ColorfulChests
    {
        get => this.Get("Enabled", StringToBool, prefix: ModConstants.Prefix.ColorfulChests);
        set => this.Set("Enabled", value, BoolToString, ModConstants.Prefix.ColorfulChests);
    }

    /// <summary>Gets or sets a value indicating whether unlimited storage is enabled.</summary>
    public bool UnlimitedStorage
    {
        get => this.Get("Enabled", StringToBool, prefix: ModConstants.Prefix.UnlimitedStorage);
        set => this.Set("Enabled", value, BoolToString, ModConstants.Prefix.UnlimitedStorage);
    }

    /// <inheritdoc />
    protected override string Prefix => ModConstants.Prefix.ExpandedStorage;
}