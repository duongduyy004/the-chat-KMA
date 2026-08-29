# Task 3 Report — Shared uGUI components and prefabs

## Delivered scope

- Added `SafeAreaFitter` with the required `Apply(Rect safeArea, Vector2Int screenSize)` seam, correct left/right landscape inset calculation, and an event-reentrancy guard for `RectTransform` dimension callbacks.
- Added `BrutalButton` pointer-down/up/exit behavior, `(+4,-4)` visual offset, 0.1-second unscaled restore, serialized visual/shadow references, and an optional `UnityEvent` hook.
- Added reusable `ScreenBase`, five-slot `HeartBar`, and prewarmed `FloatingTextPool`; none locate gameplay/UI objects at runtime.
- Added `Btn_Brutal.prefab` and `HUD_Minigame.prefab`. The HUD contains exactly one Canvas/CanvasScaler, a safe-area root, one `MinigameHUD`, one `HeartBar`, and serialized labels/fills/theme references. Its scaler uses `1920x1080`, `Scale With Screen Size`, and `matchWidthOrHeight = 1`.
- Added focused EditMode and PlayMode presentation tests. The PlayMode test verifies the prefab asset contract, including the absence of `OnGUI` and missing serialized component references; the EditMode test exercises the safe-area `Apply` seam.
- Review regression coverage now includes the HUD root's non-zero visible scale, temporary HUD instantiate/activation with TMP labels and safe-area application, BrutalButton shadow reset/restoration, and source/atlas presence for the shared Baloo2 and Nunito TMP assets.

## TDD and verification evidence

- Red: `/tmp/s2-ui-red.xml` run compiled the new tests and failed because `SafeAreaFitter`, `BrutalButton`, and `HeartBar` did not exist.
- Green EditMode: `rtk ~/.local/bin/unity test . --editor-version 6000.3.23f1 --mode EditMode --filter 'KMA.Tests.Presentation.UIComponentTests' --output /tmp/s2-components-edit.xml --timeout 600 -- -nographics` exited 0; XML records 3 passed, 0 failed.
- Green PlayMode: `rtk ~/.local/bin/unity test . --editor-version 6000.3.23f1 --mode PlayMode --filter 'KMA.Tests.Presentation.UIComponentPlayModeTests' --output /tmp/s2-components-play.xml --timeout 900 -- -nographics` exited 0; XML records 1 passed, 0 failed.
- Review red: `/tmp/task3-red-edit.xml` exited 2 on the missing TMP atlas assignment and the new pressed-shadow assertion.
- Review green EditMode: `/tmp/task3-final-edit.xml` exited 0; focused `UIComponentTests` and `UIThemeTests` passed, including font source/atlas and Vietnamese coverage checks.
- Review PlayMode: `/tmp/task3-final-play.xml` instantiated the HUD and reached the runtime assertion; the initial exact-unit-scale assertion failed because CanvasScaler correctly applies a runtime scale of `(0.44, 0.44, 0.44)`. The assertion was narrowed to positive visible scale, and no further Unity run was performed per the stop request.
- The checkout requests Unity `6000.3.22f1`, which is not locally installed. The verified adjacent S2-plan version `6000.3.23f1` was used explicitly; `ProjectVersion.txt` was restored afterward.

## Scope and limitation

- No rules engine, controller, scene, gameplay input, or Task 1/2 interface changed.
- The Task 1 TMP assets were repaired in-place using the repository TTF sources while retaining their existing `.meta` GUIDs. Baloo2, Nunito, and VietnameseFallback now serialize source font references and non-zero atlas texture file IDs; the focused EditMode checks passed. A final PlayMode rerun after relaxing the CanvasScaler assertion was intentionally not performed because verification was stopped by request.
- Follow-up P1 status: the current checkout still has empty TMP glyph/character tables in the three font assets, so those unverified follow-up edits were reverted to `a73cc5c`. Unity `6000.3.23f1` generated real static tables for Baloo2/Nunito and the fallback, but the fallback source reference was not valid after the interrupted pass; the requested final focused verification could not be completed before stopping. The temporary generators were removed and unrelated untracked files were preserved.
