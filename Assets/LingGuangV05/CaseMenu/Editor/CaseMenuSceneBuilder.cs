#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LingGuangV05.CaseMenu.Editor
{
    public static class CaseMenuSceneBuilder
    {
        const string Art = "Assets/LingGuangV05/CaseMenu/Art";

        [MenuItem("Tools/LingGuang/Build Computer Case Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building the menu.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save all open scenes before building; no unsaved scene is discarded.");
            var lcd = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/NativeLCD.prefab");
            if (lcd == null) throw new InvalidOperationException("The preserved native LCD prefab is missing.");
            foreach (string poster in new[] { "ThreeBody-v2.png", "Turing-v2.png", "VonNeumann.png" })
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Posters/" + poster) == null)
                    throw new InvalidOperationException("Import the required poster before rebuilding: " + poster);
            Directory.CreateDirectory(Art + "/Materials");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var deskSet = CaseMenuModelBuilder.Build(null);
            CaseMenuDecorBuilder.Build(deskSet.transform);

            var monitor = (GameObject)PrefabUtility.InstantiatePrefab(lcd);
            monitor.name = "Native HongmengOS Monitor";
            monitor.transform.SetPositionAndRotation(new Vector3(.46f, .725f, .40f), Quaternion.identity);
            monitor.transform.localScale = Vector3.one * .25f;
            var displayRig = new GameObject("Live Main Display").transform;
            displayRig.SetPositionAndRotation(monitor.transform.position, Quaternion.identity);
            displayRig.localScale = Vector3.one * .25f;
            var center = new GameObject("Monitor Screen Center").transform;
            center.SetParent(monitor.transform, false);
            center.localPosition = new Vector3(0, 0, .028f);
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "LCD Display";
            screen.transform.SetParent(center, false);
            screen.transform.localPosition = new Vector3(0, 0, -.002f);
            screen.transform.localScale = new Vector3(5.12f, 2.88f, 1);
            UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
            var screenMat = Material("LCD Standby", "Universal Render Pipeline/Unlit", Color.black);
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;
            screen.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            var labelObject = new GameObject("Standby Message", typeof(RectTransform), typeof(TextMeshPro));
            labelObject.transform.SetParent(center, false);
            labelObject.transform.localPosition = new Vector3(0, 0, -.012f);
            var text = labelObject.GetComponent<TextMeshPro>();
            text.font = font; text.fontSize = 2.2f;
            text.text = "hongmengos\n<size=42%><color=#617E89>2016  /  STANDBY</color></size>";
            text.color = new Color(.37f, .54f, .60f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(4.8f, 1.2f);
            text.raycastTarget = false;
            labelObject.SetActive(false);

            var cameraObject = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(-1.65f, 1.25f, -2.50f);
            camera.transform.LookAt(new Vector3(-.18f, .78f, .10f));
            camera.fieldOfView = 43; camera.nearClipPlane = .025f; camera.farClipPlane = 35;
            camera.backgroundColor = new Color(.018f, .026f, .030f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true; camera.allowMSAA = true;
            var additional = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            additional.renderPostProcessing = true;
            additional.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            additional.antialiasingQuality = AntialiasingQuality.High;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.12f, .16f, .21f);
            RenderSettings.ambientEquatorColor = new Color(.06f, .074f, .09f);
            RenderSettings.ambientGroundColor = new Color(.025f, .026f, .033f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.032f, .038f, .047f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .022f;
            var lighting = new GameObject("Menu Lighting").transform;
            AddLight(lighting, "Warm desk key", LightType.Spot, new Vector3(-1.40f, 2.0f, -1.4f),
                new Vector3(-.50f, .40f, .10f), new Color(1f, .76f, .50f), 6.0f, 7f, 68f, true);
            AddLight(lighting, "Cool window rim", LightType.Spot, new Vector3(.8f, 1.9f, 1.0f),
                new Vector3(-.70f, .45f, .05f), new Color(.38f, .60f, 1f), 7f, 6f, 85f, true);
            AddLight(lighting, "Front soft fill", LightType.Point, new Vector3(.20f, .9f, -1.8f),
                Vector3.zero, new Color(.68f, .81f, 1f), .6f, 5f, 0, false);
            AddLight(lighting, "Desk lamp pool", LightType.Point, new Vector3(-1.46f, .771f, .356f),
                Vector3.zero, new Color(1f, .65f, .33f), .50f, 2.2f, 0, false);
            AddLight(lighting, "Poster Wall Fill", LightType.Spot, new Vector3(0, 1.8f, -.3f),
                new Vector3(0, 1.48f, 1.4f), new Color(.95f, .89f, .78f), 1.6f, 4f, 90f, false);
            CreateVolume();

            var eventSystem = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var controllerObject = new GameObject("Computer Case Menu");
            var controller = controllerObject.AddComponent<CaseMenuController>();
            controller.menuCamera = camera;
            controller.startButton = ConfigureButton("StartButton", "StartKeycap", "StartRing", true, new Color(.16f, .70f, 1f));
            controller.settingsButton = ConfigureButton("SettingsButton", "SettingsKeycap", "SettingsRing", false, new Color(1f, .62f, .26f));
            controller.previewMonitor = monitor; controller.displayRig = displayRig;
            controller.monitorCenter = center; controller.font = font;
            controller.hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LingGuangV05/CaseMenu/Art/Audio/Hover.wav");
            controller.pressClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LingGuangV05/CaseMenu/Art/Audio/SwitchPress.wav");
            controller.releaseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LingGuangV05/CaseMenu/Art/Audio/SwitchRelease.wav");
            controller.bootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LingGuangV05/CaseMenu/Art/Audio/Boot.wav");
            controller.fanClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "/Audio/FanLoop.wav");

            var scenes = EditorBuildSettings.scenes.Where(x => x.path != CaseMenuController.ScenePath && x.path != Desktop.MainSceneEntry.ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(Desktop.MainSceneEntry.ScenePath, true));
            scenes.Insert(0, new EditorBuildSettingsScene(CaseMenuController.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, CaseMenuController.ScenePath)) throw new IOException("Could not save ComputerCaseMenu scene.");
            Selection.activeGameObject = controllerObject;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) sceneView.LookAt(new Vector3(-.15f, .5f, 0), camera.transform.rotation, 2.4f);
            Debug.Log("ComputerCaseMenu built: real Main preload, two physical switches, continuous camera and persistent settings.");
        }

        static CaseMenuButton ConfigureButton(string name, string cap, string ring, bool start, Color color)
        {
            var go = GameObject.Find(name);
            if (go == null) throw new InvalidOperationException("Model missing " + name);
            var button = go.AddComponent<CaseMenuButton>();
            button.keycap = go.GetComponentsInChildren<Transform>(true).First(t => t.name == cap);
            button.ring = go.GetComponentsInChildren<Renderer>(true).First(t => t.name == ring);
            button.accent = color; button.isStart = start;
            var hit = go.AddComponent<BoxCollider>();
            hit.size = new Vector3(.19f, .135f, .10f); hit.center = new Vector3(0, 0, -.012f); hit.isTrigger = true;
            var light = new GameObject("Switch Feedback Light", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(go.transform, false); light.transform.localPosition = new Vector3(0, 0, -.06f);
            light.type = LightType.Point; light.color = color; light.range = .35f; light.intensity = .04f;
            button.feedbackLight = light;
            var line = new GameObject("Confirm Pulse", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(go.transform, false); line.transform.localPosition = new Vector3(0, 0, -.033f);
            line.useWorldSpace = false; line.loop = true; line.positionCount = 48;
            line.widthMultiplier = .004f;
            float radius = start ? .066f : .049f;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            }
            var material = Material("Switch Pulse", "Universal Render Pipeline/Particles/Unlit", Color.white);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            line.sharedMaterial = material; line.enabled = false;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            button.pulse = line;
            return button;
        }

        static Material Material(string name, string shaderName, Color color)
        {
            string path = Art + "/Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(Shader.Find(shaderName)); AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", color); EditorUtility.SetDirty(result);
            return result;
        }

        static void AddLight(Transform parent, string name, LightType type, Vector3 position, Vector3 target, Color color, float intensity, float range, float spot, bool shadows)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent); light.transform.position = position;
            if (type != LightType.Point) light.transform.LookAt(target);
            light.type = type; light.color = color; light.intensity = intensity; light.range = range;
            if (type == LightType.Spot) { light.spotAngle = spot; light.innerSpotAngle = spot * .65f; }
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowBias = .025f; light.shadowNormalBias = .05f;
        }

        static void CreateVolume()
        {
            const string path = Art + "/Menu Volume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            if (!profile.TryGet<Bloom>(out var bloom)) { bloom = profile.Add<Bloom>(true); AssetDatabase.AddObjectToAsset(bloom, profile); }
            bloom.threshold.Override(1.2f); bloom.intensity.Override(.12f); bloom.scatter.Override(.45f);
            // Keep close-up metal highlights out of the wide bloom convolution.
            bloom.clamp.Override(2f);
            if (!profile.TryGet<Vignette>(out var vignette)) { vignette = profile.Add<Vignette>(true); AssetDatabase.AddObjectToAsset(vignette, profile); }
            vignette.intensity.Override(.22f); vignette.smoothness.Override(.38f);
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, profile); }
            // The native desktop pipeline already defines a tone profile. This scene deliberately
            // overrides it rather than stacking a second HDR/neutral response on the physical props.
            tone.mode.Override(TonemappingMode.None);
            EditorUtility.SetDirty(profile);
            var volume = new GameObject("Menu Post Processing", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true; volume.priority = 5; volume.sharedProfile = profile;
        }
    }
}
#endif
