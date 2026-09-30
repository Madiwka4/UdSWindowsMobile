using System;
using HelloWorldUWP.Core;

namespace HelloWorldUWP.Modules.News
{
    public class NewsModule : AppModuleBase
    {
        public override string Id => "news";
        public override string Title => LocalizationService.Instance["Nav_News"];
        public override string Description => LocalizationService.Instance["Nav_News_Desc"];
        public override string IconGlyph => "\uE90A"; // ReadingList / News icon
        public override Type TargetPageType => typeof(NewsPage);
        public override int Order => 5;
    }
}
