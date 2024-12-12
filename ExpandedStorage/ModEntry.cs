namespace LeFauxMods.ExpandedStorage;

using Common.Utilities;
using Services;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        // Init
        Log.Init(this.Monitor);

        var storageManager = new StorageManager(helper);
        ModPatches.Init(storageManager);
    }
}
