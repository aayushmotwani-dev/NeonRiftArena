# Visual Direction and Polish Record

## Design rule

**A mapped industrial training room that has been deliberately converted into an MR combat space.**

The project uses one controlled hierarchy instead of unrelated “sci-fi” decoration:

- Deep graphite and desaturated steel form the environment.
- Oxidized teal marks mapped, safe or player-owned systems.
- Safety amber marks interaction and physical infrastructure.
- Red is reserved for hostile arrivals, damage and the Guardian.
- Magenta was removed from the player palette to avoid the generic neon-prototype look.

## Authored visual decisions

- Repeating floor texture and structural inlays make the room read as a built place rather than a black void.
- Wall variants repeat on a six-segment rhythm so variation is intentional rather than random.
- Warm practical lights separate props from the cool deck.
- Player silhouette uses a cockpit, stabilisers, engines and a forward emitter instead of one primitive.
- Enemy arrival rings communicate danger before spawning.
- Enemy hit reactions combine colour flash, scale punch, particles and audio.
- The Guardian receives a dedicated health display.
- Menu and HUD use the same margins, borders, typography and semantic colours.

## Prototype presentation issues removed

- Inconsistent asset styles and unrestricted accent colours
- Placeholder primitives presented as final art
- Excessive all-caps slash-separated copy
- Unused feedback variables and effects without gameplay purpose
- Important enemies appearing without anticipation
- Low-contrast objects disappearing into the background
- Tiny text produced by scaling a fixed 1280×720 UI
- HUD labels placed directly against screen edges
- Generic default font with no documented licence
- Unverified standalone output

## Visual QA targets

- No text touches or crosses a panel boundary.
- Small HUD text remains at least 12 physical pixels high.
- Interface text uses a dark, stable backing panel and high foreground contrast.
- Teal, amber and red always carry the same meaning.
- Player, hostile projectiles and room boundaries remain distinguishable during particle-heavy combat.
- Every build is reviewed at 1280×720, 1920×1080 and the small 856×480 window used for rapid QA.
