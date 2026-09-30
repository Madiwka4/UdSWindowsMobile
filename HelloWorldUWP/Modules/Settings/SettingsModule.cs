using System;
using HelloWorldUWP.Core;

namespace HelloWorldUWP.Modules.Settings
{
    public class SettingsModule : AppModuleBase
    {
        public override string Id => "settings";
        public override string Title => LocalizationService.Instance["Nav_Settings"];
        public override string Description => LocalizationService.Instance["Nav_Settings_Desc"];
        public override string IconGlyph => "\uE713"; // Settings gear
        public override Type TargetPageType => typeof(SettingsPage);
        public override int Order => 99;
    }
}
