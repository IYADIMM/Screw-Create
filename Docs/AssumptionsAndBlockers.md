# Assumptions and Blockers

Current as of 2026-10-18.

## Verified environment

| Item | Verified state | Evidence |
|---|---|---|
| Workspace | Git repository on `codex/phase3-functional-stabilization` | Git status/log |
| HEAD at Phase 2 specification start | `e1c9ea79b0417f4a0d82a568fba6450a89bc93b3` | `git rev-parse HEAD` |
| Unity Editor | 6000.6.6f1 | Project/editor evidence |
| Android toolchain | Android player, SDK/NDK, and OpenJDK installed | Successful local APK build |
| Existing tests | 27/27 EditMode, 16/16 PlayMode, 10/10 post-build focused, 1/1 runtime evidence | Preserved XML results |
| Existing APK | 55,766,198 bytes; SHA-256 `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168` | Local file/hash check |
| Protected Core/packages | No diff from Phase 2 baseline `79e79e3` | Git path diff |
| Graybox | `Gameplay_Windmill_Graybox.unity` preserved | Filesystem and Git history |
| External-generation spend | Zero | `CreditLedger.md` |

## True blockers and approval gates

- Disassembly implementation is blocked pending approval of `DisassemblyGameDesign.md`, `DisassemblyRules.md`, `DisassemblyVerticalSliceSpec.md`, and the parallel architecture.
- Physical Android verification requires a connected phone and remains pending.
- Production asset import requires exact candidate and licence approval; no bulk acquisition is authorized.
- Production publisher/package identity, privacy, signing, and release AAB remain gated decisions.
- Procedural disassembly generation is blocked until the authored slice and solver are stable.
- The deferred construction mode requires a later product and UX approval before any player-facing entry point is added.

## Working assumptions requiring validation

- The first disassembly release uses only persistent baseboard service holes and rack slots.
- Structural holes are not storage destinations in the launch rules.
- Shape/engraving conveys compatibility without relying only on color.
- The Workshop Latch is the correct first slice; its status remains proposed until approved and solver-verified.
