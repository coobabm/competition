using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(Animator))]
    public class WidgetPreset : MonoBehaviour, IEndDragHandler
    {
        // Resources
        public Animator widgetAnimator;

        // Helpers
        float widgetPosX;
        float widgetPosY;
        float cachedAnimatorLength = 0.5f;
        bool isInitialized;
        [HideInInspector] public WidgetManager.DefaultWidgetState defaultState;
        [HideInInspector] public WidgetManager manager;
        [HideInInspector] public int index;
        [HideInInspector] public string ID;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Widgets;

        void Awake()
        {
            // Cache the anim state length
            if (widgetAnimator != null) { cachedAnimatorLength = DreamOSInternalTools.GetAnimatorClipLength(widgetAnimator, "WidgetPreset_In") + 0.1f; }
        }

        void OnEnable()
        {
            if (!isInitialized) { this.enabled = false; isInitialized = true; return; }
            else if (DreamOSDataManager.ContainsJsonKey(dataCat, ID + "_Enabled") && DreamOSDataManager.ReadBooleanData(dataCat, ID + "_Enabled")) { SetEnabled(false); }
            else if (DreamOSDataManager.ContainsJsonKey(dataCat, ID + "_Enabled") && !DreamOSDataManager.ReadBooleanData(dataCat, ID + "_Enabled")) { SetDisabled(false); }
            else if (defaultState == WidgetManager.DefaultWidgetState.Disabled) { SetDisabled(true); }
            else if (defaultState == WidgetManager.DefaultWidgetState.Enabled) { SetEnabled(true); }
        }

        public void SetEnabled(bool updateData = true)
        {
            if (updateData) { DreamOSDataManager.WriteBooleanData(dataCat, ID + "_Enabled", true); }
            manager.widgetItems[index].libraryItem.itemSwitch.SetOn(false);

            if (DreamOSDataManager.ContainsJsonKey(dataCat, ID + "_PosX"))
            {
                widgetPosX = DreamOSDataManager.ReadFloatData(dataCat, ID + "_PosX");
                widgetPosY = DreamOSDataManager.ReadFloatData(dataCat, ID + "_PosY");
                gameObject.transform.localPosition = new Vector3(widgetPosX, widgetPosY, 0);
            }

            gameObject.SetActive(true);
            widgetAnimator.enabled = true;
            widgetAnimator.Play("In");

            StopCoroutine("DisableAnimator");
            StopCoroutine("DisableObject");
            StartCoroutine("DisableAnimator");
        }

        public void SetDisabled(bool updateData = true)
        {
            if (updateData) { DreamOSDataManager.WriteBooleanData(dataCat, ID + "_Enabled", false); }
            manager.widgetItems[index].libraryItem.itemSwitch.SetOff(false);

            if (widgetAnimator.gameObject.activeInHierarchy) 
            {
                widgetAnimator.enabled = true;
                widgetAnimator.Play("Out");

                StopCoroutine("DisableAnimator");
                StopCoroutine("DisableObject");
                StartCoroutine("DisableObject");
            }
        }

        public void AlignToCenter()
        {
            gameObject.transform.localPosition = new Vector3(0, 0, 0);

            widgetPosX = gameObject.transform.localPosition.x;
            widgetPosY = gameObject.transform.localPosition.y;

            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosX", widgetPosX);
            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosY", widgetPosY);
        }

        public void OnEndDrag(PointerEventData data)
        {
            widgetPosX = gameObject.transform.localPosition.x;
            widgetPosY = gameObject.transform.localPosition.y;

            gameObject.transform.localPosition = new Vector3(widgetPosX, widgetPosY, 0);

            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosX", widgetPosX);
            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosY", widgetPosY);
        }

        IEnumerator DisableObject()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);
            gameObject.SetActive(false);
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);
            widgetAnimator.enabled = false;
        }
    }
}