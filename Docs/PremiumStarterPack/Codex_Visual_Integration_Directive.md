# Codex integration directive — Asset-only visual upgrade

Task: inspect current M2 T01 screenshot, UI and asset implementation. Use the attached PremiumStarter models / textures / sounds as candidate source assets. Make only visual and audio integration changes after inspecting the actual current project. Maintain old scene and Core as protected. Do not change the disassembly engine, transfer legality, covered-hole logic, T01 IDs or test expectations.

- Create new URP Lit material assets for each wood and metal material. Do not use existing TEMP Unlit materials. Use imported original wood diffuse and normal maps; convert roughness to smoothness (smoothness = 1-roughness) for URP.
- For wooden parts choose restrained honey-birch and natural-oak with dark walnut background, NOT saturated orange/red.
- Replace harsh dark cast shadows with physically reasonable soft contact shadows via a modest key light, limited ambient and soft shadow settings. No dramatic red tint.
- Ensure the source screw has correctly oriented +Z local mechanical axis; preserved authored transform, rotation and collider. Replace old visible art as renderer children rather than changing logical source IDs.
- Use hole ring meshes with sufficient contrast, and preserve selection highlight.
- Scale playfield within safe viewport so actual interactive plank and holes are comfortably visible. Maintain 9:16 / 9:19.5 / 9:21 / 3:4.
- Replace old teal/orange oversized HUD with quiet neutral workshop UI tones and modest accent highlights. Do not add visual clutter. Do not change input blocking or game-state semantics.
- Add restrained click/unscrew/settle/wood-drop/completion clips only at valid animation events. Expose a volume/haptics toggle later if not already supported.
- Run all existing EditMode and PlayMode suites, capture authentic Unity screenshots and Android build if toolchain connected. Report exact actual results and visual before/after; do not claim physical device verification without it.
- Never overwrite existing build or reference scene; use new visuals only in new disassembly scene. Before asset import create source checkpoint and a dedicated visual-integration branch.
- Stop for user screenshot approval after first visual pass.
