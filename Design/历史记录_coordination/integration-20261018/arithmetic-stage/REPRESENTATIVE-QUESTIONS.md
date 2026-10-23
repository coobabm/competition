# C5 representative mathematics and boundaries

These are the supported template families, not claims of C# runtime execution. Generated coefficients range from 2 to 9 and powers from 2 to 4.

| Tier | Displayed true claim | Reason / boundary |
|---|---|---|
| 1 | `2+3=5` | Addition |
| 1 | `2−3=−1` | Negative results remain valid |
| 1 | `2×3=6` | Multiplication |
| 1 | `2÷4=1/2` | Exact rational comparison; `2/4` equals `1/2`; divisor never zero |
| 1 | `(2+3)×4=20` | Parentheses first |
| 1 | `2+3×4=14` | Multiplication before addition; 20 is a false claim here |
| 2 | `(-3)^2=9` | Parenthesized negative base |
| 2 | `−3^2=−9` | Power before the leading minus; no mistaken `(-3)^2` parse |
| 2 | `√9=3` | Principal nonnegative square root; `−3` is false, though x²=9 would have two roots |
| 2 | `√((-3)²)=3` | √(x²)=|x|; not x for negative x |
| 3 | `lim[x→3] (x²−9)/(x−3)=6` | Two-sided real limit; quotient undefined exactly at 3, valid cancellation away from 3 |
| 3 | `[d(2x^3)/dx]x=4: 96` | Derivative of the named polynomial evaluated at the named point |
| 3 | `∫[0,3] 2x^2 dx=18` | Definite integral with stated bounds, continuous integrand |
| 3 | `∫ 2x^2 dx=(2/3)x^3+C` | Arbitrary real C; all antiderivatives, coefficient equality by exact rational value |

Each false generated claim offsets the expected numeric result/coefficient by +1 or −1. The integral family retains `+C` in both true and false claims; truth compares its nonconstant coefficient, so a missing constant cannot accidentally count as a full antiderivative family. No free-form response parser or symbolic-equivalence solver is exposed to players. No multi-valued root-solving problem is generated. No claim asks to evaluate a quotient at its zero denominator.

There is deliberately no stage-price multiplier and no additional payment for a harder formula, an AI leave, or a golden mole. Those economic rules were not authorized to change.
