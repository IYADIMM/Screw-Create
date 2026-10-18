# Free Asset Acquisition Plan

Status: **PLAN ONLY — no production assets downloaded or imported**
Budget: **$0**
License review date: 2026-10-18

## 1. Rules before import

1. Every asset receives a manifest row with source URL, author, exact asset/version, download date, license text or permalink, commercial-use status, modification status, and local editable source.
2. Verify the license on the exact asset page at download time; a marketplace-wide reputation is insufficient.
3. Prefer CC0. Attribution-required assets need an approved credits workflow before import.
4. Reject noncommercial, editorial-only, “personal use,” unknown, scraped/rehosted, AI-ownership-unclear, or no-derivatives content.
5. Do not copy competitor screenshots, meshes, UI, layouts, branding, or marketing art.
6. Import one reviewed candidate set at a time after approval; do not bulk-import collections.
7. Keep source files outside runtime folders and create optimized Unity derivatives with traceable provenance.

## 2. Verified source candidates

| Category | Candidate | Verified condition | Intended use | Import status |
|---|---|---|---|---|
| Wood surface | [Poly Haven — Wood Table](https://polyhaven.com/a/wood_table) | Asset page states CC0; 1K–8K PBR maps available | Clean warm workbench/baseboard material | Candidate only |
| Wood surface | [Poly Haven — Wooden Planks](https://polyhaven.com/a/wooden_planks) | Asset page states CC0; PBR maps available | Worn puzzle boards/props | Candidate only |
| Dark wood | [Poly Haven — Dark Wood](https://polyhaven.com/a/dark_wood) | Asset page states CC0 | Contrast trim and background furniture | Candidate only |
| Source licence | [Poly Haven License](https://polyhaven.com/license) | All listed HDRIs, textures, and models are CC0 and allowed for commercial use | License evidence | Verified source |
| Furniture props | [Quaternius Furniture Pack](https://quaternius.com/packs/furniture.html) | Exact pack page states CC0 and free commercial use; FBX/OBJ/Blend | Background bench/shelves after visual-fit review | Candidate only |
| Workshop-adjacent props | [Quaternius Fantasy Props MegaKit](https://quaternius.com/packs/fantasypropsmegakit.html) | Page states CC0; free subset is identified separately from paid/source tiers | Optional low-poly hand tools/props from free subset | Candidate only; inspect each included item |
| Source licence | [Quaternius FAQ](https://quaternius.com/faq.html) | States models are CC0, commercially usable, modifiable, attribution optional | License evidence | Verified source |
| UI panels/icons | [Kenney UI Pack](https://kenney.nl/assets/ui-pack) | Exact page states Creative Commons CC0 | Neutral source shapes to restyle into original wooden UI | Candidate only |
| Touch prompts | [Kenney Input Prompts](https://kenney.nl/assets/input-prompts) | Exact page states CC0 and includes touch gestures | Tutorial tap/drag cues | Candidate only |
| UI audio | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | Exact page states CC0; 100 files | Button, selection, confirmation prototypes | Candidate only |

Poly Haven, Quaternius, and Kenney assets are candidates, not automatic approvals. Visual fit, polycount, texture memory, duplicated content, and exact downloaded license files still require inspection.

`ambientCG` remains a useful possible CC0 texture source, but its current license page could not be retrieved during this review. Treat it as **unverified for import** until the exact asset and current official license are captured successfully.

## 3. Category strategy

| Need | Preferred approach | Reason |
|---|---|---|
| Wooden planks/boards | Reusable Blender parametric plank built in-house; apply reviewed CC0 PBR textures | Precise hole alignment, bevels, pivots, and consistent dimensions matter more than finding a mesh |
| Screw model | Build one reusable Blender screw family with swappable head/shaft parameters | Exact mechanical axis, collider, LOD, extraction depth, and compatibility cues are gameplay-critical |
| Screw holes/service holes | Build procedurally with shared rings/inserts and decals | Must communicate structural vs service semantics clearly |
| Wooden mechanisms | Assemble from the in-house plank and joint kit | Supports many levels with stable pivots and consistent art direction |
| Workbench/background | CC0 texture plus selected Quaternius props, heavily curated | Background should support depth without intercepting input |
| Wood textures | Poly Haven 1K/2K derivatives with packed mobile maps | Verified CC0 and scalable quality |
| Brass/steel materials | Author locally from color/metallic/roughness values; optional CC0 subtle grunge | Small memory footprint and consistent readability |
| Icons/UI panels | Original SVG/layout using Kenney CC0 shapes only as raw ingredients | Avoids copied competitor styling and supports scaling |
| Audio | Kenney CC0 UI sounds for prototypes; self-record or source exact CC0 mechanical clips later | Mechanical identity needs specific screw/wood sounds |
| Environment props | Curated CC0 furniture/tools or simple Blender models | Keep visual focus on puzzle and control draw calls |

## 4. Blender production requirements

### Modular plank

- Adjustable length, width, thickness, bevel radius, corner wear, and hole positions.
- Correct real-world scale, centered mesh, documented local axes, and deterministic pivot options.
- UV channel for tiling wood grain plus optional baked wear/AO.
- Simple box/convex colliders authored separately.
- LOD0/LOD1 only when measured benefit justifies them.

### Screw family

- One hierarchy: interaction root, head renderer, shaft renderer, collider, axis marker, optional washer.
- Local mechanical withdrawal/rotation axis documented and identical across variants.
- Phillips/slot/hex visual options; standard/broad compatibility communicated by geometry and engraving.
- Separate raised and seated reference positions; no baked world-space assumptions.

### Hole family

- Structural fastener, standard service, universal service, and rack-slot visuals must remain distinguishable without color.
- Shared dimensions with screw variants and authoring validation.
- No hidden collider larger than its visible target.

### Release paths

- Author path origin/end, pivot, clearance envelope, and motion type per component.
- Logical release remains independent from Blender animation clips; clips/curves are presentation data.

## 5. Unity import targets

- URP Lit materials; no URP replacement or migration.
- Mobile texture default: 1K for primary hero surfaces, 512 or lower for props until profiling justifies more.
- Use ASTC on Android after device checks; preserve lossless sources outside runtime output.
- Read/write disabled unless a measured runtime operation needs it.
- Mesh compression, normals, tangents, and lightmap UVs decided per asset and recorded.
- Decorative props go on noninteractive layers and must not affect playfield fitting.

These are starting constraints, not performance claims. Final budgets require device profiling.

## 6. Acquisition order after approval

1. Build the local screw, structural hole, service hole, and plank kit in Blender.
2. Import one CC0 wood texture candidate and create mobile URP materials.
3. Construct the Workshop Latch slice with no environment pack.
4. Validate gameplay, touch, readability, and device performance.
5. Add one curated background/furniture set only if the slice needs it.
6. Add prototype UI/audio assets, then replace or refine where the product identity requires original work.

No paid pack, external generation credit, or production download is authorized by this plan.
