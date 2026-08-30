# Task 3 Report: Router and ScreenTapArea Ownership

## Files

- `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- `Assets/_Project/Scripts/Input/ScreenTapArea.cs`
- `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`
- `Assets/Tests/PlayMode/Input/KMA.Input.PlayMode.Tests.asmdef`

## Tests and outcomes

- Focused final PlayMode: `rtk ~/.local/bin/unity test . --mode PlayMode --filter 'KMA.Tests.Input.GameplayInputRouterTests' --output /tmp/kma-s3-final-round2-green.xml --timeout 600 -- -nographics` — passed 20/20.
- The focused 20 tests cover shared `KMA.inputactions` Sprint, Endurance, Boss, and Punishment action paths; keyboard tap/hold/swipe/alternate routing; router-owned EnhancedTouch lifecycle; strict UI exclusion including disabled Selectables; parent-handler/child-gameplay ownership; duplicate down; drag/up cleanup; disable flushing; keyboard hold cancellation on disable/reconfigure; lifecycle subscription idempotence; rhythm offset; and multi-pointer isolation.
- Full EditMode: `rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/kma-s3-final-round2-editmode.xml --timeout 600 -- -nographics` — passed 149/149.
- Full PlayMode: `rtk ~/.local/bin/unity test . --mode PlayMode --output /tmp/kma-s3-final-playmode.xml --timeout 600 -- -nographics` — interrupted on request while repeatedly loading scenes after the headless TMP importer reported `No graphic device is available` for `MG_Boss`; no complete result count was produced.
- Aggregate scoped diff check: `rtk git diff --check -- Assets/_Project/Scripts/Input Assets/Tests/PlayMode/Input` — passed with no output. Added required `Assets/_Project/Scripts/Input.meta`; existing `Assets/Tests/PlayMode/Input.meta` was already tracked. No unrelated metadata was staged.

## Commit SHA

`e8ac8eb7da2bc8cb4dd4216a5dfd12901b6618e9` — final lifecycle/UI ownership fix.

## Concerns

- Full PlayMode remains unverified because the headless TMP package-importer path requires a graphics-capable environment; focused PlayMode 20/20 and full EditMode 149/149 are green.
- The shared router and tap area are additive only; scenes and legacy controllers remain intentionally unwired for later S3 integration work.
- Touch ownership is intentionally centralized in the router-owned EnhancedTouch lifecycle and `ScreenTapArea`; shared action-map touch bindings do not create a second detector path.
