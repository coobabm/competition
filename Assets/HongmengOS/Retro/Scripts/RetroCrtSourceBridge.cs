using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Michsky.DreamOS;

namespace HongmengOS.Retro
{
    /// <summary>Scene-local adaptation for dynamically instantiated DreamOS UI.</summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class RetroCrtSourceBridge : MonoBehaviour
    {
        public Canvas sourceCanvas;
        public Camera sourceCamera;
        public RetroCrtInputSurface inputSurface;
        public RetroCrtController controller;
        public UIManager theme;
        public ContextMenuManager contextMenu;
        private readonly List<GraphicRaycaster> raycasters=new List<GraphicRaycaster>(32);
        private readonly HashSet<GraphicRaycaster> cachedRaycasters=new HashSet<GraphicRaycaster>();
        private readonly List<ButtonManager> contextButtons=new List<ButtonManager>(32);
        private bool raycastersInitialized;
        private bool contextWasOpen;
        private Vector2 contextPosition;
        private void Start(){RefreshRaycasters();}
        private void Update()
        {
            // Run before EventSystem.Update: source raycasters must leave its
            // registry before it can query them with physical display coordinates.
            RefreshRaycasters();
            var mouse=Mouse.current;
            if(mouse==null||inputSurface==null||controller==null||!controller.CanInteract)return;
            if((mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame)&&inputSurface.TryMap(mouse.position.ReadValue(),out var sourcePoint))
            { controller.PlayDesktopClick();contextPosition=sourcePoint; }
        }
        private void RefreshRaycasters()
        {
            if(sourceCanvas==null||inputSurface==null)return;
            bool changed=false;
            if(!raycastersInitialized){
                // The initial hierarchy pass also finds serialized, already
                // disabled raycasters, which are absent from RaycasterManager.
                sourceCanvas.GetComponentsInChildren(true,raycasters);
                for(int i=0;i<raycasters.Count;i++){
                    cachedRaycasters.Add(raycasters[i]);
                    PrepareRaycaster(raycasters[i]);
                }
                raycastersInitialized=true;
                changed=true;
            }
            for(int i=raycasters.Count-1;i>=0;i--){
                var raycaster=raycasters[i];
                if(raycaster!=null&&raycaster.transform.IsChildOf(sourceCanvas.transform))continue;
                cachedRaycasters.Remove(raycaster);
                raycasters.RemoveAt(i);
                changed=true;
            }

            // Disabling a raycaster synchronously removes it from this live list.
            // Walk backwards; do not enumerate it or mutate it ourselves.
            var registered=RaycasterManager.GetRaycasters();
            for(int i=registered.Count-1;i>=0;i--){
                var raycaster=registered[i] as GraphicRaycaster;
                if(raycaster==null||!raycaster.transform.IsChildOf(sourceCanvas.transform))continue;
                if(cachedRaycasters.Add(raycaster)){
                    raycasters.Add(raycaster);
                    changed=true;
                }
                PrepareRaycaster(raycaster);
            }

            // The relay keeps querying the disabled components manually. Only
            // membership changes allocate a replacement array, not every frame.
            if(changed)inputSurface.sourceRaycasters=raycasters.ToArray();
        }
        private void PrepareRaycaster(GraphicRaycaster raycaster)
        {
            raycaster.enabled=false;
            var canvas=raycaster.GetComponent<Canvas>();
            if(canvas!=null)canvas.worldCamera=sourceCamera;
        }
        private void LateUpdate()
        {
            if(contextMenu==null||contextMenu.contentRect==null)return;
            if(!contextMenu.isOn){contextWasOpen=false;return;}
            if(contextWasOpen)return;
            contextWasOpen=true;RefreshRaycasters();
            var mouse=Mouse.current;
            if(mouse!=null&&inputSurface.TryMap(mouse.position.ReadValue(),out var p))contextPosition=p;
            var context=contextMenu.contentRect.parent as RectTransform;
            if(context!=null&&RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)sourceCanvas.transform,contextPosition,sourceCamera,out var world))context.position=world;
            var rect=contextMenu.contentRect;
            bool right=contextPosition.x>sourceCamera.pixelWidth*.55f,top=contextPosition.y>sourceCamera.pixelHeight*.5f;
            rect.pivot=new Vector2(right?1:0,top?1:0);rect.anchoredPosition=new Vector2(right?-12:12,top?-12:12);
            foreach(var e in rect.GetComponentsInChildren<UIManagerElement>(true)){
                e.themeManagerAsset=theme;e.keepAlphaValue=true;e.UpdateElement();
            }
            rect.GetComponentsInChildren(true,contextButtons);
            for(int i=0;i<contextButtons.Count;i++){
                var button=contextButtons[i];
                button.useRipple=false;
                button.useHoverEffect=false;
                button.useSounds=false;
                if(button.rippleParent!=null)button.rippleParent.SetActive(false);
                if(button.hoverEffect!=null)button.hoverEffect.gameObject.SetActive(false);
            }
        }
    }
}
