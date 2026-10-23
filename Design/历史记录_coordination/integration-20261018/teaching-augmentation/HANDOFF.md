# Actual handwriting augmentation handoff

Apply teaching-augmentation.patch by hunks. Photo/Pictures diff is against the already integrated photo-library batch. Does not modify Swipe tools or Answer logic. New Core file + Unity meta required. Copy TeachingAugmentationInteractionChecks.cs into Assets/LocalBuild/Editor for runtime checks.

- Real 28-square transforms: shift one pixel right, rotate180, mirror horizontally, dim to65%. Shown source seed normalized identically.
- Free ShiftProbe after label.ai: actual recognizer before/after; only real correct-to-wrong loss triggers shift stage quiz. No timer-based imitation.
- Augment purchase opens four keep/discard variants. Keeping actually teaches their pixels under the original digit, including wrong labels. Submission single-use/owner gated, normal factual verdict/payout path only.
- Uses actual recognizer for normal digit answers. Active child lookup avoids deferred-destroy stale AiAnswer.
- Small native28 textures; handwriting saved kind restored and excluded from unrelated legacy8×8 heatmap.
- Question elapsed time continues during augmentation choices; keyboard answering pauses until commit.
- Conservative exercise rule currently accepts shift/dim for all digits, rotation/mirror only0,1,8. Handwritten1 can be stylistically ambiguous;6/9 fixtures explicitly demonstrate changed labels. This is a teaching rule rather than semantic OCR truth for every imaginable handwriting style.

Verification: Core, Desktop, QA C# compilation succeeded. Pure-core 1898 assertions passed, including immutable transform pixels, true learned raster memory, foreign/duplicate rejection, save roundtrip, all160 actual before/after predictions.120/160 originals recognized; actual one-pixel losses4 seed10→9 and9 seed3→4. Preview augmentation-preview.png visually inspected. Runtime QA not run by this agent: parent owns Unity. QA exercises real buttons and checks actual textures, valid discard and wrong keep-all decisions.

No main workspace files or Library/Bee binaries modified by this agent.
