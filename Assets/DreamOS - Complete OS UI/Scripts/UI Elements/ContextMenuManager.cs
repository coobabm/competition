using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Michsky.DreamOS
{
    public class ContextMenuManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameObject menuPreset;
        public GameObject buttonPreset;
        public GameObject separatorPreset;

        // Settings
        public bool enableBlur = true;
        public bool autoSubMenuPosition = true;
        public CameraSource cameraSource = CameraSource.Main;

        // Bounds
        public CursorBoundHorizontal horizontalBound = CursorBoundHorizontal.Left;
        public CursorBoundVertical verticalBound = CursorBoundVertical.Top;
        [SerializeField] [Range(-50, 50)] private int vBorderTop = -10;
        [SerializeField] [Range(-50, 50)] private int vBorderBottom = 10;
        [SerializeField] [Range(-50, 50)] private int hBorderLeft = 15;
        [SerializeField] [Range(-50, 50)] private int hBorderRight = -15;

        Vector2 uiPos;
        Vector3 cursorPos;
        Vector3 contentPos = new Vector3(0, 0, 0);
        Vector3 contextVelocity = Vector3.zero;

        float cachedStateLength = 0.5f;

        ContextMenu generatedCM;
        RectTransform contextRect;
        Animator contextAnimator;
        UIBlur contextBlur;

        [HideInInspector] public bool isOn;
        [HideInInspector] public RectTransform contentRect;

        public enum CameraSource { Main, Custom }
        public enum CursorBoundHorizontal { Left, Right }
        public enum CursorBoundVertical { Bottom, Top }

        void Awake()
        {
            if (targetCanvas == null) { targetCanvas = gameObject.GetComponentInParent<Canvas>(); }
            if (cameraSource == CameraSource.Main) { targetCamera = Camera.main; }

            GameObject go = Instantiate(menuPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(targetCanvas.transform, false);
            go.transform.SetAsLastSibling();

            generatedCM = go.GetComponent<ContextMenu>();
            generatedCM.manager = this;

            contextAnimator = generatedCM.animator;
            contextRect = generatedCM.mainRect;
            contentRect = generatedCM.contentRect;
            if (enableBlur && generatedCM.blur != null) { contextBlur = generatedCM.blur; }

            contentPos = new Vector3(vBorderTop, hBorderLeft, 0);
            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(contextAnimator, "ContextMenu_In") + 0.1f;

            generatedCM.gameObject.SetActive(false);
        }

        public void CheckForBounds()
        {
            if (uiPos.x <= -100)
            {
                horizontalBound = CursorBoundHorizontal.Left;
                contentPos = new Vector3(hBorderLeft, contentPos.y, 0); contentRect.pivot = new Vector2(0f, contentRect.pivot.y);
            }

            else if (uiPos.x >= 100)
            {
                horizontalBound = CursorBoundHorizontal.Right;
                contentPos = new Vector3(hBorderRight, contentPos.y, 0); contentRect.pivot = new Vector2(1f, contentRect.pivot.y);
            }

            else
            {
                horizontalBound = CursorBoundHorizontal.Left;
                contentPos = new Vector3(hBorderLeft, contentPos.y, 0); contentRect.pivot = new Vector2(0f, contentRect.pivot.y);
            }

            if (uiPos.y <= -75)
            {
                verticalBound = CursorBoundVertical.Bottom;
                contentPos = new Vector3(contentPos.x, vBorderBottom, 0); contentRect.pivot = new Vector2(contentRect.pivot.x, 0f);
            }

            else if (uiPos.y >= 75)
            {
                verticalBound = CursorBoundVertical.Top;
                contentPos = new Vector3(contentPos.x, vBorderTop, 0); contentRect.pivot = new Vector2(contentRect.pivot.x, 1f);
            }

            else
            {
                verticalBound = CursorBoundVertical.Top;
                contentPos = new Vector3(contentPos.x, vBorderTop, 0); contentRect.pivot = new Vector2(contentRect.pivot.x, 1f);
            }
        }

        public void SetContextMenuPosition()
        {
            cursorPos = Mouse.current.position.ReadValue();

            if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                contextRect.position = targetCamera.ScreenToWorldPoint(cursorPos);
                contextRect.localPosition = new Vector3(contextRect.localPosition.x, contextRect.localPosition.y, 0);
                contentRect.transform.localPosition = Vector3.SmoothDamp(contentRect.transform.localPosition, contentPos, ref contextVelocity, 0);
            }

            else if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                contextRect.position = cursorPos;
                contentRect.transform.position = new Vector3(cursorPos.x + contentPos.x, cursorPos.y + contentPos.y, 0);
            }

            uiPos = contextRect.anchoredPosition;
            CheckForBounds();
        }

        public void SetFixedPosition()
        {
            cursorPos = Mouse.current.position.ReadValue();
            SetContextMenuPosition();

            if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                contextRect.position = targetCamera.ScreenToWorldPoint(cursorPos);
                contextRect.localPosition = new Vector3(contextRect.localPosition.x, contextRect.localPosition.y, 0);
                contentRect.transform.localPosition = Vector3.SmoothDamp(contentRect.transform.localPosition, contentPos, ref contextVelocity, 0);
            }

            else if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                contextRect.position = cursorPos;
                contentRect.transform.position = new Vector3(cursorPos.x + contentPos.x, cursorPos.y + contentPos.y, 0);
            }

            uiPos = contextRect.anchoredPosition;
            CheckForBounds();
        }

        void ProcessContextRect()
        {
            if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                contextRect.position = targetCamera.ScreenToWorldPoint(cursorPos);
                contextRect.localPosition = new Vector3(contextRect.localPosition.x, contextRect.localPosition.y, 0);
                contentRect.transform.localPosition = Vector3.SmoothDamp(contentRect.transform.localPosition, contentPos, ref contextVelocity, 0);
            }

            else if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                contextRect.position = cursorPos;
                contentRect.transform.position = new Vector3(cursorPos.x + contentPos.x, cursorPos.y + contentPos.y, 0);
            }
        }

        public void Open() 
        {
            isOn = true;
            contextAnimator.enabled = true;
            generatedCM.gameObject.SetActive(true);

            if (enableBlur && contextBlur != null) { contextBlur.BlurInAnim(); }
            contextAnimator.Play("In");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", false);
        }

        public void Close() 
        {
            isOn = false;
            contextAnimator.enabled = true;

            if (enableBlur && contextBlur != null) { contextBlur.BlurOutAnim(); }
            contextAnimator.Play("Out");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", true);
        }

        public void OpenInFixedPosition()
        {
            SetFixedPosition();
            Open();
        }

        IEnumerator DisableAnimator(bool disableObject)
        {
            yield return new WaitForSeconds(cachedStateLength);

            contextAnimator.enabled = false;
            if (disableObject) { generatedCM.gameObject.SetActive(false); }
        }
    }
}