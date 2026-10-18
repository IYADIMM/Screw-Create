# Android Device Verification Guide

Status: **PENDING — no physical device evidence recorded**
This guide verifies the existing Phase 1 mixed-workbench APK. It does not verify the proposed disassembly game.

## 1. Artifact identity

- APK: `C:\Users\Lenovo\OneDrive\Desktop\Screw & Create\Builds\Phase2\ScrewAndCreate-Phase2.apk`
- Expected bytes: `55,766,198`
- Expected SHA-256: `31D4EB2CF67D1985D89CBE33691A96A41436A445AAE267A78B1E2382786D4168`
- Development package ID: `com.UnityTechnologies.com.unity.template.urpblank`
- Build is a development artifact with temporary identity, not a release candidate.

Verify the file before installation:

```powershell
Get-Item -LiteralPath 'C:\Users\Lenovo\OneDrive\Desktop\Screw & Create\Builds\Phase2\ScrewAndCreate-Phase2.apk' | Select-Object Length, LastWriteTime
Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\Users\Lenovo\OneDrive\Desktop\Screw & Create\Builds\Phase2\ScrewAndCreate-Phase2.apk'
```

## 2. Prepare the phone

1. Use an Android phone whose model and OS version can be recorded.
2. Charge above 30% and disable battery saver for the test.
3. Enable Developer options and USB debugging.
4. Connect with a data-capable USB cable.
5. Accept the phone's USB debugging prompt for this computer.
6. Keep portrait orientation enabled; do not force landscape.

## 3. Install

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
$apk = 'C:\Users\Lenovo\OneDrive\Desktop\Screw & Create\Builds\Phase2\ScrewAndCreate-Phase2.apk'
& $adb devices
& $adb install -r $apk
```

Continue only when `adb devices` lists one device as `device`. If it says `unauthorized`, unlock the phone and accept the prompt. Preserve any existing app data unless a clean-save test is intentionally scheduled.

Launch from the app icon, or:

```powershell
& $adb shell monkey -p com.UnityTechnologies.com.unity.template.urpblank -c android.intent.category.LAUNCHER 1
```

## 4. Record test context

Record:

- phone manufacturer/model;
- Android version and security patch;
- physical resolution and display scaling;
- navigation mode: gestures or buttons;
- cutout/notch/punch-hole type;
- APK hash and install time;
- clean install or upgrade install.

## 5. Required checks

### Initial launch

- App opens into the Windmill graybox without a crash, missing material, or script error.
- Screen remains upright portrait when the phone is rotated.
- Windmill, screws, top HUD, status, and lower tray remain inside the safe screen area.
- SAVE, LOAD, and DEV controls are absent.
- Screw heads face the camera and look circular with readable cross slots.

### Screw interaction

- Tap each currently legal screw near its center and near reasonable edge positions.
- Confirm one tap produces one removal action.
- Confirm the screw rotates around its shaft and withdraws without snapping to a wrong pose.
- Confirm a blocked/invalid screw gives feedback without changing state.
- Try several rapid taps and two-finger noise; confirm there is no double removal.

### Existing tray behavior

- After removal, confirm the released wooden part is fully visible in the lower tray.
- Tap the visible part through the real touchscreen path.
- Confirm selection is clearly cyan/teal and corresponds to the correct part.
- Confirm empty tray space does not select an unrelated part.
- Rotate the selected part and confirm the visual rotation persists through hint/refresh.

### Undo and restart

- Undo after screw removal; confirm the screw returns to the authored pose and the tray clears.
- Remove/select again, request restart, cancel once, then confirm restart.
- Confirm state, pose, progress, and selection return to the initial configuration.

### Portrait framing and safe area

- Check with gesture navigation and, if possible, button navigation.
- Confirm no screw, tray item, status, or control is clipped by cutout or system bars.
- Confirm touch coordinates align with visible screws and parts.
- If the device supports display-size/font-size changes, test default and one larger accessibility setting.

### Pause/resume and backgrounding

- Begin a screw-removal animation, immediately press Home, wait at least five seconds, and resume.
- Confirm the logical removal is preserved and visuals reconcile without a floating/duplicated screw.
- Repeat while a tray part is selected.
- Open and close the in-game pause panel; confirm gameplay input is blocked only while paused.
- Lock/unlock the phone once and verify restoration.

### Save/relaunch smoke check

- Complete one removal and use the gated development save path only if deliberately enabled for this test build.
- Otherwise background/resume and process-kill/relaunch should be recorded as unverified rather than guessing.
- Confirm no corrupted or contradictory visual state appears.

## 6. Required screenshots

Capture with device buttons or ADB after:

1. initial runtime initialization;
2. one screw removal;
3. a part entering the tray;
4. the tray part selected;
5. undo or restart;
6. resume after backgrounding.

ADB example:

```powershell
& $adb exec-out screencap -p > "$env:TEMP\screw-create-device-initial.png"
```

Move approved evidence into a dated documentation folder only after checking that it contains no personal notifications or device identifiers.

## 7. Pass/fail record

| Check | Result | Evidence/notes |
|---|---|---|
| Install and launch | PENDING | |
| Portrait lock | PENDING | |
| Safe area/cutout | PENDING | |
| Screw appearance | PENDING | |
| Valid and invalid screw touch | PENDING | |
| Tray item visible/selectable | PENDING | |
| Empty-space touch | PENDING | |
| Undo/restart | PENDING | |
| Pause/background/resume | PENDING | |
| Rapid/multi-touch resilience | PENDING | |
| Device screenshots | PENDING | |

Do not mark Android verified until this table includes the device identity, build hash, outcomes, and evidence paths.
