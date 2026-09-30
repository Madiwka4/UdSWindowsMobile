using System;
using HelloWorldUWP.Core;
using HelloWorldUWP.Modules.Card.Views;

namespace HelloWorldUWP.Modules.Card
{
    public class CardModule : AppModuleBase
    {
        public override string Id => "card";
        public override string Title => LocalizationService.Instance["Nav_Card"];
        public override string Description => LocalizationService.Instance["Nav_Card_Desc"];
        public override string IconGlyph => "\uE8C7"; // Contactless Card icon in Segoe MDL2 Assets
        public override Type TargetPageType => typeof(CardPage);
        public override int Order => 3;
    }
}
