using System.IO;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;

public static partial class DesignWallpaperSmoke
{
    static int readOnlyStep;
    static string readOnlyBoard;
    static Vector2 readOnlyStart;
    static void TickReadOnly(XingGuangController controller, XgBrainWallpaper brain)
    {
        var concepts = brain.GetComponentInChildren<XgWallpaperConcepts>();
        var bridge = Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        switch (readOnlyStep++)
        {
            case 0:
                controller.enabled = false;
                controller.Window.MinimizeWindow();
                controller.Sim.Board.Train(new XgBoardCard { region = "sequence", truth = true }.Add("中奖"),
                    new XgKnobs { depth = 1, width = 32, lr = .1 });
                readOnlyBoard = JsonUtility.ToJson(controller.Sim.S.board);
                break;
            case 1:
                Check(concepts.Cells.Count > 0, "Read-only view contains actual learned concepts");
                Check(brain.GetComponent<XgBrainWallpaperPresentation>().Showcase, "Read-only input test has an uncovered desktop");
                foreach (var component in brain.GetComponentsInChildren<MonoBehaviour>(true))
                    Check(!(component is IDragHandler) && !(component is IDropHandler), "Wallpaper has no editing drag/drop handler: " + component.GetType().Name);
                Check(concepts.TryLocalPosition(concepts.Cells[0].id, out var point), "Learned concept is located");
                var source = bridge.sourceCamera.WorldToScreenPoint(concepts.transform.TransformPoint(point));
                readOnlyStart = new Vector2(source.x * Screen.width / bridge.sourceCamera.targetTexture.width,
                    source.y * Screen.height / bridge.sourceCamera.targetTexture.height);
                var down = new PointerEventData(EventSystem.current) { position = readOnlyStart, button = PointerEventData.InputButton.Left };
                bridge.inputSurface.OnPointerEnter(down); bridge.inputSurface.OnPointerDown(down);
                break;
            case 2:
                var drag = new PointerEventData(EventSystem.current)
                    { position = readOnlyStart + new Vector2(-160, -110), button = PointerEventData.InputButton.Left };
                bridge.inputSurface.OnBeginDrag(drag); bridge.inputSurface.OnDrag(drag);
                bridge.inputSurface.OnPointerUp(drag); bridge.inputSurface.OnEndDrag(drag);
                Check(JsonUtility.ToJson(controller.Sim.S.board) == readOnlyBoard, "Dragging a concept across regions cannot edit weights or connections");
                brain.GetComponent<XgBrainWallpaperPresentation>().SetQuiet(true);
                break;
            case 3:
                Check(JsonUtility.ToJson(controller.Sim.S.board) == readOnlyBoard, "Quiet only changes presentation");
                brain.GetComponent<XgBrainWallpaperPresentation>().SetQuiet(false);
                var click = new PointerEventData(EventSystem.current) { position = readOnlyStart, button = PointerEventData.InputButton.Left };
                bridge.inputSurface.OnPointerEnter(click); bridge.inputSurface.OnPointerDown(click); bridge.inputSurface.OnPointerUp(click);
                break;
            case 4:
                var card = brain.GetComponent<XgWallpaperRegionCard>();
                Check(card.IsOpen, "A concept region opens its explanatory card");
                Check(JsonUtility.ToJson(controller.Sim.S.board) == readOnlyBoard, "Inspecting a region leaves learned state unchanged");
                controller.StartCoroutine(CaptureRendered("wallpaper-readonly"));
                break;
            case 5:
                File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
        }
    }
}
