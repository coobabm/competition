using LingGuangV05.Desktop.Story;
using LingGuangV05.Core;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XingGuangView
    {
        XgUnifiedAtlas atlas;
        public bool AtlasShowing => atlas != null && atlas.Showing;
        public void OpenAtlas()
        {
            if (!Visible || Sim == null || StoryFocus.Exclusive || SetupShowing ||
                (Tab != "tree" && Tab != "home") || !FeatureOpen(Tab)) return;
            HideProfileMore();
            if (atlas == null) atlas = XgUnifiedAtlas.Create(this, root, ui);
            atlas.Open();
        }
    }
}
