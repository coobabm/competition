hongmengos desktop — team handoff / 2026-10-03

Unity: 6000.4.5f1 (source Editor)
Open: Assets/HongmengOS/Scenes/HongmengOS Desktop.unity
This is the handoff scene, not the unmodified DreamOS prefab/demo scene.

Required Package Manager packages:
com.unity.inputsystem 1.19.0
com.unity.ugui 2.0.0
com.unity.nuget.newtonsoft-json 3.2.2
com.unity.render-pipelines.universal 17.4.0

Player Settings > Active Input Handling: Input System Package (New) or Both.
For the supplied render setup, assign Assets/HongmengOS/Rendering/HongmengOS Pipeline.asset
in Graphics Settings (and remove/replace any Quality-level pipeline override in a NEW test project).
Do not blindly replace existing project settings in a teammate's production project.

The handoff scene uses a generic Player account and has no startup password or setup step.
No source local DreamOS_UserData, accounts, saves, credentials, Library or MCP packages are bundled.

Included changes:
- Lucide icons; desktop icons 78 x 78.
- Full-viewport monitor bezel and hongmengos branding.
- Settings pinned in taskbar; other apps appear only while open.
- About support/social-link blocks hidden.
- Left/right mouse click SFX (volume scale 0.45); no keyboard or hover SFX.
- Dedicated HongmengOS UI Audio config; adjust AudioManager > Mouse Click Volume.

Import into a new project/branch first. The DreamOS files retain their original paths and GUIDs;
importing into a project with DreamOS can overwrite existing DreamOS files.
Keep the licenses, original attribution, and use this package only within your appropriately licensed team.
The original DreamOS asset license is not replaced by the Lucide license.
This is an editable Unity source package, not a Windows/macOS executable.
