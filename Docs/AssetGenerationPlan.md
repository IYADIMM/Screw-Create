# Asset Generation Plan

## Current policy

Local Unity primitives, deterministic editor tooling, procedural Blender scripts, modular meshes, shared materials, and local temporary audio are preferred. Every temporary gameplay asset must use the `TEMP_` prefix and be tracked in `AssetManifest.csv`.

## Connected capability inspection

- Blender MCP exposes local scene inspection, Python execution, rendering/look, library search/import, and a `generate_3d` operation. The tool schema states that `generate_3d` may consume money or a monthly generation, so it is disabled pending explicit batch approval.
- Higgsfield plugin schemas are visible, including read-only/project tools and credit-consuming generation surfaces. No generation, enhancement, upscale, variation, audio, image, video, or 3D job was invoked.
- Specific paid model identifiers and prices have not been asserted. They will be obtained only through a confirmed-free quote/catalog operation when a concrete batch is prepared.

## First proposed production batch

None. Milestones 0–2 use zero-credit local work. A first-level batch can be prepared at Milestone 3 with requirement IDs, exact tool/model, prompts, negative prompts, references, output count, formats, dimensions, verified cost, selection/reuse rules, alternatives, and rejection handling.

