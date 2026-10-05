using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HongmengOS.Aero2010.Editor
{
    /// <summary>
    /// Original 2010-era office LCD geometry, informed by period industrial-design proportions.
    /// Negative Z is the front. Builds only its own root and persistent model assets.
    /// The actual desktop/glazing and event wiring are supplied by the scene integration.
    /// </summary>
    public static class AeroLcdModelBuilder
    {
        public const string RootName = "HongmengOS 2010 LCD Model";
        public const string AssetRoot = "Assets/HongmengOS/Aero2010/Models";
        public const float ScreenWidth = 5.12f;
        public const float ScreenHeight = 2.88f;
        public const float DisplayPlaneZ = .028f;
        public const float LowestPointY = -2.895f;
        public static readonly Vector3 ScreenCenter = new Vector3(0, 0, DisplayPlaneZ);

        public static GameObject Build()
        {
            EnsureFolder(AssetRoot);
            EnsureFolder(AssetRoot + "/Meshes");
            EnsureFolder(AssetRoot + "/Materials");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit == null) throw new InvalidOperationException("A Lit shader is required for the 2010 LCD model.");

            Material fascia = MaterialAsset("Charcoal Bezel ABS", lit, "171B1D", .23f);
            Material edge = MaterialAsset("Inner Screen Seal", lit, "090D0F", .16f);
            Material back = MaterialAsset("Matte Rear Housing", lit, "1B1F21", .17f);
            Material pedestal = MaterialAsset("Stand Black ABS", lit, "202628", .29f);
            Material keyMaterial = MaterialAsset("OSD Key Finish", lit, "293034", .32f);
            Material slotMaterial = MaterialAsset("Vent Interior", lit, "050708", .08f);
            Material rubber = MaterialAsset("Black Rubber Feet", lit, "080A0B", .03f);
            Material silver = MaterialAsset("Brushed Fastener Steel", lit, "899397", .43f, .65f);
            Material label = MaterialAsset("Service Label Grey", lit, "B9C0BF", .12f);
            Material blueLed = MaterialAsset("Blue Power Indicator", lit, "62B1E2", .40f);
            if (blueLed.HasProperty("_EmissionColor"))
            {
                blueLed.SetColor("_EmissionColor", new Color(.07f,.27f,.55f));
                blueLed.EnableKeyword("_EMISSION");
                SaveMaterial(blueLed);
            }

            Mesh frontMesh = BuildFascia();
            Mesh sealMesh = BuildSeal();
            Mesh bodyMesh = BuildBody();
            Mesh rearMesh = BuildBackPanel();
            Mesh partingMesh = BuildPartingSeam();
            Mesh backingMesh = FrontPlane("Opaque Screen Backing", ScreenWidth + .020f, ScreenHeight + .020f, .055f);
            Mesh electronicsMesh = BevelBox("Rear Electronics Housing", 3.12f, 2.32f, .18f, .045f);
            Mesh vesaPlateMesh = BevelBox("VESA Stand Interface", 1.18f, 1.18f, .17f, .030f);
            Mesh columnMesh = BevelBox("Height Adjustable Stand Column", .72f, 1.98f, .36f, .065f);
            Mesh columnRailMesh = BevelBox("Stand Column Central Rail", .27f, 1.67f, .018f, .008f);
            Mesh columnSeamMesh = BevelBox("Stand Lift Slot", .050f, 1.58f, .015f, .006f);
            Mesh baseMesh = BevelBox("Rectangular Pedestal Foot", 3.38f, .210f, 2.22f, .055f);
            Mesh baseInsetMesh = BevelBox("Pedestal Upper Inset", 3.14f, .022f, 1.97f, .008f);
            Mesh footMesh = BevelBox("Rubber Support Pad", .40f, .040f, .31f, .010f);
            Mesh hingeMesh = Cylinder("LCD Tilt Hinge", .19f, .82f, 32);
            Mesh osdKeyMesh = BevelBox("Small OSD Keycap", .090f, .055f, .020f, .008f);
            Mesh osdWellMesh = BevelBox("OSD Button Well", .112f, .076f, .012f, .004f);
            Mesh powerKeyMesh = BevelBox("Power Keycap", .104f, .093f, .022f, .010f);
            Mesh powerWellMesh = BevelBox("Power Key Well", .128f, .113f, .012f, .004f);
            Mesh powerGlyphMesh = PowerGlyph();
            Mesh ventMesh = BevelBox("Cooling Slit", .190f, .022f, .012f, .004f);
            Mesh rearLabelMesh = BevelBox("Service Specification Plate", 1.22f, .40f, .012f, .005f);
            Mesh socketMesh = BevelBox("Recessed Connector Bay", 1.54f, .25f, .040f, .016f);
            Mesh screwHeadMesh = Cylinder("VESA Fastener", .027f, .010f, 16);
            Mesh screwSlotMesh = BevelBox("Fastener Screwdriver Slot", .033f, .005f, .003f, .001f);
            Mesh cableGuideMesh = BuildCableGuide();

            // Name and scene are deliberately scoped: unrelated/other-scene objects are not touched.
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (transform != null && transform.name == RootName && transform.gameObject.scene == SceneManager.GetActiveScene())
                    Undo.DestroyObjectImmediate(transform.gameObject);

            var root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(root, "Build 2010 desktop LCD model");

            AddMesh(root.transform, "Thin Straight Front Bezel", frontMesh, fascia);
            AddMesh(root.transform, "Screen Perimeter Seal", sealMesh, edge);
            AddMesh(root.transform, "Opaque Black Screen Backing", backingMesh, slotMaterial);
            AddMesh(root.transform, "Tapered Matte Rear Housing", bodyMesh, back);
            AddMesh(root.transform, "Rear Service Cover", rearMesh, back);
            AddMesh(root.transform, "Housing Parting Seam", partingMesh, slotMaterial);
            AddMesh(root.transform, "Central Electronics Bump", electronicsMesh, back, new Vector3(0,-.03f,.530f));

            Transform displayMount = Empty(root.transform, "Screen Surface Mount", ScreenCenter);
            Empty(displayMount, "Aperture Lower Left", new Vector3(-ScreenWidth * .5f,-ScreenHeight * .5f,0));
            Empty(displayMount, "Aperture Upper Right", new Vector3(ScreenWidth * .5f,ScreenHeight * .5f,0));

            Transform stand = Empty(root.transform, "Height Adjustable Pedestal", Vector3.zero);
            AddMesh(stand, "VESA Mount And Tilt Plate", vesaPlateMesh, pedestal, new Vector3(0,-.17f,.679f));
            AddMesh(stand, "Horizontal Tilt Hinge", hingeMesh, pedestal, new Vector3(0,-.655f,.698f), Quaternion.Euler(0,0,90));
            AddMesh(stand, "Rectangular Lift Column", columnMesh, pedestal, new Vector3(0,-1.70f,.760f));
            AddMesh(stand, "Rear Lift Rail", columnRailMesh, back, new Vector3(0,-1.64f,.949f));
            AddMesh(stand, "Left Lift Seam", columnSeamMesh, slotMaterial, new Vector3(-.19f,-1.65f,.952f));
            AddMesh(stand, "Right Lift Seam", columnSeamMesh, slotMaterial, new Vector3(.19f,-1.65f,.952f));
            AddMesh(stand, "Rectangular Low Profile Base", baseMesh, pedestal, new Vector3(0,-2.765f,.670f));
            AddMesh(stand, "Inset Base Top Panel", baseInsetMesh, back, new Vector3(0,-2.650f,.670f));
            AddMesh(stand, "Cable Management Hoop", cableGuideMesh, pedestal, new Vector3(0,-2.03f,1.035f));
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
                AddMesh(stand, "Rubber Foot " + side + " " + end, footMesh, rubber,
                    new Vector3(side * 1.31f,-2.875f,.670f + end * .83f));

            // Five right-lower controls are separate moving keycaps with fixed surrounding wells.
            float[] osdY = { -.83f,-1.00f,-1.17f,-1.34f };
            string[] symbols = { "STD","-","+","MENU" };
            for (int i = 0; i < osdY.Length; i++)
            {
                Transform mount = Empty(root.transform, "OSD Button " + (i + 1) + " Mount", new Vector3(2.65f,osdY[i],-.065f));
                AddMesh(mount, "Fixed Button Well", osdWellMesh, slotMaterial, new Vector3(0,0,.026f));
                Transform keycap = AddMesh(mount, "Keycap", osdKeyMesh, keyMaterial, new Vector3(0,0,.009f)).transform;
                Text(keycap, "Key Legend", symbols[i], new Vector3(0,0,-.0115f),
                    i < 2 ? .235f : .108f, new Vector2(.088f,.040f), new Color(.62f,.68f,.70f,1));
            }
            Transform powerMount = Empty(root.transform, "Power Button Mount", new Vector3(2.65f,-1.51f,-.065f));
            AddMesh(powerMount, "Fixed Power Well", powerWellMesh, slotMaterial, new Vector3(0,0,.026f));
            Transform powerKeycap = AddMesh(powerMount, "Keycap", powerKeyMesh, keyMaterial, new Vector3(0,0,.009f)).transform;
            AddMesh(powerKeycap, "Power LED", powerGlyphMesh, blueLed, new Vector3(0,0,-.0125f));

            Text(root.transform, "Silver Hongmengos Logo", "hongmengos", new Vector3(0,-1.535f,-.046f),
                .63f, new Vector2(1.04f,.115f), new Color(.68f,.73f,.74f,1));
            Text(root.transform, "Front Product Designation", "HM-2310", new Vector3(-2.12f,-1.536f,-.046f),
                .225f, new Vector2(.50f,.074f), new Color(.36f,.41f,.43f,1));
            BuildRearDetails(root.transform, ventMesh, slotMaterial, rearLabelMesh, label,
                socketMesh, screwHeadMesh, screwSlotMesh, silver);

            EditorUtility.SetDirty(root);
            return root;
        }

        private static Mesh BuildFascia()
        {
            var mesh = new Geometry();
            Vector3[] back = Rectangle(5.50f,3.23f,.020f,.030f);
            Vector3[] front = Rectangle(5.476f,3.206f,-.043f,.020f);
            Vector3[] lip = Rectangle(5.160f,2.920f,-.043f,.006f);
            mesh.Connect(back,front,Vector3.back);
            mesh.Connect(front,lip,Vector3.back);
            return mesh.Save("Open Thin LCD Front Frame");
        }

        private static Mesh BuildSeal()
        {
            var mesh = new Geometry();
            mesh.Connect(Rectangle(5.160f,2.920f,-.043f,.006f),
                Rectangle(ScreenWidth,ScreenHeight,DisplayPlaneZ,.001f),Vector3.back);
            return mesh.Save("Aperture Flush Inner Lip");
        }

        private static Mesh BuildBody()
        {
            var mesh = new Geometry();
            Vector3[][] rings = {
                Rectangle(5.50f,3.23f,.020f,.030f), Rectangle(5.50f,3.23f,.120f,.034f),
                Rectangle(5.42f,3.15f,.310f,.046f), Rectangle(5.20f,2.970f,.460f,.055f)
            };
            for (int i = 0; i < rings.Length - 1; i++) mesh.ConnectRadial(rings[i],rings[i+1]);
            return mesh.Save("Slender LCD Rear Shell");
        }

        private static Mesh BuildBackPanel()
        {
            var mesh = new Geometry();
            Vector3[] border = Rectangle(5.20f,2.970f,.460f,.055f);
            Vector3[] cover = Rectangle(5.176f,2.946f,.472f,.044f);
            mesh.Connect(border,cover,Vector3.forward);
            mesh.Cap(cover,Vector3.forward);
            return mesh.Save("LCD Rear Service Panel");
        }

        private static Mesh BuildPartingSeam()
        {
            var mesh = new Geometry();
            mesh.ConnectRadial(Rectangle(5.502f,3.232f,.118f,.034f),Rectangle(5.499f,3.229f,.130f,.035f));
            return mesh.Save("Rear Shell Narrow Parting Seam");
        }

        private static Mesh BuildCableGuide()
        {
            var mesh = new Geometry();
            Vector3[] outer = Rectangle(.54f,.39f,-.055f,.050f);
            Vector3[] inner = Rectangle(.39f,.245f,-.055f,.038f);
            Vector3[] outerRear = Rectangle(.54f,.39f,.055f,.050f);
            Vector3[] innerRear = Rectangle(.39f,.245f,.055f,.038f);
            mesh.Connect(outer,inner,Vector3.back);
            mesh.Connect(outerRear,innerRear,Vector3.forward);
            mesh.ConnectRadial(outer,outerRear);
            mesh.ConnectRadial(inner,innerRear,true);
            return mesh.Save("Open Cable Management Ring");
        }

        private static Mesh PowerGlyph()
        {
            var mesh = new Geometry();
            const int segments = 24;
            const float outer = .025f;
            const float inner = .0175f;
            for (int i = 0; i < segments; i++)
            {
                float angle0 = (42f + i * 276f / segments) * Mathf.Deg2Rad;
                float angle1 = (42f + (i+1) * 276f / segments) * Mathf.Deg2Rad;
                Vector3 a = new Vector3(Mathf.Sin(angle0)*outer,Mathf.Cos(angle0)*outer,0);
                Vector3 b = new Vector3(Mathf.Sin(angle1)*outer,Mathf.Cos(angle1)*outer,0);
                Vector3 c = new Vector3(Mathf.Sin(angle1)*inner,Mathf.Cos(angle1)*inner,0);
                Vector3 d = new Vector3(Mathf.Sin(angle0)*inner,Mathf.Cos(angle0)*inner,0);
                mesh.Quad(a,b,c,d,Vector3.back);
            }
            mesh.Quad(new Vector3(-.0035f,.005f,0),new Vector3(.0035f,.005f,0),
                new Vector3(.0035f,.030f,0),new Vector3(-.0035f,.030f,0),Vector3.back);
            return mesh.Save("Power Symbol LED Lens");
        }

        private static void BuildRearDetails(Transform root, Mesh vent, Material black, Mesh labelMesh,
            Material label, Mesh port, Mesh screwMesh, Mesh slotMesh, Material metal)
        {
            Transform rear = Empty(root,"Rear Service Details",Vector3.zero);
            Quaternion backFacing = Quaternion.Euler(0,180,0);
            // Slots remain below the top shell chamfer and clear of the central VESA carrier.
            for (int side = -1; side <= 1; side += 2)
            for (int column = 0; column < 4; column++)
            for (int row = 0; row < 8; row++)
                AddMesh(rear,"Rear Vent " + side + "-" + column + "-" + row,vent,black,
                    new Vector3(side*(1.72f+column*.215f),.31f+row*.105f,.481f),backFacing);
            for (int column = 0; column < 12; column++)
                AddMesh(rear,"Upper Exhaust " + column,vent,black,
                    new Vector3(-1.29f+column*.235f,1.313f,.481f),backFacing);
            AddMesh(rear,"Rear Specification Label",labelMesh,label,new Vector3(-.82f,-.87f,.626f),backFacing);
            Transform labelMount = Empty(rear,"Rear Label Print",new Vector3(-.82f,-.87f,.635f));
            labelMount.localRotation = backFacing;
            Text(labelMount,"Product Information","HONGMENG  HM-2310\n16:9 WIDE LCD DISPLAY\n100-240 V AC  50/60 Hz\nSERIAL HM2010-2311",Vector3.zero,.30f,
                new Vector2(1.13f,.35f),new Color(.15f,.18f,.19f,1));
            AddMesh(rear,"Recessed Signal Connector Bay",port,black,new Vector3(.39f,-1.13f,.632f),backFacing);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3 location = new Vector3(x*.50f,-.03f+y*.50f,.631f);
                AddMesh(rear,"VESA Screw " + x + " " + y,screwMesh,metal,location,Quaternion.Euler(90,0,0));
                AddMesh(rear,"VESA Screw Slot " + x + " " + y,slotMesh,black,location+Vector3.forward*.007f,backFacing);
            }
        }

        private static Mesh FrontPlane(string name,float width,float height,float z)
        {
            var mesh = new Geometry();
            mesh.Quad(new Vector3(-width*.5f,-height*.5f,z),new Vector3(width*.5f,-height*.5f,z),
                new Vector3(width*.5f,height*.5f,z),new Vector3(-width*.5f,height*.5f,z),Vector3.back);
            return mesh.Save(name);
        }

        private static Mesh BevelBox(string name,float width,float height,float depth,float bevel)
        {
            bevel = Mathf.Min(bevel,Mathf.Min(width,Mathf.Min(height,depth))*.45f);
            var mesh = new Geometry();
            Vector3[] front = Rectangle(width-2*bevel,height-2*bevel,-depth*.5f,bevel*.5f);
            Vector3[] first = Rectangle(width,height,-depth*.5f+bevel,bevel);
            Vector3[] second = Rectangle(width,height,depth*.5f-bevel,bevel);
            Vector3[] rear = Rectangle(width-2*bevel,height-2*bevel,depth*.5f,bevel*.5f);
            mesh.Cap(front,Vector3.back);
            mesh.Connect(front,first,Vector3.back);
            mesh.ConnectRadial(first,second);
            mesh.Connect(second,rear,Vector3.forward);
            mesh.Cap(rear,Vector3.forward);
            return mesh.Save(name);
        }

        private static Mesh Cylinder(string name,float radius,float height,int segments)
        {
            var mesh = new Geometry();
            var first = new Vector3[segments];
            var second = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i*Mathf.PI*2f/segments;
                first[i] = new Vector3(Mathf.Cos(angle)*radius,-height*.5f,Mathf.Sin(angle)*radius);
                second[i] = new Vector3(first[i].x,height*.5f,first[i].z);
            }
            mesh.Cap(first,Vector3.down);
            for (int i = 0; i < segments; i++)
            {
                int next = (i+1)%segments;
                Vector3 normal = new Vector3(first[i].x+first[next].x,0,first[i].z+first[next].z);
                mesh.Quad(first[i],first[next],second[next],second[i],normal);
            }
            mesh.Cap(second,Vector3.up);
            return mesh.Save(name);
        }

        private static Vector3[] Rectangle(float width,float height,float z,float corner)
        {
            float x = width*.5f, y = height*.5f;
            float c = Mathf.Clamp(corner,.00001f,Mathf.Min(x,y)*.8f);
            return new[] { new Vector3(-x+c,-y,z),new Vector3(x-c,-y,z),
                new Vector3(x,-y+c,z),new Vector3(x,y-c,z),new Vector3(x-c,y,z),
                new Vector3(-x+c,y,z),new Vector3(-x,y-c,z),new Vector3(-x,-y+c,z) };
        }

        private sealed class Geometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> indices = new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 expectedNormal)
            {
                if (Vector3.Dot(Vector3.Cross(b-a,c-a),expectedNormal)<0) { Vector3 temp=b;b=d;d=temp; }
                Vector3 normal = Vector3.Cross(b-a,c-a).normalized;
                int start = vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                for (int i=0;i<4;i++) normals.Add(normal);
                uvs.Add(Vector2.zero);uvs.Add(Vector2.right);uvs.Add(Vector2.one);uvs.Add(Vector2.up);
                indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                indices.Add(start);indices.Add(start+2);indices.Add(start+3);
            }
            public void Connect(Vector3[] first,Vector3[] second,Vector3 normal)
            {
                for (int i=0;i<first.Length;i++)
                {
                    int next=(i+1)%first.Length;
                    Quad(first[i],first[next],second[next],second[i],normal);
                }
            }
            public void ConnectRadial(Vector3[] first,Vector3[] second,bool inward=false)
            {
                for (int i=0;i<first.Length;i++)
                {
                    int next=(i+1)%first.Length;
                    Vector3 centre=(first[i]+first[next]+second[i]+second[next])*.25f;
                    Quad(first[i],first[next],second[next],second[i],new Vector3(centre.x,centre.y,0)*(inward?-1:1));
                }
            }
            public void Cap(Vector3[] perimeter,Vector3 normal)
            {
                Vector3 centre=Vector3.zero;
                foreach (Vector3 point in perimeter) centre+=point;
                centre/=perimeter.Length;
                for (int i=0;i<perimeter.Length;i++)
                {
                    Vector3 a=perimeter[i],b=perimeter[(i+1)%perimeter.Length];
                    if (Vector3.Dot(Vector3.Cross(a-centre,b-centre),normal)<0) { Vector3 swap=a;a=b;b=swap; }
                    int start=vertices.Count;
                    vertices.Add(centre);vertices.Add(a);vertices.Add(b);
                    for (int j=0;j<3;j++) { normals.Add(normal.normalized);uvs.Add(Vector2.zero);indices.Add(start+j); }
                }
            }
            public Mesh Save(string name)
            {
                var mesh=new Mesh { name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(indices,0);
                mesh.RecalculateBounds();
                string path=AssetRoot+"/Meshes/"+name+".asset";
                Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing==null) { AssetDatabase.CreateAsset(mesh,path);return mesh; }
                EditorUtility.CopySerialized(mesh,existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                return existing;
            }
        }

        private static GameObject AddMesh(Transform parent,string name,Mesh mesh,Material material,
            Vector3 position=default,Quaternion rotation=default)
        {
            Transform node=Empty(parent,name,position);
            node.localRotation=rotation.Equals(default(Quaternion))?Quaternion.identity:rotation;
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            MeshRenderer renderer=node.gameObject.AddComponent<MeshRenderer>();
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

        private static void Text(Transform parent,string name,string text,Vector3 position,float fontSize,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(TextMeshPro));
            go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            TextMeshPro label=go.GetComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset!=null) label.font=TMP_Settings.defaultFontAsset;
            label.text=text;
            label.fontSize=fontSize;
            label.alignment=TextAlignmentOptions.Center;
            label.color=color;
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
            if (material==null) { material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path); }
            material.shader=shader;
            ColorUtility.TryParseHtmlString("#"+hex,out Color color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if (material.HasProperty("_Color")) material.SetColor("_Color",color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
            SaveMaterial(material);
            return material;
        }

        private static void SaveMaterial(Material material)
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash=path.LastIndexOf('/');
            string parent=path.Substring(0,slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,path.Substring(slash+1));
        }
    }
}
