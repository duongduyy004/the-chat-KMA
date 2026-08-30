# Task 3 Report: Router and ScreenTapArea Ownership

## Files

- `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- `Assets/_Project/Scripts/Input/ScreenTapArea.cs`
- `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`
- `Assets/Tests/PlayMode/Input/KMA.Input.PlayMode.Tests.asmdef`

## Tests and outcomes

- Focused PlayMode fix evidence: `rtk ~/.local/bin/unity test . --mode PlayMode --filter 'KMA.Tests.Input.GameplayInputRouterTests' --output /tmp/kma-s3-router-review-green.xml --timeout 600 -- -nographics` — passed 9/9 for the round 1 fix.
- Round 2 focused PlayMode: `rtk ~/.local/bin/unity test . --mode PlayMode --filter 'KMA.Tests.Input.GameplayInputRouterTests' --output /tmp/kma-s3-router-round2-green.xml --timeout 600 -- -nographics` — passed 11/11, including parent-handler/child-gameplay ownership and interactable UI-child rejection.
- Full EditMode: `rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/kma-s3-review-editmode.xml --timeout 600 -- -nographics` — passed 149/149.
- Full PlayMode was attempted with `/tmp/kma-s3-review-playmode.xml`; it was blocked before results were written by the known headless TMP package-importer path in `MG_Boss` (`No graphic device is available`).

## Commit SHA

`53938c6082deecbda0858780cdfb1af29c399c9a` — `fix: preserve child gameplay tap ownership`

## Concerns

- Raw/EnhancedTouch routing was removed. Touch controls in the shared action maps are intentionally ignored by the router callbacks; `ScreenTapArea` is the sole gameplay-touch ownership boundary, while only keyboard controls feed action callbacks.
- The shared router and tap area are additive only; scenes and legacy controllers remain intentionally unwired for later S3 integration work.
- Full PlayMode needs a graphics-capable environment or the TMP importer issue resolved before it can provide a complete XML result.
