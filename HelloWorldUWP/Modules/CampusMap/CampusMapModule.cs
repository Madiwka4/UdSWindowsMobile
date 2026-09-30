using System;
using HelloWorldUWP.Core;

namespace HelloWorldUWP.Modules.CampusMap
{
    public class CampusMapModule : AppModuleBase
    {
        public override string Id => "map";
        public override string Title => LocalizationService.Instance["Nav_CampusMap"];
        public override string Description => LocalizationService.Instance["Nav_CampusMap_Desc"];
        public override string IconGlyph => "\uE819"; // MapPin icon
        public override Type TargetPageType => typeof(CampusMapPage);
        public override int Order => 4;
    }
}
