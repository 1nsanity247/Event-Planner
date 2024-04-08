namespace Assets.Scripts
{
    using HarmonyLib;

    public class Mod : ModApi.Mods.GameMod
    {
        private Mod() : base() { }
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("com.insanity.event-planner");
            harmony.PatchAll();
        }    
    }
}