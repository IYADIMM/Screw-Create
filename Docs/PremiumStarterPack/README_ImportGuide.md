# Screw & Create — Premium Starter Asset Pack

Prepared 2026-10-19 for Unity 6000.6.6f1 / URP. **Includes actual assets**, not mockups or placeholders.

## Contents

- 10 original, reusable OBJ models, with PremiumStarter.mtl material references:
  2 beveled baseboards, 4 wooden plank/panel variants, 2 Phillips-head screws, 2 metal-rim hole meshes.
- Three original wood material families (natural oak, honey birch, warm walnut); each includes 1024x1024 base color, OpenGL-convention tangent-space normal, and grayscale roughness images.
- One metal normal map (512x512).
- Nine transparent 256x256 UI icons.
- Seven original generated WAV sound effects (prototype cues, not genuine recorded ASMR).
- Asset preview PNG, manifest with SHA-256 for every file, importer notes and tested CC0 upgrade source links.

**Not included:** actual Kenney recordings or Poly Haven photogrammetry textures. Their official source pages are documented for optional later replacement; no downloaded third-party art is falsely claimed as included.

## Installation

1. **Back up or commit your current Unity changes before integrating.**
2. Copy the supplied `Assets/_Game/Art/PremiumStarter` directory into your Unity project's `Assets/_Game/Art/`.
3. Unity will import OBJ and PNG/WAV files. The source OBJ model front is +Z; screw axis is +Z. Inspect import scale and axis in the Editor.
4. Create `Universal Render Pipeline/Lit` material assets, one per wood species and one per metal finish. Import and assign the corresponding base-color PNG and normal PNG. Set normal textures to **Texture Type: Normal map**, then enable Normal Map. Roughness is a direct grayscale map: for Unity smoothness use 1 - roughness; **do not simply plug roughness into Smoothness unchanged**. Use Unity URP Lit metallic 0 (wood) / metallic ~0.75–0.95 (metal).
5. Place OBJ models under the existing Disassembly Board view hierarchy. Do NOT replace authoritative source/hole IDs, colliders, local screw axis or state logic. Meshes are **presentation assets only**.
6. Use existing screw/hole/component view IDs and set pivots or child offsets carefully. OBJ centers are around world origin, and screw head top faces +Z.
7. Test actual Editor runtime and Android screenshots. Adjust lighting and source colors if needed; original wood tones are intentionally muted vs the orange prototype.
8. Import sound effects as AudioClips, select the correct compressed/decompressed load type for Android after profiling, and play only in response to actual state events. No sound decides puzzle validity.

## Recommended T01 mapping

- Static board: Board_Base_Walnut.obj (dark) or Board_Base_Oak.obj (light).
- Detachable plank: Plank_Wide_Birch.obj.
- Screw: Screw_Phillips_Brass.obj. Its axis is +Z, its head is near local Z=0.11.
- Empty parking hole: Hole_Brass_Rim.obj. Its local origin is hole center. Place at the authoritative hole-view transform.
- Highlight: existing T01 legal-destination ring, not an extra dynamic puzzle rule.
- Selected/complete audio: Screw_Select_Click / Screw_Unscrew_Whirr / Screw_Insert_Set / Wood_Release_Thud / Level_Complete_Chime.

## Quality caveats

- These are modular starter assets, **not** studio-grade photogrammetry or a complete AAA model kit. Wood color/grain is procedural and should be compared against actual downloaded CC0 PBR textures before release.
- Screws have a dark geometric cross **surface insert** rather than a true machined Phillips cavity. Replace with a proper beveled recessed head for final quality if needed.
- OBJ material import is a starting point; finish URP shader assignment and smoothness inversion in Unity.
- Texture normals are OpenGL/tangent-space; verify green-channel orientation in Unity on a lit test material.
- Runtime geometry and collider representations are separate; do not use tiny mesh colliders as the only mobile input targets.
- Do not commit all 7 sound files into production blindly. Select/retune by ear and keep volume subtle.
- No Unity scenes, project settings or protected Core files are included/changed by this pack.
