# Editor PlayerPrefs raw restore — completed

2026-10-19 13:50 UTC. Scope: current-user `Software\Unity\UnityEditor\DefaultCompany\competition`, bound to `competition-latest-20261016/Build/MyComputerAlive-2026.10.19.1-20261019-115400/build-result.json`. PID 53032 was confirmed exited before restoring. No Unity process was started, production C# edited, normal game-save file changed, or unrelated registry location inspected.

## Finding

`LingGuang.TechZoom_h2252391024` is genuinely stored as Win32 `REG_DWORD` (type 4), but its payload is **8 bytes**: `00-00-00-00-00-00-F0-3F`. A direct RegQueryValueExW read confirmed both type and size. The schema-1 snapshot stored it as kind DWord with a managed Int64 value `4607182418800017408`; its little-endian byte representation exactly matches the raw registry payload. The prior guard incorrectly cast all DWord values to Int32 and failed. Converting this value to REG_QWORD would change its original registry type and was not done.

The original snapshot contains 20 values: 9 Binary and 11 DWord (10 managed Int32, one managed Int64). These existing snapshot values can all be reconstructed losslessly as bytes. No unrelated value contents are printed in this report.

## Fix and verification

- `EditorPrefs-Guard.ps1` now reads/compares raw Win32 type, byte length and bytes using the adjacent `EditorPrefs-RawRegistry.cs` helper. RegSetValueExW writes the recorded type and exact byte count without managed RegistryKey.SetValue conversion.
- Before any mutation, all original schema-1 values are validated and converted. Unsupported legacy types fail before mutation. New schema-2 snapshots directly preserve raw types/bytes and need no legacy conversion.
- Identity checks still bind SID, exact leaf key and build-result SHA256. The guard refuses unexpected subkeys, snapshot overwrite, or Restore while the recorded PID is running. It changes only differing values and removes additions only within the specified leaf.
- Original script retained as `EditorPrefs-Guard.before-raw-fix.ps1`; original `editor-prefs-before-visual.clixml` was not modified.
- Restore completed at `2026-10-19T13:50:14.1798822Z`: **5 values written**, **0 additional values removed** (the earlier partial restore had already removed additions). Result: `restored=true`.
- A separate Compare against the original snapshot returned **unchanged=true, 20/20 values**. Evidence: `editor-prefs-restore-raw-result.json` and `editor-prefs-compare-raw-result.json`.
- Created requested `editor-prefs-before-visual-2.clixml` using schema 2; separate Compare again returned **unchanged=true, 20/20**. TechZoom in the new snapshot remains type 4 and 8 bytes with the exact payload above.

Parent was notified that restoration and the second-run snapshot are complete. This subagent did not launch the next visual run.
