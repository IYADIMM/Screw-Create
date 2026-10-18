# Build and Release

## Verified local environment — 2026-10-18

- Unity Editor: 6000.6.6f1 (`ProductVersion 6000.6.6f1_d20c9ac777da`).
- Unity CLI: 1.0.0-beta.13.
- Installed template: `com.unity.template.urp-blank` 17.2.6.
- Input System in template: 1.20.0; URP: 17.6.0; uGUI: 2.6.0.
- Android Build Support, SDK/NDK Tools, and OpenJDK: installed under the Unity 6000.6.6f1 editor and used successfully on 2026-10-18.
- Blender: 5.2.2 LTS.
- Git: 2.56.0.windows.2.
- Free disk at preflight: 15.64 GB.
- Unity CLI auth/license state: unverified due `LOCAL_STORE_UNWRITABLE` and `LICENSING_CLIENT_UNAVAILABLE`.

## Android release decisions still gated

Minimum API, current Google Play target API, final package/publisher identity, privacy URL, production ad provider/account, consent declarations, and signing configuration will be verified/decided at the relevant release gate. No production secrets belong in source control.

## Provisional budgets before final art

| Metric | Provisional budget |
|---|---:|
| Frame rate | 60 FPS target; 30 FPS fallback |
| Main-thread frame time | <= 12 ms at 60 FPS target |
| GPU frame time | <= 14 ms at 60 FPS target |
| Managed allocations during steady gameplay | 0 B/frame |
| Draw calls | <= 120 |
| Visible triangles | <= 250k |
| Texture memory | <= 256 MB |
| Audio memory | <= 32 MB |
| Scene transition | <= 3 s on agreed midrange device |
| Installed application | <= 250 MB initial target |

These are planning targets, not measured results.

## Phase 1 Android development build — 2026-10-18

- Build method: `ScrewAndCreate.Editor.VerticalSlice.WindmillVerticalSliceBuilder.BuildAndroidDevelopmentFromCommandLine`.
- Result: Succeeded.
- Artifact: `Builds/Phase2/ScrewAndCreate-Phase2.apk`.
- Actual file size: 55,766,198 bytes.
- SHA-256: `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`.
- Log: `Logs/Phase1AndroidBuild.log` (local generated evidence; not committed).
- Serialized orientation: portrait default; upside-down and both landscape autorotation options disabled.
- Device status: `adb devices` returned no connected device. Installation, physical touch, lifecycle, cutout/navigation-inset behavior, and performance remain blocked.
- This is a development APK. Production identity, signing, current store/API requirements, release AAB, and privacy decisions remain gated.

