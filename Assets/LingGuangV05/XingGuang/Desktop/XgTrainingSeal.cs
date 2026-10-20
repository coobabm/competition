using System;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    // A module-local overlay. The caller reserves this block's layout and calls RefreshState
    // after simulation/language changes. This does not rearrange the training page.
    public sealed class XgTrainingSeal : MonoBehaviour
    {
        public const float MinimumHeight=84;
        public XgBtn Control { get; private set; }
        public TMP_Text NameLabel { get; private set; }
        public TMP_Text DescriptionLabel { get; private set; }
        public TMP_Text PriceLabel { get; private set; }
        public string NodeId { get; private set; }
        public RectTransform Halo { get; private set; }
        Func<bool> locked,animate;
        Func<string> nameText,descriptionText,priceText;
        static Sprite haloSprite;
        public static XgTrainingSeal Create(RectTransform parent,XgUi kit,string nodeId,
            Func<bool> isLocked,Func<string> name,Func<string> description,Func<string> price,Func<bool> motion,Action openDetails)
        {
            var b=kit.Button(parent,"",null,14);b.rt.name="TrainingSeal "+nodeId;
            b.rt.anchorMin=Vector2.zero;b.rt.anchorMax=Vector2.one;b.rt.offsetMin=b.rt.offsetMax=Vector2.zero;
            var seal=b.rt.gameObject.AddComponent<XgTrainingSeal>();seal.Control=b;seal.NodeId=nodeId;
            seal.locked=isLocked;seal.animate=motion;seal.nameText=name;seal.descriptionText=description;seal.priceText=price;
            b.label.gameObject.SetActive(false);b.button.onClick.AddListener(()=>openDetails?.Invoke());
            b.Visual.gameObject.AddComponent<RectMask2D>();
            // Shared original texture. No blur pass, render texture or per-frame mesh regeneration.
            var halo=Rect("SlowHalo",b.Visual,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-52,-52),new Vector2(52,52));
            seal.Halo=halo;var glow=Panel(halo,Color.white);glow.sprite=HaloSprite();glow.raycastTarget=false;
            var glass=Rect("Glass",b.Visual,Vector2.zero,Vector2.one,new Vector2(2,2),new Vector2(-2,-2));
            var background=Panel(glass,new Color32(13,17,23,242));background.raycastTarget=false;RoundSurface(background,12);
            for(int i=1;i<=5;i++)
            {
                var line=Rect("Grain"+i,glass,new Vector2(0,i/6f),new Vector2(1,i/6f),new Vector2(7,0),new Vector2(-7,1));Panel(line,new Color(1,1,1,.025f)).raycastTarget=false;
            }
            seal.NameLabel=kit.Text(Rect("Name",glass,new Vector2(0,1),Vector2.one,new Vector2(10,-27),new Vector2(-10,-5)),"",14,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
            seal.DescriptionLabel=kit.Text(Rect("Description",glass,new Vector2(0,1),Vector2.one,new Vector2(10,-52),new Vector2(-10,-28)),"",11,XgDark.Muted,TextAlignmentOptions.MidlineLeft);
            seal.PriceLabel=kit.Text(Rect("Price",glass,Vector2.zero,new Vector2(1,0),new Vector2(10,5),new Vector2(-10,27)),"",12,XgDark.Gold,TextAlignmentOptions.MidlineLeft);
            foreach(var label in new[]{seal.NameLabel,seal.DescriptionLabel,seal.PriceLabel}){label.raycastTarget=false;label.enableAutoSizing=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;}
            seal.RefreshState();return seal;
        }
        public static XgTrainingSeal ForModule(RectTransform parent,XgUi kit,XingGuangView view,string part,string nodeId,string explanation,string explanationEn)
        {
            return Create(parent,kit,nodeId,()=>!view.Sim.TrainPart(part),
                ()=>{var node=XgCatalog.Node(nodeId);return GameText.T("封印 · ","Locked · ")+(node==null?nodeId:GameText.T(node.name,node.nameEn));},
                ()=>GameText.T(explanation,explanationEn),
                ()=>{var node=XgCatalog.Node(nodeId);return node==null?GameText.T("科技未找到","Tech unavailable"):"¥"+view.Sim.NodeCost(node).ToString("0",System.Globalization.CultureInfo.InvariantCulture)+GameText.T(" · 查看科技 ›"," · View tech ›");},
                ()=>view.Visible&&view.Tab=="train"&&!view.Juice.Reduced&&!view.Sim.S.reduceFx&&!GamePerformance.Low,
                ()=>{
                    if(view.Sim.TrainPart(part))return;
                    view.Juice.Play(XgJuice.Sfx.Id.Click);view.ShowTab("tree");
                    // ShowTab already explains the opening gate; do not focus a hidden page.
                    if(view.Tab!="tree")return;
                    if(!view.Tree.FocusNodeDetails(nodeId))view.ShowToast(GameText.T("暂时无法找到对应科技。","The matching technology is currently unavailable."),3);
                });
        }
        public void RefreshState()
        {
            bool show=locked!=null&&locked();if(gameObject.activeSelf!=show)gameObject.SetActive(show);if(!show)return;
            Set(NameLabel,nameText?.Invoke());Set(DescriptionLabel,descriptionText?.Invoke());Set(PriceLabel,priceText?.Invoke());
        }
        static void Set(TMP_Text label,string text){text=text??"";if(label.text!=text)label.text=text;}
        void Update(){if(animate!=null&&animate()&&Halo!=null)Halo.Rotate(0,0,-360f/14f*Time.unscaledDeltaTime);}
        static Sprite HaloSprite()
        {
            if(haloSprite!=null)return haloSprite;
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Original training seal halo",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                var p=new Vector2((x+.5f)/n*2-1,(y+.5f)/n*2-1);float ring=Mathf.Clamp01(1-Mathf.Abs(p.magnitude-.72f)/.24f);
                var c=Color.Lerp(new Color(.30f,.84f,.70f),new Color(.65f,.55f,.92f),(Mathf.Atan2(p.y,p.x)/Mathf.PI+1)*.5f);c.a=ring*.22f;pixels[y*n+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);haloSprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),64);haloSprite.name="Original training seal halo";haloSprite.hideFlags=HideFlags.HideAndDontSave;return haloSprite;
        }
    }
}
