namespace Assets.Scripts
{
    using UnityEngine;

    public class Mod : ModApi.Mods.GameMod
    {
        private Mod() : base() { }
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            try {
                HarmonyLoader.LoadHarmony();
            }
            catch (System.Exception e) {
                Game.Instance.UserInterface.CreateMessageDialog("Failed to load Event Planner, make sure you have the Juno Harmony mod installed and enabled before enabling Event Planner!");
                throw e;
            }
        }
    }
}