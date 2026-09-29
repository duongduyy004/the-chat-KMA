# One UI Kit Across Sprint, Football and Volleyball

**Date:** 2026-09-29
**Status:** Approved in conversation (target style, identical tokens, Football on shared screens, scope)

## Goal

The three minigames look like one game. Buttons, joysticks, sliders, bars, panels, chips,
countdown, pause and result screens share one visual language: the Sprint navy "broadcast"
style. Every game keeps its own controls and gameplay; only how they look changes, plus
Football moving onto the shared countdown, pause and result screens.

## Decisions

- **Target style:** the Sprint navy style (`SprintUiTheme`): translucent navy surfaces,
  yellow accent, rounded corners, soft drop shadow. The neo-brutalist `UITheme` was
  considered and rejected for minigames; it stays for Menu and Map.
- **Identical tokens:** no per-game accent colour. Volleyball's orange action button and
  Football's lime shoot button both become the shared accent.
- **Football moves onto the shared screens:** `PhaseOverlay` countdown, `PausePanel` and
  `ResultPanel` replace its own countdown panel, back button and result panel. Its
  difficulty picker stays.
- **Scope:** the shared prefabs (`HUD_Minigame`, `PhaseOverlay`, `ResultPanel`) are edited
  in place, so `GameOver` and `Punishment` also turn navy. `Menu` and `Map` are untouched.
- **Approach:** a shared UI kit with sprites baked to assets (approach A). Rejected:
  building all UI at runtime (approach B, large rewrite) and a runtime skin component that
  only recolours existing UI (approach C, leaves sizes, layout and press states
  inconsistent).

## Current state (before)

| | Sprint | Football | Volleyball |
|---|---|---|---|
| Built by | Runtime code | Editor configurator writes the scene | Editor configurator plus shared S2 prefabs |
| Dark surface | #08233D | #123C34 / #0E2B39 | #0A283D |
| Accent | #FFCA3A | #FFCA28 (#FFD34C at runtime) | #FF9800 (#FF9722 at runtime) |
| Shapes | Runtime anti-aliased rounded rects | `panel.png` 9-slice | Built-in `Knob.psd` stretched into ellipses |
| Shadow | uGUI Shadow (0,-4) | None | Action button only |
| Countdown / pause / result | Own countdown and pause, shared brutalist result | All its own, no pause | Shared brutalist prefabs |

## Part 1: tokens and sprites

All kit code lives in the existing `KMA.Gameplay.UI` assembly under `Scripts/UI/Kit/`,
namespace `KMA.UI.Kit`. Sprint already references this assembly. `KMA.Gameplay.Football`
and `KMA.Gameplay.Volleyball` add a reference to it. `KMA.Gameplay.UI` references neither
game, so this adds no cycle.

### `MinigameUiTheme`

A static class that moves `SprintUiTheme` into the kit with every existing value unchanged.
`SprintUiTheme` is deleted and its callers and tests switch to `MinigameUiTheme`.
`WithAlpha` and `ContrastRatio` move with it.

| Group | Tokens |
|---|---|
| Colours | `Surface` #08233D, `TextPrimary` #FFF9E7, `Accent` #FFCA3A, `Player` #3AE6FF, `Energy` #FF595E, `TextOutline` #031221 |
| New colours | `Success` #5EDE8C, `Track` = white at 22% alpha, `Scrim` = #031221 at 70% alpha |
| Surface alphas | `SurfaceOpaque` .92, `SurfaceSoft` .82, `SurfaceControl` .42, `SurfaceControlActive` .55 |
| Font sizes | `Display` 160, `Title` 54, `Headline` 48, `BodyLarge` 40, `Body` 32, `Caption` 24; minimum 24 |
| Radii | `RadiusPanel` 24, `RadiusControl` 36, `RadiusPause` 20; bars use half their height; `BorderWidth` 3 |
| Spacing | `SpaceXs` 8, `SpaceSm` 16, `SpaceMd` 24, `SpaceLg` 32 |
| Shadow and press | Shadow black at 35% alpha, offset (0,-4); press scale 0.94, restore after 0.1 s |
| Sizes (reference px, 1080 high) | `ButtonHeight` 88, `RoundButton` 220, `JoystickBase` 224, `JoystickKnob` 112, `BarHeight` 32 |

Colour roles: `Accent` for primary buttons, countdown and bar fills; `Player` for the
player; `Energy` for warnings, the opponent and failure; `Success` for a win.

The old per-game colours map onto the tokens:

| Old | New |
|---|---|
| Volleyball Ink #0A283D, Football Ink #0E2B39, direction panel #123C34 | `Surface` |
| Volleyball cyan #4BE6F4 | `Player` |
| Volleyball coral #FF7753, Football warning #FF9875, error #BE2D1C | `Energy` |
| Volleyball orange #FF9800, Football gold #FFCA28 / #FFD34C, lime #D6F789, crosshair #E0FF96 | `Accent` |
| Football cards #F8FAFC | `Surface` card with `TextPrimary` text |

### Baked sprites

- `SprintUiShapes.Coverage` becomes `UiShapeRaster`, a pure function that returns
  `Color32[]` and works at runtime and in the editor. `SprintUiShapes` is deleted; Sprint no
  longer creates textures at runtime.
- Editor menu `KMA/UI/Bake UI Kit Sprites` (in `Assets/Editor`) writes white, anti-aliased
  PNGs with their 9-slice border to `Assets/_Project/Art/UI/Kit/`:

| File | Use |
|---|---|
| `RoundRect20.png` | Pause button |
| `RoundRect24.png` | Panels, chips, bars |
| `RoundRect36.png` | Buttons, control plates |
| `Circle.png` (128 px) | Joystick, slider knob, round button, dots |
| `Ring.png` (128 px, ring width 3/64 of the diameter) | Joystick and round button rims |

- Import settings: Sprite, bilinear, no mipmaps, 100 PPU, border equal to the radius.
- **`UiKitAssets`** (ScriptableObject, `Assets/_Project/Settings/UI/UiKitAssets.asset`)
  references the five sprites, the `Baloo2-ExtraBold` font asset and the
  `Baloo2-ExtraBold-TextStrokeDark` material. Editor code loads it with `AssetDatabase`.
  At runtime Sprint gets it from a serialized field on `SprintHud` that the scene
  configurator assigns, so no `Resources` folder is needed.
- Unused afterwards: Volleyball's `Knob.psd` reference and Football's
  `Art/Football/GoalView/{panel,fill,knob}.png`. Delete the Football files that nothing
  references anymore; check first whether `fill.png` is used outside UI (for example by the
  goal).

## Part 2: widgets

`UiKit` is a static class. Each function takes a `RectTransform parent`, creates GameObjects,
assigns sprites from `UiKitAssets` and returns a small handle holding its parts. The same
functions run at runtime (Sprint) and in the editor (configurators, prefab rebuild). **The
kit decides look and feedback; each game decides position and size.**

| Widget | Look | Used by |
|---|---|---|
| `Panel(radius, alpha, shadow)` | `RoundRect` tinted `Surface`, alpha .92 by default, shadow (0,-4) | Scoreboards, cards, Football aim panel |
| `Chip(text)` | Panel at .82 plus `Body` text, sized to its text | Volleyball hint, Sprint instruction plate and mode chip, Football shot feedback |
| `Label(text, size, color, outline)` | TMP with Baloo2; `outline` uses the dark stroke material | All text |
| `Button(label, variant)` | `RoundRect36`, height 88. **Primary:** `Accent` fill, `Surface` text. **Secondary:** Surface .42 fill, 3 px `Accent` border, `TextPrimary` text. **Danger:** `Energy` fill, `TextPrimary` text | Football shoot, difficulty and start buttons; pause and result buttons |
| `RoundButton(label)` | Circle shadow, `Ring` in `TextPrimary`, `Accent` face, `Surface` text at `Headline` | Volleyball ĐÁNH |
| `ControlPlate(arrow, label)` | Today's Sprint control: Surface .42, `Accent` border .25, radius 36 | Sprint tap zones |
| `Bar(showPip, showLabel)` | `Track` background, `Accent` fill, radius half the height (`pixelsPerUnitMultiplier`). The fill is a sliced child sized by `anchorMax.x`, so both ends stay round. Optional `Player` pip | Sprint progress, Football power |
| `Slider()` | uGUI `Slider`, track like `Bar`, `Circle` knob in `Accent` with a `Surface` `Ring` | Football aim |
| `Joystick()` | Base `Circle` Surface .42 plus `Ring` `Accent` .25; knob `Circle` `TextPrimary` .9 | Volleyball |
| `PauseButton()` | Sprint's 88 px square, radius 20, two bars | Pause trigger in all three games |
| `Countdown()` | `Display` 160, `Accent`, `TextOutline` stroke | `PhaseOverlay`, Sprint countdown |

The font comes from `UiKitAssets`. Sprint stops borrowing the font of the first TMP text in
the canvas, and `PausePanel` stops using legacy `UnityEngine.UI.Text`.

### Shared interaction states

`KitPressFeedback` replaces the colour and scale code in `ActionButton`,
`SprintControlPresenter` and Football's `ColorTint` transitions. It applies to every control:

| State | Fill | Border |
|---|---|---|
| Rest | Surface .42 | `Accent` .25 |
| Hint (Sprint's expected side; joystick while held) | Surface .55 | `Accent` .75 |
| Pressed | Face 15% lighter | unchanged |

Pressing scales the control to 0.94 and restores it after 0.1 s. Input logic is unchanged:
`VirtualJoystick`, `ActionButton` (fires on pointer down), `FootballHoldButton` and
`ScreenTapArea` keep their behaviour and only lose their hard-coded colours.

## Part 3: applying the kit

### Shared screens

- **`ResultPanel`** is rebuilt with the kit: `Scrim` backdrop; a 900×720 `Panel` card;
  status at `Title` in `Success` or `Energy`; score at `Display` in `Accent`; rank at
  `Headline`; **Chơi lại** (Primary) and **Tiếp tục** (Secondary). It now implements
  `IRetryResultPreviewPanel` and gains:
  - an optional detail line (for example "4/5 BÀN" and the lives left);
  - the Chơi lại button, shown only when `RetryAvailable` (Sprint and Volleyball keep it
    false, so their behaviour does not change);
  - the pending and error states from Football's `SetActionPending`.

  The scrim, scale and score-count animation of `SprintResultPresentation` moves into
  `ResultPanel`, so all three games get it. `FootballResultPanel` is deleted.
- **`PhaseOverlay`** stops comparing class names (`"SprintController"`,
  `"VolleyballController"`). `MinigameBase` gains two virtual properties,
  `UsesSharedStartPresentation` and `UsesSharedCountdown`, both `true` by default:

| Game | Shared tutorial | Shared countdown |
|---|---|---|
| Sprint | off | off (its own countdown is built with `UiKit.Countdown`, so it looks the same) |
| Volleyball | off | on |
| Football | off (it has the difficulty picker) | **on**, replacing the "SẴN SÀNG!" panel; Football's lifecycle already passes through `Countdown` |

  Its countdown root uses `UiKit.Countdown` and its tutorial root uses `Chip`.
- **`PausePanel`** and the pause block in **`MinigameUIAssembler`**: the trigger is
  `UiKit.PauseButton` at the top right. The menu is a 560×440 `Panel` card with a `Headline`
  title and three buttons: Tiếp tục (Primary), Chơi lại (Secondary), Thoát (Danger). Sprint
  keeps its current pause behaviour; only the button is shared.
- **`HUD_Minigame`**: timer, phase and score use `Label` and `Chip`; progress and stamina use
  `Bar`; hearts use `Circle` in `Energy`. All three games hide these widgets, so the change
  shows in `GameOver` and `Punishment`.

### Sprint (smallest change)

`SprintUiLayout` and `SprintChromeLayout` stay; the height-based layout is fine.
`SprintFestivalPresentation` and `SprintControlPresenter` drop their private Rect, Panel and
Text helpers and call `UiKit.*`: progress rail is `Bar(showPip)`, scoreboard is `Panel`,
tap zones are `ControlPlate`, instruction plate and mode chip are `Chip`.

### Volleyball

The configurator drops its palette and `Knob.psd` and calls the kit:

- Joystick: `UiKit.Joystick`, same position and radius 88. `VirtualJoystick` reads its
  state colours from the theme.
- ĐÁNH: `RoundButton` at 220; the hit area stays 310.
- Scoreboard: `Panel` 650×84 with **real rounded corners instead of an ellipse**; PLAYER in
  `Player`, ENEMY in `Energy`, score at `Title`.
- Feedback: `Headline` in `Accent` with the stroke. Hint: `Chip`.
- World markers are recoloured only: Ink edge to `Surface`, cyan to `Player`, coral to
  `Energy`. Shape and position stay, so the marker tests still hold.

### Football (largest change)

- **Canvas:** remove the `FootballHUD` canvas and `MinigamePresentationOwner`. The assembler
  then adds `S2_HUD_Minigame`, `PhaseOverlay`, `ResultPanel` and `PausePanel`, as for
  Volleyball. Football's HUD is built under `SafeAreaRoot`, and the prefab's default widgets
  are hidden.
- **Top:** score `Chip` at top centre. A "LƯỢT CÒN" `Chip` at top right holds five `Circle`
  dots: `Accent` for a goal, `Energy` for a miss, `Track` for a shot not taken yet. They
  replace the •/●/× glyphs. **The back button "‹" is replaced by `PauseButton`**; the
  player leaves through Thoát in the pause menu.
- **Bottom left:** aim `Panel` with a `Caption` title, a `Body` value in `Accent` and
  `UiKit.Slider`.
- **Bottom right:** power `Bar` with a % label. Over power, the fill and the warning text
  turn `Energy`. **GIỮ ĐỂ SÚT** is a Primary `Button` that keeps `FootballHoldButton`.
- **Start screen:** navy `Panel` card. Difficulty buttons are Secondary and the selected one
  is Primary; colours come from the theme, which removes the #FFD34C / #FFCA28 mismatch.
  BẮT ĐẦU is Primary.
- **Code:** `FootballController` keeps its flow. Retry, Continue and Thoát go through
  `ResultPanel` and `PausePanel`. While paused, the controller suspends through
  `IsSuspended`, which it already uses. `FootballHud` loses `ShowCountdown` and
  `HideCountdown`.

### Risk to check first

`GameplayPauseFlowController` Restart and Thoát must work in the Football scene. The plan
verifies this before building on it.

## Part 4: tests and verification

### Tests to update

| Test | Change |
|---|---|
| `SprintUiThemeTests`, `SprintUiShapesTests` | Become `MinigameUiThemeTests` (plus `Success` contrast on `Surface`) and `UiShapeRasterTests` |
| `SprintPresentationGateTests` (PlayMode) | Use `MinigameUiTheme` |
| `FootballSceneConfiguratorTests` | **Inverted:** the scene must have `S2_HUD_Minigame`, `PhaseOverlay`, `ResultPanel` and `PausePanel`, and must not have `FootballResultPanel`, `MinigamePresentationOwner` or the `FootballHUD` canvas |
| `FootballSceneTests` (PlayMode) and the `SceneRouter` Football Retry/Continue tests | Go through the shared `ResultPanel` |
| `PauseFlowTests` | Add a Football scene case (covers the risk above) |
| `VolleyballSceneConfiguratorTests` | Add "no `Knob.psd`" |
| `UIThemeTests` | Unchanged (Menu and Map still use it) |

### New tests

- `UiKitAssetsTests`: the asset exists, holds all five sprites, the font and the material;
  each sprite's 9-slice border equals its radius; import is bilinear.
- `UiKitTests` (EditMode): each widget has its parts, uses sprites from `UiKitAssets` and
  token colours, font size is at least 24, text on its background has contrast ≥ 4.5.
- `KitPressFeedbackTests`: rest, hint and pressed states; press scale and restore.
- `ResultPanelRetryTests`: retry hidden when `RetryAvailable` is false; pending and error
  states; detail line.
- `PhaseOverlay` tests: follows `UsesSharedStartPresentation` and `UsesSharedCountdown`.
- **`MinigameStyleConsistencyTests`**, a guard in the style of `HeroConsistencyTests`. It
  opens `MG_Football`, `MG_Volleyball`, `GameOver` and `Punishment` in EditMode, and
  `MG_Sprint` in PlayMode (its UI is built at runtime). For every minigame canvas:
  - every `Image` uses a sprite from `UiKitAssets`, except transparent hit areas (alpha 0)
    and the scrim; no built-in `Knob` or `UISprite`;
  - every `TMP_Text` uses Baloo2 at size 24 or more;
  - every colour, ignoring alpha, is a `MinigameUiTheme` token;
  - no legacy `UnityEngine.UI.Text`.

### Visual check

Use the `testing-unity-ui-with-screenshots` skill to capture before and after for each game
in play, countdown, pause and result, plus Football's difficulty screen, and put them side
by side for review.

### Done when

- The full EditMode and PlayMode suites pass in Unity batch mode.
- The screenshots show the three games in one visual language.
- `SprintUiTheme`, `SprintUiShapes`, `FootballResultPanel` and the unreferenced Football
  sprites are deleted.

## Out of scope

- Menu, Map, Settings and other non-minigame screens (they keep `UITheme`).
- Gameplay, input behaviour and minigame layout beyond what is listed above.
- New animations other than moving Sprint's result animation into `ResultPanel`.
