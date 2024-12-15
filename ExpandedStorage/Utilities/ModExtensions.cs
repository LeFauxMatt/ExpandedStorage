namespace LeFauxMods.ExpandedStorage.Utilities;

using Common.Integrations.ExpandedStorage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.Objects;

internal static class ModExtensions
{
    public static void DrawChest(this StorageData storage, Chest chest, SpriteBatch spriteBatch, int x, int y,
        float alpha, bool local, int currentLidFrame, bool farmerNearby)
    {
        var drawX = (float)x;
        var drawY = (float)y;
        if (chest.localKickStartTile.HasValue)
        {
            drawX = Utility.Lerp(chest.localKickStartTile.Value.X, drawX, chest.kickProgress);
            drawY = Utility.Lerp(chest.localKickStartTile.Value.Y, drawY, chest.kickProgress);
        }

        var baseSortOrder =
            local ? 0.89f : Math.Max(0f, (((drawY + 1f) * Game1.tileSize) - 24f) / 10000f) + (drawX * 1E-05f);
        if (chest.localKickStartTile.HasValue)
        {
            spriteBatch.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, new Vector2(drawX + 0.5f, drawY + 0.5f) * Game1.tileSize),
                Game1.shadowTexture.Bounds,
                Color.Black * 0.5f,
                0f,
                Game1.shadowTexture.Bounds.Center.ToVector2(),
                Game1.pixelZoom,
                SpriteEffects.None,
                0.0001f);

            drawY -= (float)Math.Sin(chest.kickProgress * Math.PI) * 0.5f;
        }

        var playerChoiceColor = chest.playerChoiceColor.Value;
        var colorSelection = Math.Max(0, DiscreteColorPicker.getSelectionFromColor(playerChoiceColor));

        var colored = storage.PlayerColor;
        if (playerChoiceColor is { R: 0, G: 0, B: 0 })
        {
            colored = false;
        }

        if (colored && colorSelection > 0 && colorSelection - 1 < storage.TintOverride.Length)
        {
            playerChoiceColor = storage.TintOverride[colorSelection - 1] is { R: 0, G: 0, B: 0 }
                ? Utility.GetPrismaticColor(0, 2f)
                : storage.TintOverride[colorSelection - 1];
        }

        var color = colored ? playerChoiceColor : chest.Tint;
        var texture = ItemRegistry.GetDataOrErrorItem(chest.QualifiedItemId).GetTexture();
        var pos = local
            ? new Vector2(x, y - Game1.tileSize)
            : Game1.GlobalToLocal(Game1.viewport, new Vector2(drawX, drawY - 1f) * Game1.tileSize);

        var startingLidFrame = chest.startingLidFrame.Value;
        var lastLidFrame = chest.getLastLidFrame();
        if (storage.Animation is Animation.Loop && (!storage.OpenNearby || farmerNearby))
        {
            currentLidFrame = Game1.ticks / 5 % storage.Frames;
        }

        var sourceRect = new Rectangle(
            Math.Min(lastLidFrame - startingLidFrame + 1, Math.Max(0, currentLidFrame - startingLidFrame)) * 16,
            colored ? 32 : 0,
            16,
            32);

        // Draw Base Layer
        spriteBatch.Draw(
            texture,
            pos + (chest.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            sourceRect,
            color * alpha,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            baseSortOrder);

        if (sourceRect.Y == 0)
        {
            return;
        }

        // Draw Top Layer
        sourceRect.Y = 64 + (colorSelection * 32);
        if (sourceRect.Y + 32 > texture.Height)
        {
            sourceRect.Y = 64;
        }

        spriteBatch.Draw(
            texture,
            pos + (chest.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            sourceRect,
            chest.Tint * alpha,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            baseSortOrder + 1E-05f);
    }
}
