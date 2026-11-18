using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;


namespace MatchZy;

// Native grenade projectile factories used by practice rethrows. Each one is resolved on
// first use: a signature that no longer matches gives null (with one console warning) and
// GrenadeThrownData.Throw creates that grenade through the entity API instead, so a CS2
// update can't break the other grenades or throw from a static initializer.
//
// Linux signatures checked against CS2 1.41.9.8 (2026-11-17): each matches once, on the
// function that loads its own "<type>_projectile" class name. The previous smoke signature
// also matched the flashbang factory (first), and the HE and decoy ones matched nothing.
// Same values as upstream MatchZy's gamedata/matchzy.json (2026-11-05).
public static class GrenadeFunctions
{
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    private static readonly HashSet<string> warned = new();

    private static T? Resolve<T>(string name, string linux, string windows, Func<string, T> create) where T : BaseMemoryFunction
    {
        try
        {
            T function = create(IsLinux ? linux : windows);
            if (function.Handle != IntPtr.Zero) return function;
            Warn(name, "signature not found in this CS2 build");
        }
        catch (Exception e)
        {
            Warn(name, e.Message);
        }
        return null;
    }

    private static void Warn(string name, string reason)
    {
        if (!warned.Add(name)) return;
        Console.WriteLine($"[MatchZy] {name} could not be resolved ({reason}). Rethrows of this grenade use the entity API instead.");
    }

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>?> smokeCreate =
        new(() => Resolve("CSmokeGrenadeProjectile_Create",
            "55 4C 89 C1 48 89 E5 41 57 41 56",
            "48 89 5C 24 ? 48 89 6C 24 ? 48 89 74 24 ? 57 41 56 41 57 48 83 EC ? 48 8B B4 24 ? ? ? ?",
            sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>?> heCreate =
        new(() => Resolve("CHEGrenadeProjectile_Create",
            "55 4C 89 C1 48 89 E5 41 57 49 89 FF 41 56 49 89 D6",
            "48 89 5C 24 ? 48 89 6C 24 ? 48 89 74 24 ? 57 48 83 EC ? 48 8B AC 24 ? ? ? ? 49 8B F8",
            sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>?> molotovCreate =
        new(() => Resolve("CMolotovProjectile_Create",
            "55 48 8D 05 ? ? ? ? 48 89 E5 41 57 41 56 41 55 41 54 49 89 FC 53 48 81 EC ? ? ? ? 4C 8D 35 ? ? ? ?",
            "48 8B C4 48 89 58 ? 48 89 70 ? 48 89 78 ? 4C 89 40 ? 55 41 54 41 55 41 56 41 57 48 8D 6C 24 ?",
            sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>(sig)));

    private static readonly Lazy<MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>?> decoyCreate =
        new(() => Resolve("CDecoyProjectile_Create",
            "55 4C 89 C1 48 89 E5 41 57 45 89 CF 41 56 49 89 FE 41 55 49 89 D5 48 89 F2 48 89 FE 41 54 48 8D 3D ? ? ? ? 4D 89 C4 53 48 83 EC ? E8 ? ? ? ? 45 31 C0",
            "48 8B C4 55 56 48 81 EC ? ? ? ? 48 89 58 ? 48 8B D9",
            sig => new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>(sig)));

    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile>? CSmokeGrenadeProjectile_CreateFunc => smokeCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CHEGrenadeProjectile>? CHEGrenadeProjectile_CreateFunc => heCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CMolotovProjectile>? CMolotovProjectile_CreateFunc => molotovCreate.Value;
    public static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, CDecoyProjectile>? CDecoyProjectile_CreateFunc => decoyCreate.Value;
}
