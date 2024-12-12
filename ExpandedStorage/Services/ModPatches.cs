namespace LeFauxMods.ExpandedStorage.Services;

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;

internal static class ModPatches
{
    private static readonly Harmony Harmony;

    private static StorageManager storageManager = null!;

    static ModPatches() => Harmony = new Harmony(Constants.ModId);

    public static void Init(StorageManager sm)
    {
        storageManager = sm;

        // Replace the default chest sounds with the custom sounds
        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.checkForAction)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Chest_checkForAction_transpiler)));

        var checkForActionDelegate = typeof(Chest)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .First(method => method.Name.Contains("<checkForAction>", StringComparison.Ordinal));

        _ = Harmony.Patch(
            checkForActionDelegate,
            transpiler: new HarmonyMethod(typeof(ModPatches),
                nameof(Chest_checkForAction_delegate_transpiler)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.OpenMiniShippingMenu)),
            transpiler: new HarmonyMethod(typeof(ModPatches),
                nameof(Chest_OpenMiniShippingMenu_transpiler)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.UpdateFarmerNearby)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Chest_UpdateFarmerNearby_transpiler)));

        // Replace the draw method with a custom texture
        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(Chest),
                nameof(Chest.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)]),
            new HarmonyMethod(typeof(ModPatches), nameof(Chest_draw_prefix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(Chest),
                nameof(Chest.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(bool)]),
            new HarmonyMethod(typeof(ModPatches), nameof(Chest_drawLocal_prefix)));

        // Fix the lid starting lid frame to match the custom texture
        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.getLastLidFrame)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Chest_getLastLidFrame_postfix)));

        // Allow mini-shipping bin behavior on other types of chests
        // (e.g. the mini-shipping bin plays its lid opening animation as the player is within range)
        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.ShowMenu)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Chest_SpecialChestType_transpiler)));

        var itemGrabMenuConstructor = AccessTools
            .GetDeclaredConstructors(typeof(ItemGrabMenu))
            .First(ctor => ctor.GetParameters().Length >= 10);

        var specialChestTypeTranspiler = new HarmonyMethod(typeof(ModPatches),
            nameof(ItemGrabMenu_SpecialChestType_transpiler));

        _ = Harmony.Patch(
            itemGrabMenuConstructor,
            transpiler: specialChestTypeTranspiler);

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.draw)),
            transpiler: specialChestTypeTranspiler);

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.gameWindowSizeChanged)),
            transpiler: specialChestTypeTranspiler);

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.setSourceItem)),
            transpiler: specialChestTypeTranspiler);

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.snapToDefaultClickableComponent)),
            transpiler: specialChestTypeTranspiler);

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.snapToDefaultClickableComponent)),
            transpiler: specialChestTypeTranspiler);

        // Enable or disable the color picker depending on if the texture supports player-color
        _ = Harmony.Patch(
            itemGrabMenuConstructor,
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_constructor_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.gameWindowSizeChanged)),
            postfix: new HarmonyMethod(typeof(ModPatches),
                nameof(ItemGrabMenu_gameWindowSizeChanged_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.setSourceItem)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_setSourceItem_postfix)));

        // Place the object as a Chest with Expanded Storage mod data.
        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_placementAction_postfix)));
    }

    private static IEnumerable<CodeInstruction>
        Chest_checkForAction_delegate_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.ChestOpenSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.LidOpenSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .InstructionEnumeration();

    private static IEnumerable<CodeInstruction>
        Chest_checkForAction_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.ChestOpenSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .InstructionEnumeration();

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Chest_draw_prefix(
        Chest __instance,
        ref int ___currentLidFrame,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha)
    {
        if (!__instance.playerChest.Value || !storageManager.TryGetData(__instance.ItemId, out var storage))
        {
            return true;
        }

        var drawX = (float)x;
        var drawY = (float)y;
        if (__instance.localKickStartTile.HasValue)
        {
            drawX = Utility.Lerp(__instance.localKickStartTile.Value.X, drawX, __instance.kickProgress);
            drawY = Utility.Lerp(__instance.localKickStartTile.Value.Y, drawY, __instance.kickProgress);
        }

        var baseSortOrder = Math.Max(0f, (((drawY + 1f) * 64f) - 24f) / 10000f) + (drawX * 1E-05f);
        if (__instance.localKickStartTile.HasValue)
        {
            spriteBatch.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, new Vector2((drawX + 0.5f) * 64f, (drawY + 0.5f) * 64f)),
                Game1.shadowTexture.Bounds,
                Color.Black * 0.5f,
                0f,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                4f,
                SpriteEffects.None,
                0.0001f);

            drawY -= (float)Math.Sin(__instance.kickProgress * Math.PI) * 0.5f;
        }

        var playerChoiceColor = __instance.playerChoiceColor.Value;
        if (storage.TintOverride is not { R: 0, G: 0, B: 0 })
        {
            playerChoiceColor = storage.TintOverride;
        }

        var colored = storage.PlayerColor;
        if (playerChoiceColor is { R: 0, G: 0, B: 0 })
        {
            colored = false;
        }

        var color = colored ? playerChoiceColor : __instance.Tint;

        var data = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        var texture = string.IsNullOrWhiteSpace(storage.TextureOverride)
            ? data.GetTexture()
            : storageManager.GetTexture(storage.TextureOverride);

        var pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(drawX, drawY - 1f) * Game1.tileSize);
        var startingLidFrame = __instance.startingLidFrame.Value;
        var lastLidFrame = __instance.getLastLidFrame();
        var frame = new Rectangle(
            Math.Min(lastLidFrame - startingLidFrame + 1, Math.Max(0, ___currentLidFrame - startingLidFrame)) * 16,
            colored ? 32 : 0,
            16,
            32);

        // Draw Base Layer
        spriteBatch.Draw(
            texture,
            pos + (__instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            frame,
            color * alpha,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            baseSortOrder);

        if (frame.Y == 0)
        {
            return false;
        }

        // Draw Top Layer
        frame.Y = 64;
        spriteBatch.Draw(
            texture,
            pos + (__instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            frame,
            __instance.Tint * alpha,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            baseSortOrder + 1E-05f);

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Chest_drawLocal_prefix(
        Chest __instance,
        ref int ___currentLidFrame,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha,
        bool local)
    {
        if (!__instance.playerChest.Value || !storageManager.TryGetData(__instance.ItemId, out var storage))
        {
            return true;
        }

        var colored = storage.PlayerColor && !__instance.playerChoiceColor.Value.Equals(Color.Black);
        var color = colored ? __instance.playerChoiceColor.Value : __instance.Tint;

        var data = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        var texture = string.IsNullOrWhiteSpace(storage.TextureOverride)
            ? data.GetTexture()
            : storageManager.GetTexture(storage.TextureOverride);

        var pos = local
            ? new Vector2(x, y - 64)
            : Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y - 1) * Game1.tileSize);

        var startingLidFrame = __instance.startingLidFrame.Value;
        var lastLidFrame = __instance.getLastLidFrame();
        var frame = new Rectangle(
            Math.Min(lastLidFrame - startingLidFrame + 1, Math.Max(0, ___currentLidFrame - startingLidFrame)) * 16,
            colored ? 32 : 0,
            16,
            32);

        var baseSortOrder = local ? 0.89f : ((y * 64) + 4) / 10000f;

        // Draw Base Layer
        spriteBatch.Draw(
            texture,
            pos + (__instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            frame,
            color * alpha,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            baseSortOrder);

        if (frame.Y == 0)
        {
            return false;
        }

        // Draw Top Layer
        frame.Y = 64;
        spriteBatch.Draw(
            texture,
            pos + (__instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), 0) : Vector2.Zero),
            frame,
            __instance.Tint * alpha,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            baseSortOrder + 1E-05f);

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Chest_getLastLidFrame_postfix(Chest __instance, ref int __result)
    {
        if (!__instance.playerChest.Value || !storageManager.TryGetData(__instance.ItemId, out var storage))
        {
            return;
        }

        __result = __instance.startingLidFrame.Value + storage.Frames - 1;
    }

    private static IEnumerable<CodeInstruction>
        Chest_OpenMiniShippingMenu_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(instruction => instruction.LoadsConstant(Constants.MiniShippingBinOpenSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .InstructionEnumeration();

    private static IEnumerable<CodeInstruction>
        Chest_SpecialChestType_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    instruction =>
                        instruction.Calls(AccessTools.PropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)))))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSpecialChestType)))
            .InstructionEnumeration();

    private static IEnumerable<CodeInstruction>
        Chest_UpdateFarmerNearby_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.LidOpenSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.LidCloseSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .InstructionEnumeration();

    private static string GetSound(string sound, Chest chest)
    {
        if (!storageManager.TryGetData(chest.ItemId, out var storage))
        {
            return sound;
        }

        var customSound = sound switch
        {
            Constants.ChestOpenSound or Constants.MiniShippingBinOpenSound => storage.OpenSound,
            Constants.LidOpenSound => storage.OpenNearbySound,
            Constants.LidCloseSound => storage.CloseNearbySound,
            _ => sound
        };

        return string.IsNullOrWhiteSpace(customSound) ? sound : customSound;
    }

    private static Chest.SpecialChestTypes GetSpecialChestType(
        Chest.SpecialChestTypes specialChestType,
        Item sourceItem)
    {
        if (!storageManager.TryGetData(sourceItem.ItemId, out var storage) || !storage.OpenNearby)
        {
            return specialChestType;
        }

        return Chest.SpecialChestTypes.None;
    }


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void ItemGrabMenu_constructor_postfix(ItemGrabMenu __instance, ref Item ___sourceItem) =>
        UpdateColorPicker(__instance, ___sourceItem);


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void ItemGrabMenu_gameWindowSizeChanged_postfix(ItemGrabMenu __instance, ref Item ___sourceItem) =>
        UpdateColorPicker(__instance, ___sourceItem);


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void ItemGrabMenu_setSourceItem_postfix(ItemGrabMenu __instance, ref Item ___sourceItem) =>
        UpdateColorPicker(__instance, ___sourceItem);

    private static IEnumerable<CodeInstruction>
        ItemGrabMenu_SpecialChestType_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    instruction =>
                        instruction.Calls(AccessTools.PropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)))))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(ItemGrabMenu), nameof(ItemGrabMenu.sourceItem)),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSpecialChestType)))
            .InstructionEnumeration();


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Object_placementAction_postfix(
        SObject __instance,
        ref bool __result,
        GameLocation location,
        int x,
        int y)
    {
        if (!__result || !storageManager.TryGetData(__instance.ItemId, out var storage))
        {
            return;
        }

        var tile = new Vector2((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize));

        // Disallow the placement of chests in the MineShaft and VolcanoDungeon locations
        if (location is MineShaft or VolcanoDungeon)
        {
            location.Objects[tile] = null;
            Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
            __result = false;
            return;
        }

        var chest = new Chest(true, tile, __instance.ItemId)
        {
            GlobalInventoryId = storage.GlobalInventoryId,
            shakeTimer = 50,
            SpecialChestType =
                storage.OpenNearby ? Chest.SpecialChestTypes.MiniShippingBin : Chest.SpecialChestTypes.None,
            fridge = { Value = storage.IsFridge }
        };

        if (storage.ModData?.Any() == true)
        {
            foreach (var (key, value) in storage.ModData)
            {
                chest.modData[key] = value;
            }
        }

        location.Objects[tile] = chest;
        location.playSound(storage.PlaceSound);
        __result = true;
    }

    private static void UpdateColorPicker(ItemGrabMenu itemGrabMenu, Item sourceItem)
    {
        if (sourceItem is not Chest chest || !storageManager.TryGetData(chest.ItemId, out var storage))
        {
            return;
        }

        if (storage.PlayerColor || itemGrabMenu.chestColorPicker is not null)
        {
            return;
        }

        itemGrabMenu.chestColorPicker = null;
        itemGrabMenu.colorPickerToggleButton = null;
        itemGrabMenu.discreteColorPickerCC = null;
        itemGrabMenu.RepositionSideButtons();
    }
}
