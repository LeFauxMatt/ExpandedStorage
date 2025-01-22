using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
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
    private readonly List<ClickableTextureComponent> components = [];
    private readonly IModHelper helper;
    private readonly int[] lidFrames;
    private readonly List<Slot> slots = [];
    private int extraHeight;
    private int selectedIndex = -1;

    public ExpandedStorageOption(IModHelper helper)
    {
        this.helper = helper;
        this.baseHeight = Game1.tileSize * (int)Math.Ceiling(ModState.Data.Count / 14f);

        var index = 0;
        foreach (var (itemId, storageData) in ModState.Data)
        {
            var row = index / 14;
            var col = index % 14;
            var component = new ClickableComponent(
                new Rectangle(col * Game1.tileSize, row * Game1.tileSize, Game1.tileSize, Game1.tileSize),
                itemId) { myID = index };

            if (col > 0)
            {
                component.leftNeighborID = index - 1;
                this.slots[component.leftNeighborID].Component.rightNeighborID = index;
            }

            if (row > 0)
            {
                component.upNeighborID = index - 12;
                this.slots[component.upNeighborID].Component.downNeighborID = index;
            }

            var chest = storageData.CreateChest(Vector2.Zero, itemId);
            var itemData = ItemRegistry.GetDataOrErrorItem(chest.QualifiedItemId);
            var sourceRect = itemData.GetSourceRect(0, chest.ParentSheetIndex);

            this.slots.Add(new Slot(component, chest, itemData, sourceRect));
            index++;
        }

        this.lidFrames = new int[index];

        if (helper.ModRegistry.IsLoaded("furyx639.ColorfulChests"))
        {
            this.components.Add(new ClickableTextureComponent(
                "colorful",
                new Rectangle(
                    0,
                    0,
                    OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                    OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
                I18n.ConfigOption_ColorfulChests_Name(),
                I18n.ConfigOption_ColorfulChests_Description(),
                Game1.mouseCursors,
                OptionsCheckbox.sourceRectChecked,
                Game1.pixelZoom) { drawLabel = false });
        }

        if (helper.ModRegistry.IsLoaded("furyx639.UnlimitedStorage"))
        {
            this.components.Add(new ClickableTextureComponent(
                "unlimited",
                new Rectangle(
                    0,
                    0,
                    OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                    OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
                I18n.ConfigOption_UnlimitedStorage_Name(),
                I18n.ConfigOption_UnlimitedStorage_Description(),
                Game1.mouseCursors,
                OptionsCheckbox.sourceRectChecked,
                Game1.pixelZoom) { drawLabel = false });
        }
    }

    /// <inheritdoc />
    public override int Height => this.baseHeight + this.extraHeight;

    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        var availableWidth = Math.Min(1200, Game1.uiViewport.Width - 200);
        pos.X -= availableWidth / 2f;
        var (originX, originY) = pos.ToPoint();
        var (mouseX, mouseY) = this.helper.Input.GetCursorPosition().GetScaledScreenPixels().ToPoint();

        mouseX -= originX;
        mouseY -= originY;

        var mouseLeft = this.helper.Input.GetState(SButton.MouseLeft);
        var controllerA = this.helper.Input.GetState(SButton.ControllerA);
        var hoverText = default(string);

        for (var index = 0; index < this.slots.Count; index++)
        {
            var (slot, _, _, _) = this.slots[index];

            spriteBatch.Draw(
                Game1.menuTexture,
                pos + slot.bounds.Location.ToVector2(),
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
                    pos + slot.bounds.Location.ToVector2(),
                    Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56),
                    Color.Red,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.5f);
            }
        }

        for (var index = 0; index < this.slots.Count; index++)
        {
            var (slot, chest, _, sourceRect) = this.slots[index];

            if (!ModState.Data.TryGetValue(slot.name, out var storageData))
            {
                continue;
            }

            slot.scale = Math.Max(1f, slot.scale - 0.025f);
            if (slot.bounds.Contains(mouseX, mouseY))
            {
                slot.scale = Math.Min(slot.scale + 0.05f, 1.1f);

                // Check for click
                if (mouseLeft is SButtonState.Pressed || controllerA is SButtonState.Pressed)
                {
                    Game1.playSound("smallSelect");
                    this.selectedIndex = index;
                }

                hoverText ??= chest.DisplayName;
            }

            this.lidFrames[index] = storageData.Animation is Animation.Loop || slot.bounds.Contains(mouseX, mouseY)
                ? this.lidFrames[index] + 1
                : this.lidFrames[index] - 1;

            this.lidFrames[index] = storageData.Animation is not Animation.Loop
                ? Math.Max(0, Math.Min(storageData.Frames * 5, this.lidFrames[index]))
                : this.lidFrames[index] % (storageData.Frames * 5);

            storageData.DrawChest(
                chest,
                spriteBatch,
                (int)(pos.X + slot.bounds.Center.X),
                (int)(pos.Y + slot.bounds.Center.Y) + Game1.tileSize,
                1f,
                new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f),
                Game1.pixelZoom * slot.scale / 2f,
                true,
                this.lidFrames[index] / 5,
                false);
        }

        if (this.selectedIndex == -1)
        {
            return;
        }

        pos.Y += this.baseHeight + 16;

        var (_, _, itemData, _) = this.slots[this.selectedIndex];
        if (!ModState.ConfigHelper.Temp.TryGetValue(itemData.ItemId, out var storageOptions))
        {
            storageOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ModState.ConfigHelper.Temp.Add(itemData.ItemId, storageOptions);
        }

        var (textWidth, textHeight) = Game1.dialogueFont.MeasureString(itemData.DisplayName);
        Utility.drawTextWithShadow(
            spriteBatch,
            itemData.DisplayName,
            Game1.dialogueFont,
            pos,
            SpriteText.color_Gray);

        pos.Y += textHeight;

        (textWidth, textHeight) = Game1.smallFont.MeasureString(itemData.Description);
        if (textWidth > availableWidth)
        {
            var words = itemData.Description.Split(' ');

            while (words.Length > 0)
            {
                var subText = string.Empty;
                for (var offset = 0; offset < words.Length; offset++)
                {
                    subText = string.Join(' ', words[..^offset]);
                    (textWidth, textHeight) = Game1.smallFont.MeasureString(subText);
                    if (textWidth > availableWidth)
                    {
                        continue;
                    }

                    words = words[^offset..];
                    break;
                }

                if (string.IsNullOrWhiteSpace(subText))
                {
                    break;
                }

                spriteBatch.DrawString(
                    Game1.smallFont,
                    subText,
                    pos,
                    SpriteText.color_Gray);

                pos.Y += textHeight;
            }

            pos.Y += 16;
        }
        else
        {
            spriteBatch.DrawString(
                Game1.smallFont,
                itemData.Description,
                pos,
                SpriteText.color_Gray);

            pos.Y += textHeight + 16;
        }

        foreach (var component in this.components)
        {
            (textWidth, textHeight) = Game1.dialogueFont.MeasureString(component.label);
            Utility.drawTextWithShadow(
                spriteBatch,
                component.label,
                Game1.dialogueFont,
                pos,
                SpriteText.color_Gray);

            var hovered = component.containsPoint(mouseX - (availableWidth / 2), mouseY - (int)pos.Y + originY);
            switch (component.name)
            {
                case "colorful":
                    component.sourceRect = storageOptions.ContainsKey(Constants.ColorfulChestsEnabled)
                        ? OptionsCheckbox.sourceRectChecked
                        : OptionsCheckbox.sourceRectUnchecked;

                    if (hovered && (mouseLeft is SButtonState.Pressed || controllerA is SButtonState.Pressed))
                    {
                        Game1.playSound("drumkit6");
                        if (!storageOptions.TryAdd(Constants.ColorfulChestsEnabled, "true"))
                        {
                            storageOptions.Remove(Constants.ColorfulChestsEnabled);
                        }
                    }

                    break;
                case "unlimited":
                    component.sourceRect = storageOptions.ContainsKey(Constants.UnlimitedStorageEnabled)
                        ? OptionsCheckbox.sourceRectChecked
                        : OptionsCheckbox.sourceRectUnchecked;

                    if (hovered && (mouseLeft is SButtonState.Pressed || controllerA is SButtonState.Pressed))
                    {
                        Game1.playSound("drumkit6");
                        if (!storageOptions.TryAdd(Constants.UnlimitedStorageEnabled, "true"))
                        {
                            storageOptions.Remove(Constants.UnlimitedStorageEnabled);
                        }
                    }

                    break;
            }

            if (hovered)
            {
                hoverText = component.hoverText;
            }

            component.draw(
                spriteBatch,
                Color.White,
                1f,
                0,
                (int)pos.X + (availableWidth / 2),
                (int)pos.Y);

            pos.Y += textHeight + 16;
        }

        this.extraHeight = (int)pos.Y - originY - this.baseHeight;

        if (!string.IsNullOrWhiteSpace(hoverText))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont);
        }
    }

    private readonly record struct Slot(
        ClickableComponent Component,
        Chest Chest,
        ParsedItemData Data,
        Rectangle SourceRect);
}