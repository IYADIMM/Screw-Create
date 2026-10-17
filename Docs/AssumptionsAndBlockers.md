# Assumptions and Blockers

## Verified environment

| Item | Verified state | Evidence |
|---|---|---|
| Workspace | Initialized local Git repository on `main`; Unity project rooted in the workspace | `git status` and project verification |
| Repository instructions | No `AGENTS.md` found in workspace or Desktop ancestors searched | Recursive filename search |
| Unity CLI | 1.0.0-beta.13 | `unity --version` |
| Unity Editor | 6000.6.6f1, x86_64 | CLI registry and executable product version |
| Installed Unity modules | Documentation and Web Build Support | `unity modules list 6000.6.6f1` |
| Android module/toolchain | Missing: Android Player, SDK, NDK, OpenJDK | Module status plus filesystem checks |
| Blender | 5.2.2 LTS at `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe` | Direct executable version |
| Git | 2.56.0.windows.2 | `git --version` |
| Disk | 15.64 GB free on C: during preflight | `Get-PSDrive` |
| Connected creative tools | Blender MCP schemas and Higgsfield plugin schemas visible | Session tool metadata only; no generation invoked |
| Unity project | Compiles and runs EditMode tests under 6000.6.6f1 | 11/11 tests passed; strict project verification clean |

## True blockers

- Android builds are blocked until Android Build Support, SDK/NDK Tools, and OpenJDK are installed for Unity 6000.6.6f1.
- The standalone Unity CLI auth/license probes were inaccessible, but the editor resolved a Unity Personal license and completed compilation/tests. This is no longer a current test blocker.
- Real-device verification requires an available Android phone at Milestone 7/8.
- Production publisher/package identity, privacy URL, ad provider/account, and signing remain gated user decisions.
- First finished-level mass production remains blocked on the explicit visual/gameplay approval gate.
- Paid generation remains blocked pending a separately specified and approved batch.

## Safe assumptions

- Local Git is the reversible source-control default; no remote is created.
- The development project name is `ScrewAndCreate`; production identity is unset.
- The first logic milestone uses no external accounts, production ads, signing data, or generated media.

