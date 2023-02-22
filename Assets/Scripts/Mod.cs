namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi.Ui.Inspector;
    using Ookii.Dialogs;
    using UnityEngine;

    public class Mod : ModApi.Mods.GameMod
    {
        private Mod() : base() {}
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            base.OnModInitialized();

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if(e.Scene == "Flight")
            {
                Game.Instance.UserInterface.BuildUserInterfaceFromResource<EPFlightUIScript>(
                    "EventPlanner/EventPlannerFlightPanel", 
                    (script, controller) => script.OnLayoutRebubuilt(controller));
            }
        }

    }
}