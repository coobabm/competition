namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgEndingPreparationHost
    {
        // The profile is another entry into the existing gallery, with the same save and narrative gates.
        public int ProfileMemoryCount => Current ? boundLab.S.endingPreparation?.memories?.Count ?? 0 : 0;
        public bool CanOpenProfileMemories => isActiveAndEnabled && NarrativeAvailable && seedPanel == null && ProfileMemoryCount > 0;

        public void OpenProfileMemories()
        {
            if (!CanOpenProfileMemories) return;
            OpenGallery();
        }
    }
}
