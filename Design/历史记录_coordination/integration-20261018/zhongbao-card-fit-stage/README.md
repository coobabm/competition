# Zhongbao contract card text fit

Candidate only; production untouched. Two patch paths: one product layout file and an evidence-only addition to the existing actual UI check. No new test and no relaxed assertion.

## Actual failure evidence

Source report: `competition-latest-20261016/LocalVerification/SidebarLayout/20261018-223810-133/report.json`.

Reviewed screenshot: `zhongbao-business-failure.png` in that report's directory (1280×720 actual LCD capture; the suite sets the native business window to 960×600). The Chinese flow had signed/reloaded and exercised teaching plus all five routes; its first card fit assertion failed at `Rowcheque Accuracy no overflow, ellipsis or truncation`. The English loop had not yet run. This is not a bilingual pass or a new business-logic failure.

The screenshot shows the intended three-column business cards, ink/green accuracy text and green progress bars, signed stamps, income/advance and signing controls. Tiny screen pixels cannot establish TMP's exact preferred extent. The old report records no preferred-width/height values, so the precise first overflowing dimension is not claimed.

## Source layout measurements and change

| Element | Baseline (logical units) | Candidate (logical units) |
|---|---|---|
| Card | 174 high, three columns | 198 high, same three columns |
| Accuracy | fixed x12–162 = 150 wide, y58–74 = 16 high | x12 to right inset12, y56–80 = 24 high |
| Progress bar | same line, x168 to right inset12, y63–69 | separate full-width line, y84–90 |
| Note | y80–106 | y94–120 |
| Income / advance | y108–132, 24 high | y124–150, 26 high |
| Signing control | bottom inset12, 28 high | unchanged bottom inset12, 28 high |

At the authored 1266-wide root, page width is 1110 (left144/right12); each card is 362 wide, its inner area360, so Accuracy grows from150 to336. At larger available widths it follows the card anchors. UiFitScale scales the authored root into the actual window; it did not remove the former fixed150×16 local constraint. Extra card height flows through the existing rows-height and ScrollRect calculations, keeping full cards reachable.

Both languages retain all accuracy values, threshold values, text, font sizes, business colors, three-column arrangement and signing controls. No font reduction, autosizing, truncation, copy shortening, business rule, data, save or income change. The progress bar uses the same value/color logic and simply receives its own line.

The existing Fits check now records actual font, size, rect, preferred dimensions, scale and overflow/truncation flags for every checked label. Its assertions are unchanged; the next real run supplies measured font/layout evidence for both locales, including any later failure.

## Verification boundary

Static delimiter/baseline packaging and `git apply --check` passed. No Unity, compiler, tests, Player, model or audio run. Root must rerun the existing bilingual960×600 actual suite and review its screenshots/metrics; this stage contains no fabricated after screenshot or UI-pass claim.
