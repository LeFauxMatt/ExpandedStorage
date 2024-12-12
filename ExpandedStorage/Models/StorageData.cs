namespace LeFauxMods.ExpandedStorage.Models;

using Common.Models;
using Microsoft.Xna.Framework;

/// <summary>Initializes a new instance of the <see cref="StorageData" /> class.</summary>
/// <param name="dictionaryModel">The backing dictionary.</param>
internal sealed class StorageData(IDictionaryModel dictionaryModel) : DictionaryDataModel(dictionaryModel)
{
    /// <summary>Gets or sets the sound to play when the lid closing animation plays.</summary>
    public string CloseNearbySound
    {
        get => this.Get(nameof(this.CloseNearbySound), Constants.LidCloseSound);
        set => this.Set(nameof(this.CloseNearbySound), value);
    }

    /// <summary>Gets or sets the number of frames in the lid animation.</summary>
    public int Frames
    {
        get => this.Get(nameof(this.Frames), StringToInt, 1);
        set => this.Set(nameof(this.Frames), value, IntToString);
    }

    /// <summary>Gets or sets the global inventory id.</summary>
    public string? GlobalInventoryId
    {
        get => this.Get(nameof(this.GlobalInventoryId));
        set => this.Set(nameof(this.GlobalInventoryId), value ?? string.Empty);
    }

    /// <summary>Gets or sets a value indicating whether the storage is a fridge.</summary>
    public bool IsFridge
    {
        get => this.Get(nameof(this.IsFridge), StringToBool);
        set => this.Set(nameof(this.IsFridge), value, BoolToString);
    }

    /// <summary>Gets or sets any mod data that should be added to the chest on creation.</summary>
    public Dictionary<string, string>? ModData
    {
        get => this.Get(nameof(this.ModData), StringToDict);
        set => this.Set(nameof(this.ModData), value, DictToString);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the storage will play its lid opening animation when the player is
    ///     nearby.
    /// </summary>
    public bool OpenNearby
    {
        get => this.Get(nameof(this.OpenNearby), StringToBool);
        set => this.Set(nameof(this.OpenNearby), value, BoolToString);
    }

    /// <summary>Gets or sets the sound to play when the lid opening animation plays.</summary>
    public string OpenNearbySound
    {
        get => this.Get(nameof(this.OpenNearbySound), Constants.LidOpenSound);
        set => this.Set(nameof(this.OpenNearbySound), value);
    }

    /// <summary>Gets or sets the sound to play when the storage is opened.</summary>
    public string OpenSound
    {
        get => this.Get(nameof(this.OpenSound), Constants.ChestOpenSound);
        set => this.Set(nameof(this.OpenSound), value);
    }

    /// <summary>Gets or sets the sound to play when storage is placed.</summary>
    public string PlaceSound
    {
        get => this.Get(nameof(this.PlaceSound), Constants.ChestPlacementSound);
        set => this.Set(nameof(this.PlaceSound), value);
    }

    /// <summary>Gets or sets a value indicating whether player color is enabled.</summary>
    public bool PlayerColor
    {
        get => this.Get(nameof(this.PlayerColor), StringToBool);
        set => this.Set(nameof(this.PlayerColor), value, BoolToString);
    }

    /// <summary>Gets or sets a value to override the texture.</summary>
    public string TextureOverride
    {
        get => this.Get(nameof(this.TextureOverride));
        set => this.Set(nameof(this.TextureOverride), value);
    }

    /// <summary>Gets or sets a color to apply to the tinted layer.</summary>
    public Color TintOverride
    {
        get => this.Get(nameof(this.TintOverride), StringToColor, Color.Black);
        set => this.Set(nameof(this.TintOverride), value, ColorToString);
    }

    /// <inheritdoc />
    protected override string Prefix { get; } = Constants.ModDataPrefix;
}
