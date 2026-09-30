# How to Run Spatial Rift Lab

## Fastest method — no Unity needed

1. Unzip `NeonRiftArena-Windows.zip` to a normal folder.
2. Keep the `.exe`, `_Data` folder, DLLs and `MonoBleedingEdge` folder together.
3. Double-click `SpatialRiftLab.exe`.
4. If Windows shows a SmartScreen message, use **More info → Run anyway** only if the file came from this project folder. The build is unsigned because it is a portfolio build.

## Controls

- `1`, `2`, `3` — select a different simulated room scan
- `Tab` — cycle through room layouts
- `Enter` — deploy after scanning finishes
- `WASD` — move
- Mouse — aim
- Left mouse button — fire
- `Space` — dash with brief invulnerability
- `Escape` — pause/resume
- `R` — restart after victory or defeat

## What to demonstrate in the interview

1. Stay on the opening screen and press `1`, `2`, and `3`.
2. Explain that walls, physical-object anchors, no-go volumes and safe area are regenerated from layout data.
3. Choose the L-shaped layout and point out the unavailable upper-right volume.
4. Press Enter and show that enemies spawn inside the solved playable area.
5. Mention that the same layout interface can be fed by real scene-mesh data on Quest/PICO/visionOS.

## Run from Unity

1. Open Unity Hub.
2. Click **Add** and select the `NeonRiftArena` project folder.
3. Open it with Unity `6000.5.3f1`.
4. Wait until the progress indicator finishes.
5. In the Project panel open `Assets → NeonRift → Scenes → NeonRiftArena`.
6. Press Play.

## Make a new Windows build

1. Open the project in Unity.
2. Use the top menu **Neon Rift → Build Interview Demo**.
3. Wait for the build to finish.
4. Open the project folder, then `Build → Windows`.
5. Run `SpatialRiftLab.exe`.

## If it does not open

- Extract the ZIP first; do not run the game while it is still inside the ZIP.
- Keep the complete Windows build folder together.
- Update the graphics driver if Unity reports a Direct3D problem.
- On a university/work computer, copy it to a folder where you can write files.
- The game writes its log to `%USERPROFILE%\AppData\LocalLow\Aayush Portfolio\Spatial Rift Lab\Player.log`.
