using System;
using HelloWorldUWP.Core;
using HelloWorldUWP.Modules.Transport.Views;

namespace HelloWorldUWP.Modules.Transport
{
    public class TransportModule : AppModuleBase
    {
        public override string Id => "transport";
        public override string Title => LocalizationService.Instance["Nav_Transport"];
        public override string Description => LocalizationService.Instance["Nav_Transport_Desc"];
        public override string IconGlyph => "\uE787"; // Bus glyph in Segoe MDL2 Assets
        public override Type TargetPageType => typeof(TransportPage);
        public override int Order => 2;
    }
}
