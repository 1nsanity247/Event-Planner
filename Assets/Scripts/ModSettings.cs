namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Common;
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public class ModSettings : SettingsCategory<ModSettings>
    {
        /// <summary>
        /// The mod settings instance.
        /// </summary>
        private static ModSettings _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        public ModSettings() : base("EventPlanner")
        {
        }

        /// <summary>
        /// Gets the mod settings instance.
        /// </summary>
        /// <value>
        /// The mod settings instance.
        /// </value>
        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        public BoolSetting KeepEventsOnRevert { get; private set; }
        
        public enum NothingnessLevel { Nothing, Nothinger, Nothingest };

        ///// <summary>
        ///// Gets the value
        ///// </summary>
        ///// <value>
        ///// The  value.
        ///// </value>
        public EnumSetting<NothingnessLevel> Nothingness { get; private set; }

        /// <summary>
        /// Initializes the settings in the category.
        /// </summary>
        protected override void InitializeSettings()
        {
            KeepEventsOnRevert = CreateBool("Keep Events On Revert")
                .SetDefault(false);

            Nothingness = CreateEnum<NothingnessLevel>("Nothing")
                .SetDescription("This setting does nothing.")
                .SetDefault(NothingnessLevel.Nothing);
        }
    }
}