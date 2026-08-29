# Task 3 Report — Shared uGUI components and prefabs

## Delivered scope

- Added `SafeAreaFitter` with the required `Apply(Rect safeArea, Vector2Int screenSize)` seam, correct left/right landscape inset calculation, and an event-reentrancy guard for `RectTransform` dimension callbacks.
- Added `BrutalButton` pointer-down/up/exit behavior, `(+4,-4)` visual offset, 0.1-second unscaled restore, serialized visual/shadow references, and an optional `UnityEvent` hook.
- Added reusable `ScreenBase`, five-slot `HeartBar`, and prewarmed `FloatingTextPool`; none locate gameplay/UI objects at runtime.
- Added `Btn_Brutal.prefab` and `HUD_Minigame.prefab`. The HUD contains exactly one Canvas/CanvasScaler, a safe-area root, one `MinigameHUD`, one `HeartBar`, and serialized labels/fills/theme references. Its scaler uses `1920x1080`, `Scale With Screen Size`, and `matchWidthOrHeight = 1`.
- Added focused EditMode and PlayMode presentation tests. The PlayMode test verifies the prefab asset contract, including the absence of `OnGUI` and missing serialized component references; the EditMode test exercises the safe-area `Apply` seam.

## TDD and verification evidence

- Red: `/tmp/s2-ui-red.xml` run compiled the new tests and failed because `SafeAreaFitter`, `BrutalButton`, and `HeartBar` did not exist.
- Green EditMode: `rtk ~/.local/bin/unity test . --editor-version 6000.3.23f1 --mode EditMode --filter 'KMA.Tests.Presentation.UIComponentTests' --output /tmp/s2-components-edit.xml --timeout 600 -- -nographics` exited 0; XML records 3 passed, 0 failed.
- Green PlayMode: `rtk ~/.local/bin/unity test . --editor-version 6000.3.23f1 --mode PlayMode --filter 'KMA.Tests.Presentation.UIComponentPlayModeTests' --output /tmp/s2-components-play.xml --timeout 900 -- -nographics` exited 0; XML records 1 passed, 0 failed.
- The checkout requests Unity `6000.3.22f1`, which is not locally installed. The verified adjacent S2-plan version `6000.3.23f1` was used explicitly; `ProjectVersion.txt` was restored afterward.

## Scope and limitation

- No rules engine, controller, scene, gameplay input, or Task 1/2 interface changed.
- The committed Task 1 TMP font assets serialize `m_AtlasTextures: {fileID: 0}`. Activating TMP labels that reference them logs `UnassignedReferenceException` before test assertions. Repairing those font assets belongs to Task 1 and is outside Task 3 scope, so the prefab PlayMode contract test inspects the loaded prefab asset without activating TMP. The existing theme/font references remain serialized on the prefabs.
