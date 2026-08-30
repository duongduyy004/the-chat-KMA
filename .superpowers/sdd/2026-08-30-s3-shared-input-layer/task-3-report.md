# Task 3 Report: Router and ScreenTapArea Ownership

## Files

- `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- `Assets/_Project/Scripts/Input/ScreenTapArea.cs`
- `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`
- `Assets/Tests/PlayMode/Input/KMA.Input.PlayMode.Tests.asmdef`

## Tests and outcomes

- Focused final PlayMode: `rtk ~/.local/bin/unity test . --mode PlayMode --filter 'KMA.Tests.Input.GameplayInputRouterTests' --output /tmp/kma-s3-final-round-green.xml --timeout 600 -- -nographics` — passed 18/18.
- The focused 18 tests cover shared `KMA.inputactions` Sprint, Endurance, Boss, and Punishment action paths; keyboard tap/hold/swipe/alternate routing; EnhancedTouch-enabled `ScreenTapArea` ownership; strict UI exclusion; parent-handler/child-gameplay ownership; duplicate down; drag/up cleanup; disable flushing; lifecycle subscription idempotence; rhythm offset; and multi-pointer isolation.
- Full EditMode: `rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/kma-s3-final-editmode.xml --timeout 600 -- -nographics` — passed 149/149.
- Full PlayMode: `rtk ~/.local/bin/unity test . --mode PlayMode --output /tmp/kma-s3-final-playmode.xml --timeout 600 -- -nographics` — interrupted on request while repeatedly loading scenes after the headless TMP importer reported `No graphic device is available` for `MG_Boss`; no complete result count was produced.
- Aggregate scoped diff check: `rtk git diff --check -- Assets/_Project/Scripts/Input Assets/Tests/PlayMode/Input` — passed with no output. Required `Assets/Tests/PlayMode/Input.meta` is already tracked; no unrelated folder metadata was staged.

## Commit SHA

`dcb5fbd3edceeb87aaaad5c3f82bc52d1259a637` — final broad-review fix.

## Concerns

- Full PlayMode remains unverified because the headless TMP package-importer path requires a graphics-capable environment; focused PlayMode and full EditMode are green.
- The shared router and tap area are additive only; scenes and legacy controllers remain intentionally unwired for later S3 integration work.
- Touch ownership is intentionally centralized in EnhancedTouch-enabled `ScreenTapArea`; shared action-map touch bindings do not create a second detector path.
