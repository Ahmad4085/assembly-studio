# Assembly Studio Logo

| Field | Value |
|---|---|
| Document | Logo kit and usage notes |
| Version | 1.0 |
| Last updated | 2026-11-04 |
| Task | TASK-118 (concept C "Bracket step", chosen by the user) |

<p><img src="assembly-studio-icon.svg" alt="Assembly Studio app icon" width="96"> &nbsp; <img src="assembly-studio-symbol.svg" alt="Assembly Studio symbol" width="96"></p>

## Idea
NASM's memory brackets `[ ]` around the debugger's execution arrow ▶: stepping through what happens inside the machine. The
brackets are the app's blue; the arrow is the yellow execution arrow you follow in the editor.

## Files
| File | Use |
|---|---|
| `assembly-studio-symbol.svg` | The symbol in colour, transparent background; for 48 px and larger |
| `assembly-studio-symbol-small.svg` | Small-size cut (heavier brackets, larger arrow) for 16–32 px |
| `assembly-studio-symbol-black.svg`, `-white.svg` | One-colour versions for single-colour printing and dark or photo backgrounds |
| `assembly-studio-icon.svg` | App-icon tile (48 px and up) |
| `assembly-studio-icon-small.svg` | App-icon tile for 16–32 px (uses the small-size cut) |
| `assembly-studio.ico` | Windows icon: 16, 24, 32, 48, 64, 128 and 256 px (PNG entries); the app's executable, window and installer use a copy in `src/AssemblyStudio.App/Assets/` |
| `png/assembly-studio-icon-*.png` | The icon as PNG at 16–256 px |

## Colours
| Role | Hex | RGB | Token in the app |
|---|---|---|---|
| Brackets | `#5AA2FF` | 90, 162, 255 | `Accent.Primary` |
| Arrow | `#F5C542` | 245, 197, 66 | `Accent.ExecutionArrow` |
| Tile | `#1B1C1F` | 27, 28, 31 | `Surface.Window` |
| Tile edge | `#3A3D44` | 58, 61, 68 | — |

## Name
With the symbol, write **Assembly Studio** in IBM Plex Sans SemiBold, as live text; the symbol is about the height of the
text's capitals plus a quarter. The name is not drawn as an outlined lockup yet; the website and README set it as text.

## Rules
- **Clear space:** keep at least the width of one bracket's serif (a quarter of the symbol's width) free around the symbol.
- **Minimum size:** 16 px, with the small-size cut below 48 px.
- **Backgrounds:** the colour symbol on dark backgrounds or the dark tile; on light backgrounds use the tile or the black version.
- **Don't:** recolour the brackets and arrow differently, rotate or flip the arrow (it always points right), add effects or
  gradients, or redraw the brackets as `< >` or `( )`.

## Construction
256-unit grid. Brackets 32 units wide with 30-unit serifs; the arrow's edges run at exactly ±30°, and it is centred on its
centroid so it sits optically in the middle. The small-size cut uses 44-unit brackets and 40-unit serifs.

## Notes
- **Trademark:** the mark has not been checked against existing trademarks; a professional search is advised before
  registering it.
- **Font:** IBM Plex is under the SIL Open Font License, which allows logo use.
