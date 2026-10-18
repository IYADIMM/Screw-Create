# Known Limitations

Current as of 2026-10-18.

- The playable scene is a stabilized TEMP mixed-workbench graybox, not the newly specified disassembly game.
- Controlled-hybrid disassembly rules, screw destinations, service holes, rack state, structural release closure, disassembly completion, and deadlock proof are specifications only.
- The proposed Workshop Latch level is hand-validated against the written rules and has not been implemented or solver-verified.
- The existing deterministic generator and solver target mixed removal/assembly and must not be presented as disassembly evidence.
- Android Build Support, SDK/NDK, and OpenJDK are installed and can build the development APK, but no physical Android device has been tested.
- Production publisher/package identity, signing, current store requirements, and release AAB remain gated.
- The current TEMP scene uses primitive geometry and Unlit materials; production meshes, Lit materials, final UI, audio, and haptics remain pending.
- No production assets have been downloaded or imported. Free candidates still require exact per-asset approval and recorded license evidence.
- The part inventory and assembly presentation remain in the preserved prototype but are planned to be dormant in the primary disassembly game.
- Procedural disassembly generation remains deferred until an authored vertical slice and its solver are approved and verified.
- Two Unity `InitTestScene…` files remain untracked and preserved pending explicit treatment.
- External-generation spend remains zero.
