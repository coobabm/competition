#if UNITY_EDITOR
using UnityEngine;
public static class PriorityReleaseUiChecks
{
    public static void Run()
    {
        TitleArchivesQa.Run();
        StartMenuSettingsChecks.Run();
        TaohuoProductLayoutChecks.Run();
        CrowdNoticeChecks.Run();
        ArchiveDesignPreview.Capture();
        Debug.Log("PRIORITY_RELEASE_UI_PASS");
    }
}
#endif
