# Visual raster handoff

Apply visual-raster.patch (adds XgVisualRaster.cs and updates four existing source files plus credits). Unity should create metadata on import. No main files were edited.

Implemented:
- mnist_write pack now renders original project stroke paths into real 28x28 grayscale, 16 deterministic variants per digit; the actual question displays a 224x224 pixel grid. Four choices moved to the right to avoid covering the image. Drawing-yourself mode remains.
- captcha pack alternates four-character alphanumeric image entry with its existing nine-image selection. 256x80 native image, deterministic warped glyphs and light interference; physical keyboard, on-screen character keys, backspace and explicit submit. AI answer remains unlimited with its existing technology gate.
- Saved question metadata reproduces these original images in existing question/marked-sheet paths. New image kinds preserve legacy 8x8 digit behavior. The 8x8 demonstration model explicitly declines handwriting28 instead of drawing mismatching attribution; its original tests remain valid.
- No external photographs or MNIST data were acquired. Existing six photo classes remain six. Full 8x10 photo collection, five images/class ImageNet desk, and new-image attention model are NOT complete.

Validation:
- Core and Desktop independent compiler passed initially; refreshed Core after handwritten stroke refinement passed.
- 907 deterministic raster assertions passed against refreshed compiled Core (160 digit samples, 128 mixed captchas, all 32 glyphs, exact redraw, unsupported heat guard).
- procedural-preview.png visually reviewed: all ten digits recognizable; captcha characters visible with mild warping. This contact sheet is not a Unity UI screenshot.
- Need Unity interaction pass: mnist_write 224x224 image + four right-side choices + own-drawing return; captcha type exact code via screen keys and keyboard -> submit -> next-frame nine-grid; reopening notebook reproduces input image; check English layout and hidden window keyboard guard.
- Compilation outputs and refout explicitly stay in this staging directory.
