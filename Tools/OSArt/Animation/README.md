# OSArt / Animation

Our own animation data for the desktop UI (clips + animator controllers that used to be DreamOS data).

* `unityyaml.py`   minimal Unity-YAML reader (values stay raw strings).
* `animlib.py`     spec <-> YAML: `parse_clip`, `parse_controller`, `emit_clip`, `emit_controller`.
* `extract_spec.py ROOT [OUT.json] [META_ROOT]`  writes the data contract of every `.anim` / `.controller` to
  `spec/os_anim_spec.json` (names, lengths, loop flag, bindings with path/attribute/class/script/type ids,
  start and end values, timing waypoints = hold edges + turning points, events, controller layers / parameters /
  states / motions / transitions). `.meta` files are only read for the GUIDs.
* `gen_anim.py OUT_ROOT`  re-emits everything from the spec. Curves are authored here: ease-out cubic for "In/On/Next"
  clips, ease-in cubic for "Out/Hide/ToInactive" clips, ease-out sine for hover transitions, zero-slope sine-like
  waves (periodic through the wrap point) for loops, linear for steady spins, stepped for booleans (m_IsActive,
  m_Interactable ...). Keys carry explicit free tangents. Controllers get our own object IDs and a grid layout.
  Overwrite in place: `python3 gen_anim.py "../../../Assets/LingGuangV05/DesktopBridge/OSArt/Animations"`.
* `selfcheck.py ORIG_ROOT NEW_ROOT [Assets]`  re-parses every output and asserts the contract equals the originals
  (also checks bindings, editor curves, numeric range, fileID references, code string names).
* `test_synthetic.py` round trip for sprite curves, events and transitions (not used by the DreamOS set).

Easing per clip is the `easing` field of the spec (`in`, `out`, `neutral`, `loop`); edit the JSON to change a clip.
No Unity is needed; the tools are plain Python 3.
