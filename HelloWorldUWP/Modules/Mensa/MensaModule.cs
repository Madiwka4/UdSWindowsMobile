using System;
using HelloWorldUWP.Core;
using HelloWorldUWP.Modules.Mensa.Views;

namespace HelloWorldUWP.Modules.Mensa
{
    public class MensaModule : AppModuleBase
    {
        public override string Id => "mensa";
        public override string Title => LocalizationService.Instance["Nav_Mensa"];
        public override string Description => LocalizationService.Instance["Nav_Mensa_Desc"];
        public override string IconGlyph => "\uE7BE"; // Cafe / Food icon
        public override Type TargetPageType => typeof(MensaPage);
        public override int Order => 1;
    }
}
