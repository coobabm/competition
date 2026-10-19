#!/usr/bin/env python3
"""Writes the DreamOS-folder localization assets (7 tables, 3 languages, settings) from locdata_*.py.

Keys, table IDs, language IDs and every GUID are fixed contracts; only the values are ours. The .meta files stay as they are.
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from locdata_ui import UI
from locdata_misc import GAMEHUB, MAIL, NOTEPAD, BROWSER, WIDGET
from locdata_chat import MESSAGING

LOC = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..",
                                    "Assets", "LingGuangV05/DesktopBridge/OSArt", "Localization"))
SETTINGS_GUID = "306a0ca089807d34787bf5a5b674abee"
TABLE_SCRIPT = "c7243e79ffe3c1d4192600a08c44f8d0"
LANG_SCRIPT = "741ce2ace64af9a449fd306c0372eba2"
SETTINGS_SCRIPT = "f9f512c8b124a07408070efd128c9497"
# Order = order in the settings asset and in every language asset.
TABLES = [  # (tableID, file name, table asset guid, rows)
    ("UI", "UI", "3a533ebcf12a719498067d0c32240e88", UI),
    ("Game Hub", "Game Hub", "5be61a8f2561b1b459eeba0252755c94", GAMEHUB),
    ("Mail", "Mail", "f6e6b4aaa6f2f004e915a560d310d6a5", MAIL),
    ("Messaging", "Messaging", "c426ec06101062a4dbc9ef60778cd378", MESSAGING),
    ("Notepad", "Notepad", "77e4b89821ef6914c86f04674a1d6b5b", NOTEPAD),
    ("Web Browser", "Web Browser", "931c4ecb0810e844098897ad26a2de6c", BROWSER),
    ("Widget", "Widget", "f0c6e838ab9555c409da2eb645d16de2", WIDGET),
]
# (column, asset name, languageID, languageName, localizedName, guid)
LANGS = [
    (1, "English (en-US)", "en-US", "English", "English (US)", "86147c30731746f4cbbce63c6b15ead7"),
    (2, "German (de-DE)", "de-DE", "German", "Deutsch", "3fce77647edd54a4d8e473b1ab43162d"),
    (3, "Turkish (tr-TR)", "tr-TR", "Turkish", "Türkçe", "4e26f7f1322fb5d4eac1bd5279162d93"),
]
HEAD = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n" \
       "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n" \
       "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"

def q(s):
    out = '"'
    for ch in s:
        o = ord(ch)
        if ch == '"': out += '\\"'
        elif ch == "\\": out += "\\\\"
        elif ch == "\n": out += "\\n"
        elif o < 32 or o > 126: out += ("\\x%02X" % o) if o <= 0xFF else ("\\u%04X" % o)
        else: out += ch
    return out + '"'

def check_keys():
    seen = {}
    for tid, _, _, rows in TABLES:
        keys = [r[0] for r in rows]
        assert len(keys) == len(set(keys)), "duplicate key in " + tid
        for r in rows:
            assert len(r) == 4 and all(isinstance(x, str) and x for x in r), r
        # keep the existing tables authoritative: keys must match what is on disk
        path = os.path.join(LOC, "Tables", tid + ".asset")
        if os.path.exists(path):
            disk = [l.split("key: ", 1)[1].rstrip("\n") for l in open(path, encoding="utf-8") if l.startswith("  - key: ")]
            assert disk == keys, "key mismatch in table " + tid
        seen[tid] = keys

def write(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

def main():
    check_keys()
    for tid, fname, guid, rows in TABLES:
        t = HEAD + "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: %s\n  m_EditorClassIdentifier: \n" % (TABLE_SCRIPT, tid)
        t += "  tableID: %s\n  localizationSettings: {fileID: 11400000, guid: %s, type: 2}\n  tableContent:\n" % (tid, SETTINGS_GUID)
        t += "".join("  - key: %s\n" % r[0] for r in rows)
        write(os.path.join(LOC, "Tables", fname + ".asset"), t)
    for col, name, lid, lname, loc, guid in LANGS:
        t = HEAD + "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: %s\n  m_EditorClassIdentifier: \n" % (LANG_SCRIPT, name)
        t += "  localizationSettings: {fileID: 11400000, guid: %s, type: 2}\n  languageID: %s\n  languageName: %s\n  localizedName: %s\n  tableList:\n" % (
            SETTINGS_GUID, lid, lname, q(loc) if not loc.isascii() else loc)
        for tid, fname, tguid, rows in TABLES:
            t += "  - table: {fileID: 11400000, guid: %s, type: 2}\n    tableContent:\n" % tguid
            for r in rows:
                t += "    - key: %s\n      value: %s\n      audioValue: {fileID: 0}\n      spriteValue: {fileID: 0}\n" % (r[0], q(r[col]))
        write(os.path.join(LOC, "Languages", name + ".asset"), t)
    s = HEAD + "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: Localization Settings\n  m_EditorClassIdentifier: \n  languages:\n" % SETTINGS_SCRIPT
    for col, name, lid, lname, loc, guid in LANGS:
        s += "  - languageID: %s\n    languageName: %s\n    localizedName: %s\n    localizationLanguage: {fileID: 11400000, guid: %s, type: 2}\n    isExpanded: 0\n" % (
            lid, lname, q(loc) if not loc.isascii() else loc, guid)
    s += "  tables:\n"
    for tid, fname, tguid, rows in TABLES:
        s += "  - tableID: %s\n    localizationTable: {fileID: 11400000, guid: %s, type: 2}\n    isExpanded: 0\n" % (tid, tguid)
    s += "  defaultLanguageID: English (en-US)\n  defaultLanguageIndex: 0\n  enableExperimental: 0\n"
    write(os.path.join(LOC, "Localization Settings.asset"), s)
    print("wrote %d tables, %d languages (%d keys each) and settings" % (len(TABLES), len(LANGS), sum(len(t[3]) for t in TABLES)))

if __name__ == "__main__":
    main()
