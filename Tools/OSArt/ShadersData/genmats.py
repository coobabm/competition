#!/usr/bin/env python3
"""Re-emits DreamOS's blur materials, the demo ground material and the two render textures as clean Unity YAML.

Usage: python3 genmats.py   (run from anywhere; paths are resolved from this file)
Shader GUIDs, names and sizes are fixed contracts - see README.md. The .meta files are never touched.
"""
import os, re

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..",
                                     "Assets", "LingGuangV05/DesktopBridge/OSArt"))
FAST = "26a8b63f33f582f4685e7d7c2a6a7336"    # DreamOS/UI Blur Fast
MASKED = "04c1e11819e88034588a3a1e5bc20962"  # DreamOS/UI Blur Masked

HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"

def tex(name):
    return ("    - %s:\n        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n"
            "        m_Offset: {x: 0, y: 0}\n") % name

def blur_material(name, guid, size):
    fast = guid == FAST
    texs = ["_MainTex"] + (["_BumpMap"] if fast else [])
    floats = [("_ColorMask", 15), ("_Size", size), ("_Stencil", 0), ("_StencilComp", 8), ("_StencilOp", 0),
              ("_StencilReadMask", 255), ("_StencilWriteMask", 255)]
    if fast:
        floats.append(("_BumpAmt", 0))          # glass refraction off: plain frosted Aero blur
        floats.sort()
    out = HEADER + "--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n"
    out += "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
    out += "  m_Name: %s\n  m_Shader: {fileID: 4800000, guid: %s, type: 3}\n  m_Parent: {fileID: 0}\n" % (name, guid)
    out += "  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n"
    out += "  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n"
    out += "  stringTagMap: {}\n  disabledShaderPasses: []\n  m_LockedProperties: \n  m_SavedProperties:\n"
    out += "    serializedVersion: 3\n    m_TexEnvs:\n" + "".join(tex(t) for t in sorted(texs))
    out += "    m_Ints: []\n    m_Floats:\n" + "".join("    - %s: %s\n" % (k, fmt(v)) for k, v in floats)
    out += "    m_Colors:\n"
    if fast:
        out += "    - _Color: {r: 0.96, g: 0.98, b: 1, a: 0}\n"   # faint cool glass tint; alpha unused
    else:
        out += "    []\n" if False else ""
    out += "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n"
    if not fast:
        out = out.replace("    m_Colors:\n  m_BuildTextureStacks", "    m_Colors: []\n  m_BuildTextureStacks")
    return out

def fmt(v):
    return repr(v) if isinstance(v, float) and v != int(v) and abs(v) < 1e6 and len(repr(v)) < 12 else ("%g" % v)

def ground_material():
    texs = ["_BaseMap", "_BumpMap", "_DetailAlbedoMap", "_DetailMask", "_DetailNormalMap", "_EmissionMap",
            "_MetallicGlossMap", "_OcclusionMap", "_ParallaxMap", "_SpecGlossMap"]
    floats = [("_AddPrecomputedVelocity", 0), ("_AlphaClip", 0), ("_AlphaToMask", 0), ("_Blend", 0),
              ("_BlendModePreserveSpecular", 1), ("_BumpScale", 1), ("_ClearCoatMask", 0), ("_ClearCoatSmoothness", 0),
              ("_Cull", 2), ("_Cutoff", 0.5), ("_DetailAlbedoMapScale", 1), ("_DetailNormalMapScale", 1),
              ("_DstBlend", 0), ("_DstBlendAlpha", 0), ("_EnvironmentReflections", 1), ("_GlossMapScale", 1),
              ("_Glossiness", 0.15), ("_GlossyReflections", 1), ("_Metallic", 0), ("_OcclusionStrength", 1),
              ("_Parallax", 0.02), ("_QueueOffset", 0), ("_ReceiveShadows", 1), ("_Smoothness", 0.15),
              ("_SmoothnessTextureChannel", 0), ("_SpecularHighlights", 1), ("_SrcBlend", 1), ("_SrcBlendAlpha", 1),
              ("_Surface", 0), ("_WorkflowMode", 1), ("_XRMotionVectorsPass", 1), ("_ZWrite", 1)]
    # cool slate floor for the Game Hub 3D test scene
    c = (0.16, 0.19, 0.24)
    out = HEADER + "--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n"
    out += "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
    out += "  m_Name: Ground\n  m_Shader: {fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}\n  m_Parent: {fileID: 0}\n"
    out += "  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n"
    out += "  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n"
    out += "  stringTagMap:\n    RenderType: Opaque\n  disabledShaderPasses:\n  - MOTIONVECTORS\n"
    out += "  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n"
    out += "".join(tex(t) for t in texs)
    out += "    m_Ints: []\n    m_Floats:\n" + "".join("    - %s: %s\n" % (k, fmt(v)) for k, v in floats)
    out += "    m_Colors:\n"
    out += "    - _BaseColor: {r: %g, g: %g, b: %g, a: 1}\n    - _Color: {r: %g, g: %g, b: %g, a: 1}\n" % (c + c)
    out += "    - _EmissionColor: {r: 0, g: 0, b: 0, a: 1}\n    - _SpecColor: {r: 0.2, g: 0.2, b: 0.2, a: 1}\n"
    out += "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n"
    out += ("--- !u!114 &7501528651615794859\nMonoBehaviour:\n  m_ObjectHideFlags: 11\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_Script: {fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}\n  m_Name: \n"
            "  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion\n"
            "  version: 10\n")
    return out

def render_texture(name, color_format, srgb):
    # size / depth / format are bound to cameras and RawImages: 1920x1080, depth-stencil 92
    return (HEADER + "--- !u!84 &8400000\nRenderTexture:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            "  m_Name: %s\n  m_ImageContentsHash:\n    serializedVersion: 2\n    Hash: 00000000000000000000000000000000\n"
            "  m_IsAlphaChannelOptional: 0\n  serializedVersion: 6\n  m_Width: 1920\n  m_Height: 1080\n"
            "  m_AntiAliasing: 1\n  m_MipCount: -1\n  m_DepthStencilFormat: 92\n  m_ColorFormat: %d\n"
            "  m_MipMap: 0\n  m_GenerateMips: 1\n  m_SRGB: %d\n  m_UseDynamicScale: 0\n  m_UseDynamicScaleExplicit: 0\n"
            "  m_BindMS: 0\n  m_EnableCompatibleFormat: 1\n  m_EnableRandomWrite: 0\n  m_TextureSettings:\n"
            "    serializedVersion: 2\n    m_FilterMode: 1\n    m_Aniso: 0\n    m_MipBias: 0\n    m_WrapU: 1\n"
            "    m_WrapV: 1\n    m_WrapW: 1\n  m_Dimension: 2\n  m_VolumeDepth: 1\n  m_ShadowSamplingMode: 2\n"
            % (name, color_format, srgb))

def write(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

def current(path, key):
    t = open(path, encoding="utf-8").read()
    return t

if __name__ == "__main__":
    mats = os.path.join(ROOT, "Materials")
    # (shader, initial _Size) per material; _Size is animated at runtime by UIBlur and reset to 0 on Awake.
    table = {
        "Blur_CalculatorBG": (FAST, 0), "Blur_CommanderBG": (FAST, 0), "Blur_ContextMenu": (MASKED, 0),
        "Blur_CoriBG": (MASKED, 2.170085), "Blur_EmailBG": (FAST, 0), "Blur_GameHubBG": (FAST, 0),
        "Blur_GameHubSlider": (MASKED, 3), "Blur_LockScreenBG": (FAST, 0), "Blur_MessagingBG": (FAST, 0),
        "Blur_MiniPlayerBG": (FAST, 3), "Blur_ModalWindowBG": (FAST, 0), "Blur_MusicPlayerBG": (FAST, 0),
        "Blur_NetworkCenterBG": (MASKED, 0), "Blur_NotepadBG": (FAST, 0), "Blur_NtfCenterBG": (MASKED, 0),
        "Blur_PhotoGalleryBG": (FAST, 0), "Blur_QuickCenterBG": (MASKED, 0), "Blur_ReminderBG": (FAST, 0),
        "Blur_SettingsBG": (FAST, 0), "Blur_TaskbarBG": (FAST, 5), "Blur_VideoPlayerBG": (FAST, 0),
        "Blur_VolumeMixerBG": (MASKED, 0), "Blur_WebBrowserBG": (FAST, 0), "Blur_WidgetLibraryBG": (FAST, 0),
    }
    for name, (guid, size) in table.items():
        write(os.path.join(mats, name + ".mat"), blur_material(name, guid, size))
    write(os.path.join(ROOT, "_Demo Assets", "Models", "Ground.mat"), ground_material())
    write(os.path.join(ROOT, "Textures", "Placeholders", "Game Hub", "3D Test Renderer.renderTexture"),
          render_texture("3D Test Renderer", 4, 1))
    write(os.path.join(ROOT, "Textures", "Rendering", "Video Player.renderTexture"),
          render_texture("Video Player", 8, 0))
    print("wrote %d blur materials + Ground + 2 render textures" % len(table))
