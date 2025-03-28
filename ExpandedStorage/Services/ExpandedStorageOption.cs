using System.Collections.Immutable;
using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.ExpandedStorage.Models;
using LeFauxMods.ExpandedStorage.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.ExpandedStorage.Services;

internal sealed class ExpandedStorageOption : ComplexOption
{
    private readonly int baseHeight;
    private readonly Dictionary<string, CachedItemData> cachedItems = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ClickableComponent> components = [];
    private readonly int height;
    private readonly int[] lidFrames;
    private int selectedIndex = -1;

    public ExpandedStorageOption(IModHelper helper)
        : base(helper)
    {
        var itemIds = ModState.Data.Keys.ToImmutableArray();
        this.lidFrames = new int[itemIds.Length];
        this.baseHeight = Game1.tileSize * (int)Math.Ceiling(itemIds.Length / 14f);

        ClickableComponent component;
        for (var index = 0; index < itemIds.Length; index++)
        {
            var itemId = itemIds[index];
            if (!ModState.Data.TryGetValue(itemId, out var storageData))
            {
                continue;
            }

            var row = index / 14;
            var col = index % 14;

            component = new ClickableComponent(
                new Rectangle(col * Game1.tileSize, row * Game1.tileSize, Game1.tileSize, Game1.tileSize),
                itemId) { myID = index };

            var chest = storageData.CreateChest(Vector2.Zero, itemId);
            var parsedItemData = ItemRegistry.GetDataOrErrorItem(chest.QualifiedItemId);
            var sourceRect = parsedItemData.GetSourceRect(0, parsedItemData.SpriteIndex);

            this.components.Add(component);
            this.cachedItems.Add(itemId, new CachedItemData(chest, parsedItemData, sourceRect));
        }

        this.height = this.baseHeight + 16;

        if (helper.ModRegistry.IsLoaded("furyx639.ColorfulChests"))
        {
            component = new ClickableTextureComponent(
                "colorful",
                new Rectangle(
                    0,
                    this.height + 16,
                    OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                    OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
                null,
                null,
                Game1.mouseCursors,
                OptionsCheckbox.sourceRectChecked,
                Game1.pixelZoom);

            this.components.Add(component);

            var (textWidth, textHeight) =
                Game1.dialogueFont.MeasureString(I18n.ConfigOption_ColorfulChests_Name()).ToPoint();
            component = new ClickableComponent(
                new Rectangle(0, this.height + 16, textWidth, textHeight),
                "config-option.colorful-chests.description",
                I18n.ConfigOption_ColorfulChests_Name());

            this.components.Add(component);
            this.height += textHeight + 16;
        }

        if (helper.ModRegistry.IsLoaded("furyx639.UnlimitedStorage"))
        {
            component = new ClickableTextureComponent(
                "unlimited",
                new Rectangle(
                    0,
                    this.height + 16,
                    OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                    OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
                null,
                null,
                Game1.mouseCursors,
                OptionsCheckbox.sourceRectChecked,
                Game1.pixelZoom);

            this.components.Add(component);

            var (textWidth, textHeight) =
                Game1.dialogueFont.MeasureString(I18n.ConfigOption_UnlimitedStorage_Name()).ToPoint();
            component = new ClickableComponent(
                new Rectangle(0, this.height + 16, textWidth, textHeight),
                "config-option.unlimited-storage.description",
                I18n.ConfigOption_UnlimitedStorage_Name());

            this.components.Add(component);
            this.height += textHeight + 16;
        }

        if (this.height != this.baseHeight)
        {
            this.height += 16;
        }
    }

    /// <inheritdoc />
    public override int Height => this.selectedIndex != -1 ? this.height : this.baseHeight;

    public override void DrawOption(SpriteBatch spriteBatch, Vector2 pos)
    {
        var (mouseX, mouseY) = this.MousePos;
        var hoverText = default(string);
        var hoverTitle = default(string);
        StorageConfig? storageConfig = null;

        if (this.selectedIndex != -1 &&
            this.cachedItems.TryGetValue(this.components[this.selectedIndex].name, out var cachedItem) &&
            !ModState.ConfigHelper.Temp.TryGetValue(cachedItem.Data.ItemId, out storageConfig))
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            storageConfig = new StorageConfig(new DictionaryModel(() => values));
            ModState.ConfigHelper.Temp.Add(cachedItem.Data.ItemId, storageConfig);
        }

        foreach (var component in this.components)
        {
            var hovered = component.bounds.Contains(mouseX, mouseY);
            var index = component.myID;

            if (this.cachedItems.TryGetValue(component.name, out cachedItem) &&
                ModState.Data.TryGetValue(component.name, out var storageData))
            {
                var (chest, parsedItemData, sourceRect) = cachedItem;

                component.scale = Math.Max(1f, component.scale - 0.025f);
                if (hovered)
                {
                    component.scale = Math.Min(component.scale + 0.05f, 1.1f);
                    hoverTitle ??= chest.DisplayName;
                    hoverText ??= chest.getDescription();
                    if (this.Pressed)
                    {
                        Game1.playSound("smallSelect");
                        this.selectedIndex = component.myID;
                    }
                }

                spriteBatch.Draw(
                    Game1.menuTexture,
                    pos + component.bounds.Location.ToVector2(),
                    Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.5f);

                if (index == this.selectedIndex)
                {
                    spriteBatch.Draw(
                        Game1.menuTexture,
                        pos + component.bounds.Location.ToVector2(),
                        Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56),
                        Color.Red,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        0.5f);
                }

                this.lidFrames[index] = hovered || storageData.Animation is Animation.Loop
                    ? this.lidFrames[index] + 1
                    : this.lidFrames[index] - 1;

                this.lidFrames[index] = storageData.Animation is not Animation.Loop
                    ? Math.Max(0, Math.Min(storageData.Frames * 5, this.lidFrames[index]))
                    : this.lidFrames[index] % (storageData.Frames * 5);

                storageData.DrawChest(
                    chest,
                    spriteBatch,
                    (int)(pos.X + component.bounds.Center.X),
                    (int)(pos.Y + component.bounds.Center.Y) + Game1.tileSize,
                    1f,
                    sourceRect.Size.ToVector2() / 2f,
                    Game1.pixelZoom * component.scale / 2f,
                    true,
                    this.lidFrames[index] / 5,
                    false);
            }

            if (storageConfig is null)
            {
                continue;
            }

            if (component is ClickableTextureComponent clickableTextureComponent)
            {
                switch (component.name)
                {
                    case "colorful":
                        if (this.Pressed &&
                            (component.bounds with { X = this.AvailableWidth / 2 }).Contains(mouseX, mouseY))
                        {
                            Game1.playSound("drumkit6");
                            storageConfig.ColorfulChests = !storageConfig.ColorfulChests;
                        }

                        clickableTextureComponent.sourceRect = storageConfig.ColorfulChests
                            ? OptionsCheckbox.sourceRectChecked
                            : OptionsCheckbox.sourceRectUnchecked;

                        clickableTextureComponent.draw(
                            spriteBatch,
                            Color.White,
                            1f,
                            0,
                            (int)pos.X + (this.AvailableWidth / 2),
                            (int)pos.Y);

                        continue;

                    case "unlimited":
                        if (this.Pressed &&
                            (component.bounds with { X = this.AvailableWidth / 2 }).Contains(mouseX, mouseY))
                        {
                            Game1.playSound("drumkit6");
                            storageConfig.UnlimitedStorage = !storageConfig.UnlimitedStorage;
                        }

                        clickableTextureComponent.sourceRect = storageConfig.UnlimitedStorage
                            ? OptionsCheckbox.sourceRectChecked
                            : OptionsCheckbox.sourceRectUnchecked;

                        clickableTextureComponent.draw(
                            spriteBatch,
                            Color.White,
                            1f,
                            0,
                            (int)pos.X + (this.AvailableWidth / 2),
                            (int)pos.Y);

                        continue;

                    default:
                        continue;
                }
            }

            if (component.bounds.Contains(mouseX, mouseY))
            {
                hoverTitle ??= component.label;
                hoverText ??= this.Helper.Translation.Get(component.name);
            }

            Utility.drawTextWithShadow(
                spriteBatch,
                component.label,
                Game1.dialogueFont,
                pos + component.bounds.Location.ToVector2(),
                SpriteText.color_Gray);
        }

        if (!string.IsNullOrWhiteSpace(hoverTitle))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont, boldTitleText: hoverTitle);
        }
        else if (!string.IsNullOrWhiteSpace(hoverText))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont);
        }
    }

    private readonly record struct CachedItemData(Chest Chest, ParsedItemData Data, Rectangle SourceRect);
}