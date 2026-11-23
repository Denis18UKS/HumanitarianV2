# Ghost Camaro — Humanitarian / Unity 6 driving demo

Target editor: **Unity 6000.6.4f1** (URP 17.6.0, Input System 1.20.0).

## What this PR does

- GitHub includes **the actual original Ghost Camaro FBX (40.4 MB) and four PNG textures**, not Minecraft/Fabric assets. Because the connected GitHub uploader limits large single requests, the five-file vehicle-only ZIP is stored in `Source/Parts/part000.bin` through `part042.bin` and reassembled by the Unity Editor.
- On the first Unity import, `GhostCamaroAutoBootstrap` joins the 43 binary fragments, checks their **SHA-256**, extracts exactly the vehicle FBX and four textures, and generates a drivable prefab and test scene.
- Physically simulated rear-wheel-drive car, four WheelColliders, Rigidbody, collisions, throttle/brake/reverse, handbrake, independent visual wheel movement and steering wheel.
- Chase/cockpit/front camera cycling (F5), walking demo character (E enter/exit), compact speed HUD.
- Headlights, brake lights, left/right indicators, hazard lights.
- Source FBX animation clips indexed into a preview AnimatorController for inspection where Unity actually imports them. **These clips are not all simultaneously layered in gameplay yet.**

## Open and drive

1. Check out `feature/ghost-camaro-driveable-unity6` in the repository (you do **not** need the original mixed ZIP).
2. Open the Unity project using **Unity 6000.6.4f1**. Wait until import and script compilation finish. The first-time bootstrap should auto-create and open:
   `Assets/Humanitarian/Scenes/GhostCamaro_DrivingDemo.unity`.
3. If not created automatically, open **Humanitarian > Ghost Camaro > 3. Build DRIVEABLE car + demo scene**.
4. Press **Play**. Driver starts in the car with engine on.
5. Use controls below. Press **E** to exit, approach the car on foot, and **E** to enter again.

| Key | Action |
|---|---|
| W | Accelerate |
| S | Brake, then reverse |
| A / D | Steer |
| Space | Handbrake |
| R | Engine on/off |
| F5 | Cycle chase, front, cockpit |
| E | Enter/exit |
| L | Headlamps |
| Z / X | Left/right turn signal |
| C | Hazard lights |
| Esc | Unlock/relock cursor |

## Generated project assets

After import Unity creates:

- `Assets/Humanitarian/Vehicles/GhostCamaro/Models/GhostCamaro.fbx`
- `Assets/Humanitarian/Vehicles/GhostCamaro/Textures/*.png`
- `Assets/Humanitarian/Vehicles/GhostCamaro/Materials/*.mat`
- `Assets/Humanitarian/Vehicles/GhostCamaro/Animations/GhostCamaro_Preview.controller`
- `Assets/Humanitarian/Vehicles/GhostCamaro/Prefabs/GhostCamaro_Driveable.prefab`
- `Assets/Humanitarian/Scenes/GhostCamaro_DrivingDemo.unity`

Source fragments are tracked in Git; generated files are ignored to avoid huge duplicate commits.

## Bundled model verification

- Packed archive size: **13,209,459 bytes**
- Part count: **43**
- SHA-256: `a3f68a701ef60946c1c93fc5a0add5d9c400110b7be515010a33dea18cbc7348`
- Contents: `Models/GhostCamaro.fbx` plus four vehicle PNGs only.
- Source GLB inspected separately: **716 nodes, 604 meshes, 62 named animation clips**. The number of animation clips retained by FBX must be verified inside Unity, and should not be assumed to be 62.

## Limitations and validation

This change was **not compiled or played in Unity on this execution environment**. Code, archive contents and SHA-256 were inspected; no in-editor behaviour is claimed verified yet. Verify wheel positions / collision geometry / directional orientation, cockpit view, FBX material/UV mapping, transparent glass, model scaling and actual animation clip imports.

The current `GhostCamaroAnimationPreview` and generated AnimatorController offer **single-clip preview**, not full multi-layer coordination of every animation. Wipers, doors, hood, trunk, damage, interior hand animations, exhaust and complex engine audio are next milestones. The current car controller uses procedurally updated wheel/steering poses and actual Unity Light components.

## Troubleshooting

- **Missing model**: Console should contain `Ghost Camaro: verified original FBX and 4 textures from GitHub (SHA-256 OK)`. If not, check `Source/Parts/part000.bin` ... `part042.bin`.
- **No scene**: choose `Humanitarian > Ghost Camaro > 3. Build DRIVEABLE car + demo scene`.
- **No scripts/menu**: inspect Unity **Console** for the first C# compilation error.
- **Prefab incorrect**: rebuild with menu #3, inspect `GhostCamaroRigReferences` and WheelCollider positions.
- **Git status shows imported generated assets**: ensure the branch .gitignore matches this stage.

**Branch policy**: do not push directly to `main`; this milestone is delivered in a dedicated feature branch with its own PR.
