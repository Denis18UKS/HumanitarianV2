# Ghost Camaro — Stage 1 Unity Import (HumanitarianV2)

Target: Unity 6000.6.4f1, URP 17.6.0, Input System 1.20.0.

## Status

The repository branch contains a selective archive importer and scripts that generate a prefab with named subpart references and a **preview** AnimatorController. The binary FBX/textures are **not pushed** in this branch; import them from the original local mixed ZIP to avoid adding Minecraft/Fabric and unneeded assets. This code has not been executed inside the user's Unity Editor.

## Steps

1. Open the Unity project on `feature/ghost-camaro-unity-import`.
2. Choose **Humanitarian > Ghost Camaro > 1. Import from ZIP (original or Unity package)...**
3. Select either the original `GhostCamaro-NEXT-FULL-PROJECT(1).zip` or the prepared `Humanitarian_GhostCamaro_Unity_Import_Stage1.zip`. Both layouts are supported, including ZIPs with an extra top-level directory.
4. Importer extracts only the Ghost Camaro FBX and four vehicle PNGs, whether they live under `Exports/FBX/` + `Textures/Vehicles/` or the prepared Unity `Assets/Humanitarian/Vehicles/GhostCamaro/` paths. It validates all five entries before writing, and reports missing paths if necessary.
5. It generates `Assets/Humanitarian/Vehicles/GhostCamaro/Prefabs/GhostCamaro.prefab` and an AnimatorController.
6. Drag the prefab into a test scene, inspect materials/size/pivots and test clips in Play Mode using `GhostCamaroAnimationPreview`.
7. Inspect Import Settings > Animation: the original GLB includes 62 named clips; actual FBX import count may vary.

## Important caveats

* This is **asset staging only**. No car physics, independent animations, HUD, camera or damage gameplay is implemented here.
* The preview AnimatorController can play one state at a time, not independent door/wheels/brake/light layers.
* Check the glass transparency and material remapping manually: the FBX's source materials may import differently under URP.
* Source GLB: 716 nodes, 604 meshes, 62 animation clips. The FBX may not preserve every clip precisely.
* Do not import any Minecraft files, .jar, Fabric assets, Blockbench packs, or other objects into this Unity stage.
* Keep `Assets/`, `Packages/`, `ProjectSettings/` under version control; ignore `Library/`, `Temp/` and `Logs/`.

The local asset-ready ZIP package is available separately from ChatGPT. Its Unity `Assets/` folder can be copied into the project if needed.
