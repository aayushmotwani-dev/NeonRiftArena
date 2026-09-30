# Turning the Desktop Demo into a Real VR/MR Build

The included Windows version is intentionally desktop-first so an interviewer can run it without a headset. `XRSpatialBridge` already separates device detection from gameplay, but a headset build needs platform configuration and real spatial data.

## Recommended Unity stack

- Unity XR Plug-in Management
- OpenXR Plug-in for cross-device HMD/controller input
- XR Interaction Toolkit for grabbing, ray interaction, sockets and locomotion
- Input System with action-based controls
- Meta XR SDK, PICO SDK or visionOS/PolySpatial features when platform-specific scene data is required

## Implementation order

1. Add XR Plug-in Management, OpenXR, Input System and XR Interaction Toolkit.
2. Create an XR Origin with tracked head and controller/hand objects.
3. Replace mouse aiming with an XR ray/direct interactor.
4. Convert the weapon into a grab interactable with a stable attach transform.
5. Replace the simulated layout provider with a platform scene-mesh provider.
6. Convert detected planes/volumes into the layout constraint format.
7. Add guardian visualization and prevent virtual content from encouraging unsafe motion.
8. Provide snap-turn and teleport options even if the primary design uses real walking.
9. Profile on the target standalone headset, not only in the Editor.

## VR comfort checklist

- Keep the virtual camera controlled only by tracked head motion.
- Avoid forced acceleration, camera shake and screen-space flashes in headset mode.
- Offer snap turning, teleport and seated/standing calibration where appropriate.
- Keep important UI at a comfortable distance and use world-space panels.
- Maintain a stable horizon and communicate physical boundaries early.
- Reduce controller latency and preserve consistent frame pacing.

## Standalone-headset optimization checklist

- Use URP and single-pass instanced rendering where supported.
- Bake lighting and avoid many real-time point lights.
- Pool projectiles, particles and enemies.
- Use LODs, occlusion culling and GPU instancing.
- Compress textures and cap their resolution based on measured visual value.
- Simplify collision meshes independently from render meshes.
- Record CPU/GPU timings on device with the Unity Profiler and platform tools.

## Honest scope statement

> “The supplied build simulates the spatial pipeline so anyone can evaluate it on a laptop. I did not pretend to have a commercial room-scanning SDK or a room-scanning headset. The provider boundary is designed so Meta, PICO or Apple scan data can replace the presets without rewriting gameplay.”
