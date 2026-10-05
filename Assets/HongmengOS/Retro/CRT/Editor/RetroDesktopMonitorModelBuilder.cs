using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HongmengOS.Retro.Editor
{
    /// <summary>
    /// Generates a real, hollow-front desktop CRT. Front is -Z; screen centre is (0,.12,-.12).
    /// Owns only its named root in the active scene and assets below AssetRoot.
    /// </summary>
    public static class RetroDesktopMonitorModelBuilder
    {
        public const string RootName = "HongmengOS CRT 3D Model";
        public const string AssetRoot = "Assets/HongmengOS/Retro/CRT/RetroDesktopMonitorModelAssets";
        public const float ScreenWidth = 4.35f;
        public const float ScreenHeight = 3.2625f;
        public static readonly Vector3 ScreenCenter = new Vector3(0f, .12f, -.12f);

        public static GameObject Build()
        {
            EnsureFolder(AssetRoot);
            EnsureFolder(AssetRoot + "/Meshes");
            EnsureFolder(AssetRoot + "/Materials");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit == null) throw new InvalidOperationException("No supported Lit shader was found for the desktop CRT model.");

            Material front = MaterialAsset("Warm Grey ABS Fascia", lit, "DDDCD3", .25f);
            Material body = MaterialAsset("Cool Grey ABS Housing", lit, "CFD1C9", .24f);
            Material basePlastic = MaterialAsset("Swivel Base Grey", lit, "BABFB8", .30f);
            Material keys = MaterialAsset("Control Key ABS", lit, "C9CEC5", .35f);
            Material gasket = MaterialAsset("Dark Grey Glass Seal", lit, "424B47", .14f);
            Material recess = MaterialAsset("Vent Recess", lit, "303835", .09f);
            Material metal = MaterialAsset("Screw Nickel", lit, "969E96", .52f, .60f);
            Material rubber = MaterialAsset("Rubber Feet", lit, "383E3A", .08f);
            Material labelPlate = MaterialAsset("Rear Label Paper", lit, "DADDD3", .12f);
            Material led = MaterialAsset("Green Power LED", lit, "99C761", .55f);
            if (led.HasProperty("_EmissionColor"))
            {
                led.SetColor("_EmissionColor", new Color(.12f,.30f,.045f));
                led.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(led);
            }

            // Complete asset creation before replacing the previous generated scene object.
            Mesh fasciaMesh = BuildFascia();
            Mesh housingMesh = BuildHousing();
            Mesh rearMesh = BuildRearCover();
            Mesh glassSealMesh = BuildGlassSeal();
            Mesh cavityMesh = BuildCavityWalls();
            Mesh seamMesh = BuildBodySeam();
            Mesh standBaseMesh = EllipticalSolid("Swivel Lower Plinth", new[] {
                new EllipseRing(-.17f,1.73f,1.20f), new EllipseRing(-.09f,1.94f,1.34f),
                new EllipseRing(.10f,1.94f,1.34f), new EllipseRing(.17f,1.78f,1.21f) }, 64);
            Mesh swivelMesh = EllipticalSolid("Swivel Turntable", new[] {
                new EllipseRing(-.07f,1.16f,.98f), new EllipseRing(.035f,1.20f,1.02f),
                new EllipseRing(.08f,1.10f,.94f) }, 48);
            Mesh neckMesh = EllipticalSolid("Stand Pivot Neck", new[] {
                new EllipseRing(-.20f,.58f,.49f), new EllipseRing(.16f,.50f,.45f),
                new EllipseRing(.22f,.43f,.40f) }, 32);
            Mesh hingeMesh = EllipticalSolid("Tilt Hinge Cylinder", new[] {
                new EllipseRing(-.87f,.16f,.16f), new EllipseRing(.87f,.16f,.16f) }, 32);
            Mesh controlMesh = BevelBox("Small Control Key", .25f, .115f, .075f, .022f);
            Mesh powerMesh = BevelBox("Power Button Key", .34f, .24f, .085f, .035f);
            Mesh trayMesh = BevelBox("Control Bank Recess", 1.88f, .205f, .025f, .035f);
            Mesh powerWellMesh = BevelBox("Power Key Well", .405f, .305f, .030f, .045f);
            Mesh sideVentMesh = BevelBox("Side Cooling Slot", .58f, .030f, .016f, .006f);
            Mesh rearVentMesh = BevelBox("Rear Cooling Slot", .55f, .025f, .014f, .006f);
            Mesh bottomFootMesh = BevelBox("Rubber Foot", .40f, .10f, .36f, .055f);
            Mesh backLabelMesh = BevelBox("Rear Specification Label", 1.34f, .53f, .012f, .018f);
            Mesh screwMesh = EllipticalSolid("Fastener Head", new[] {
                new EllipseRing(-.007f,.035f,.035f), new EllipseRing(.007f,.031f,.031f) }, 16);
            Mesh screwSlotMesh = BevelBox("Fastener Slot", .044f, .007f, .004f, .002f);
            Mesh ledMesh = EllipticalSolid("Power Indicator Lens", new[] {
                new EllipseRing(-.01f,.027f,.027f), new EllipseRing(.009f,.023f,.023f) }, 24);
            Mesh connectorMesh = BevelBox("Rear Connector Block", 1.06f, .38f, .045f, .025f);

            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (item != null && item.name == RootName && item.gameObject.scene == SceneManager.GetActiveScene())
                    Undo.DestroyObjectImmediate(item.gameObject);
            }
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build desktop CRT 3D model");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            AddMesh(root.transform, "Front Bezel - Open Aperture", fasciaMesh, front);
            AddMesh(root.transform, "Wedge Rear Housing", housingMesh, body);
            AddMesh(root.transform, "Removable Back Cover", rearMesh, body);
            AddMesh(root.transform, "Fine Grey Glass Gasket", glassSealMesh, gasket);
            AddMesh(root.transform, "Inner Screen Cavity Walls", cavityMesh, recess);
            AddMesh(root.transform, "Front Housing Parting Seam", seamMesh, recess);

            // Geometry stops at the aperture boundary: no front cap or dummy screen is generated.
            Transform screenMount = Empty(root.transform, "Screen Surface Mount", ScreenCenter);
            screenMount.localScale = Vector3.one;
            Empty(screenMount, "Aperture Lower Left", new Vector3(-ScreenWidth * .5f,-ScreenHeight * .5f,0));
            Empty(screenMount, "Aperture Upper Right", new Vector3(ScreenWidth * .5f,ScreenHeight * .5f,0));

            Transform stand = Empty(root.transform, "Tilt And Swivel Stand", new Vector3(0,.115f,0));
            AddMesh(stand, "Low Oval Swivel Foot", standBaseMesh, basePlastic, new Vector3(0,-2.79f,1.00f));
            AddMesh(stand, "Circular Turntable Upper Plate", swivelMesh, body, new Vector3(0,-2.56f,1.03f));
            AddMesh(stand, "Load Bearing Pivot Neck", neckMesh, basePlastic, new Vector3(0,-2.36f,1.06f));
            AddMesh(stand, "Horizontal Tilt Hinge", hingeMesh, basePlastic, new Vector3(0,-2.20f,1.05f), Quaternion.Euler(0,0,90));
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
                AddMesh(stand, "Rubber Pad " + side + " " + end, bottomFootMesh, rubber,
                    new Vector3(side * 1.27f,-2.955f,1.00f + end * .79f));

            // Small tactile key bank is on the chin, never on a laboratory-style side panel.
            AddMesh(root.transform, "Inset Four Key Tray", trayMesh, gasket, new Vector3(.21f,-1.94f,-.103f));
            Transform focusMount = Empty(root.transform, "Focus Control Mount", new Vector3(-.30f,-1.94f,-.20f));
            Transform brightnessMount = Empty(root.transform, "Brightness Control Mount", new Vector3(.72f,-1.94f,-.20f));
            BuildControlKey(focusMount, "Focus Minus Mount", -.20f, controlMesh, keys, "-");
            BuildControlKey(focusMount, "Focus Plus Mount", .20f, controlMesh, keys, "+");
            BuildControlKey(brightnessMount, "Brightness Minus Mount", -.20f, controlMesh, keys, "-");
            BuildControlKey(brightnessMount, "Brightness Plus Mount", .20f, controlMesh, keys, "+");
            Text(root.transform, "Focus Legend", "FOCUS", new Vector3(-.30f,-1.755f,-.106f), .56f, new Vector2(.68f,.13f));
            Text(root.transform, "Brightness Legend", "BRIGHTNESS", new Vector3(.72f,-1.755f,-.106f), .56f, new Vector2(.94f,.13f));

            Transform powerMount = Empty(root.transform, "Power Button Mount", new Vector3(1.93f,-1.94f,-.20f));
            AddMesh(powerMount, "Power Switch Recess", powerWellMesh, gasket, new Vector3(0,0,.097f));
            Transform powerKeycap = AddMesh(powerMount, "Keycap", powerMesh, keys, new Vector3(0,0,.032f)).transform;
            Text(powerKeycap, "Power Mark", "I / O", new Vector3(0,0,-.047f), .70f, new Vector2(.30f,.13f));
            Transform indicator = Empty(root.transform, "Power Indicator Mount", new Vector3(2.32f,-1.94f,-.122f));
            AddMesh(indicator, "Power Indicator Lens", ledMesh, led, Vector3.zero, Quaternion.Euler(90,0,0));
            Text(root.transform, "Power Legend", "POWER", new Vector3(1.93f,-2.145f,-.095f), .48f, new Vector2(.48f,.10f));

            Text(root.transform, "Hongmengos Wordmark", "hongmengos", new Vector3(-1.74f,-1.83f,-.109f), 1.18f, new Vector2(1.32f,.20f));
            Text(root.transform, "Front Model Legend", "HM-98  MULTISCAN", new Vector3(-1.74f,-2.025f,-.103f), .40f, new Vector2(1.32f,.12f));

            BuildSideVents(root.transform, sideVentMesh, recess);
            BuildRearDetails(root.transform, rearVentMesh, recess, backLabelMesh, labelPlate,
                screwMesh, screwSlotMesh, metal, connectorMesh);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(root);
            return root;
        }

        private static Mesh BuildFascia()
        {
            var data = new MeshData();
            Vector3[] edge = Octagon(5.4f,4.4f,0,.00f,.18f);
            Vector3[] crown = Octagon(5.24f,4.24f,0,-.095f,.12f);
            Vector3[] openingLip = Octagon(4.59f,3.5025f,.12f,-.095f,.090f);
            Vector3[] sealOuter = Octagon(4.40f,3.3125f,.12f,.035f,.060f);
            data.Connect(edge,crown,Vector3.back);
            data.Connect(crown,openingLip,Vector3.back);
            data.Connect(openingLip,sealOuter,Vector3.back);
            return data.Save("Hollow Bevelled Front Fascia");
        }

        private static Mesh BuildGlassSeal()
        {
            var data = new MeshData();
            data.Connect(Octagon(4.40f,3.3125f,.12f,.035f,.060f),
                Octagon(ScreenWidth,ScreenHeight,.12f,.044f,.045f),Vector3.back);
            return data.Save("Thin Aperture Glass Seal");
        }

        private static Mesh BuildCavityWalls()
        {
            var data = new MeshData();
            Vector3[] front = Octagon(ScreenWidth,ScreenHeight,.12f,.044f,.045f);
            Vector3[] rear = Octagon(4.49f,3.40f,.12f,.50f,.055f);
            data.ConnectRadial(front,rear,true);
            return data.Save("Open CRT Tube Cavity");
        }

        private static Mesh BuildHousing()
        {
            var data = new MeshData();
            Vector3[][] rings = {
                Octagon(5.4f,4.4f,0,.008f,.18f), Octagon(5.4f,4.4f,0,.20f,.18f),
                Octagon(5.28f,4.32f,.015f,.55f,.20f), Octagon(4.75f,3.92f,.06f,1.95f,.22f),
                Octagon(4.36f,3.62f,.08f,2.50f,.20f), Octagon(4.26f,3.52f,.08f,2.68f,.18f)
            };
            for (int i=0;i<rings.Length-1;i++) data.ConnectRadial(rings[i],rings[i+1],false);
            return data.Save("Deep Wedge ABS Body");
        }

        private static Mesh BuildRearCover()
        {
            var data = new MeshData();
            Vector3[] edge = Octagon(4.26f,3.52f,.08f,2.68f,.18f);
            Vector3[] back = Octagon(4.18f,3.44f,.08f,2.72f,.15f);
            data.Connect(edge,back,Vector3.forward);
            data.Cap(back,Vector3.forward);
            return data.Save("Chamfered Rear Service Cover");
        }

        private static Mesh BuildBodySeam()
        {
            var data = new MeshData();
            data.ConnectRadial(Octagon(5.405f,4.405f,0,.205f,.18f),
                Octagon(5.400f,4.400f,0,.222f,.18f),false);
            return data.Save("Thin Injection Mould Parting Seam");
        }

        private static void BuildControlKey(Transform group, string mountName, float x, Mesh mesh, Material material, string glyph)
        {
            Transform mount = Empty(group,mountName,new Vector3(x,0,0));
            Transform keycap=AddMesh(mount,"Keycap",mesh,material,new Vector3(0,0,.052f)).transform;
            Text(keycap,"Printed Key Symbol",glyph,new Vector3(0,0,-.042f),.58f,new Vector2(.18f,.085f));
        }

        private static void BuildSideVents(Transform root, Mesh slot, Material recess)
        {
            Transform vents = Empty(root,"Side Ventilation Louvres",Vector3.zero);
            for (int side=-1;side<=1;side+=2)
            for (int bank=0;bank<2;bank++)
            {
                float depth = bank==0 ? 1.05f : 1.72f;
                float x = Mathf.Lerp(5.28f*.5f,4.75f*.5f,(depth-.55f)/(1.95f-.55f));
                Vector3 outward = new Vector3(side,0,.19f).normalized;
                Quaternion rotation = Quaternion.LookRotation(-outward,Vector3.up);
                for (int row=0;row<12;row++)
                    AddMesh(vents,(side<0?"Left":"Right") + " Air Slot " + bank + "-" + row,
                        slot,recess,new Vector3(side*(x+.007f),.40f + row*.086f,depth),rotation);
            }
            // Lower slits complement the large high exhaust bank.
            for (int side=-1;side<=1;side+=2)
            {
                float depth=1.10f;
                float x=Mathf.Lerp(2.64f,2.375f,(depth-.55f)/1.4f);
                Quaternion rotation=Quaternion.LookRotation(-new Vector3(side,0,.19f).normalized,Vector3.up);
                for (int row=0;row<4;row++)
                    AddMesh(vents,(side<0?"Left":"Right") + " Intake " + row,slot,recess,
                        new Vector3(side*(x+.007f),-1.30f+row*.086f,depth),rotation);
            }
        }

        private static void BuildRearDetails(Transform root, Mesh ventMesh, Material recess,
            Mesh plateMesh, Material plate, Mesh screwMesh, Mesh screwSlot, Material metal, Mesh connectorMesh)
        {
            Transform rear=Empty(root,"Rear Service Details",Vector3.zero);
            Quaternion rearFacing=Quaternion.Euler(0,180,0);
            for(int column=0;column<5;column++)
            for(int row=0;row<9;row++)
                AddMesh(rear,"Rear Exhaust " + column + "-" + row,ventMesh,recess,
                    new Vector3(-1.32f+column*.66f,.57f+row*.105f,2.730f),rearFacing);
            AddMesh(rear,"Specification Plate",plateMesh,plate,new Vector3(0,-.48f,2.733f),rearFacing);
            Transform label=Empty(rear,"Rear Label Text Mount",new Vector3(0,-.48f,2.742f));
            label.localRotation=rearFacing;
            Text(label,"Printed Specification","HONGMENG  HM-98\nCOLOUR DISPLAY TERMINAL\n100-240 V AC   50/60 Hz\nS/N  HM98-040128",Vector3.zero,.44f,new Vector2(1.20f,.44f));
            AddMesh(rear,"Recessed Connector Bay",connectorMesh,recess,new Vector3(.68f,-1.30f,2.738f),rearFacing);
            for(int side=-1;side<=1;side+=2)
            for(int top=-1;top<=1;top+=2)
            {
                Vector3 position=new Vector3(side*1.87f,.08f+top*1.47f,2.736f);
                AddMesh(rear,"Rear Cover Screw "+side+" "+top,screwMesh,metal,position,Quaternion.Euler(90,0,0));
                AddMesh(rear,"Rear Screw Slot "+side+" "+top,screwSlot,recess,position+Vector3.forward*.011f,rearFacing);
            }
        }

        private static Mesh BevelBox(string name,float width,float height,float depth,float bevel)
        {
            bevel=Mathf.Min(bevel,Mathf.Min(width,Mathf.Min(height,depth))*.45f);
            var data=new MeshData();
            Vector3[] front=Octagon(width-2*bevel,height-2*bevel,0,-depth*.5f,bevel*.5f);
            Vector3[] shoulder=Octagon(width,height,0,-depth*.5f+bevel,bevel);
            Vector3[] rearShoulder=Octagon(width,height,0,depth*.5f-bevel,bevel);
            Vector3[] rear=Octagon(width-2*bevel,height-2*bevel,0,depth*.5f,bevel*.5f);
            data.Cap(front,Vector3.back);
            data.Connect(front,shoulder,Vector3.back);
            data.ConnectRadial(shoulder,rearShoulder,false);
            data.Connect(rearShoulder,rear,Vector3.forward);
            data.Cap(rear,Vector3.forward);
            return data.Save(name);
        }

        private readonly struct EllipseRing
        {
            public readonly float Y,RadiusX,RadiusZ;
            public EllipseRing(float y,float radiusX,float radiusZ) { Y=y;RadiusX=radiusX;RadiusZ=radiusZ; }
        }

        private static Mesh EllipticalSolid(string name,EllipseRing[] rings,int segments)
        {
            var data=new MeshData();
            var loops=new Vector3[rings.Length][];
            for(int row=0;row<rings.Length;row++)
            {
                loops[row]=new Vector3[segments];
                for(int i=0;i<segments;i++)
                {
                    float angle=i*Mathf.PI*2f/segments;
                    loops[row][i]=new Vector3(Mathf.Cos(angle)*rings[row].RadiusX,rings[row].Y,Mathf.Sin(angle)*rings[row].RadiusZ);
                }
            }
            data.Cap(loops[0],Vector3.down);
            for(int row=0;row<loops.Length-1;row++)
            for(int i=0;i<segments;i++)
            {
                int next=(i+1)%segments;
                Vector3 centre=(loops[row][i]+loops[row][next]+loops[row+1][i]+loops[row+1][next])*.25f;
                data.Quad(loops[row][i],loops[row][next],loops[row+1][next],loops[row+1][i],new Vector3(centre.x,0,centre.z));
            }
            data.Cap(loops[loops.Length-1],Vector3.up);
            return data.Save(name);
        }

        private static Vector3[] Octagon(float width,float height,float centreY,float z,float corner)
        {
            float x=width*.5f,y=height*.5f;
            float c=Mathf.Clamp(corner,0,Mathf.Min(x,y)*.8f);
            return new[] { new Vector3(-x+c,centreY-y,z),new Vector3(x-c,centreY-y,z),
                new Vector3(x,centreY-y+c,z),new Vector3(x,centreY+y-c,z),
                new Vector3(x-c,centreY+y,z),new Vector3(-x+c,centreY+y,z),
                new Vector3(-x,centreY+y-c,z),new Vector3(-x,centreY-y+c,z) };
        }

        private sealed class MeshData
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<Vector3> normals=new List<Vector3>();
            private readonly List<Vector2> uvs=new List<Vector2>();
            private readonly List<int> indices=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 expectedNormal)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),expectedNormal)<0) { Vector3 temp=b;b=d;d=temp; }
                Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                int start=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                for(int i=0;i<4;i++) normals.Add(normal);
                uvs.Add(Vector2.zero);uvs.Add(Vector2.right);uvs.Add(Vector2.one);uvs.Add(Vector2.up);
                indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                indices.Add(start);indices.Add(start+2);indices.Add(start+3);
            }
            public void Connect(Vector3[] first,Vector3[] second,Vector3 normal)
            {
                if(first.Length!=second.Length) throw new ArgumentException("Ring sizes differ.");
                for(int i=0;i<first.Length;i++) { int next=(i+1)%first.Length;Quad(first[i],first[next],second[next],second[i],normal); }
            }
            public void ConnectRadial(Vector3[] first,Vector3[] second,bool inside)
            {
                for(int i=0;i<first.Length;i++)
                {
                    int next=(i+1)%first.Length;
                    Vector3 centre=(first[i]+first[next]+second[i]+second[next])*.25f;
                    Vector3 normal=new Vector3(centre.x,centre.y,0)*(inside?-1:1);
                    Quad(first[i],first[next],second[next],second[i],normal);
                }
            }
            public void Cap(Vector3[] perimeter,Vector3 normal)
            {
                Vector3 centre=Vector3.zero;
                foreach(Vector3 point in perimeter) centre+=point;
                centre/=perimeter.Length;
                for(int i=0;i<perimeter.Length;i++)
                {
                    Vector3 a=perimeter[i],b=perimeter[(i+1)%perimeter.Length];
                    if(Vector3.Dot(Vector3.Cross(a-centre,b-centre),normal)<0) { Vector3 swap=a;a=b;b=swap; }
                    int start=vertices.Count;
                    vertices.Add(centre);vertices.Add(a);vertices.Add(b);
                    for(int j=0;j<3;j++) { normals.Add(normal.normalized);uvs.Add(Vector2.zero);indices.Add(start+j); }
                }
            }
            public Mesh Save(string name)
            {
                var mesh=new Mesh { name=name, indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(indices,0);
                mesh.RecalculateBounds();
                string path=AssetRoot+"/Meshes/"+name+".asset";
                Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null) { AssetDatabase.CreateAsset(mesh,path);return mesh; }
                EditorUtility.CopySerialized(mesh,existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
        }

        private static GameObject AddMesh(Transform parent,string name,Mesh mesh,Material material,
            Vector3 position=default,Quaternion rotation=default)
        {
            Transform node=Empty(parent,name,position);
            node.localRotation=rotation.Equals(default(Quaternion))?Quaternion.identity:rotation;
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            renderer.receiveShadows=true;
            return node.gameObject;
        }

        private static Transform Empty(Transform parent,string name,Vector3 position)
        {
            var go=new GameObject(name);
            go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            return go.transform;
        }

        private static void Text(Transform parent,string name,string text,Vector3 position,float fontSize,Vector2 size)
        {
            var go=new GameObject(name,typeof(TextMeshPro));
            go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            TextMeshPro label=go.GetComponent<TextMeshPro>();
            if(TMP_Settings.defaultFontAsset!=null) label.font=TMP_Settings.defaultFontAsset;
            label.text=text;
            label.fontSize=fontSize;
            label.alignment=TextAlignmentOptions.Center;
            label.color=new Color(.22f,.26f,.24f,1);
            label.textWrappingMode=TextWrappingModes.NoWrap;
            label.overflowMode=TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta=size;
            label.raycastTarget=false;
            label.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        private static Material MaterialAsset(string name,Shader shader,string hex,float smoothness,float metallic=0)
        {
            string path=AssetRoot+"/Materials/"+name+".mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path); }
            material.shader=shader;
            ColorUtility.TryParseHtmlString("#"+hex,out Color color);
            if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Color")) material.SetColor("_Color",color);
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smoothness);
            if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",smoothness);
            if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            int slash=path.LastIndexOf('/');
            string parent=path.Substring(0,slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,path.Substring(slash+1));
        }
    }
}
