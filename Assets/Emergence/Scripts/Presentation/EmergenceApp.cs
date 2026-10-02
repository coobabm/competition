using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Emergence.EmergenceUI;

namespace Emergence
{
    [DisallowMultipleComponent]
    public sealed class EmergenceApp : MonoBehaviour
    {
        public EmergenceEngine Engine { get; private set; }
        public bool IsAnimating => busy;
        public string CurrentHint => hint;
        public StimulusResult LastResult { get; private set; }
        public bool HasModal => modalRoot.childCount > 0;
        RectTransform canvasRoot, left, right, bottom, boardRect, hitRoot, modalRoot, floatRoot;
        NeuralBoardGraphic board;
        EmergenceAudio sound;
        Text totalText, chargeText, multText, structureText, statusText, toastText, phaseText, roundText, speedText, mutedText;
        Image progressFill;
        Button primary;
        Text primaryText;
        int selected=-1, candidate, itemIndex=-1, linkSource=-1, bindingSlot=-1;
        bool moving, busy, paused, skip, suppressSave;
        float speed=1, toastTime;
        string hint="选择一枚神经元，再把它放入网络。";
        double shownCharge,shownMult=1;
        int animationBaseScore;
        Text cumulativeText, progressText, hintText;
        readonly Dictionary<int,Text> nodeLabels=new Dictionary<int,Text>();
        readonly List<GameObject> transientLabels=new List<GameObject>();
        readonly List<string> recentLog=new List<string>();
        readonly Dictionary<int,Image> slotImages=new Dictionary<int,Image>();
        string SavePath => Path.Combine(Application.persistentDataPath,"emergence-run-v1.json");
        const float BoardX=328, BoardY=231, BoardW=862, BoardH=568;

        void Awake()
        {
            Application.runInBackground=true; Application.targetFrameRate=60;
            EmergenceUI.Font=Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if(EmergenceUI.Font==null)EmergenceUI.Font=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"Hiragino Sans GB","Arial"},24);
            sound=gameObject.AddComponent<EmergenceAudio>(); sound.Muted=PlayerPrefs.GetInt("Emergence.Muted",0)==1;
            Engine=LoadRun()??new EmergenceEngine(20261001);
            BuildShell();
            hint=Engine.State.phase==RunPhase.Grow?"选择一枚神经元，再把它放入网络。":Engine.State.phase==RunPhase.Shop?"本轮已完成。进入集市，或继续生长。":"进度已恢复。选择起点继续这一轮。";
            Refresh();if(Engine.State.phase==RunPhase.Shop)ShowShop();if(Engine.State.phase==RunPhase.Victory||Engine.State.phase==RunPhase.Defeat)ShowEnd();
        }
        EmergenceEngine LoadRun()
        {
            try { if(File.Exists(SavePath)) { var saved=JsonUtility.FromJson<RunState>(File.ReadAllText(SavePath)); if(saved!=null&&saved.nodes!=null&&saved.nodes.Count>0)return new EmergenceEngine(saved); } }
            catch(Exception ex) { Debug.LogWarning("Emergence save could not be loaded: "+ex.Message); }
            try{if(File.Exists(SavePath+".bak")){var backup=JsonUtility.FromJson<RunState>(File.ReadAllText(SavePath+".bak"));if(backup!=null&&backup.nodes!=null&&backup.nodes.Count>0)return new EmergenceEngine(backup);}}catch(Exception ex){Debug.LogWarning("Emergence backup: "+ex.Message);}
            return null;
        }
        public void SaveRun()
        {
            if(suppressSave||Engine==null)return;
            try { Directory.CreateDirectory(Application.persistentDataPath); string tmp=SavePath+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(Engine.State,true));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath); }
            catch(Exception ex) { Debug.LogWarning("Emergence save: "+ex.Message); }
        }
        void OnApplicationQuit(){if(!busy)SaveRun();}
        void OnDestroy(){if(!busy)SaveRun();}
        void BuildShell()
        {
            var cg=new GameObject("Emergence · Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));cg.transform.SetParent(transform,false);
            var c=cg.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=10;
            var scaler=cg.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var outer=(RectTransform)cg.transform;
            var backdrop=Fill(outer,"Backdrop");backdrop.gameObject.AddComponent<Image>().color=Ink;
            canvasRoot=Box(outer,"Design frame",0,0,1600,1000);canvasRoot.anchorMin=canvasRoot.anchorMax=new Vector2(.5f,.5f);canvasRoot.pivot=new Vector2(.5f,.5f);canvasRoot.anchoredPosition=Vector2.zero;
            var existing=FindAnyObjectByType<EventSystem>();
            if(existing==null){var e=new GameObject("Emergence · Input",typeof(EventSystem),typeof(InputSystemUIInputModule));e.transform.SetParent(transform,false);e.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            var background=Fill(canvasRoot,"Background");background.gameObject.AddComponent<Image>().color=Ink;
            Surface(canvasRoot,"Top divider",40,108,1520,1,Line,false);
            Surface(canvasRoot,"Brand accent",40,34,4,50,Teal,false);
            Label(canvasRoot,"涌 现",59,21,194,57,40,Foreground);
            Label(canvasRoot,"E M E R G E N C E",61,76,235,21,12,Muted);
            roundText=Label(canvasRoot,"",332,34,500,31,20,Foreground);
            phaseText=Label(canvasRoot,"",334,73,590,24,13,Muted);
            Button(canvasRoot,"Help","玩法说明",1162,38,126,42,()=>ShowHelp(),null,false,16);
            var sb=Button(canvasRoot,"Speed","1×",1300,38,73,42,()=>{speed=speed==1?2:speed==2?4:1;speedText.text=speed+"×";sound.PlayUI();},null,false,16);speedText=sb.GetComponentInChildren<Text>();
            var mb=Button(canvasRoot,"Mute",sound.Muted?"静音":"声音",1385,38,73,42,ToggleSound,null,false,16);mutedText=mb.GetComponentInChildren<Text>();
            Button(canvasRoot,"Pause","Ⅱ",1470,38,90,42,ShowPause,null,false,18);
            left=Box(canvasRoot,"Run status",40,132,266,651);
            right=Box(canvasRoot,"Decision panel",1212,132,348,648);
            bottom=Box(canvasRoot,"Synapses",328,818,862,151);
            Surface(canvasRoot,"Score band",328,132,862,89,Panel);
            Label(canvasRoot,"灵感",352,150,80,20,13,Muted);chargeText=Label(canvasRoot,"0",350,172,170,40,29,Foreground);
            Label(canvasRoot,"×",529,166,36,38,26,Muted,TextAnchor.MiddleCenter);
            Label(canvasRoot,"倍率",589,150,80,20,13,Muted);multText=Label(canvasRoot,"1",587,172,166,40,29,Teal);
            Surface(canvasRoot,"Score separator",775,152,1,48,Line,false);
            Label(canvasRoot,"本次涌现",801,150,130,20,13,Muted);totalText=Label(canvasRoot,"0",800,171,360,41,31,Gold);
            boardRect=Box(canvasRoot,"Neural field",BoardX,BoardY,BoardW,BoardH);boardRect.pivot=new Vector2(.5f,.5f);boardRect.anchoredPosition=new Vector2(BoardX+BoardW*.5f,-BoardY-BoardH*.5f);
            board=boardRect.gameObject.AddComponent<NeuralBoardGraphic>();board.raycastTarget=false;
            hitRoot=Box(canvasRoot,"Neural input",BoardX,BoardY,BoardW,BoardH);
            structureText=Label(canvasRoot,"潜能未定 · 等待第一次放电",348,779,820,22,13,Muted,TextAnchor.MiddleCenter);
            Surface(canvasRoot,"Lower divider",40,805,1520,1,Line,false);
            Label(canvasRoot,"构筑记录",40,834,246,24,14,Muted);
            statusText=Label(canvasRoot,"",40,870,266,99,16,Foreground);
            primary=Button(canvasRoot,"Primary action","",1212,887,348,71,Primary,Teal,true,21);primaryText=primary.GetComponentInChildren<Text>();
            Label(canvasRoot,"点击选择 · 空格放电 · Esc 暂停",1212,969,348,22,12,Muted,TextAnchor.MiddleCenter);
            hintText=Label(canvasRoot,"",1212,818,348,57,14,Muted);hintText.name="Context hint";
            floatRoot=Fill(canvasRoot,"Score particles");
            var toastPanel=Surface(canvasRoot,"Toast",428,105,684,37,Hex("183C42"));toastPanel.gameObject.SetActive(false);
            toastText=Label(toastPanel.transform,"",12,0,660,37,15,Teal,TextAnchor.MiddleCenter);
            modalRoot=Fill(canvasRoot,"Dialogs");
        }
        public void Refresh()
        {
            var s=Engine.State;
            roundText.text=$"意识层 {((s.round-1)/3+1):00}   /   回合 {s.round:00} — 18";
            phaseText.text=s.phase==RunPhase.Grow?"01  生长     →     02  刺激     →     03  重塑":s.phase==RunPhase.Shop?"本轮记忆已形成     /     在下一次涌现前，重塑你的网络":"每一条连接，都可能改变下一次涌现。";
            BuildLeft();BuildRight();BuildSynapses();if(!busy)BuildBoard();UpdatePrimary();
            statusText.text=recentLog.Count>0?string.Join("\n",recentLog.Take(3)):"从一枚神经元开始。\n让微弱的信号彼此相遇。";
            if(!busy) { chargeText.text=Number(shownCharge);multText.text=shownMult.ToString("0.##");totalText.text=Number(shownCharge*shownMult); }
        }
        void BuildLeft()
        {
            Clear(left);var s=Engine.State;
            var objective=Surface(left,"Objective",0,0,266,254,Panel);
            Label(left,s.round%3==0?"B O S S   /   意识阻抗":"T A R G E T   /   本轮目标",20,19,231,25,12,s.round%3==0?Red:Muted);
            Label(left,Number(s.target),19,51,231,60,43,Foreground);
            Label(left,"已收集灵感",21,125,220,23,13,Muted);
            cumulativeText=Label(left,Number(busy?animationBaseScore:s.roundScore),19,153,230,38,25,Teal);
            Surface(left,"Progress track",20,207,226,5,Hex("25404A"));
            progressFill=Surface(left,"Progress fill",20,207,226*Mathf.Clamp01((float)((double)s.roundScore/Math.Max(1,s.target))),5,Teal);
            progressText=Label(left,$"{Mathf.RoundToInt(Mathf.Min(9.99f,(float)((double)(busy?animationBaseScore:s.roundScore)/Math.Max(1,s.target)))*100)}%  /  达到目标即可进入下一轮",20,223,230,21,11,Muted);
            Surface(left,"Resources",0,269,266,101,Panel);
            Label(left,"刺激机会",19,284,114,24,13,Muted);Label(left,"资源",151,284,90,24,13,Muted);
            Label(left,new string('◆',Math.Max(0,s.clicksRemaining))+new string('◇',Math.Max(0,2-s.clicksRemaining)),19,319,118,34,22,Teal);
            Label(left,"◈ "+s.currency,150,314,105,40,25,Gold);
            Surface(left,"Boss preview",0,386,266,129,Hex("1B2530"));
            Label(left,s.round%3==0?"当前阻抗  /  "+s.bossName:"本层 BOSS  /  "+s.bossName,18,402,230,28,15,s.round%3==0?Red:Violet);
            Label(left,s.bossDescription,18,440,230,64,14,Muted);
            Label(left,"网络  "+s.nodes.Count+" / 61",0,537,150,27,14,Muted);
            Label(left,"种子  "+s.seed,0,571,255,25,12,Muted);
            Label(left,s.phase==RunPhase.Grow?"生长提示\n相邻节点自动连接。敏感节点只需 1 点电位，适合延长连锁。":s.phase==RunPhase.Shop?"重塑提示\n围绕你的连接购买突触。高倍率需要网络提供触发条件。":"连接提示\n先选择一个起点，亮线会预演前两拍。休眠节点本轮不能再次放电。",0,596,266,100,13,Muted);
        }
        void BuildRight()
        {
            Clear(right);var s=Engine.State;
            if(s.phase==RunPhase.Grow)
            {
                Label(right,"为网络增添一个念头",0,0,348,36,22,Foreground);
                Label(right,"三选一  /  随后点击棋盘空位",0,45,348,29,14,Muted);
                for(int i=0;i<s.candidates.Count;i++)
                {
                    int idx=i;var n=s.candidates[i];float y=94+i*130;
                    var b=Button(right,"Candidate "+i,"",0,y,348,116,()=>{candidate=idx;selected=-1;hint="点击棋盘上的空位，植入「"+Catalog.NodeName(n.kind)+"」。";sound.PlayUI();Refresh();},NodeColor(n.kind));
                    b.image.color=idx==candidate?Hex("173B3D"):Panel;
                    Surface(b.transform,"Selected",0,15,3,86,idx==candidate?Teal:Line,false);
                    Label(b.transform,Catalog.NodeName(n.kind),19,14,203,29,20,NodeColor(n.kind));
                    Label(b.transform,n.variant==NodeVariant.None?"自然":Catalog.VariantName(n.variant),241,19,90,25,13,n.variant==NodeVariant.None?Muted:Gold,TextAnchor.UpperRight);
                    Label(b.transform,Catalog.NodeDescription(n.kind),20,53,305,52,14,Muted);
                }
                Label(right,"每回合生长一次。放置后可使用一次免费移动调整布局。",0,506,348,78,15,Muted);
            }
            else
            {
                Label(right,"神经观察",0,0,348,36,22,Foreground);
                Label(right,busy?"信号正在寻找它的路径…":"选择节点，观察这次涌现的可能。",0,45,348,45,14,Muted);
                var n=Engine.FindNode(selected);
                Surface(right,"Neuron inspection",0,96,348,221,Panel);
                if(itemIndex>=0&&itemIndex<s.inventory.Count)
                {
                    var effect=Catalog.Get(s.inventory[itemIndex].id);Label(right,effect.name,20,113,304,35,24,Violet);Label(right,effect.description,20,167,304,93,17,Foreground);Label(right,linkSource<0?"选择第一个目标神经元":"起点 #"+linkSource+" 已选 · 再选择目标",20,275,304,28,14,Teal);
                }
                else if(n!=null)
                {
                    Label(right,Catalog.NodeName(n.kind)+"  ·  "+n.id.ToString("00"),20,113,270,34,23,NodeColor(n.kind));
                    Label(right,(n.variant==NodeVariant.None?"自然节点":Catalog.VariantName(n.variant))+"   /   "+(n.fired?"本轮休眠":"电位 "+n.charge+" / "+n.Threshold),20,158,301,30,15,Muted);
                    Label(right,Catalog.NodeDescription(n.kind),20,201,304,55,15,Foreground);
                    Label(right,"基础灵感  "+n.BaseScore+"     出线  "+Engine.GetEdges().Count(e=>e.sourceId==n.id&&!e.blocked),20,275,300,25,14,Teal);
                }
                else if(LastResult!=null)
                {
                    Label(right,LastResult.structureName+"  Lv."+LastResult.structureLevel,20,113,304,38,25,Gold);Label(right,Number(LastResult.score)+" 灵感",20,167,304,46,28,Teal);Label(right,"最长 "+LastResult.longestLayer+" 层 · 同拍最多 "+LastResult.maxWave+" 枚\n"+LastResult.firedIds.Count+" 次放电 · "+LastResult.rescores+" 次回响",20,235,304,67,15,Muted);
                }
                else
                {
                    Label(right,"一个起点，无数可能。",20,124,305,38,21,Teal);
                    Label(right,"点击神经元查看电位、连接与前两拍预演。\n准备好后，释放一次脉冲。",20,178,305,110,16,Muted);
                }
                var move=Button(right,"Move","移动节点  ·  "+s.movesRemaining+" 次",0,334,169,44,()=>{moving=!moving;itemIndex=bindingSlot=-1;linkSource=selected;hint=moving?"选择要移动的节点，再点击空位。":"选择起点。";Refresh();},Muted,false,14);
                move.interactable=!busy&&s.phase==RunPhase.Prepare&&s.clicksRemaining==2&&s.movesRemaining>0;
                var cancel=Button(right,"Cancel edit","取消选择",179,334,169,44,()=>{CancelTool();Refresh();},Muted,false,14);cancel.interactable=!busy;
                Label(right,"随身物品",0,406,160,29,16,Foreground);
                Label(right,"记忆 / 药品 / 星图",142,408,206,22,12,Muted,TextAnchor.UpperRight);
                if(s.inventory.Count==0)Label(right,"暂无物品 · 通关后在商店获得",0,455,348,60,15,Muted);
                for(int i=0;i<Math.Min(2,s.inventory.Count);i++)
                {
                    int idx=i;var def=Catalog.Get(s.inventory[i].id);float y=445+i*72;
                    var b=Button(right,"Inventory "+i,"",0,y,348,64,()=>SelectItem(idx),Violet);
                    Label(b.transform,def.name,15,7,204,28,16,def.category==ContentCategory.Drug?Teal:Violet);
                    Label(b.transform,CategoryName(def.category),15,35,309,22,11,Muted);
                    Label(b.transform,"使用",275,10,52,26,13,Violet,TextAnchor.MiddleRight);
                    b.interactable=!busy&&(s.phase==RunPhase.Prepare||s.phase==RunPhase.Shop);
                }
            }
            if(s.inventory.Count>0 && s.phase!=RunPhase.Grow)Button(right,"Manage inventory","查看背包  ·  "+s.inventory.Count+" 件",0,601,348,43,ShowInventory,Muted,false,14);
        }
        void BuildSynapses()
        {
            Clear(bottom);slotImages.Clear();var s=Engine.State;
            Label(bottom,"突 触",0,0,90,21,12,Muted);Label(bottom,"拖动调整顺序 · 点击查看",484,0,378,21,12,Muted,TextAnchor.UpperRight);
            for(int i=0;i<5;i++)
            {
                int idx=i;float x=i*175;bool filled=i<s.synapses.Count;
                var b=Button(bottom,"Synapse "+i,"",x,31,162,111,()=>{if(filled)ShowSynapse(idx);},Teal);slotImages[i]=b.image;
                if(filled)
                {
                    var d=Catalog.Get(s.synapses[i].id);bool silent=s.round%3==0&&s.bossName=="静默"&&i==0;
                    Surface(b.transform,"Accent",12,13,25,2,silent?Muted:d.rare?Gold:Teal,false);
                    Label(b.transform,(i+1).ToString("00"),116,10,34,22,12,Muted,TextAnchor.MiddleRight);
                    Label(b.transform,d.name,12,29,138,30,19,silent?Muted:d.rare?Gold:Teal);
                    Label(b.transform,silent?"本轮被静默":d.description,12,65,138,38,11,Muted);
                    var drag=b.gameObject.AddComponent<SynapseDrag>();drag.app=this;drag.slot=i;
                }
                else
                {
                    b.image.color=Hex("0B1C25");Label(b.transform,"＋",0,16,162,42,28,Line,TextAnchor.MiddleCenter);
                    Label(b.transform,"待形成的突触",0,70,162,22,12,Muted,TextAnchor.MiddleCenter);
                }
                b.interactable=filled&&!busy;
            }
        }
        void BuildBoard()
        {
            board.nodes.Clear();board.edges.Clear();board.previewIds.Clear();nodeLabels.Clear();Clear(hitRoot);
            foreach(var n in Engine.State.nodes)
                board.nodes.Add(new BoardNodeVisual{id=n.id,q=n.q,r=n.r,kind=(int)n.kind,variant=(int)n.variant,charge=n.charge,threshold=n.Threshold,fired=n.fired});
            foreach(var e in Engine.GetEdges())board.edges.Add(new BoardEdgeVisual{source=e.sourceId,target=e.targetId,delay=e.delay,echo=e.echo,blocked=e.blocked,special=e.origin!="天然"});
            for(int q=-4;q<=4;q++)for(int r=-4;r<=4;r++)
            {
                if(!EmergenceEngine.IsCell(q,r))continue;int cq=q,cr=r;
                Vector2 p=board.CellPosition(q,r);float x=BoardW*.5f+p.x-24,y=BoardH*.5f-p.y-24;
                var cell=Box(hitRoot,"Cell "+q+","+r,x,y,48,48);var img=cell.gameObject.AddComponent<Image>();img.color=Color.clear;
                var b=cell.gameObject.AddComponent<Button>();b.targetGraphic=img;b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>ClickCell(cq,cr));
                var n=Engine.State.nodes.Find(v=>v.q==cq&&v.r==cr);
                var hover=cell.gameObject.AddComponent<HoverTarget>();hover.entered=()=>HoverCell(cq,cr);hover.exited=()=>{ClearGhost();board.hoveredId=-1;if(!busy)UpdatePreview(selected);};
                if(n!=null)
                {var t=Label(cell,ShortKind(n.kind),0,-2,48,32,13,n.fired?Hex("59777E"):NodeColor(n.kind),TextAnchor.MiddleCenter);nodeLabels[n.id]=t;if(Engine.State.round%3==0&&Engine.State.bossName=="双生"&&(n.id==Engine.State.twinA||n.id==Engine.State.twinB))Label(cell,"双生",-4,40,56,18,9,Gold,TextAnchor.MiddleCenter);}
            }
            board.membrane=Engine.State.round%3==0&&Engine.State.bossName=="隔膜";
            board.selectedId=selected;board.showEmptyCells=Engine.State.phase==RunPhase.Grow||moving;UpdatePreview(selected);board.SetVerticesDirty();
        }
        void HoverCell(int q,int r)
        {
            if(busy||HasModal)return;ClearGhost();var n=Engine.State.nodes.Find(x=>x.q==q&&x.r==r);board.hoveredId=n==null?-1:n.id;
            if(n==null&&Engine.State.phase==RunPhase.Grow){var c=Engine.State.candidates[candidate];board.nodes.Add(new BoardNodeVisual{id=-100,q=q,r=r,kind=(int)c.kind,variant=(int)c.variant,threshold=c.kind==NodeKind.Sensitive?1:c.kind==NodeKind.Capacitor?3:2});foreach(var other in Engine.State.nodes){int distance=EmergenceEngine.Distance(q,r,other.q,other.r);if(distance<=(c.kind==NodeKind.Projector?2:1))board.edges.Add(new BoardEdgeVisual{source=-100,target=other.id,delay=1,special=true});if(distance<=(other.kind==NodeKind.Projector?2:1))board.edges.Add(new BoardEdgeVisual{source=other.id,target=-100,delay=1,special=true});}board.selectedId=-100;board.SetVerticesDirty();}
            if(n!=null&&Engine.State.phase==RunPhase.Prepare&&!moving&&itemIndex<0&&bindingSlot<0)UpdatePreview(n.id);
        }
        void ClearGhost(){board.nodes.RemoveAll(n=>n.id==-100);board.edges.RemoveAll(e=>e.source==-100||e.target==-100);board.selectedId=selected;board.SetVerticesDirty();}
        void UpdatePreview(int id)
        {
            if(busy)return;board.previewIds.Clear();
            var n=Engine.FindNode(id);if(n!=null&&!n.fired&&Engine.State.phase==RunPhase.Prepare&&itemIndex<0&&!moving)
            {var p=Engine.Preview(id);if(p!=null&&p.success)foreach(var fid in p.firedIds)board.previewIds.Add(fid);}
            board.SetVerticesDirty();
        }
        public void ClickCell(int q,int r)
        {
            if(busy||HasModal)return;var s=Engine.State;var n=s.nodes.Find(x=>x.q==q&&x.r==r);sound.PlayUI();
            if(s.phase==RunPhase.Grow)
            {
                if(n!=null){selected=n.id;Toast("选择一处空位来生长。");Refresh();return;}
                if(Engine.PlaceCandidate(candidate,q,r)){selected=s.nodes.Last().id;hint="神经元已植入。选择起点，观察连锁。";Log("生长 · "+Catalog.NodeName(s.nodes.Last().kind));SaveRun();Refresh();}else Toast(Engine.LastError);return;
            }
            if(s.phase!=RunPhase.Prepare && !(s.phase==RunPhase.Shop&&(itemIndex>=0||bindingSlot>=0)))return;
            if(moving)
            {
                if(n!=null){linkSource=selected=n.id;hint="点击一处空位，迁移这个神经元。";Refresh();return;}
                if(linkSource<0){Toast("先选择要移动的神经元。");return;}
                if(Engine.MoveNode(linkSource,q,r)){CancelTool();Log("迁移 · 网络连接已更新");SaveRun();Refresh();}else Toast(Engine.LastError);return;
            }
            if(n==null)return;
            selected=n.id;
            if(itemIndex>=0||bindingSlot>=0)
            {
                if(itemIndex>=s.inventory.Count||bindingSlot>=s.synapses.Count){CancelTool();Toast("背包已变化，请重新选择物品。");Refresh();return;}
                int count=bindingSlot>=0?2:Catalog.Get(s.inventory[itemIndex].id).targetCount;
                if(count>1&&linkSource<0){linkSource=n.id;hint="已选起点，再选择目标神经元。";Refresh();return;}
                bool ok=bindingSlot>=0?Engine.BindSynapse(bindingSlot,linkSource,n.id):Engine.ApplyItem(itemIndex,count>1?linkSource:n.id,count>1?n.id:-1);
                if(ok){Log("重塑 · 连接已改变");CancelTool();SaveRun();Refresh();}else{Toast(Engine.LastError);Refresh();}return;
            }
            hint=n.fired?"这个神经元已休眠，将在下一轮苏醒。":"已预演前两拍。准备好后释放脉冲。";Refresh();
        }
        void SelectItem(int index)
        {
            var s=Engine.State;if(busy||index<0||index>=s.inventory.Count)return;CancelTool();
            var d=Catalog.Get(s.inventory[index].id);
            if(d.targetCount==0){if(Engine.ApplyItem(index,-1)){Log("升级 · "+d.name);SaveRun();Refresh();}else Toast(Engine.LastError);return;}
            itemIndex=index;bindingSlot=-1;moving=false;linkSource=-1;
            hint="使用「"+d.name+"」："+(d.targetCount>1?"依次选择两个神经元。":"选择一个神经元。");Toast(hint);Refresh();
        }
        void CancelTool(){itemIndex=bindingSlot=linkSource=-1;moving=false;hint="选择起点，观察前两拍，再释放脉冲。";}
        void UpdatePrimary()
        {
            var s=Engine.State;
            primary.interactable=true;
            if(busy){primaryText.text=paused?"继续播放":"立即结算  →";}
            else if(s.phase==RunPhase.Grow){primaryText.text="选择空位，植入神经元";primary.interactable=false;}
            else if(s.phase==RunPhase.Shop){primaryText.text=(itemIndex>=0||bindingSlot>=0)?"取消改造":"进入神经集市  →";}
            else if(s.phase==RunPhase.Victory){primaryText.text="涌现完成  ·  查看记录";}
            else if(s.phase==RunPhase.Defeat){primaryText.text="查看复盘  →";}
            else if(s.roundScore>=s.target){primaryText.text="达成目标  ·  收集奖励  →";primary.image.color=Gold;}
            else if(itemIndex>=0||bindingSlot>=0||moving){primaryText.text="取消改造";primary.image.color=Violet;}
            else{primaryText.text=s.clicksRemaining==2?"释放第一次脉冲  →":"释放第二次脉冲  →";var n=Engine.FindNode(selected);primary.interactable=n!=null&&!n.fired;primary.image.color=Teal;}
            hintText.text=busy?"连锁结算中 · 可随时加速":hint;
        }
        public void Primary()
        {
            if(busy){if(paused){paused=false;CloseModal();}else skip=true;return;}
            var s=Engine.State;
            if(s.phase==RunPhase.Shop){if(itemIndex>=0||bindingSlot>=0){CancelTool();Refresh();}else ShowShop();return;}
            if(s.phase==RunPhase.Victory||s.phase==RunPhase.Defeat){ShowEnd();return;}
            if(s.phase!=RunPhase.Prepare)return;
            if(s.roundScore>=s.target){if(Engine.EndRound()){Log("达标 · 获得 "+Engine.State.lastReward+" 资源");SaveRun();Refresh();if(Engine.State.phase==RunPhase.Victory)ShowEnd();else ShowShop();}else Toast(Engine.LastError);return;}
            if(itemIndex>=0||bindingSlot>=0||moving){CancelTool();Refresh();return;}
            BeginStimulus(selected);
        }
        public bool BeginStimulus(int nodeId)
        {
            if(busy||Engine.State.phase!=RunPhase.Prepare)return false;
            animationBaseScore=Engine.State.roundScore;
            var result=Engine.Stimulate(nodeId);if(result==null||!result.success){Toast(result?.error??Engine.LastError);return false;}
            LastResult=result;busy=true;skip=false;paused=false;board.previewIds.Clear();CancelTool();StartCoroutine(Playback(result));return true;
        }
        IEnumerator Playback(StimulusResult result)
        {
            shownCharge=0;shownMult=1;structureText.text="脉冲释放  /  连锁正在涌现";Refresh();
            int previous=-1;
            foreach(var e in result.events)
            {
                if(e.tick!=previous)
                {
                    if(previous>=0){float delay=.33f;while(delay>0&&!skip){if(!paused)delay-=Time.unscaledDeltaTime*speed;yield return null;}}
                    previous=e.tick;
                }
                while(paused)yield return null;
                shownCharge=e.charge;shownMult=Math.Max(1,e.multiplier);
                chargeText.text=Number(shownCharge);multText.text=shownMult.ToString("0.##");totalText.text=Number(shownCharge*shownMult);
                if(cumulativeText!=null)cumulativeText.text=Number(animationBaseScore+shownCharge*shownMult);
                if(progressText!=null)progressText.text=Mathf.RoundToInt(Mathf.Min(9.99f,(float)((animationBaseScore+shownCharge*shownMult)/Math.Max(1,Engine.State.target)))*100)+"%  /  达到目标即可进入下一轮";
                if(progressFill!=null)progressFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,226*Mathf.Clamp01((float)((animationBaseScore+shownCharge*shownMult)/Math.Max(1,Engine.State.target))));
                if(e.kind==EventKind.Multiplier)totalText.rectTransform.localScale=Vector3.one*1.06f;
                if(!skip&&e.kind==EventKind.Signal){board.PulseEdge(e.sourceId,e.nodeId);var nv=board.nodes.Find(n=>n.id==e.nodeId);if(nv!=null&&!nv.fired)nv.charge=Math.Max(0,nv.charge+(int)e.value);}
                if(!skip&&(e.kind==EventKind.Fire||e.kind==EventKind.Rescore))
                {
                    board.PulseNode(e.nodeId,e.kind==EventKind.Rescore?1.6f:1);
                    var nv=board.nodes.Find(n=>n.id==e.nodeId);if(nv!=null)nv.fired=true;
                    sound.PlayNode(e.tick,e.kind==EventKind.Rescore);
                    if(e.kind==EventKind.Rescore)FloatAtNode(e.nodeId,"回响",Violet);
                }
                if(!skip&&e.kind==EventKind.Charge && e.nodeId>=0 && e.value>0)FloatAtNode(e.nodeId,"+"+Number(e.value),Teal);
                if(!skip&&e.synapseIndex>=0&&e.value!=0&&slotImages.TryGetValue(e.synapseIndex,out var flashed))StartCoroutine(FlashSlot(flashed));
                if(!skip&&e.kind==EventKind.Multiplier&&e.value>0)sound.PlayMultiplier();
                if(!skip)yield return null;
            }
            shownCharge=result.charge;shownMult=result.multiplier;busy=false;paused=false;selected=-1;
            structureText.text=result.structureName+"  Lv."+result.structureLevel+"   ·   "+result.firedIds.Count+" 次放电 / "+result.rescores+" 次回响";
            Log(result.structureName+" · +"+Number(result.score)+" 灵感");
            if(Engine.State.roundScore>=Engine.State.target){hint="目标达成！收集奖励，继续重塑网络。";sound.PlayWin();Toast("涌现成功  +"+Number(result.score));}
            else{hint="还差 "+Number(Engine.State.target-Engine.State.roundScore)+" 灵感。剩余电位已保留。";}
            SaveRun();Refresh();
            if(Engine.State.phase==RunPhase.Defeat){sound.PlayLose();ShowEnd();}
        }
        IEnumerator FlashSlot(Image slot)
        {if(slot==null)yield break;Color old=slot.color;slot.color=Hex("456651");float t=.4f;while(t>0){t-=Time.unscaledDeltaTime;yield return null;}if(slot!=null)slot.color=old;}
        void FloatAtNode(int id,string value,Color color)
        {
            var n=Engine.FindNode(id);if(n==null||skip)return;var p=board.CellPosition(n.q,n.r);
            var label=Label(floatRoot,value,BoardX+BoardW*.5f+p.x-65,BoardY+BoardH*.5f-p.y-42,130,34,18,color,TextAnchor.MiddleCenter);
            StartCoroutine(FloatLabel(label));
        }
        IEnumerator FloatLabel(Text label)
        {var r=label.rectTransform;var p=r.anchoredPosition;var color=label.color;float t=0;while(t<.85f){if(!paused)t+=Time.unscaledDeltaTime;r.anchoredPosition=p+new Vector2(0,36*t);label.color=new Color(color.r,color.g,color.b,1-t/.85f);yield return null;}Destroy(label.gameObject);}
        void Log(string line){recentLog.Insert(0,line);if(recentLog.Count>6)recentLog.RemoveAt(6);}
        void Toast(string text){if(string.IsNullOrEmpty(text))return;toastText.text=text;toastText.transform.parent.gameObject.SetActive(true);modalRoot.SetAsLastSibling();toastText.transform.parent.SetAsLastSibling();toastTime=4;}
        void ToggleSound(){sound.Muted=!sound.Muted;mutedText.text=sound.Muted?"静音":"声音";PlayerPrefs.SetInt("Emergence.Muted",sound.Muted?1:0);}
        void Update()
        {
            if(board!=null)board.Tick(paused?0:Time.unscaledDeltaTime);
            if(totalText!=null)totalText.rectTransform.localScale=Vector3.Lerp(totalText.rectTransform.localScale,Vector3.one,Time.unscaledDeltaTime*9);
            if(toastTime>0){toastTime-=Time.unscaledDeltaTime;if(toastTime<=0)toastText.transform.parent.gameObject.SetActive(false);}
            var k=Keyboard.current;if(k==null)return;
            if(k.escapeKey.wasPressedThisFrame){if(HasModal){CloseModal();paused=false;}else if(itemIndex>=0||moving||bindingSlot>=0){CancelTool();Refresh();}else ShowPause();}
            if(k.spaceKey.wasPressedThisFrame&&!HasModal&&primary.interactable)Primary();
        }
        RectTransform Dialog(string title,string subtitle,float width=1032,float height=744)
        {
            CloseModal();var shade=Fill(modalRoot,"Modal backdrop");var im=shade.gameObject.AddComponent<Image>();im.color=new Color(.012f,.032f,.045f,.93f);im.raycastTarget=true;
            float x=(1600-width)*.5f,y=(1000-height)*.5f;
            var body=Surface(modalRoot,"Dialog",x,y,width,height,Hex("10252E")).rectTransform;
            Surface(body,"Top edge",28,0,width-56,2,Teal,false);
            Label(body,title,36,27,width-114,51,30,Foreground);
            Label(body,subtitle,37,85,width-74,43,15,Muted);
            Button(body,"Close dialog","×",width-73,28,40,40,()=>{CloseModal();paused=false;},Muted,false,23);
            return body;
        }
        void CloseModal(){Clear(modalRoot);}
        public void ShowShop()
        {
            if(Engine.State.phase!=RunPhase.Shop)return;CancelTool();var s=Engine.State;
            var d=Dialog("神 经 集 市","第 "+s.round+" 轮完成  /  奖励 ◈ "+s.lastReward+" 已收集。把短暂的闪念，变成下一轮的力量。",1200,840);
            Label(d,"◈ "+s.currency,920,30,200,48,29,Gold,TextAnchor.MiddleRight);
            var ordinary=s.shop.Select((o,i)=>new{offer=o,index=i}).Where(x=>!x.offer.bossReward).ToList();
            for(int k=0;k<ordinary.Count;k++)
            {
                int idx=ordinary[k].index;var o=ordinary[k].offer;var def=Catalog.Get(o.id);float x=36+k*228;
                var panel=Surface(d,"Offer "+idx,x,147,216,239,Hex("17313A"));
                Color accent=def.category==ContentCategory.Synapse?(def.rare?Gold:Teal):def.category==ContentCategory.Memory?Violet:def.category==ContentCategory.Drug?Teal:Gold;
                Surface(panel.transform,"Accent",16,18,23,2,accent,false);
                Label(panel.transform,CategoryName(def.category)+(def.rare?" · 稀有":""),16,34,184,28,11,accent);
                Label(panel.transform,def.name,16,68,184,37,22,Foreground);
                Label(panel.transform,def.description,16,113,184,68,13,Muted);
                var buy=Button(panel.transform,"Buy "+idx,o.sold?"已获得":"获取  ◈ "+o.price,16,190,184,34,()=>{if(Engine.BuyOffer(idx)){sound.PlayUI();Log("获得 · "+def.name);SaveRun();Refresh();ShowShop();}else Toast(Engine.LastError);},accent,false,15);
                buy.interactable=!o.sold&&s.currency>=o.price;
            }
            Label(d,"已装突触  "+s.synapses.Count+" / 5   ·   满槽时可出售替换",36,408,650,26,14,Muted);
            for(int i=0;i<s.synapses.Count;i++)
            {int idx=i;var def=Catalog.Get(s.synapses[i].id);Button(d,"Shop sell "+i,def.name+" · 售 ◈"+(def.price/2),36+i*228,449,216,40,()=>{if(Engine.SellSynapse(idx)){SaveRun();Refresh();ShowShop();}else Toast(Engine.LastError);},Muted,false,13);}
            var rewards=s.shop.Select((o,i)=>new{offer=o,index=i}).Where(x=>x.offer.bossReward).ToList();
            if(rewards.Count>0)
            {
                Label(d,s.bossRewardClaimed?"BOSS 记忆 · 已领取":"BOSS 记忆 · 免费选择一个突触",36,520,1000,28,17,Gold);
                for(int i=0;i<rewards.Count;i++)
                {
                    int idx=rewards[i].index;var o=rewards[i].offer;var def=Catalog.Get(o.id);float x=36+i*380;
                    var panel=Surface(d,"Boss reward "+i,x,561,368,148,Hex("263332"));
                    Label(panel.transform,def.name,16,12,330,32,21,Gold);Label(panel.transform,def.description,16,49,330,44,13,Muted);
                    var b=Button(panel.transform,"Claim reward "+i,o.sold?"已结束选择":"免费选择",16,104,336,32,()=>{if(Engine.BuyOffer(idx)){SaveRun();Refresh();ShowShop();}else Toast(Engine.LastError);},Gold,false,14);b.interactable=!o.sold;
                }
            }
            else
            {
                Surface(d,"Shop advice",36,522,710,185,Panel);
                Label(d,"围绕连接，选择你的构筑。",56,544,669,38,23,Teal);
                Label(d,"敏感节点延伸长链，核心收集汇流，回声让高价值节点再次计分。先观察网络缺少什么，再决定把资源花在哪里。",56,598,650,78,17,Muted);
                Surface(d,"Backpack card",768,522,396,185,Panel);
                Label(d,"随身物品  "+s.inventory.Count+" 件",791,544,343,35,21,Violet);
                Button(d,"Shop inventory","整理背包 / 使用星图",791,623,348,52,ShowInventory,Violet,false,16);
            }
            var reroll=Button(d,"Reroll","刷新货架  ◈ "+s.refreshCost,36,757,239,48,()=>{if(Engine.RerollShop()){sound.PlayUI();SaveRun();Refresh();ShowShop();}else Toast(Engine.LastError);},Muted,false,16);reroll.interactable=s.currency>=s.refreshCost;
            Button(d,"Open inventory","背包  ·  "+s.inventory.Count,294,757,188,48,ShowInventory,Muted,false,15);
            Label(d,"操作后自动保存",511,770,290,27,13,Muted);
            Button(d,"Next round",rewards.Count>0&&!s.bossRewardClaimed?"跳过免费奖励并继续  →":"继续生长  →",834,752,330,56,NextRound,Teal,true,20);
        }
        void ShowInventory()
        {
            CancelTool();var s=Engine.State;var d=Dialog("随 身 记 忆","每类物品最多携带两份。记忆永久改变网络，药品只影响下一次刺激。",1040,804);
            if(s.inventory.Count==0)Label(d,"还没有随身物品。",38,181,960,80,25,Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<s.inventory.Count;i++)
            {
                int idx=i;var def=Catalog.Get(s.inventory[i].id);float x=36+(i%2)*494,y=145+(i/2)*182;
                var panel=Surface(d,"Backpack item "+i,x,y,474,165,Panel);
                Label(panel.transform,def.name,17,11,434,34,21,def.category==ContentCategory.Drug?Teal:Violet);
                Label(panel.transform,def.description,17,53,434,54,14,Muted);
                var b=Button(panel.transform,"Use backpack item "+i,"使用",17,117,323,34,()=>{CloseModal();SelectItem(idx);},Teal,false,15);
                b.interactable=!busy&&(s.phase==RunPhase.Prepare||(s.phase==RunPhase.Shop&&def.category!=ContentCategory.Drug));
                Button(panel.transform,"Discard item "+i,"弃置",353,117,104,34,()=>{if(Engine.DiscardItem(idx)){SaveRun();Refresh();ShowInventory();}else Toast(Engine.LastError);},Muted,false,14);
            }
            Button(d,"Return from inventory",s.phase==RunPhase.Shop?"返回集市":"返回网络",698,724,306,49,()=>{if(s.phase==RunPhase.Shop)ShowShop();else CloseModal();},Teal,false,17);
        }
        static string CategoryName(ContentCategory c){switch(c){case ContentCategory.Synapse:return "突触 / 持续生效";case ContentCategory.Memory:return "记忆 / 永久改造";case ContentCategory.Drug:return "药品 / 一次刺激";default:return "星图 / 结构升级";}}
        public void NextRound()
        {
            if(Engine.NextRound()){CloseModal();CancelTool();selected=-1;candidate=0;shownCharge=0;shownMult=1;LastResult=null;structureText.text="新的意识层，等待一个起点。";hint="选择新神经元，然后点击空位。";sound.PlayUI();SaveRun();Refresh();}else Toast(Engine.LastError);
        }
        void ShowSynapse(int index)
        {
            if(busy||index>=Engine.State.synapses.Count)return;CancelTool();var s=Engine.State;var def=Catalog.Get(s.synapses[index].id);
            var d=Dialog(def.name,"突触  "+(index+1)+" / 5     ·     "+(def.rare?"稀有":"普通"),690,456);
            Label(d,def.description,38,143,612,99,21,Foreground);
            Label(d,"网络决定触发的机会，突触决定这次机会的价值。",38,250,612,42,14,Muted);
            if(def.targetCount>0)
            {var bind=Button(d,"Bind synapse","连接两个神经元",38,313,288,50,()=>{CloseModal();bindingSlot=index;itemIndex=-1;linkSource=-1;moving=false;hint="依次选择两个节点，建立专用连接。";Refresh();},Teal,true,17);bind.interactable=s.phase==RunPhase.Prepare&&s.clicksRemaining==2;}
            else
            {var b=Button(d,"Move synapse left","向左移动",38,313,142,50,()=>{if(Engine.SwapSynapses(index,index-1)){SaveRun();Refresh();ShowSynapse(index-1);}else Toast(Engine.LastError);},Muted,false,16);b.interactable=index>0;
             var b2=Button(d,"Move synapse right","向右移动",190,313,142,50,()=>{if(Engine.SwapSynapses(index,index+1)){SaveRun();Refresh();ShowSynapse(index+1);}else Toast(Engine.LastError);},Muted,false,16);b2.interactable=index<s.synapses.Count-1;}
            Button(d,"Sell synapse","出售  ◈ "+(def.price/2),355,313,295,50,()=>{if(Engine.SellSynapse(index)){CloseModal();SaveRun();Refresh();}else Toast(Engine.LastError);},Gold,false,17);
            Label(d,"第一次刺激后，突触与永久改造将锁定。",38,391,612,25,13,Muted);
        }
        public void DragSynapse(int from,Vector2 screenPosition)
        {
            if(busy||HasModal)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(bottom,screenPosition,null,out var point))
            {int to=Mathf.FloorToInt(point.x/175);if(to>=0&&to<Engine.State.synapses.Count&&from!=to){if(Engine.SwapSynapses(from,to)){sound.PlayUI();SaveRun();Refresh();}else Toast(Engine.LastError);}}
        }
        void ShowPause()
        {
            paused=true;var d=Dialog("思 绪 暂 停","给每一次连接，留一点思考的时间。",680,487);
            Button(d,"Resume","继续涌现",38,155,604,58,()=>{paused=false;CloseModal();},Teal,true,21);
            Button(d,"Restart","以相同种子重新开始",38,235,604,53,()=>ConfirmRestart(false),Muted,false,18);
            Button(d,"New seed","开启新的意识",38,305,604,53,()=>ConfirmRestart(true),Muted,false,18);
            Label(d,busy?"结算暂停中。恢复后会继续播放并保存。":"当前进度已自动保存，可以随时关闭游戏。",38,402,604,40,14,Muted,TextAnchor.MiddleCenter);
            if(!busy)SaveRun();
        }
        void ConfirmRestart(bool newSeed)
        {
            var d=Dialog("重新形成意识？","当前局的进度会被新一局替换。",640,325);
            Button(d,"Cancel restart","保留当前意识",34,213,272,61,()=>ShowPause(),Muted,false,18);
            Button(d,"Confirm restart","重新开始",330,213,272,61,()=>NewRun(newSeed?Environment.TickCount&int.MaxValue:Engine.State.seed),Teal,true,18);
        }
        public void NewRun(int seed)
        {
            StopAllCoroutines();Clear(floatRoot);busy=paused=skip=false;CloseModal();Engine=new EmergenceEngine(seed);selected=-1;candidate=0;shownCharge=0;shownMult=1;LastResult=null;recentLog.Clear();CancelTool();hint="选择一枚神经元，再点击空位。";structureText.text="潜能未定 · 等待第一次放电";SaveRun();Refresh();
        }
        void ShowEnd()
        {
            var s=Engine.State;bool win=s.phase==RunPhase.Victory;
            var d=Dialog(win?"涌 现 · 意识已形成":"这一次，思绪未能抵达",win?"18 轮神经构筑完成。每一个微小连接，汇成了这次涌现。":"回看连接的断点，让下一次尝试走得更远。",920,628);
            Label(d,win?Number(s.totalScore)+"  总灵感":"还差 "+Number(Math.Max(0,s.target-s.roundScore))+" 灵感",37,150,845,67,40,win?Gold:Red);
            Label(d,"抵达回合  "+s.round+" / 18     ·     最高涌现  "+Number(s.bestScore)+"     ·     网络  "+s.nodes.Count+" 节点",38,236,843,42,17,Muted);
            var unfired=s.nodes.Where(n=>!n.fired).OrderByDescending(n=>n.BaseScore).Take(3).ToList();
            string analysis=win?"你让独立的神经元，组成了一台彼此成就的引擎。":"未放电节点："+(unfired.Count==0?"所有节点均已放电，可尝试提升倍率。":string.Join("、",unfired.Select(n=>Catalog.NodeName(n.kind)+" #"+n.id+"（差 "+Math.Max(0,n.Threshold-n.charge)+" 电位）")));
            Label(d,analysis,38,301,843,105,19,Foreground);
            var inactive=new List<string>();if(LastResult!=null)for(int i=0;i<s.synapses.Count;i++){int slot=i;if(Catalog.Get(s.synapses[i].id).targetCount==0&&!LastResult.events.Any(e=>e.synapseIndex==slot))inactive.Add(Catalog.Get(s.synapses[i].id).name);}
            string advice=inactive.Count>0?"本次未触发："+string.Join("、",inactive)+"。下一次可围绕这些突触的条件重新连接。":"下一次可以用敏感节点延长链路，或让两路信号汇入同一个核心。";
            Label(d,win?"保留这个种子，试试另一条构筑路线。":advice,38,418,843,62,16,Muted);
            Button(d,"Retry seed","相同种子再来一次",38,526,407,63,()=>NewRun(s.seed),Muted,false,19);
            Button(d,"New consciousness","开启新的意识  →",472,526,407,63,()=>NewRun(Environment.TickCount&int.MaxValue),Teal,true,19);
        }
        void ShowHelp()
        {
            if(busy)paused=true;var d=Dialog("让思绪，连成星河。","只需选择一个起点，让神经元替你完成接下来的事情。",1000,738);
            string[] titles={"01 / 生长","02 / 放电","03 / 重塑"};
            string[] texts={"每轮从三枚神经元中选一枚，植入空位。\n\n相邻节点自动连接。敏感节点更容易放电；核心擅长收集多路输入。","选择起点，再释放脉冲。主动放电输出 2，被动通常输出 1。\n\n达到阈值就会继续放电；休眠节点不再转发。","达标后进入集市，获得突触、记忆和药品。\n\n把连接改成你需要的样子，让灵感与倍率一起成长。"};
            for(int i=0;i<3;i++){float x=36+i*314;Surface(d,"Help column",x,146,300,308,Panel);Label(d,titles[i],x+19,168,266,34,22,Teal);Label(d,texts[i],x+19,220,261,215,17,Foreground);}
            Label(d,"两次机会，一个计划。",37,488,920,35,23,Gold);
            Label(d,"第一次刺激后，未放电节点会保留电位。第二次刺激可以补上最后一点能量。回响会让休眠节点再计分一次，但不会再次传播。",37,541,920,72,18,Muted);
            Button(d,"Got it","开始连接  →",635,643,329,59,()=>{CloseModal();paused=false;},Teal,true,20);
            Label(d,"每 3 轮遭遇 BOSS · 规则提前公开",38,662,555,30,15,Muted);
        }
        public string DiagnosticSnapshot()
        {
            return JsonUtility.ToJson(new AppDiagnostic{round=Engine.State.round,phase=Engine.State.phase.ToString(),nodes=Engine.State.nodes.Count,animating=busy,selected=selected,saveExists=File.Exists(SavePath),hint=hint});
        }
        [Serializable]class AppDiagnostic{public int round,nodes,selected;public string phase,hint;public bool animating,saveExists;}
    }
    public sealed class SynapseDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public EmergenceApp app;public int slot;Vector3 start;
        public void OnBeginDrag(PointerEventData e){start=transform.localPosition;}
        public void OnDrag(PointerEventData e){transform.localPosition=start+new Vector3(0,8,0);}
        public void OnEndDrag(PointerEventData e){transform.localPosition=start;app.DragSynapse(slot,e.position);}
    }
}
