using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Integrations.ExpandedStorage;
using LeFauxMods.Common.Utilities;
using LeFauxMods.ExpandedStorage.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.ExpandedStorage.Services;

internal static class ModPatches
{
    private static readonly Harmony Harmony = new(Constants.ModId);

    private static ModEntry.TryGetDataDelegate? tryGetData;

    public static void Init(ModEntry.TryGetDataDelegate getDataDelegate)
    {
        tryGetData = getDataDelegate;

        // Place the object as a Chest with Expanded Storage mod data.
        try
        {
            Log.TraceOnce("Applying patches to place objects as Chest with Expanded Storage mod data");

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_placementAction_postfix)));
        }
        catch (Exception)
        {
            Log.WarnOnce("Failed to apply patches to place objects as Chest with Expanded Storage mod data");
            return;
        }

        try
        {
            Log.TraceOnce("Applying patches to customize chest textures");

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
        }
        catch (Exception)
        {
            Log.WarnOnce("Failed to apply patches to customize chest textures");
            return;
        }

        try
        {
            Log.TraceOnce("Applying patches to customize chest sounds");

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
        }
        catch (Exception)
        {
            Log.WarnOnce("Failed to apply patches to customize chest sounds");
            return;
        }

        // Allow mini-shipping bin behavior on other types of chests
        // (e.g. the mini-shipping bin plays its lid opening animation as the player is within range)
        try
        {
            Log.TraceOnce("Applying patches to enable mini-shipping bin behavior on other chest types");

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.updateWhenCurrentLocation)),
                transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Chest_updateWhenCurrentLocation_transpiler)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.fixLidFrame)),
                new HarmonyMethod(typeof(ModPatches), nameof(Chest_fixLidFrame_prefix)));
        }
        catch (Exception)
        {
            Log.WarnOnce("Failed to apply patches to enable mini-shipping bin behavior on other chest types");
            return;
        }

        try
        {
            Log.TraceOnce("Applying patches to enable/disable the color picker if the chest supports player color");

            _ = Harmony.Patch(
                AccessTools.Method(typeof(ItemGrabMenu), nameof(ItemGrabMenu.CanHaveColorPicker)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_CanHaveColorPicker_postfix)));
        }
        catch (Exception)
        {
            Log.WarnOnce(
                "Failed to apply patches to enable/disable the color picker if the chest supports player color");
        }
    }

    private static IEnumerable<CodeInstruction>
        Chest_checkForAction_delegate_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    instruction =>
                        instruction.Calls(AccessTools.PropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)))))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetMiniShippingBin)))
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
        ref bool ____farmerNearby,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha)
    {
        if (!__instance.playerChest.Value || !TryGetData(__instance.ItemId, out var storage))
        {
            return true;
        }

        storage.DrawChest(__instance, spriteBatch, x, y, alpha, false, ___currentLidFrame, ____farmerNearby);
        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Chest_drawLocal_prefix(
        Chest __instance,
        ref int ___currentLidFrame,
        ref bool ____farmerNearby,
        SpriteBatch spriteBatch,
        int x,
        int y,
        float alpha,
        bool local)
    {
        if (!__instance.playerChest.Value || !TryGetData(__instance.ItemId, out var storage))
        {
            return true;
        }

        storage.DrawChest(__instance, spriteBatch, x, y, alpha, local, ___currentLidFrame, ____farmerNearby);
        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Chest_fixLidFrame_prefix(Chest __instance) =>
        !TryGetData(__instance.ItemId, out var storage) || !storage.OpenNearby;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Chest_getLastLidFrame_postfix(Chest __instance, ref int __result)
    {
        if (!__instance.playerChest.Value || !TryGetData(__instance.ItemId, out var storage) || storage.Frames <= 1)
        {
            return;
        }

        if (__instance.ItemId == "6480.StorageVariety_LumberPile" && __instance.GetMutex().IsLockHeld())
        {
            Debugger.Break();
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

    private static IEnumerable<CodeInstruction> Chest_updateWhenCurrentLocation_transpiler(
        IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    instruction =>
                        instruction.Calls(AccessTools.PropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)))))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetMiniShippingBin)))
            .MatchEndForward(new CodeMatch(instruction => instruction.LoadsConstant(Constants.LidCloseSound)))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetSound)))
            .InstructionEnumeration();

    private static Chest.SpecialChestTypes GetMiniShippingBin(
        Chest.SpecialChestTypes specialChestType,
        Item item)
    {
        if (!TryGetData(item.ItemId, out var storage) || !storage.OpenNearby)
        {
            return specialChestType;
        }

        return Chest.SpecialChestTypes.MiniShippingBin;
    }

    private static string GetSound(string sound, Chest chest)
    {
        if (!TryGetData(chest.ItemId, out var storage))
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

    private static void ItemGrabMenu_CanHaveColorPicker_postfix(ItemGrabMenu __instance, ref bool __result)
    {
        if (__instance.sourceItem is not Chest chest || !TryGetData(chest.ItemId, out var storage))
        {
            return;
        }

        __result = storage.PlayerColor;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Object_placementAction_postfix(
        SObject __instance,
        ref bool __result,
        GameLocation location,
        int x,
        int y)
    {
        if (!__result || !TryGetData(__instance.ItemId, out var storage))
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
            fridge = { Value = storage.IsFridge },
            SpecialChestType = storage.SpecialChestType
        };

        if (storage.ModData?.Any() == true)
        {
            foreach (var (key, value) in storage.ModData)
            {
                chest.modData[key] = value;
            }
        }

        chest.resetLidFrame();
        location.Objects[tile] = chest;
        location.playSound(storage.PlaceSound);
        __result = true;
    }

    private static bool TryGetData(string itemId, [NotNullWhen(true)] out StorageData? storageData)
    {
        storageData = null;
        return tryGetData?.Invoke(itemId, out storageData) ?? false;
    }
}
