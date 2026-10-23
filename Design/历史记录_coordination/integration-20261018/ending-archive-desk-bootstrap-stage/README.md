# Archive shared desktop bootstrap

Minimal runner/check-only follow-up to the imported bootstrap-order patch. No product file changes and no weakened assertion.

The actual `EndingArchiveActual/20261018-224732-045` failure moved past bootstrap and view readiness to `ChoiceTransactions:136`. Its lifecycle-02 proves a live current Main runtime with prologue2/install2, prepared shell and registered custom view. The expression `PrologueDirector.Desk.layer` then dereferenced a null Desk.

`PrepareDesktopShell` installs PrologueDirector; its normal `Update -> BuildDesk` owns the shared LCD desktop. The earlier isolation froze this component before its first Update. The fix allows that component's normal lifecycle only after asserting the restored source is past its prologue and has no unplayed legacy ending. It waits for the real shared desk, then freezes the component again, asserting both the exact owner reference and current Main scene. It neither invokes private BuildDesk nor creates a substitute canvas, and keeps controller/economy/story/model isolation.

The choice suite now fails with an explicit owner/desk precondition instead of a bare NullReference. Lifecycle diagnostic snapshots include the PrologueDirector state and shared desk; one additional snapshot records successful initialization and refreeze. Original failed evidence is untouched.

Static packaging and clean-application checks passed at delivery. No Unity, compiler, tests, Player, model or audio was run. A real rerun remains required; this fixes the identified harness initialization omission, not a claimed product defect or completed archive pass.
