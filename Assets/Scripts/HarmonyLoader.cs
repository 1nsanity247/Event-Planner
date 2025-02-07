using HarmonyLib;

public class HarmonyLoader
{
    public static void LoadHarmony() {
        Harmony harmony = new Harmony("com.insanity.event-planner");
        harmony.PatchAll();
    }
}
