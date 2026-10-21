# Screw & Create — Premium Starter Asset Audit

Date: 2026-10-21  
Source: `ScrewCreate_Premium_Asset_Starter_Kit_2026-10-19.zip`  
ZIP SHA-256: `D112921656069AF0A52C27A13B71BE91F8FD4BB35D7EA4C305C3F69D563756A1`

## Integrity and licensing

- The ZIP opened and extracted without an archive error.
- All 37 files listed in `AssetManifest.csv` matched their recorded SHA-256 hashes.
- Git attributes keep the text-based OBJ and MTL source files byte-stable across Windows checkouts so those manifest hashes remain reproducible.
- The manifest identifies every included asset as original work with no downloaded third-party content and permits commercial use.
- The optional Poly Haven and Kenney links are documentation only. No external asset was downloaded or imported during this integration.
- No paid generation service or credit was used.

## Model validation

All ten OBJ models parsed successfully. Every vertex and index was finite and valid. Every face had UV coordinates. The supplied geometry report counts triangulated faces; an independent parser produced the same triangle counts: 142 for each board/plank model, 288 for each hole rim, and 508 for each screw. Raw OBJ vertex counts differ from the report for the beveled board/plank family because the report collapses equivalent vertices while the OBJ keeps UV-boundary duplicates; this is a representation difference rather than invalid geometry.

The board and plank pivots are centered. Hole origins are centered in X/Y and offset only `-0.0085` on Z by their modeled depth. Screw models are centered on X/Y, use the documented +Z mechanical axis, extend from Z `-0.370` to `+0.113`, and place the head on the positive-Z end. The premium builder rotates the screw visual under the existing logical screw transform, preserving the authoritative collider and animation axis.

The OBJ files contain complete UVs but no authored vertex normals. This is the only import caveat. Unity is configured to calculate normals at a 52-degree smoothing angle and calculate MikkTSpace tangents. The models are accepted with that explicit importer correction.

## Texture and material validation

- Oak, birch, and walnut base-color, normal, and roughness maps are 1024×1024.
- The brushed-metal normal is 512×512.
- Normal maps have centered red/green channels and a strong positive blue channel, consistent with tangent-space OpenGL normal maps.
- Roughness ranges from 135–211 and is not treated as smoothness directly.
- Three generated metallic/smoothness textures store zero metallic in RGB and `1 - roughness` in alpha for URP Lit.
- Base-color textures import as sRGB; normal and metallic/smoothness textures import as linear data.
- The nine 256×256 UI icons contain functional transparency and import as single sprites.

## Audio validation

All seven WAV files are mono, 44.1 kHz, 16-bit PCM. Durations range from 0.14 to 0.72 seconds. No clip contains clipped samples, and measured DC offsets are effectively zero. Peak levels are approximately -2.27 dBFS. Unity imports them as mono, preloaded, short Decompress-on-Load clips with restrained runtime volume.

Integrated event clips:

- `Screw_Select_Click.wav`
- `Screw_Unscrew_Whirr.wav`
- `Screw_Insert_Set.wav`
- `Wood_Release_Thud.wav`
- `Level_Complete_Chime.wav`

Retained but not connected in T01:

- `UI_Soft_Tap.wav` — no additional generic UI audio layer was added.
- `Wood_Slide_Swish.wav` — the release event uses the more readable wood thud.

## Imported and deferred assets

All validated source assets were imported under `Assets/_Game/Art/PremiumStarter/` so their manifests, models, textures, icons, and audio remain together. T01 uses the oak, birch, walnut, brass screw, brass hole, Undo, Restart, Pause, and Check assets. Steel variants and Home, Hint, Arrow, SoundOn, and SoundOff icons are valid but unused in this minimal scene.

No asset was rejected for corruption, licensing, geometry, texture, or audio failure. The two documented limitations remain: the procedural grain is less varied than production photogrammetry, and the Phillips recess is a dark geometric insert rather than a machined cavity.

