using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Emergence.VisualDemos
{
    // Isolated presentation sandbox. No references to the game's simulation or score state.
    public sealed class SparkNeuronLifeDemo : MonoBehaviour
    {
        public Texture2D neuronTexture;
        public Shader livingShader;
        public TMP_FontAsset uiFont;
        public Camera viewCamera;
        public bool autoPulse = true;
        public bool animate = true;
        public bool soundEnabled = false;
        public int NodeCount => nodes.Count;
        public int ActivePulseCount => pulses.Count;
        public int TotalActivations { get; private set; }
        public int ManualStimulations { get; private set; }
        public float AnimationTime => clock;
        public bool Paused => !animate;
        public Vector3 FirstNodePosition => nodes.Count > 0 ? nodes[0].position : Vector3.zero;

        sealed class Node
        {
            public Vector3 position;
            public Transform body, halo;
            public Renderer renderer, haloRenderer;
            public float size, rotation, phase, firedAt = -99;
            public Color tint;
        }
        sealed class Edge { public int a,b; public LineRenderer line; public Vector3[] points = new Vector3[33]; }
        sealed class Wave { public readonly bool[] visited = new bool[8]; }
        sealed class Pulse { public int edge, destination; public bool reverse; public float age, duration; public Wave wave; public int slot; }
        struct Dust { public Transform transform; public Vector3 position; public float phase; }
        readonly List<Node> nodes = new List<Node>();
        readonly List<Edge> edges = new List<Edge>();
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        readonly List<Dust> dust = new List<Dust>();
        MaterialPropertyBlock block;
        readonly LineRenderer[] trails = new LineRenderer[32];
        readonly bool[] trailBusy = new bool[32];
        readonly Vector3[] trailPoints = new Vector3[9];
        Material neuronMaterial, lightMaterial;
        Mesh neuronMesh, quad;
        Transform generated;
        AudioSource audioSource;
        AudioClip note;
        TMP_Text status, autoLabel, pauseLabel, soundLabel;
        float clock, nextAuto = 1.1f, lastManual = -10;
        int autoIndex;

        void Start()
        {
            block = new MaterialPropertyBlock();
            if (!neuronTexture || !livingShader || !viewCamera)
            { Debug.LogError("Spark neuron demo: missing texture, shader or camera.", this); enabled=false; return; }
            generated = new GameObject("Generated · Visuals only").transform;
            generated.SetParent(transform, false);
            neuronMesh = Grid(24, new Vector2(.452f,.557f));
            quad = Grid(1, new Vector2(.5f,.5f));
            neuronMaterial = new Material(livingShader) { name="Neuron · Runtime" };
            neuronMaterial.mainTexture=neuronTexture;
            lightMaterial=new Material(livingShader) { name="Soft light · Runtime" };
            lightMaterial.mainTexture=GlowTexture(); lightMaterial.SetFloat("_Sway",0);
            owned.Add(neuronMaterial); owned.Add(lightMaterial);
            MakeAtmosphere(); MakeNodes(); MakeEdges(); MakeUI();
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false; audioSource.spatialBlend=0;
            note=CreateNote(); owned.Add(note);
        }

        Mesh Grid(int steps, Vector2 pivot)
        {
            var mesh=new Mesh { name="Neuron deform grid" };
            var v=new Vector3[(steps+1)*(steps+1)]; var uv=new Vector2[v.Length]; var col=new Color[v.Length];
            var tri=new int[steps*steps*6]; int k=0;
            for(int y=0;y<=steps;y++) for(int x=0;x<=steps;x++)
            { int i=y*(steps+1)+x; uv[i]=new Vector2((float)x/steps,(float)y/steps); v[i]=uv[i]-pivot; col[i]=Color.white; }
            for(int y=0;y<steps;y++) for(int x=0;x<steps;x++)
            { int i=y*(steps+1)+x; tri[k++]=i; tri[k++]=i+steps+1; tri[k++]=i+1; tri[k++]=i+1; tri[k++]=i+steps+1; tri[k++]=i+steps+2; }
            mesh.vertices=v; mesh.uv=uv; mesh.colors=col; mesh.triangles=tri;
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*2); owned.Add(mesh); return mesh;
        }
        Texture2D GlowTexture()
        {
            var t=new Texture2D(64,64,TextureFormat.RGBA32,false) {name="Procedural glow",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[4096];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            { float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))/32; pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Max(0,1-r),3)); }
            t.SetPixels(pixels); t.Apply(); owned.Add(t); return t;
        }
        Renderer Visual(string label, Mesh mesh, Material mat, Vector3 position, float size, int order, Color color)
        {
            var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(generated,false);
            go.transform.position=position; go.transform.localScale=Vector3.one*size;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>(); r.sharedMaterial=mat; r.sortingOrder=order;
            SetTint(r,color); return r;
        }
        void SetTint(Renderer r,Color color)
        { block.Clear(); block.SetColor("_Tint",color); r.SetPropertyBlock(block); }
        void MakeAtmosphere()
        {
            Visual("Teal ambient haze",quad,lightMaterial,new Vector3(-4,1,4),24,-20,new Color(.02f,.27f,.3f,.35f));
            Visual("Violet ambient haze",quad,lightMaterial,new Vector3(7,-2,4),20,-20,new Color(.18f,.05f,.35f,.30f));
            var random=new System.Random(712);
            for(int i=0;i<70;i++)
            {
                var p=new Vector3((float)random.NextDouble()*26-13,(float)random.NextDouble()*14-7,2);
                float s=.035f+(float)random.NextDouble()*.06f;
                var r=Visual("Ambient mote",quad,lightMaterial,p,s,-5,new Color(.24f,.65f,.7f,.4f));
                dust.Add(new Dust { transform=r.transform,position=p,phase=(float)random.NextDouble()*6.28f });
            }
        }
        void MakeNodes()
        {
            var positions=new[]{new Vector3(-8,1),new Vector3(-4,3.2f),new Vector3(0,.2f),new Vector3(4.6f,3),new Vector3(8.4f,.1f),new Vector3(4.7f,-3.0f),new Vector3(-.6f,-3.5f),new Vector3(-5.3f,-2.4f)};
            float[] sizes={4.4f,4.0f,5.7f,4.1f,4.6f,4.4f,4.0f,4.8f};
            float[] rotations={-35,70,5,-85,130,45,-100,160};
            for(int i=0;i<positions.Length;i++)
            {
                var tint=Color.Lerp(new Color(.72f,1,.96f),new Color(.93f,.73f,1),i%3*.20f);
                var body=Visual("Living neuron "+(i+1),neuronMesh,neuronMaterial,positions[i],sizes[i],5,tint);
                var halo=Visual("Soma halo "+(i+1),quad,lightMaterial,positions[i],1.5f,6,new Color(.30f,.85f,1,.13f));
                nodes.Add(new Node {position=positions[i],body=body.transform,renderer=body,halo=halo.transform,haloRenderer=halo,size=sizes[i],rotation=rotations[i],phase=i*1.37f,tint=tint});
            }
        }
        LineRenderer Line(string label,int count,float width,int order,Color color)
        {
            var go=new GameObject(label); go.transform.SetParent(generated,false);
            var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=lightMaterial; line.useWorldSpace=true;
            line.positionCount=count; line.widthMultiplier=width; line.startColor=line.endColor=color;
            line.sortingOrder=order; line.numCapVertices=3; line.textureMode=LineTextureMode.Stretch; return line;
        }
        void MakeEdges()
        {
            int[,] links={{0,1},{0,7},{1,2},{1,3},{2,3},{2,6},{2,7},{3,4},{4,5},{5,6},{6,7}};
            for(int i=0;i<links.GetLength(0);i++)
            {
                var edge=new Edge {a=links[i,0],b=links[i,1],line=Line("光丝 "+(i+1),33,.10f,1,new Color(.2f,.58f,.65f,.4f))};
                edges.Add(edge); UpdateEdge(edge,i);
            }
            for(int i=0;i<trails.Length;i++) { trails[i]=Line("Impulse pool "+i,9,.15f,8,new Color(.8f,1,1,1)); trails[i].enabled=false; }
        }
        Vector3 EdgePoint(Edge e,float t,int index)
        {
            Vector3 a=nodes[e.a].position,b=nodes[e.b].position;
            Vector3 d=b-a; var normal=new Vector3(-d.y,d.x,0).normalized;
            float arc=Mathf.Sin(t*Mathf.PI)*(.4f*Mathf.Sin(index*2.3f)+.04f*Mathf.Sin(clock*.8f+index));
            float ripple=Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*16+clock*.65f+index)*.045f;
            return Vector3.Lerp(a,b,t)+normal*(arc+ripple);
        }
        void UpdateEdge(Edge e,int i)
        { for(int p=0;p<e.points.Length;p++) e.points[p]=EdgePoint(e,p/32f,i); e.line.SetPositions(e.points); }
        public bool Stimulate(int index)
        {
            if(!animate || index<0 || index>=nodes.Count || clock-lastManual<.30f || pulses.Count>22) return false;
            lastManual=clock; ManualStimulations++; nextAuto=clock+5;
            var wave=new Wave(); wave.visited[index]=true; Fire(index,wave); return true;
        }
        public bool StimulateScreen(Vector2 screen)
        {
            if(!viewCamera) return false;
            var p=viewCamera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-viewCamera.transform.position.z));
            int best=-1; float distance=1.25f;
            for(int i=0;i<nodes.Count;i++) { float d=Vector2.Distance(p,nodes[i].position); if(d<distance) {best=i;distance=d;} }
            return Stimulate(best);
        }
        void Fire(int index,Wave wave)
        {
            nodes[index].firedAt=clock; TotalActivations++;
            if(soundEnabled && audioSource && note) {audioSource.pitch=.85f+index*.09f;audioSource.PlayOneShot(note,.09f);}
            for(int i=0;i<edges.Count;i++)
            {
                Edge e=edges[i]; int other=e.a==index?e.b:e.b==index?e.a:-1;
                if(other<0 || wave.visited[other]) continue;
                int slot=Array.FindIndex(trailBusy,x=>!x); if(slot<0) break;
                wave.visited[other]=true;trailBusy[slot]=true;trails[slot].enabled=true;
                pulses.Add(new Pulse {edge=i,destination=other,reverse=e.b==index,duration=Vector3.Distance(nodes[index].position,nodes[other].position)/7.5f,wave=wave,slot=slot});
            }
        }
        public void SetPaused(bool paused) {animate=!paused;UpdateLabels();}
        void Update()
        {
            if(nodes.Count==0) return;
            if(Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame) SetPaused(animate);
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject())) StimulateScreen(Mouse.current.position.ReadValue());
            if(!animate) return;
            clock+=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(autoPulse && clock>=nextAuto && pulses.Count==0)
            { var wave=new Wave(); int index=autoIndex++%nodes.Count;wave.visited[index]=true;Fire(index,wave);nextAuto=clock+5; }
            for(int i=0;i<nodes.Count;i++)
            {
                Node n=nodes[i];float age=clock-n.firedAt;
                float flash=Mathf.Exp(-age*5);float breath=Mathf.Sin(clock*.8f+n.phase);
                n.body.localScale=Vector3.one*n.size*(1+breath*.012f+flash*.035f);
                n.body.localRotation=Quaternion.Euler(0,0,n.rotation+Mathf.Sin(clock*.46f+n.phase)*1.1f);
                block.Clear();block.SetColor("_Tint",n.tint*(.86f+flash*.65f));block.SetFloat("_Clock",clock);
                block.SetFloat("_Phase",n.phase);block.SetFloat("_PulseAge",age);n.renderer.SetPropertyBlock(block);
                n.halo.localScale=Vector3.one*(1.4f+flash*1.7f+breath*.08f);
                SetTint(n.haloRenderer,Color.Lerp(new Color(.25f,.8f,.9f,.12f),new Color(1,.66f,.19f,.78f),flash));
            }
            for(int i=0;i<edges.Count;i++) UpdateEdge(edges[i],i);
            for(int i=pulses.Count-1;i>=0;i--)
            {
                Pulse p=pulses[i];p.age+=Mathf.Min(Time.unscaledDeltaTime,.05f); float t=p.age/p.duration;
                if(t>=1) {trails[p.slot].enabled=false;trailBusy[p.slot]=false;pulses.RemoveAt(i);Fire(p.destination,p.wave);continue;}
                for(int j=0;j<9;j++) {float u=Mathf.Clamp01(t-j*.014f);if(p.reverse)u=1-u;trailPoints[j]=EdgePoint(edges[p.edge],u,p.edge);}
                trails[p.slot].SetPositions(trailPoints);
            }
            for(int i=0;i<dust.Count;i++) {var d=dust[i];d.transform.position=d.position+new Vector3(Mathf.Sin(clock*.12f+d.phase)*.20f,Mathf.Cos(clock*.1f+d.phase)*.16f,0);}
            if(status) status.text=pulses.Count>0?"信号正在传播  /  "+pulses.Count.ToString("00"):"静息中  ·  点击任一胞体";
        }
        void MakeUI()
        {
            var go=new GameObject("Demo HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(generated,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var root=(RectTransform)go.transform;
            Text(root,"spark",new Vector2(60,-42),new Vector2(500,95),64,new Color(.84f,.98f,1),new Vector2(0,1));
            Text(root,"神经元生命实验  /  2D 动态演示",new Vector2(64,-140),new Vector2(900,40),22,new Color(.39f,.68f,.73f),new Vector2(0,1));
            Text(root,"点击胞体，唤醒一次回响",new Vector2(62,110),new Vector2(800,44),26,new Color(.75f,.88f,.9f),Vector2.zero);
            Text(root,"呼吸 · 枝梢摆动 · 光丝脉冲    |    空格暂停",new Vector2(64,66),new Vector2(1050,35),20,new Color(.36f,.57f,.63f),Vector2.zero);
            status=Text(root,"",new Vector2(-64,-65),new Vector2(650,45),20,new Color(.47f,.77f,.79f),Vector2.one);status.alignment=TextAlignmentOptions.Right;
            autoLabel=Button(root,new Vector2(-620,70),()=>{autoPulse=!autoPulse;nextAuto=clock+2;UpdateLabels();});
            pauseLabel=Button(root,new Vector2(-410,70),()=>SetPaused(animate));
            soundLabel=Button(root,new Vector2(-200,70),()=>{soundEnabled=!soundEnabled;UpdateLabels();});
            UpdateLabels();
            if(EventSystem.current==null)
            {var events=new GameObject("Demo EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(generated,false);}
        }
        TMP_Text Text(RectTransform parent,string value,Vector2 pos,Vector2 size,float fontSize,Color color,Vector2 anchor)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();if(uiFont)text.font=uiFont;text.text=value;text.fontSize=fontSize;text.color=color;text.raycastTarget=false;return text;
        }
        TMP_Text Button(RectTransform parent,Vector2 pos,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject("Demo control",typeof(RectTransform),typeof(Image),typeof(Button));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=pos;rect.sizeDelta=new Vector2(184,56);
            go.GetComponent<Image>().color=new Color(.035f,.10f,.13f,.94f);var button=go.GetComponent<Button>();button.onClick.AddListener(action);
            var label=Text(rect,"",Vector2.zero,rect.sizeDelta,21,new Color(.62f,.87f,.89f),new Vector2(.5f,.5f));label.alignment=TextAlignmentOptions.Center;return label;
        }
        void UpdateLabels()
        { if(autoLabel)autoLabel.text=autoPulse?"自发活动：开":"自发活动：关";if(pauseLabel)pauseLabel.text=animate?"暂停活动":"继续活动";if(soundLabel)soundLabel.text=soundEnabled?"声音：开":"声音：关"; }
        AudioClip CreateNote()
        {
            const int rate=22050; var data=new float[4410];
            for(int i=0;i<data.Length;i++) {float t=(float)i/rate;data[i]=Mathf.Sin(2*Mathf.PI*(540*t+260*t*t))*Mathf.Exp(-t*24)*Mathf.Min(t*200,1);}
            var clip=AudioClip.Create("Soft neural tick",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy()
        {for(int i=0;i<owned.Count;i++)if(owned[i])Destroy(owned[i]);}
    }
}
