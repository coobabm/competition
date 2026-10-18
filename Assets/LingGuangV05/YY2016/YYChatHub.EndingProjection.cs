namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        public bool EndingBoundTo(LingGuangV05.Core.ChapterOneSim save) => object.ReferenceEquals(linked,save);
        // Called after the owner's atomic save commits a prepared YY append; no second message or save is made.
        public void NotifyEndingProjectionSaved() { Changed?.Invoke(); }
    }
}
