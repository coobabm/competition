using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A widget on the desktop. It animates in and out, remembers whether it is enabled and where the player dragged
    /// it, and keeps its library switch in step. Replaces DreamOS's WidgetPreset (2026-10-19): same GUID, serialized
    /// fields and public API.
    ///
    /// State is saved in DreamOS's Widgets data as "&lt;ID&gt;_Enabled", "&lt;ID&gt;_PosX" and "&lt;ID&gt;_PosY".
    /// The component starts itself disabled once; <see cref="WidgetManager"/> re-enables it after wiring, and that
    /// second OnEnable restores the saved state (or the default state on the first run).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WidgetPreset : MonoBehaviour, IEndDragHandler
    {
        // Resources
        public Animator widgetAnimator;

        float cachedAnimatorLength = 0.5f;
        bool isInitialized;
        [HideInInspector] public WidgetManager.DefaultWidgetState defaultState;
        [HideInInspector] public WidgetManager manager;
        [HideInInspector] public int index;
        [HideInInspector] public string ID;
        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Widgets;

        Coroutine disableAnimatorRoutine;
        Coroutine disableObjectRoutine;

        void Awake()
        {
            if (widgetAnimator != null) { cachedAnimatorLength = DreamOSInternalTools.GetAnimatorClipLength(widgetAnimator, "WidgetPreset_In") + 0.1f; }
        }

        void OnEnable()
        {
            if (!isInitialized) { this.enabled = false; isInitialized = true; return; }

            string enabledKey = ID + "_Enabled";
            if (DreamOSDataManager.ContainsJsonKey(dataCat, enabledKey))
            {
                if (DreamOSDataManager.ReadBooleanData(dataCat, enabledKey)) { SetEnabled(false); }
                else { SetDisabled(false); }
            }
            else if (defaultState == WidgetManager.DefaultWidgetState.Disabled) { SetDisabled(true); }
            else if (defaultState == WidgetManager.DefaultWidgetState.Enabled) { SetEnabled(true); }
        }

        public void SetEnabled(bool updateData = true)
        {
            if (updateData) { DreamOSDataManager.WriteBooleanData(dataCat, ID + "_Enabled", true); }
            manager.widgetItems[index].libraryItem.itemSwitch.SetOn(false);

            if (DreamOSDataManager.ContainsJsonKey(dataCat, ID + "_PosX"))
            {
                float posX = DreamOSDataManager.ReadFloatData(dataCat, ID + "_PosX");
                float posY = DreamOSDataManager.ReadFloatData(dataCat, ID + "_PosY");
                gameObject.transform.localPosition = new Vector3(posX, posY, 0);
            }

            gameObject.SetActive(true);
            widgetAnimator.enabled = true;
            widgetAnimator.Play("In");

            Stop(ref disableAnimatorRoutine);
            Stop(ref disableObjectRoutine);
            disableAnimatorRoutine = StartCoroutine(DisableAnimator());
        }

        public void SetDisabled(bool updateData = true)
        {
            if (updateData) { DreamOSDataManager.WriteBooleanData(dataCat, ID + "_Enabled", false); }
            manager.widgetItems[index].libraryItem.itemSwitch.SetOff(false);

            // An inactive widget has nothing to animate out
            if (widgetAnimator.gameObject.activeInHierarchy)
            {
                widgetAnimator.enabled = true;
                widgetAnimator.Play("Out");

                Stop(ref disableAnimatorRoutine);
                Stop(ref disableObjectRoutine);
                disableObjectRoutine = StartCoroutine(DisableObject());
            }
        }

        public void AlignToCenter()
        {
            gameObject.transform.localPosition = new Vector3(0, 0, 0);
            SavePosition();
        }

        public void OnEndDrag(PointerEventData data)
        {
            // Drop any z drift picked up while dragging
            Vector3 position = gameObject.transform.localPosition;
            gameObject.transform.localPosition = new Vector3(position.x, position.y, 0);
            SavePosition();
        }

        void SavePosition()
        {
            Vector3 position = gameObject.transform.localPosition;
            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosX", position.x);
            DreamOSDataManager.WriteFloatData(dataCat, ID + "_PosY", position.y);
        }

        void Stop(ref Coroutine routine)
        {
            if (routine != null) { StopCoroutine(routine); }
            routine = null;
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
