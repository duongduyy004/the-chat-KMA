# Chess Final Exam and Celebration Scene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fourth journey stop, "Bài kiểm tra cuối", where Tân Thủ (White) must mate in 2 against Cô Thể Chất within 90 s and 2 recoverable mistakes. Winning it completes the course and opens a skippable "Đã qua thể chất!" celebration scene.

**Architecture:**
- A pure C# chess core (`KMA.Gameplay.Chess.Core`, no `UnityEngine`) owns the board model, legal moves, puzzle grading, the think clock and the level state machine.
- Puzzles are offline-verified JSON produced by `tools/chess/*.py` with python-chess + Stockfish. Game and tests grade only against that exhaustive solution tree; a C# verifier re-checks every shipped file.
- The level plugs into the existing challenge pipeline as `SubjectId.Chess` / `ChallengeKind.Final` / `chess_final` (10th catalog entry). `JourneyProgress` exempts `Final` from lives and frog jumps.
- `SceneRouter` gains a `Celebration` route. The celebration scene reads a `CelebrationSummary` built from the session and only writes `celebrationSeen`.

**Tech Stack:** Unity 6000.3.23f1, C# (asmdef-split), uGUI + TextMeshPro, NUnit EditMode/PlayMode via `tools/run-unity-tests.sh`, Python 3.14 + `chess` + `zstandard`, Stockfish (Windows x86-64, authoring only), Node + `@resvg/resvg-js` (piece art).

**Spec:** `docs/superpowers/specs/2026-10-06-chess-final-celebration-design.md`

## Global Constraints

- Level config (demo, Normal): mate in **2** player moves, player **White**, **90 s** think time, **2** recoverable mistakes (the 3rd ends the attempt). The clock runs **only** in `PlayerTurn`.
- Grading order: illegal → `Illegal` (no mistake); board is checkmate → `Solved` (even outside the tree); move in tree → `Accepted`, boss plays the **first** reply; anything else → `Wrong`.
- `Final` challenges never touch `failCounts`, never cost a life, never create a frog jump, and may start at 0 lives. No chess code may reference FrogJump types, sprites or scenes.
- New enum members go **at the end**: `SubjectId.Chess = 8`, `ChallengeKind.Final`, `SessionRoute.Celebration`.
- `SaveData.CurrentVersion` 8 → 9. Saves of version ≥ 7 keep their journey; only `< 7` goes through `MigrateLegacy`.
- Character names stay unchanged: left panel **"Tân Thủ"** (sprite `MaleAdventurer`), right panel **"Cô Thể Chất"** (sprite `BossPE`). The dialogue portrait for Cô Thể Chất switches from `FemaleAdventurer` to `BossPE`.
- Import only `BossPE/` from `boss-final-level-assets.zip`. Never stage the zip, `StudentPenalty/`, `PROMPTS.md`, `CHESS-AND-FROG-PROMPTS.md` or `tools/chess/stockfish/`. PPU 200, pivot Bottom Center. Never rewrite an existing `.meta`.
- UI copy is Vietnamese, short, with no em dash, no English, and no FEN/UCI/engine names. Every TMP string goes through `VietText.Fix`. Fixed strings:
  - Labels: `Bài kiểm tra cuối`, `Chiếu hết trong 2 nước`, `Lượt của bạn`, `Lượt giảng viên`, `Sai: x/2`, `Gợi ý`, `Bắt đầu`, `Chơi lại`, `Hết giờ`.
  - Teacher lines: `Chưa đúng. Em xem lại.`, `Còn một lần sửa.`, `Được, em qua.`
  - Celebration: `Đã qua thể chất!`, `Bỏ qua`, `Về menu`.
- Commit directly to `master`. Commit messages carry **no** `Co-Authored-By` trailer (user memory rule). Always `git add` explicit paths.
- Unity must be closed for batch runs.
  - Tests: `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>`, then read `Builds/TestResults/<name>.xml`; Unity can exit 0 on failures.
  - Editor methods: `"$UNITY" -batchmode -projectPath . -executeMethod <Method> -quit -logFile Builds/<name>.log`, with `UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"`.

## Review Focus

1. A v8 save from a player who is mid-course must load into v9 with its journey intact (checkpoint, fail counts, dialogue flags). Today `SaveSystem.Migrate` sends every `version < Current` save through `MigrateLegacy`, which rebuilds the journey from passed subjects only. Pinned in Task 7 (`Load_Version8MidCourse_KeepsJourney`).
2. A double tap on "Tiếp tục" after the first chess win must route to Celebration exactly once. A second win after `celebrationSeen` must go to the Map. Pinned in Task 8.
3. A tap on a square while the boss animates, while paused or after the result must never submit a move or double-count. Pinned in Task 5 (state machine ignores `Submit` outside `PlayerTurn`) and Task 10 (view not interactable).
4. Underpromotion: a pawn reaching the last rank must offer all four pieces, and `a7a8n` must be graded as its own move, not as `a7a8q`. Pinned in Task 2 (perft position 4) and Task 3 (grader test).
5. The player has 0 lives, for example after failing `soccer_exam` repeatedly, and reaches the chess stop. The chess lesson card and `TryBegin` must still allow the attempt. Pinned in Task 6 and Task 12.

## File Structure

| File | Responsibility |
| --- | --- |
| `Assets/_Project/Scripts/Gameplay/Chess/Core/KMA.Gameplay.Chess.Core.asmdef` | Pure C# chess assembly (`noEngineReferences: true`) |
| `.../Chess/Core/ChessTypes.cs` | `PieceColor`, `Piece`, `Square`, `CastlingRights` |
| `.../Chess/Core/ChessMove.cs` | Move value type, UCI round trip |
| `.../Chess/Core/ChessPosition.cs` | Immutable position, FEN, `Apply` |
| `.../Chess/Core/MoveGenerator.cs` | Legal moves, attacks, check/mate/stalemate, perft |
| `.../Chess/Core/PuzzleDefinition.cs` | Serializable puzzle JSON model and shape validation |
| `.../Chess/Core/PuzzleVerifier.cs` | Full tree verification (coverage, mates, no missing alternatives) |
| `.../Chess/Core/PuzzleGrader.cs` | Grades one player move |
| `.../Chess/Core/PuzzleHints.cs` | Three hint levels |
| `.../Chess/Core/ThinkClock.cs` | Think-time clock |
| `.../Chess/Core/ChessFinalStateMachine.cs` | Level phases, mistakes, boss reply, restart |
| `Assets/_Project/Scripts/Gameplay/Chess/KMA.Gameplay.Chess.asmdef` | Unity side of the level |
| `.../Chess/ChessPuzzleLibrary.cs` | Loads `Resources/Chess/Puzzles/*.json` |
| `.../Chess/ChessBoardView.cs` | 64 squares, pieces, markers, selection, move animation |
| `.../Chess/ChessCastView.cs` | Two avatars, pose swaps, speech bubble |
| `.../Chess/ChessFinalHud.cs` | Title, objective, clock, mistakes, turn label, hint text |
| `.../Chess/PromotionPicker.cs` | Hậu/Xe/Tượng/Mã popup |
| `.../Chess/ChessFinalController.cs` | `MinigameBase` wiring, boss turn routine, result + metrics |
| `Assets/_Project/Scripts/Gameplay/Celebration/KMA.Gameplay.Celebration.asmdef` | Celebration assembly |
| `.../Celebration/CelebrationTimeline.cs` | Pure 11 s beat schedule + skip |
| `.../Celebration/CelebrationSceneController.cs` | Drives timeline, poses, confetti, summary, buttons |
| `Assets/_Project/Scripts/Progression/Journey/CelebrationSummary.cs` | Read-only course summary built from `GameSession` |
| `Assets/_Project/Scripts/Core/SceneRouter.Celebration.cs` | Celebration route |
| `Assets/Editor/ChessFinalSceneConfigurator.cs` | Builds `MG_ChessFinal.unity` |
| `Assets/Editor/CelebrationSceneConfigurator.cs` | Builds `Celebration.unity` |
| `tools/chess/fetch_candidates.py`, `verify_puzzles.py`, `puzzle_sources.json`, `requirements.txt`, `README.md` | Puzzle authoring |
| `tools/render-chess-pieces.js` | 12 piece PNGs + `SportIcon_Chess.png` |
| `Assets/_Project/Resources/Chess/Puzzles/*.json` | Verified puzzles |
| Modified | `SubjectId.cs`, `ChallengeDefinition.cs`, `ChallengeCatalog.cs`, `ChallengeContracts.cs`, `ChallengeAttempt.cs`, `JourneyProgress.cs`, `JourneyStateData.cs`, `JourneySaveMigration.cs`, `GameSession.cs`, `SaveData.cs`, `SaveSystem.cs`, `GameManager.cs`, `JourneyControllerAdapter.cs`, `SceneRouter.cs`, `SceneRouter.Journey.cs`, `S5RouteBootstrap.cs`, `ResultPanel.cs`, `JourneyDialoguePresenter.cs`, `MapScreen.cs`, `MapPresentationBuilder.cs`, `MapJourneyPathLayout.cs`, `JourneyLessonList.cs`, `JourneyLessonPresentation.cs`, `JourneyCourseSummary.cs`, `UITheme.cs`, `UITheme.asset`, `CharacterArt.cs`, `StudentJourneyContentBuilder.cs`, `PlayModeScreenshotQaStates.cs`, `.gitignore`, `README.md`, `PLAN.md` |

---

### Task 0: Preflight

**Files:** none

- [ ] **Step 1: Make sure the user's work in progress is committed**

Run: `git status --short`

The working tree had uncommitted user changes when this plan was written. Tasks 8 and 12 edit `ResultPanel.cs`, `MapPresentationBuilder.cs` and `JourneyLessonList.cs`, which are among those files. If any of `Assets/_Project/Scripts/UI/*.cs`, `Assets/_Project/Scenes/*.unity` or `Assets/Editor/*SceneConfigurator.cs` show ` M`, **stop and ask the user** to commit or stash them. Never commit, stash or revert them yourself.

- [ ] **Step 2: Confirm the toolchain**

Run:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -version; python --version; node --version
```
Expected: `6000.3.23f1`, Python 3.x, Node ≥ 18. If Node is missing, ask the user before installing it.

- [ ] **Step 3: Baseline the test suites**

Run:
```bash
tools/run-unity-tests.sh EditMode "" baseline-edit
tools/run-unity-tests.sh PlayMode "" baseline-play
```
Record pass/fail counts from both XML files in a scratch note. Later tasks must not add failures beyond this baseline.

---

### Task 1: Chess core types and FEN

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/KMA.Gameplay.Chess.Core.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/ChessTypes.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/ChessMove.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/ChessPosition.cs`
- Create: `Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/ChessPositionTests.cs`

**Interfaces:**
- Produces (namespace `KMA.Gameplay.Chess`):
  - `enum PieceColor { White, Black }`
  - `[Flags] enum CastlingRights { None = 0, WhiteKing = 1, WhiteQueen = 2, BlackKing = 4, BlackQueen = 8, All = 15 }`
  - `static class Piece`: `Pawn=1 … King=6`, `TypeOf(sbyte)`, `ColorOf(sbyte)`, `Make(int, PieceColor)`, `ToFen(sbyte)`, `FromFen(char)`
  - `static class Square`: `File`, `Rank`, `At(file, rank)`, `Name(int)`, `Parse(string) → -1 on error`, `Opposite(PieceColor)`
  - `readonly struct ChessMove(int from, int to, int promotion = 0)`: `ToUci()`, `static bool TryParseUci(string, out ChessMove)`
  - `sealed class ChessPosition`: `StartFen`, `FromFen(string)` (throws `FormatException`), `ToFen()`, `this[int]`, `SideToMove`, `Castling`, `EnPassant` (-1 = none), `HalfmoveClock`, `FullmoveNumber`, `KingSquare(PieceColor)`, `Apply(ChessMove) → ChessPosition`

- [ ] **Step 1: Create the assemblies**

`Assets/_Project/Scripts/Gameplay/Chess/Core/KMA.Gameplay.Chess.Core.asmdef`:
```json
{
    "name": "KMA.Gameplay.Chess.Core",
    "rootNamespace": "KMA.Gameplay.Chess",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef`:
```json
{
    "name": "KMA.Gameplay.Chess.EditMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.Chess",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "KMA.Gameplay.Chess.Core"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": true,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Chess/ChessPositionTests.cs`:
```csharp
using System;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessPositionTests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        [TestCase(ChessPosition.StartFen)]
        [TestCase(Kiwipete)]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("rnbqkbnr/pp1ppppp/8/2pP4/8/8/PPP1PPPP/RNBQKBNR b KQkq c6 0 2")]
        public void FenRoundTrips(string fen) =>
            Assert.That(ChessPosition.FromFen(fen).ToFen(), Is.EqualTo(fen));

        [TestCase("")]
        [TestCase("8/8/8/8/8/8/8/8 w - - 0 1")]                       // no kings
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP w KQkq - 0 1")]   // 7 ranks
        [TestCase("rnbqkbnr/pppppppp/9/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x KQkq - 0 1")]
        public void InvalidFenThrows(string fen) =>
            Assert.Throws<FormatException>(() => ChessPosition.FromFen(fen));

        [Test]
        public void SquaresAndUciRoundTrip()
        {
            Assert.That(Square.Parse("a1"), Is.EqualTo(0));
            Assert.That(Square.Parse("h8"), Is.EqualTo(63));
            Assert.That(Square.Parse("e9"), Is.EqualTo(-1));
            Assert.That(Square.Name(Square.Parse("e4")), Is.EqualTo("e4"));
            Assert.That(ChessMove.TryParseUci("a7a8n", out ChessMove promo), Is.True);
            Assert.That(promo.Promotion, Is.EqualTo(Piece.Knight));
            Assert.That(promo.ToUci(), Is.EqualTo("a7a8n"));
            Assert.That(ChessMove.TryParseUci("e2e4", out ChessMove plain), Is.True);
            Assert.That(plain.Promotion, Is.EqualTo(0));
            Assert.That(ChessMove.TryParseUci("e2e4k", out _), Is.False);
            Assert.That(ChessMove.TryParseUci("z2e4", out _), Is.False);
        }

        [Test]
        public void ApplyDoublePushSetsEnPassantAndKeepsOriginalUnchanged()
        {
            ChessPosition start = ChessPosition.FromFen(ChessPosition.StartFen);
            ChessMove.TryParseUci("e2e4", out ChessMove move);
            ChessPosition next = start.Apply(move);
            Assert.That(next.ToFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));
            Assert.That(start.ToFen(), Is.EqualTo(ChessPosition.StartFen));
        }

        [Test]
        public void ApplyCastlingMovesTheRookAndDropsRights()
        {
            ChessPosition p = ChessPosition.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
            ChessMove.TryParseUci("e1g1", out ChessMove castle);
            Assert.That(p.Apply(castle).ToFen(), Is.EqualTo("r3k2r/8/8/8/8/8/8/R4RK1 b kq - 1 1"));
            ChessMove.TryParseUci("e1c1", out ChessMove longCastle);
            Assert.That(p.Apply(longCastle).ToFen(), Is.EqualTo("r3k2r/8/8/8/8/8/8/2KR3R b kq - 1 1"));
        }

        [Test]
        public void ApplyEnPassantRemovesTheCapturedPawn()
        {
            ChessPosition p = ChessPosition.FromFen("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3");
            ChessMove.TryParseUci("e5f6", out ChessMove ep);
            Assert.That(p.Apply(ep).ToFen(), Is.EqualTo("rnbqkbnr/ppp1p1pp/5P2/3p4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3"));
        }

        [Test]
        public void ApplyPromotionAndRookCaptureClearsRights()
        {
            ChessPosition p = ChessPosition.FromFen("r3k3/1P6/8/8/8/8/8/4K3 w q - 0 1");
            ChessMove.TryParseUci("b7a8n", out ChessMove promo);
            Assert.That(p.Apply(promo).ToFen(), Is.EqualTo("N3k3/8/8/8/8/8/8/4K3 b - - 0 1"));
        }

        [Test]
        public void ApplyRejectsMovingTheWrongSide()
        {
            ChessPosition start = ChessPosition.FromFen(ChessPosition.StartFen);
            ChessMove.TryParseUci("e7e5", out ChessMove move);
            Assert.Throws<InvalidOperationException>(() => start.Apply(move));
        }
    }
}
```

- [ ] **Step 3: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-core-1`
Expected: compilation errors (types missing), so no results XML. Check `Builds/TestResults/chess-core-1.log` for `CS0246`.

- [ ] **Step 4: Implement `ChessTypes.cs`**

```csharp
using System;

namespace KMA.Gameplay.Chess
{
    public enum PieceColor { White, Black }

    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKing = 1,
        WhiteQueen = 2,
        BlackKing = 4,
        BlackQueen = 8,
        All = 15
    }

    /// Pieces are signed bytes: positive White, negative Black, 0 empty.
    public static class Piece
    {
        public const sbyte None = 0;
        public const int Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6;
        const string Letters = " pnbrqk";

        public static int TypeOf(sbyte piece) => piece < 0 ? -piece : piece;
        public static PieceColor ColorOf(sbyte piece) => piece > 0 ? PieceColor.White : PieceColor.Black;
        public static sbyte Make(int type, PieceColor color) => (sbyte)(color == PieceColor.White ? type : -type);

        public static char ToFen(sbyte piece)
        {
            char letter = Letters[TypeOf(piece)];
            return piece > 0 ? char.ToUpperInvariant(letter) : letter;
        }

        public static sbyte FromFen(char c)
        {
            int type = Letters.IndexOf(char.ToLowerInvariant(c));
            if (type <= 0) throw new FormatException($"Unknown piece '{c}'.");
            return Make(type, char.IsUpper(c) ? PieceColor.White : PieceColor.Black);
        }
    }

    /// Square index = rank * 8 + file, a1 = 0, h8 = 63.
    public static class Square
    {
        public static int File(int square) => square & 7;
        public static int Rank(int square) => square >> 3;
        public static int At(int file, int rank) => rank * 8 + file;
        public static string Name(int square) => $"{(char)('a' + File(square))}{Rank(square) + 1}";
        public static PieceColor Opposite(PieceColor color) =>
            color == PieceColor.White ? PieceColor.Black : PieceColor.White;

        public static int Parse(string name)
        {
            if (name == null || name.Length != 2) return -1;
            int file = name[0] - 'a', rank = name[1] - '1';
            return file >= 0 && file < 8 && rank >= 0 && rank < 8 ? At(file, rank) : -1;
        }
    }
}
```

- [ ] **Step 5: Implement `ChessMove.cs`**

```csharp
using System;

namespace KMA.Gameplay.Chess
{
    public readonly struct ChessMove : IEquatable<ChessMove>
    {
        const string PromotionLetters = "nbrq"; // Knight = 2 ... Queen = 5

        public int From { get; }
        public int To { get; }
        /// Piece type to promote to, 0 when the move is not a promotion.
        public int Promotion { get; }

        public ChessMove(int from, int to, int promotion = 0)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }

        public string ToUci() => Square.Name(From) + Square.Name(To) +
            (Promotion == 0 ? string.Empty : PromotionLetters[Promotion - Piece.Knight].ToString());

        public static bool TryParseUci(string text, out ChessMove move)
        {
            move = default;
            if (text == null || (text.Length != 4 && text.Length != 5)) return false;
            int from = Square.Parse(text.Substring(0, 2));
            int to = Square.Parse(text.Substring(2, 2));
            if (from < 0 || to < 0 || from == to) return false;
            int promotion = 0;
            if (text.Length == 5)
            {
                int index = PromotionLetters.IndexOf(text[4]);
                if (index < 0) return false;
                promotion = Piece.Knight + index;
            }
            move = new ChessMove(from, to, promotion);
            return true;
        }

        public bool Equals(ChessMove other) => From == other.From && To == other.To && Promotion == other.Promotion;
        public override bool Equals(object obj) => obj is ChessMove other && Equals(other);
        public override int GetHashCode() => (From * 64 + To) * 8 + Promotion;
        public override string ToString() => ToUci();
    }
}
```

- [ ] **Step 6: Implement `ChessPosition.cs`**

```csharp
using System;
using System.Text;

namespace KMA.Gameplay.Chess
{
    /// Immutable: Apply returns a new position, so a snapshot is just a kept reference.
    public sealed class ChessPosition
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        readonly sbyte[] board;

        public PieceColor SideToMove { get; }
        public CastlingRights Castling { get; }
        public int EnPassant { get; }
        public int HalfmoveClock { get; }
        public int FullmoveNumber { get; }

        ChessPosition(sbyte[] board, PieceColor side, CastlingRights castling, int enPassant, int halfmove,
            int fullmove)
        {
            this.board = board;
            SideToMove = side;
            Castling = castling;
            EnPassant = enPassant;
            HalfmoveClock = halfmove;
            FullmoveNumber = fullmove;
        }

        public sbyte this[int square] => board[square];

        public int KingSquare(PieceColor color)
        {
            sbyte king = Piece.Make(Piece.King, color);
            for (int i = 0; i < 64; i++)
                if (board[i] == king) return i;
            throw new InvalidOperationException($"No {color} king on the board.");
        }

        public static ChessPosition FromFen(string fen)
        {
            string[] parts = (fen ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts.Length > 6) throw new FormatException("A FEN needs 4 to 6 fields.");

            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8) throw new FormatException("A FEN board needs 8 ranks.");
            var board = new sbyte[64];
            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i, file = 0;
                foreach (char c in ranks[i])
                {
                    if (c >= '1' && c <= '8') file += c - '0';
                    else
                    {
                        if (file > 7) throw new FormatException("A FEN rank is too long.");
                        board[Square.At(file++, rank)] = Piece.FromFen(c);
                    }
                    if (file > 8) throw new FormatException("A FEN rank is too long.");
                }
                if (file != 8) throw new FormatException("A FEN rank must cover 8 files.");
            }
            if (Count(board, Piece.Make(Piece.King, PieceColor.White)) != 1 ||
                Count(board, Piece.Make(Piece.King, PieceColor.Black)) != 1)
                throw new FormatException("A FEN needs exactly one king per side.");

            PieceColor side = parts[1] == "w" ? PieceColor.White
                : parts[1] == "b" ? PieceColor.Black
                : throw new FormatException("Side to move must be w or b.");

            CastlingRights castling = CastlingRights.None;
            if (parts[2] != "-")
            {
                foreach (char c in parts[2])
                {
                    castling |= c switch
                    {
                        'K' => CastlingRights.WhiteKing,
                        'Q' => CastlingRights.WhiteQueen,
                        'k' => CastlingRights.BlackKing,
                        'q' => CastlingRights.BlackQueen,
                        _ => throw new FormatException($"Unknown castling flag '{c}'.")
                    };
                }
            }

            int enPassant = -1;
            if (parts[3] != "-")
            {
                enPassant = Square.Parse(parts[3]);
                if (enPassant < 0 || (Square.Rank(enPassant) != 2 && Square.Rank(enPassant) != 5))
                    throw new FormatException("Invalid en passant square.");
            }

            int halfmove = parts.Length > 4 ? ParseCounter(parts[4], 0) : 0;
            int fullmove = parts.Length > 5 ? ParseCounter(parts[5], 1) : 1;
            return new ChessPosition(board, side, castling, enPassant, halfmove, fullmove);
        }

        public string ToFen()
        {
            var text = new StringBuilder(90);
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    sbyte piece = board[Square.At(file, rank)];
                    if (piece == 0) { empty++; continue; }
                    if (empty > 0) { text.Append(empty); empty = 0; }
                    text.Append(Piece.ToFen(piece));
                }
                if (empty > 0) text.Append(empty);
                if (rank > 0) text.Append('/');
            }
            text.Append(SideToMove == PieceColor.White ? " w " : " b ");
            if (Castling == CastlingRights.None) text.Append('-');
            else
            {
                if ((Castling & CastlingRights.WhiteKing) != 0) text.Append('K');
                if ((Castling & CastlingRights.WhiteQueen) != 0) text.Append('Q');
                if ((Castling & CastlingRights.BlackKing) != 0) text.Append('k');
                if ((Castling & CastlingRights.BlackQueen) != 0) text.Append('q');
            }
            text.Append(' ').Append(EnPassant < 0 ? "-" : Square.Name(EnPassant));
            text.Append(' ').Append(HalfmoveClock).Append(' ').Append(FullmoveNumber);
            return text.ToString();
        }

        /// Applies a move that the generator produced. It does not check legality.
        public ChessPosition Apply(ChessMove move)
        {
            sbyte piece = board[move.From];
            if (piece == 0 || Piece.ColorOf(piece) != SideToMove)
                throw new InvalidOperationException($"{move} does not move a {SideToMove} piece.");
            int type = Piece.TypeOf(piece);
            sbyte captured = board[move.To];
            var next = (sbyte[])board.Clone();
            next[move.From] = 0;
            next[move.To] = move.Promotion != 0 ? Piece.Make(move.Promotion, SideToMove) : piece;

            bool enPassantCapture = type == Piece.Pawn && move.To == EnPassant && captured == 0;
            if (enPassantCapture)
                next[move.To + (SideToMove == PieceColor.White ? -8 : 8)] = 0;

            if (type == Piece.King && Math.Abs(move.To - move.From) == 2)
            {
                bool kingSide = move.To > move.From;
                int rookFrom = kingSide ? move.From + 3 : move.From - 4;
                int rookTo = kingSide ? move.From + 1 : move.From - 1;
                next[rookTo] = next[rookFrom];
                next[rookFrom] = 0;
            }

            int enPassant = type == Piece.Pawn && Math.Abs(move.To - move.From) == 16
                ? (move.From + move.To) / 2 : -1;
            CastlingRights rights = Castling & ~(LostRights(move.From) | LostRights(move.To));
            int halfmove = type == Piece.Pawn || captured != 0 || enPassantCapture ? 0 : HalfmoveClock + 1;
            int fullmove = SideToMove == PieceColor.Black ? FullmoveNumber + 1 : FullmoveNumber;
            return new ChessPosition(next, Square.Opposite(SideToMove), rights, enPassant, halfmove, fullmove);
        }

        static CastlingRights LostRights(int square) => square switch
        {
            4 => CastlingRights.WhiteKing | CastlingRights.WhiteQueen,
            7 => CastlingRights.WhiteKing,
            0 => CastlingRights.WhiteQueen,
            60 => CastlingRights.BlackKing | CastlingRights.BlackQueen,
            63 => CastlingRights.BlackKing,
            56 => CastlingRights.BlackQueen,
            _ => CastlingRights.None
        };

        static int Count(sbyte[] board, sbyte piece)
        {
            int count = 0;
            foreach (sbyte p in board) if (p == piece) count++;
            return count;
        }

        static int ParseCounter(string text, int minimum) =>
            int.TryParse(text, out int value) && value >= minimum
                ? value
                : throw new FormatException($"Invalid move counter '{text}'.");
    }
}
```

- [ ] **Step 7: Run the tests and confirm they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-core-1`
Expected: `ChessPositionTests` all pass (13 cases).

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess Assets/_Project/Scripts/Gameplay/Chess.meta Assets/Tests/EditMode/Gameplay/Chess Assets/Tests/EditMode/Gameplay/Chess.meta
git commit -m "feat(chess): add board model, moves and FEN"
```
(`.meta` files are created by Unity during the test run. Add every new `.meta` alongside its asset.)

---

### Task 2: Legal move generator

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/MoveGenerator.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/MoveGeneratorTests.cs`

**Interfaces:**
- Consumes: Task 1 types.
- Produces: `static class MoveGenerator`:
  - `List<ChessMove> LegalMoves(ChessPosition)`
  - `bool IsSquareAttacked(ChessPosition, int square, PieceColor by)`
  - `bool IsInCheck(ChessPosition)`
  - `bool IsCheckmate(ChessPosition)`
  - `bool IsStalemate(ChessPosition)`
  - `long Perft(ChessPosition, int depth)`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Chess/MoveGeneratorTests.cs`:
```csharp
using System.Linq;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class MoveGeneratorTests
    {
        // Reference counts from the Chess Programming Wiki "Perft Results" page.
        [TestCase(ChessPosition.StartFen, 1, 20L)]
        [TestCase(ChessPosition.StartFen, 2, 400L)]
        [TestCase(ChessPosition.StartFen, 3, 8902L)]
        [TestCase(ChessPosition.StartFen, 4, 197281L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2039L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97862L)]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4, 43238L)]
        [TestCase("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 3, 9467L)]
        [TestCase("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379L)]
        public void PerftMatchesReference(string fen, int depth, long expected) =>
            Assert.That(MoveGenerator.Perft(ChessPosition.FromFen(fen), depth), Is.EqualTo(expected));

        [Test]
        public void CastlingThroughAnAttackedSquareIsIllegal()
        {
            // The black rook on f8 covers f1, so White may castle queen side only.
            ChessPosition p = ChessPosition.FromFen("5r1k/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            string[] moves = MoveGenerator.LegalMoves(p).Select(m => m.ToUci()).ToArray();
            Assert.That(moves, Does.Not.Contain("e1g1"));
            Assert.That(moves, Does.Contain("e1c1"));
        }

        [Test]
        public void EnPassantThatExposesTheKingAlongTheRankIsIllegal()
        {
            ChessPosition p = ChessPosition.FromFen("8/8/8/K2pP2r/8/8/8/7k w - d6 0 1");
            Assert.That(MoveGenerator.LegalMoves(p).Select(m => m.ToUci()), Does.Not.Contain("e5d6"));
        }

        [Test]
        public void PromotionOffersAllFourPieces()
        {
            ChessPosition p = ChessPosition.FromFen("7k/P7/8/8/8/8/8/K7 w - - 0 1");
            string[] promotions = MoveGenerator.LegalMoves(p).Select(m => m.ToUci())
                .Where(u => u.StartsWith("a7a8")).OrderBy(u => u).ToArray();
            Assert.That(promotions, Is.EqualTo(new[] { "a7a8b", "a7a8n", "a7a8q", "a7a8r" }));
        }

        [Test]
        public void DetectsCheckmateAndStalemate()
        {
            ChessPosition mate = ChessPosition.FromFen("R5k1/5ppp/8/8/8/8/8/6K1 b - - 1 1");
            Assert.That(MoveGenerator.IsCheckmate(mate), Is.True);
            Assert.That(MoveGenerator.IsStalemate(mate), Is.False);

            ChessPosition stalemate = ChessPosition.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
            Assert.That(MoveGenerator.IsStalemate(stalemate), Is.True);
            Assert.That(MoveGenerator.IsCheckmate(stalemate), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess.MoveGeneratorTests" chess-movegen`
Expected: compile error, because `MoveGenerator` is not defined.

- [ ] **Step 3: Implement `MoveGenerator.cs`**

```csharp
using System.Collections.Generic;

namespace KMA.Gameplay.Chess
{
    public static class MoveGenerator
    {
        static readonly int[] KnightDf = { 1, 2, 2, 1, -1, -2, -2, -1 };
        static readonly int[] KnightDr = { 2, 1, -1, -2, -2, -1, 1, 2 };
        static readonly int[] KingDf = { 1, 1, 0, -1, -1, -1, 0, 1 };
        static readonly int[] KingDr = { 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] DiagDf = { 1, 1, -1, -1 };
        static readonly int[] DiagDr = { 1, -1, 1, -1 };
        static readonly int[] OrthoDf = { 1, -1, 0, 0 };
        static readonly int[] OrthoDr = { 0, 0, 1, -1 };
        static readonly int[] Promotions = { Piece.Queen, Piece.Rook, Piece.Bishop, Piece.Knight };

        public static List<ChessMove> LegalMoves(ChessPosition position)
        {
            var pseudo = new List<ChessMove>(64);
            AddPseudoMoves(position, pseudo);
            var legal = new List<ChessMove>(pseudo.Count);
            PieceColor mover = position.SideToMove;
            foreach (ChessMove move in pseudo)
            {
                ChessPosition next = position.Apply(move);
                if (!IsSquareAttacked(next, next.KingSquare(mover), Square.Opposite(mover)))
                    legal.Add(move);
            }
            return legal;
        }

        public static bool IsInCheck(ChessPosition position) => IsSquareAttacked(position,
            position.KingSquare(position.SideToMove), Square.Opposite(position.SideToMove));

        public static bool IsCheckmate(ChessPosition position) =>
            IsInCheck(position) && LegalMoves(position).Count == 0;

        public static bool IsStalemate(ChessPosition position) =>
            !IsInCheck(position) && LegalMoves(position).Count == 0;

        public static long Perft(ChessPosition position, int depth)
        {
            if (depth == 0) return 1;
            List<ChessMove> moves = LegalMoves(position);
            if (depth == 1) return moves.Count;
            long nodes = 0;
            foreach (ChessMove move in moves) nodes += Perft(position.Apply(move), depth - 1);
            return nodes;
        }

        public static bool IsSquareAttacked(ChessPosition p, int square, PieceColor by)
        {
            int f = Square.File(square), r = Square.Rank(square);
            // A white pawn attacks diagonally upward, so it sits one rank below the target.
            int pawnRank = by == PieceColor.White ? r - 1 : r + 1;
            sbyte pawn = Piece.Make(Piece.Pawn, by);
            if (Has(p, f - 1, pawnRank, pawn) || Has(p, f + 1, pawnRank, pawn)) return true;
            sbyte knight = Piece.Make(Piece.Knight, by);
            for (int i = 0; i < 8; i++)
                if (Has(p, f + KnightDf[i], r + KnightDr[i], knight)) return true;
            sbyte king = Piece.Make(Piece.King, by);
            for (int i = 0; i < 8; i++)
                if (Has(p, f + KingDf[i], r + KingDr[i], king)) return true;
            return SlidingAttack(p, f, r, DiagDf, DiagDr, by, Piece.Bishop) ||
                   SlidingAttack(p, f, r, OrthoDf, OrthoDr, by, Piece.Rook);
        }

        static bool SlidingAttack(ChessPosition p, int f, int r, int[] df, int[] dr, PieceColor by, int slider)
        {
            for (int d = 0; d < df.Length; d++)
            {
                int x = f + df[d], y = r + dr[d];
                while (OnBoard(x, y))
                {
                    sbyte piece = p[Square.At(x, y)];
                    if (piece != 0)
                    {
                        int type = Piece.TypeOf(piece);
                        if (Piece.ColorOf(piece) == by && (type == slider || type == Piece.Queen)) return true;
                        break;
                    }
                    x += df[d];
                    y += dr[d];
                }
            }
            return false;
        }

        static void AddPseudoMoves(ChessPosition p, List<ChessMove> moves)
        {
            PieceColor us = p.SideToMove;
            for (int sq = 0; sq < 64; sq++)
            {
                sbyte piece = p[sq];
                if (piece == 0 || Piece.ColorOf(piece) != us) continue;
                int f = Square.File(sq), r = Square.Rank(sq);
                switch (Piece.TypeOf(piece))
                {
                    case Piece.Pawn: AddPawnMoves(p, sq, f, r, us, moves); break;
                    case Piece.Knight: AddSteps(p, sq, f, r, KnightDf, KnightDr, us, moves); break;
                    case Piece.Bishop: AddSlides(p, sq, f, r, DiagDf, DiagDr, us, moves); break;
                    case Piece.Rook: AddSlides(p, sq, f, r, OrthoDf, OrthoDr, us, moves); break;
                    case Piece.Queen:
                        AddSlides(p, sq, f, r, DiagDf, DiagDr, us, moves);
                        AddSlides(p, sq, f, r, OrthoDf, OrthoDr, us, moves);
                        break;
                    case Piece.King:
                        AddSteps(p, sq, f, r, KingDf, KingDr, us, moves);
                        AddCastling(p, sq, us, moves);
                        break;
                }
            }
        }

        static void AddPawnMoves(ChessPosition p, int sq, int f, int r, PieceColor us, List<ChessMove> moves)
        {
            int dir = us == PieceColor.White ? 1 : -1;
            int startRank = us == PieceColor.White ? 1 : 6;
            int lastRank = us == PieceColor.White ? 7 : 0;
            int forward = r + dir;
            if (!OnBoard(f, forward)) return;

            int one = Square.At(f, forward);
            if (p[one] == 0)
            {
                AddPawnMove(sq, one, forward == lastRank, moves);
                if (r == startRank)
                {
                    int two = Square.At(f, r + 2 * dir);
                    if (p[two] == 0) moves.Add(new ChessMove(sq, two));
                }
            }

            for (int df = -1; df <= 1; df += 2)
            {
                int x = f + df;
                if (!OnBoard(x, forward)) continue;
                int target = Square.At(x, forward);
                sbyte victim = p[target];
                if ((victim != 0 && Piece.ColorOf(victim) != us) || (victim == 0 && target == p.EnPassant))
                    AddPawnMove(sq, target, forward == lastRank, moves);
            }
        }

        static void AddPawnMove(int from, int to, bool promotes, List<ChessMove> moves)
        {
            if (!promotes)
            {
                moves.Add(new ChessMove(from, to));
                return;
            }
            foreach (int type in Promotions) moves.Add(new ChessMove(from, to, type));
        }

        static void AddSteps(ChessPosition p, int sq, int f, int r, int[] df, int[] dr, PieceColor us,
            List<ChessMove> moves)
        {
            for (int i = 0; i < df.Length; i++)
            {
                int x = f + df[i], y = r + dr[i];
                if (!OnBoard(x, y)) continue;
                int target = Square.At(x, y);
                sbyte victim = p[target];
                if (victim == 0 || Piece.ColorOf(victim) != us) moves.Add(new ChessMove(sq, target));
            }
        }

        static void AddSlides(ChessPosition p, int sq, int f, int r, int[] df, int[] dr, PieceColor us,
            List<ChessMove> moves)
        {
            for (int d = 0; d < df.Length; d++)
            {
                int x = f + df[d], y = r + dr[d];
                while (OnBoard(x, y))
                {
                    int target = Square.At(x, y);
                    sbyte victim = p[target];
                    if (victim == 0) moves.Add(new ChessMove(sq, target));
                    else
                    {
                        if (Piece.ColorOf(victim) != us) moves.Add(new ChessMove(sq, target));
                        break;
                    }
                    x += df[d];
                    y += dr[d];
                }
            }
        }

        static void AddCastling(ChessPosition p, int kingSquare, PieceColor us, List<ChessMove> moves)
        {
            int home = us == PieceColor.White ? 4 : 60;
            if (kingSquare != home) return;
            PieceColor them = Square.Opposite(us);
            if (IsSquareAttacked(p, home, them)) return;
            sbyte rook = Piece.Make(Piece.Rook, us);
            CastlingRights kingSide = us == PieceColor.White ? CastlingRights.WhiteKing : CastlingRights.BlackKing;
            CastlingRights queenSide = us == PieceColor.White ? CastlingRights.WhiteQueen : CastlingRights.BlackQueen;

            if ((p.Castling & kingSide) != 0 && p[home + 3] == rook && p[home + 1] == 0 && p[home + 2] == 0 &&
                !IsSquareAttacked(p, home + 1, them) && !IsSquareAttacked(p, home + 2, them))
                moves.Add(new ChessMove(home, home + 2));
            if ((p.Castling & queenSide) != 0 && p[home - 4] == rook && p[home - 1] == 0 && p[home - 2] == 0 &&
                p[home - 3] == 0 && !IsSquareAttacked(p, home - 1, them) && !IsSquareAttacked(p, home - 2, them))
                moves.Add(new ChessMove(home, home - 2));
        }

        static bool OnBoard(int f, int r) => f >= 0 && f < 8 && r >= 0 && r < 8;

        static bool Has(ChessPosition p, int f, int r, sbyte piece) => OnBoard(f, r) && p[Square.At(f, r)] == piece;
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-movegen`
Expected: all `MoveGeneratorTests` and `ChessPositionTests` pass. If a perft count is off, split it per root move (`Perft(p.Apply(m), depth-1)` for each `m`) and compare against python-chess. Run `python -c "import chess; b=chess.Board(FEN); ..."` once python-chess is installed in Task 4, or install it now with `pip install chess`.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess/Core/MoveGenerator.cs Assets/_Project/Scripts/Gameplay/Chess/Core/MoveGenerator.cs.meta Assets/Tests/EditMode/Gameplay/Chess/MoveGeneratorTests.cs Assets/Tests/EditMode/Gameplay/Chess/MoveGeneratorTests.cs.meta
git commit -m "feat(chess): generate legal moves with perft coverage"
```

---

### Task 3: Puzzle model, verifier, grader and hints

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/PuzzleDefinition.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/PuzzleVerifier.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/PuzzleGrader.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/PuzzleHints.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/PuzzleGraderTests.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/PuzzleVerifierTests.cs`
- Test helper: `Assets/Tests/EditMode/Gameplay/Chess/TestPuzzles.cs`

**Interfaces:**
- Consumes: Tasks 1–2.
- Produces:
  - `[Serializable] PuzzleDefinition` with public fields `id, sourcePuzzleId, sourceFen, sourceMoves, startFen, playerColor ("w"), objective ("mate"), maxPlayerMoves, timeLimitSeconds, maxRecoverableMistakes, difficulty ("Easy"|"Normal"|"Hard"), ideaHint, verifiedBy, PuzzleNode[] nodes`. Also `PieceColor PlayerColor`, `bool TryValidateShape(out string error)`.
  - `PuzzleNode { PuzzleMove[] moves }`, `PuzzleMove { string uci; bool mate; PuzzleReply[] replies }`, `PuzzleReply { string uci; int next }`. Node 0 is the root.
  - `static class PuzzleVerifier { bool Verify(PuzzleDefinition, out string error) }`
  - `enum GradeKind { Illegal, Wrong, Accepted, Solved }`
  - `readonly struct GradeResult { GradeKind Kind; ChessMove Move; ChessPosition After; ChessMove? BossReply; int NextNode }`
  - `sealed class PuzzleGrader(PuzzleDefinition)` with `GradeResult Grade(ChessPosition position, int node, int playerMovesMade, ChessMove move)`. It throws `InvalidOperationException` when the puzzle data is inconsistent with the board.
  - `static class PuzzleHints { const int MaxLevel = 3; string For(PuzzleDefinition, int node, ChessPosition, int level); string PieceName(int type) }`

JSON shape is flat on purpose: JsonUtility cannot read dictionaries and warns on recursive types. A player move is a `PuzzleMove`; each `PuzzleReply` points at the node index where the player moves next. This refines the nested example in spec §2.1 without changing its meaning.

- [ ] **Step 1: Write the test helper**

`Assets/Tests/EditMode/Gameplay/Chess/TestPuzzles.cs`:
```csharp
using KMA.Gameplay.Chess;

namespace KMA.Tests.Gameplay.Chess
{
    internal static class TestPuzzles
    {
        // After 1.e4 e5 2.Bc4 Nc6. The tree pretends 3.Qh5 Nf6 4.Qxf7# is forced; that
        // is enough to exercise the grader, which never searches.
        public const string ScholarStart = "r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3";
        // After 3.Qh5 Nf6: Qxf7 is the only mate in one.
        public const string ScholarMateInOne = "r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4";

        public static PuzzleDefinition ScholarTwoMover() => new PuzzleDefinition
        {
            id = "test_m2", startFen = ScholarStart, playerColor = "w", objective = "mate",
            maxPlayerMoves = 2, timeLimitSeconds = 90f, maxRecoverableMistakes = 2, difficulty = "Normal",
            ideaHint = "Nhắm vào ô f7.",
            nodes = new[]
            {
                Node(Move("d1h5", false, Reply("g8f6", 1))),
                Node(Move("h5f7", true))
            }
        };

        public static PuzzleDefinition ScholarOneMover() => new PuzzleDefinition
        {
            id = "test_m1", startFen = ScholarMateInOne, playerColor = "w", objective = "mate",
            maxPlayerMoves = 1, timeLimitSeconds = 90f, maxRecoverableMistakes = 3, difficulty = "Easy",
            ideaHint = "Ô f7 chỉ có vua giữ.",
            nodes = new[] { Node(Move("h5f7", true)) }
        };

        public static PuzzleNode Node(params PuzzleMove[] moves) => new PuzzleNode { moves = moves };

        public static PuzzleMove Move(string uci, bool mate, params PuzzleReply[] replies) =>
            new PuzzleMove { uci = uci, mate = mate, replies = replies };

        public static PuzzleReply Reply(string uci, int next) => new PuzzleReply { uci = uci, next = next };

        public static ChessMove Uci(string text)
        {
            ChessMove.TryParseUci(text, out ChessMove move);
            return move;
        }
    }
}
```

- [ ] **Step 2: Write the failing grader tests**

`Assets/Tests/EditMode/Gameplay/Chess/PuzzleGraderTests.cs`:
```csharp
using System;
using KMA.Gameplay.Chess;
using NUnit.Framework;
using static KMA.Tests.Gameplay.Chess.TestPuzzles;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class PuzzleGraderTests
    {
        [Test]
        public void TreeMoveIsAcceptedAndTheBossPlaysTheFirstReply()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            var grader = new PuzzleGrader(puzzle);
            ChessPosition start = ChessPosition.FromFen(puzzle.startFen);

            GradeResult result = grader.Grade(start, 0, 0, Uci("d1h5"));

            Assert.That(result.Kind, Is.EqualTo(GradeKind.Accepted));
            Assert.That(result.BossReply.Value.ToUci(), Is.EqualTo("g8f6"));
            Assert.That(result.NextNode, Is.EqualTo(1));
            Assert.That(result.After.SideToMove, Is.EqualTo(PieceColor.Black));
        }

        [Test]
        public void LegalMoveOutsideTheTreeIsWrong()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            GradeResult result = new PuzzleGrader(puzzle).Grade(ChessPosition.FromFen(puzzle.startFen), 0, 0, Uci("d1f3"));
            Assert.That(result.Kind, Is.EqualTo(GradeKind.Wrong));
        }

        [Test]
        public void IllegalMoveIsIllegal()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            GradeResult result = new PuzzleGrader(puzzle).Grade(ChessPosition.FromFen(puzzle.startFen), 0, 0, Uci("e1e3"));
            Assert.That(result.Kind, Is.EqualTo(GradeKind.Illegal));
        }

        [Test]
        public void MateIsSolvedEvenWhenTheTreeListsAnotherMove()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            puzzle.nodes[1] = Node(Move("h5e5", false));
            GradeResult result = new PuzzleGrader(puzzle)
                .Grade(ChessPosition.FromFen(ScholarMateInOne), 1, 1, Uci("h5f7"));
            Assert.That(result.Kind, Is.EqualTo(GradeKind.Solved));
        }

        [Test]
        public void PromotionPieceIsPartOfTheMove()
        {
            var puzzle = new PuzzleDefinition
            {
                startFen = "7k/P7/8/8/8/8/8/K7 w - - 0 1", playerColor = "w", maxPlayerMoves = 2,
                timeLimitSeconds = 90f, maxRecoverableMistakes = 2,
                nodes = new[] { Node(Move("a7a8q", false, Reply("h8g7", 0))) }
            };
            var grader = new PuzzleGrader(puzzle);
            ChessPosition start = ChessPosition.FromFen(puzzle.startFen);
            Assert.That(grader.Grade(start, 0, 0, Uci("a7a8n")).Kind, Is.EqualTo(GradeKind.Wrong));
            Assert.That(grader.Grade(start, 0, 0, Uci("a7a8q")).Kind, Is.EqualTo(GradeKind.Accepted));
        }

        [Test]
        public void InconsistentDataThrowsInsteadOfDeclaringAWin()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            puzzle.nodes[1] = Node(Move("h5e5", true)); // flagged mate, but it is not
            Assert.Throws<InvalidOperationException>(() => new PuzzleGrader(puzzle)
                .Grade(ChessPosition.FromFen(ScholarMateInOne), 1, 1, Uci("h5e5")));

            PuzzleDefinition lastMove = ScholarTwoMover();
            lastMove.nodes[1] = Node(Move("h5e5", false, Reply("e8e7", 1)));
            Assert.Throws<InvalidOperationException>(() => new PuzzleGrader(lastMove)
                .Grade(ChessPosition.FromFen(ScholarMateInOne), 1, 1, Uci("h5e5")));
        }

        [Test]
        public void HintsRevealIdeaThenPieceThenMove()
        {
            PuzzleDefinition puzzle = ScholarTwoMover();
            ChessPosition start = ChessPosition.FromFen(puzzle.startFen);
            Assert.That(PuzzleHints.For(puzzle, 0, start, 1), Is.EqualTo("Nhắm vào ô f7."));
            Assert.That(PuzzleHints.For(puzzle, 0, start, 2), Is.EqualTo("Xem quân Hậu ở d1."));
            Assert.That(PuzzleHints.For(puzzle, 0, start, 3), Is.EqualTo("Đi Hậu từ d1 đến h5."));
        }
    }
}
```

- [ ] **Step 3: Write the failing verifier tests**

`Assets/Tests/EditMode/Gameplay/Chess/PuzzleVerifierTests.cs`:
```csharp
using KMA.Gameplay.Chess;
using NUnit.Framework;
using static KMA.Tests.Gameplay.Chess.TestPuzzles;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class PuzzleVerifierTests
    {
        [Test]
        public void CompleteMateInOneTreePasses()
        {
            Assert.That(PuzzleVerifier.Verify(ScholarOneMover(), out string error), Is.True, error);
        }

        [Test]
        public void MissingDefenceFails()
        {
            // Black has many answers to Qh5; the tree lists only Nf6.
            Assert.That(PuzzleVerifier.Verify(ScholarTwoMover(), out string error), Is.False);
            Assert.That(error, Does.Contain("replies"));
        }

        [Test]
        public void LeafThatIsNotMateFails()
        {
            PuzzleDefinition puzzle = ScholarOneMover();
            puzzle.nodes[0] = Node(Move("c4f7", true));
            Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.False);
            Assert.That(error, Does.Contain("not checkmate"));
        }

        [Test]
        public void UnlistedMatingMoveFails()
        {
            // Two mates in one exist; the tree lists only one.
            var puzzle = new PuzzleDefinition
            {
                startFen = "6k1/5ppp/8/8/8/8/5PPP/RR4K1 w - - 0 1", playerColor = "w", maxPlayerMoves = 1,
                timeLimitSeconds = 90f, maxRecoverableMistakes = 2,
                nodes = new[] { Node(Move("a1a8", true)) }
            };
            Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.False);
            Assert.That(error, Does.Contain("b1b8"));
        }

        [Test]
        public void ShapeErrorsAreReported()
        {
            PuzzleDefinition wrongSide = ScholarOneMover();
            wrongSide.startFen = wrongSide.startFen.Replace(" w ", " b ");
            Assert.That(wrongSide.TryValidateShape(out _), Is.False);

            PuzzleDefinition badIndex = ScholarTwoMover();
            badIndex.nodes[0].moves[0].replies[0].next = 7;
            Assert.That(badIndex.TryValidateShape(out string error), Is.False);
            Assert.That(error, Does.Contain("node"));
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-puzzle`
Expected: compile errors for `PuzzleDefinition`, `PuzzleGrader`, `PuzzleVerifier` and `PuzzleHints`.

- [ ] **Step 5: Implement `PuzzleDefinition.cs`**

```csharp
using System;

namespace KMA.Gameplay.Chess
{
    [Serializable]
    public sealed class PuzzleDefinition
    {
        public string id;
        public string sourcePuzzleId;
        public string sourceFen;
        public string sourceMoves;
        public string startFen;
        public string playerColor = "w";
        public string objective = "mate";
        public int maxPlayerMoves;
        public float timeLimitSeconds;
        public int maxRecoverableMistakes;
        public string difficulty;
        public string ideaHint;
        public string verifiedBy;
        public PuzzleNode[] nodes;

        public PieceColor PlayerColor => playerColor == "b" ? PieceColor.Black : PieceColor.White;

        /// Structural checks only; PuzzleVerifier checks the chess.
        public bool TryValidateShape(out string error)
        {
            error = null;
            if (playerColor != "w" && playerColor != "b") error = "playerColor must be w or b.";
            else if (objective != "mate") error = "Only the mate objective is supported.";
            else if (maxPlayerMoves < 1) error = "maxPlayerMoves must be at least 1.";
            else if (!(timeLimitSeconds > 0f)) error = "timeLimitSeconds must be positive.";
            else if (maxRecoverableMistakes < 0) error = "maxRecoverableMistakes cannot be negative.";
            else if (nodes == null || nodes.Length == 0) error = "The puzzle has no nodes.";
            if (error != null) return false;

            ChessPosition start;
            try { start = ChessPosition.FromFen(startFen); }
            catch (FormatException exception) { error = "startFen: " + exception.Message; return false; }
            if (start.SideToMove != PlayerColor)
            {
                error = "startFen must have the player to move.";
                return false;
            }

            for (int n = 0; n < nodes.Length; n++)
            {
                PuzzleNode node = nodes[n];
                if (node?.moves == null || node.moves.Length == 0)
                {
                    error = $"node {n} has no moves.";
                    return false;
                }
                foreach (PuzzleMove move in node.moves)
                {
                    if (move == null || !ChessMove.TryParseUci(move.uci, out _))
                    {
                        error = $"node {n} has an unreadable move.";
                        return false;
                    }
                    if (move.mate != (move.replies == null || move.replies.Length == 0))
                    {
                        error = $"node {n} move {move.uci}: a mate has no replies and a non-mate has some.";
                        return false;
                    }
                    if (move.replies == null) continue;
                    foreach (PuzzleReply reply in move.replies)
                    {
                        if (reply == null || !ChessMove.TryParseUci(reply.uci, out _) ||
                            reply.next < 0 || reply.next >= nodes.Length)
                        {
                            error = $"node {n} move {move.uci} has a reply with a bad node index or move.";
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [Serializable]
    public sealed class PuzzleNode
    {
        public PuzzleMove[] moves;
    }

    [Serializable]
    public sealed class PuzzleMove
    {
        public string uci;
        public bool mate;
        public PuzzleReply[] replies;
    }

    [Serializable]
    public sealed class PuzzleReply
    {
        public string uci;
        public int next;
    }
}
```

- [ ] **Step 6: Implement `PuzzleGrader.cs`**

```csharp
using System;
using System.Linq;

namespace KMA.Gameplay.Chess
{
    public enum GradeKind { Illegal, Wrong, Accepted, Solved }

    public readonly struct GradeResult
    {
        public GradeKind Kind { get; }
        public ChessMove Move { get; }
        public ChessPosition After { get; }
        public ChessMove? BossReply { get; }
        public int NextNode { get; }

        public GradeResult(GradeKind kind, ChessMove move, ChessPosition after, ChessMove? bossReply, int nextNode)
        {
            Kind = kind;
            Move = move;
            After = after;
            BossReply = bossReply;
            NextNode = nextNode;
        }
    }

    public sealed class PuzzleGrader
    {
        readonly PuzzleDefinition puzzle;

        public PuzzleGrader(PuzzleDefinition puzzle) =>
            this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));

        public GradeResult Grade(ChessPosition position, int node, int playerMovesMade, ChessMove move)
        {
            if (!MoveGenerator.LegalMoves(position).Contains(move))
                return new GradeResult(GradeKind.Illegal, move, position, null, node);

            ChessPosition after = position.Apply(move);
            if (MoveGenerator.IsCheckmate(after))
                return new GradeResult(GradeKind.Solved, move, after, null, node);

            string uci = move.ToUci();
            PuzzleMove listed = puzzle.nodes[node].moves.FirstOrDefault(m => m.uci == uci);
            if (listed == null)
                return new GradeResult(GradeKind.Wrong, move, position, null, node);

            if (listed.mate)
                throw new InvalidOperationException($"Puzzle {puzzle.id}: {uci} is flagged mate but is not.");
            if (playerMovesMade + 1 >= puzzle.maxPlayerMoves)
                throw new InvalidOperationException($"Puzzle {puzzle.id}: {uci} uses the last move without mate.");

            PuzzleReply first = listed.replies[0];
            if (!ChessMove.TryParseUci(first.uci, out ChessMove reply) ||
                !MoveGenerator.LegalMoves(after).Contains(reply))
                throw new InvalidOperationException($"Puzzle {puzzle.id}: reply {first.uci} is not legal.");
            return new GradeResult(GradeKind.Accepted, move, after, reply, first.next);
        }
    }
}
```

- [ ] **Step 7: Implement `PuzzleVerifier.cs`**

```csharp
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay.Chess
{
    /// Checks that a solution tree is exhaustive: every listed move forces mate in the moves
    /// left against every defence, every defence is listed, and no unlisted move also forces it.
    public static class PuzzleVerifier
    {
        public static bool Verify(PuzzleDefinition puzzle, out string error)
        {
            if (puzzle == null) { error = "No puzzle."; return false; }
            if (!puzzle.TryValidateShape(out error)) return false;
            return VerifyNode(puzzle, 0, ChessPosition.FromFen(puzzle.startFen), puzzle.maxPlayerMoves, out error);
        }

        static bool VerifyNode(PuzzleDefinition puzzle, int index, ChessPosition position, int movesLeft,
            out string error)
        {
            PuzzleNode node = puzzle.nodes[index];
            List<ChessMove> legal = MoveGenerator.LegalMoves(position);
            var listed = new HashSet<string>();
            foreach (PuzzleMove entry in node.moves)
            {
                ChessMove.TryParseUci(entry.uci, out ChessMove move);
                if (!listed.Add(entry.uci)) { error = $"node {index}: {entry.uci} is listed twice."; return false; }
                if (!legal.Contains(move)) { error = $"node {index}: {entry.uci} is not legal."; return false; }
                ChessPosition after = position.Apply(move);
                if (entry.mate)
                {
                    if (!MoveGenerator.IsCheckmate(after))
                    {
                        error = $"node {index}: {entry.uci} is not checkmate.";
                        return false;
                    }
                    continue;
                }
                if (movesLeft <= 1) { error = $"node {index}: {entry.uci} is not checkmate on the last move."; return false; }

                List<ChessMove> defences = MoveGenerator.LegalMoves(after);
                string[] expected = defences.Select(m => m.ToUci()).OrderBy(u => u).ToArray();
                string[] given = entry.replies.Select(r => r.uci).OrderBy(u => u).ToArray();
                if (expected.Length == 0 || !expected.SequenceEqual(given))
                {
                    error = $"node {index}: replies to {entry.uci} must list exactly the {expected.Length} legal defences.";
                    return false;
                }
                foreach (PuzzleReply reply in entry.replies)
                {
                    ChessMove.TryParseUci(reply.uci, out ChessMove defence);
                    if (!VerifyNode(puzzle, reply.next, after.Apply(defence), movesLeft - 1, out error)) return false;
                }
            }

            foreach (ChessMove move in legal)
            {
                if (listed.Contains(move.ToUci())) continue;
                if (ForcesMate(position.Apply(move), movesLeft))
                {
                    error = $"node {index}: {move.ToUci()} also forces mate but is not listed.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// The defender is to move in `after`; true if the attacker mates within `movesLeft` attacker moves
        /// (the move that produced `after` counts as one).
        static bool ForcesMate(ChessPosition after, int movesLeft)
        {
            if (MoveGenerator.IsCheckmate(after)) return true;
            if (movesLeft <= 1) return false;
            List<ChessMove> defences = MoveGenerator.LegalMoves(after);
            if (defences.Count == 0) return false; // stalemate
            foreach (ChessMove defence in defences)
            {
                ChessPosition reply = after.Apply(defence);
                bool mated = MoveGenerator.LegalMoves(reply).Any(attack => ForcesMate(reply.Apply(attack), movesLeft - 1));
                if (!mated) return false;
            }
            return true;
        }
    }
}
```

- [ ] **Step 8: Implement `PuzzleHints.cs`**

```csharp
namespace KMA.Gameplay.Chess
{
    public static class PuzzleHints
    {
        public const int MaxLevel = 3;

        public static string For(PuzzleDefinition puzzle, int node, ChessPosition position, int level)
        {
            if (level <= 1) return puzzle.ideaHint;
            ChessMove.TryParseUci(puzzle.nodes[node].moves[0].uci, out ChessMove move);
            string piece = PieceName(Piece.TypeOf(position[move.From]));
            return level == 2
                ? $"Xem quân {piece} ở {Square.Name(move.From)}."
                : $"Đi {piece} từ {Square.Name(move.From)} đến {Square.Name(move.To)}.";
        }

        public static string PieceName(int type) => type switch
        {
            Piece.Pawn => "Tốt",
            Piece.Knight => "Mã",
            Piece.Bishop => "Tượng",
            Piece.Rook => "Xe",
            Piece.Queen => "Hậu",
            _ => "Vua"
        };
    }
}
```

- [ ] **Step 9: Run the tests and confirm they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-puzzle`
Expected: all Chess tests pass. `UnlistedMatingMoveFails` relies on both rooks mating on the 8th rank. `Ra8#` and `Rb8#` are both mate: the f7/g7/h7 pawns box the king in, and the other rook guards the rank behind.

- [ ] **Step 10: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess/Core/Puzzle*.cs Assets/_Project/Scripts/Gameplay/Chess/Core/Puzzle*.cs.meta Assets/Tests/EditMode/Gameplay/Chess/Puzzle*.cs Assets/Tests/EditMode/Gameplay/Chess/Puzzle*.cs.meta Assets/Tests/EditMode/Gameplay/Chess/TestPuzzles.cs Assets/Tests/EditMode/Gameplay/Chess/TestPuzzles.cs.meta
git commit -m "feat(chess): grade moves against verified puzzle trees"
```

---

### Task 4: Puzzle authoring tools and the shipped puzzles

**Files:**
- Create: `tools/chess/requirements.txt`, `tools/chess/README.md`, `tools/chess/fetch_candidates.py`, `tools/chess/verify_puzzles.py`, `tools/chess/puzzle_sources.json`
- Modify: `.gitignore` (append `tools/chess/stockfish/`, `tools/chess/cache/`, `tools/chess/*.zip`)
- Create: `Assets/_Project/Scripts/Gameplay/Chess/KMA.Gameplay.Chess.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessPuzzleLibrary.cs`
- Create: `Assets/_Project/Resources/Chess/Puzzles/m2_001.json` (generated), plus `m1_001.json` if an Easy puzzle verifies
- Modify: `Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef` (add references)
- Test: `Assets/Tests/EditMode/Gameplay/Chess/ShippedPuzzleTests.cs`

**Interfaces:**
- Consumes: Task 3 model and `PuzzleVerifier`.
- Produces: `static class ChessPuzzleLibrary` (namespace `KMA.Gameplay.Chess`):
  - `const string ResourceFolder = "Chess/Puzzles"`
  - `IReadOnlyList<PuzzleDefinition> LoadAll()`, ordered by asset name
  - `PuzzleDefinition ForDifficulty(ChallengeDifficulty)`, returning the first puzzle whose `difficulty` equals the enum name, or throwing `InvalidOperationException`
  - `PuzzleDefinition Parse(string name, string json)`

- [ ] **Step 1: Install the Python packages and Stockfish (authoring only)**

Create `tools/chess/requirements.txt`:
```text
chess==1.11.2
zstandard==0.23.0
```
Then run:
```bash
python -m pip install -r tools/chess/requirements.txt
curl -L -o tools/chess/stockfish.zip https://github.com/official-stockfish/Stockfish/releases/latest/download/stockfish-windows-x86-64-avx2.zip
mkdir -p tools/chess/stockfish && unzip -o tools/chess/stockfish.zip -d tools/chess/stockfish && rm tools/chess/stockfish.zip
find tools/chess/stockfish -name "stockfish*.exe"
printf '\n# Chess puzzle authoring (Stockfish binary and Lichess cache stay local)\ntools/chess/stockfish/\ntools/chess/cache/\ntools/chess/*.zip\n' >> .gitignore
```
Expected: one `stockfish-windows-x86-64-avx2.exe` path is printed. If `pip install` fails on Python 3.14 (no `zstandard` wheel), run `python -m pip install --only-binary=:all: zstandard` before trying anything else. If that also fails, ask the user.

- [ ] **Step 2: Write `tools/chess/fetch_candidates.py`**

```python
"""Stream the Lichess puzzle database and keep a few puzzles where White delivers mate.

The database FEN is the position *before* the opponent's opening move, so side 'b'
in the FEN means White solves. Stops reading once enough candidates are kept.

Usage:
  python tools/chess/fetch_candidates.py --theme mateIn2 --count 15
Writes tools/chess/cache/candidates-<theme>.csv (gitignored).
"""
import argparse
import csv
import io
import itertools
import os
import urllib.request

import zstandard

URL = "https://database.lichess.org/lichess_db_puzzle.csv.zst"
HERE = os.path.dirname(os.path.abspath(__file__))
COLUMNS = ["PuzzleId", "FEN", "Moves", "Rating", "RatingDeviation", "Popularity",
           "NbPlays", "Themes", "GameUrl", "OpeningTags"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--theme", default="mateIn2")
    parser.add_argument("--count", type=int, default=15)
    parser.add_argument("--min-rating", type=int, default=1000)
    parser.add_argument("--max-rating", type=int, default=1700)
    parser.add_argument("--max-pieces", type=int, default=16)
    parser.add_argument("--scan-limit", type=int, default=500000)
    args = parser.parse_args()

    os.makedirs(os.path.join(HERE, "cache"), exist_ok=True)
    out_path = os.path.join(HERE, "cache", f"candidates-{args.theme}.csv")
    request = urllib.request.Request(URL, headers={"User-Agent": "kma-chess-final-tools"})
    kept = []
    with urllib.request.urlopen(request) as response:
        reader = zstandard.ZstdDecompressor().stream_reader(response)
        rows = csv.reader(io.TextIOWrapper(reader, encoding="utf-8", newline=""))
        first = next(rows)
        has_header = bool(first) and first[0] == "PuzzleId"
        col = {name: i for i, name in enumerate(COLUMNS)}
        source = rows if has_header else itertools.chain([first], rows)
        for scanned, row in enumerate(source):
            if scanned >= args.scan_limit or len(kept) >= args.count:
                break
            fen = row[col["FEN"]].split()
            if args.theme not in row[col["Themes"]].split() or fen[1] != "b":
                continue
            if not args.min_rating <= int(row[col["Rating"]]) <= args.max_rating:
                continue
            if int(row[col["Popularity"]]) < 85 or int(row[col["NbPlays"]]) < 500:
                continue
            if sum(ch.isalpha() for ch in fen[0]) > args.max_pieces:
                continue
            kept.append(row)

    with open(out_path, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(COLUMNS)
        writer.writerows(kept)
    print(f"kept {len(kept)} -> {out_path}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Write `tools/chess/verify_puzzles.py`**

```python
"""Build exhaustive, Stockfish-ordered solution trees for the puzzles in puzzle_sources.json.

For each source: apply the opponent's opening move from Lichess `Moves` to get startFen,
check White is to move and Stockfish sees mate within N, then search every White move.
A White move enters the tree only if it forces mate in the moves left against *every*
defence, and every legal defence is listed (longest resistance first, so the boss plays
the toughest one). The Lichess main line must be in the tree. Any failure exits 1.

Usage:
  python tools/chess/verify_puzzles.py [--engine PATH] [--depth 18]
"""
import argparse
import csv
import glob
import json
import os
import sys

import chess
import chess.engine

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Chess", "Puzzles")
# difficulty -> (player moves, think seconds, recoverable mistakes); spec section 2 table
DIFFICULTY = {"Easy": (1, 90, 3), "Normal": (2, 90, 2), "Hard": (3, 120, 1)}


def fail(message):
    print("FAIL: " + message, file=sys.stderr)
    sys.exit(1)


def load_candidates():
    rows = {}
    for path in glob.glob(os.path.join(HERE, "cache", "candidates-*.csv")):
        with open(path, encoding="utf-8", newline="") as handle:
            for row in csv.DictReader(handle):
                rows[row["PuzzleId"]] = row
    return rows


def forces(board, moves_left):
    """Defender to move. True if the attacker mates within moves_left attacker moves
    (the move that produced this position counts as one)."""
    if board.is_checkmate():
        return True
    if moves_left <= 1 or board.is_game_over():
        return False
    for defence in list(board.legal_moves):
        board.push(defence)
        ok = any(attack_forces(board, attack, moves_left - 1) for attack in list(board.legal_moves))
        board.pop()
        if not ok:
            return False
    return True


def attack_forces(board, move, moves_left):
    board.push(move)
    ok = forces(board, moves_left)
    board.pop()
    return ok


class TreeBuilder:
    def __init__(self, engine, depth):
        self.engine = engine
        self.depth = depth
        self.nodes = []

    def mate_distance(self, board):
        info = self.engine.analyse(board, chess.engine.Limit(depth=self.depth))
        return info["score"].white().mate()

    def node(self, board, moves_left):
        index = len(self.nodes)
        self.nodes.append(None)
        candidates = [m for m in board.legal_moves if attack_forces(board, m, moves_left)]
        if not candidates:
            fail(f"no forcing move at {board.fen()}")

        def player_key(move):
            board.push(move)
            mate = 0 if board.is_checkmate() else (self.mate_distance(board) or 99)
            board.pop()
            return (mate, move.uci())  # fastest mate first, so hints show the cleanest line

        entries = []
        for move in sorted(candidates, key=player_key):
            board.push(move)
            if board.is_checkmate():
                entries.append({"uci": move.uci(), "mate": True, "replies": []})
            else:
                replies = []
                for defence in self.order_defences(board):
                    board.push(defence)
                    replies.append({"uci": defence.uci(), "next": self.node(board, moves_left - 1)})
                    board.pop()
                entries.append({"uci": move.uci(), "mate": False, "replies": replies})
            board.pop()
        self.nodes[index] = {"moves": entries}
        return index

    def order_defences(self, board):
        scored = []
        for defence in board.legal_moves:
            board.push(defence)
            mate = self.mate_distance(board)
            board.pop()
            scored.append((-(mate if mate is not None else 99), defence.uci(), defence))
        scored.sort(key=lambda item: (item[0], item[1]))
        return [defence for _, _, defence in scored]


def main_line_in_tree(nodes, solution):
    node = 0
    entry = None
    for ply, uci in enumerate(solution):
        if ply % 2 == 0:
            entry = next((m for m in nodes[node]["moves"] if m["uci"] == uci), None)
            if entry is None:
                return False
        else:
            reply = next((r for r in entry["replies"] if r["uci"] == uci), None)
            if reply is None:
                return False
            node = reply["next"]
    return True


def main():
    parser = argparse.ArgumentParser()
    found = glob.glob(os.path.join(HERE, "stockfish", "**", "stockfish*.exe"), recursive=True)
    parser.add_argument("--engine", default=found[0] if found else None)
    parser.add_argument("--depth", type=int, default=18)
    args = parser.parse_args()
    if not args.engine:
        fail("Stockfish not found; see tools/chess/README.md")

    with open(os.path.join(HERE, "puzzle_sources.json"), encoding="utf-8") as handle:
        sources = json.load(handle)
    rows = load_candidates()
    os.makedirs(OUT, exist_ok=True)

    with chess.engine.SimpleEngine.popen_uci(args.engine) as engine:
        engine_name = engine.id.get("name", "Stockfish")
        for source in sources:
            row = rows.get(source["lichessId"])
            if row is None:
                fail(f"{source['lichessId']} is not in tools/chess/cache")
            if source["difficulty"] not in DIFFICULTY:
                fail(f"{source['id']}: unknown difficulty {source['difficulty']}")
            moves_needed, seconds, mistakes = DIFFICULTY[source["difficulty"]]
            board = chess.Board(row["FEN"])
            line = row["Moves"].split()
            opener = chess.Move.from_uci(line[0])
            if opener not in board.legal_moves:
                fail(f"{source['id']}: opener {line[0]} is illegal")
            board.push(opener)
            if board.turn != chess.WHITE:
                fail(f"{source['id']}: White must be to move after the opener")
            solution = line[1:]
            if len(solution) != 2 * moves_needed - 1:
                fail(f"{source['id']}: solution has {len(solution)} plies, "
                     f"{source['difficulty']} needs {2 * moves_needed - 1}")
            builder = TreeBuilder(engine, args.depth)
            mate = builder.mate_distance(board)
            if mate is None or not 0 < mate <= moves_needed:
                fail(f"{source['id']}: Stockfish sees mate {mate}, expected within {moves_needed}")

            builder.node(board.copy(), moves_needed)
            if not main_line_in_tree(builder.nodes, solution):
                fail(f"{source['id']}: the Lichess main line is not in the tree")

            data = {
                "id": source["id"],
                "sourcePuzzleId": "lichess:" + source["lichessId"],
                "sourceFen": row["FEN"],
                "sourceMoves": row["Moves"],
                "startFen": board.fen(),
                "playerColor": "w",
                "objective": "mate",
                "maxPlayerMoves": moves_needed,
                "timeLimitSeconds": seconds,
                "maxRecoverableMistakes": mistakes,
                "difficulty": source["difficulty"],
                "ideaHint": source["ideaHint"],
                "verifiedBy": f"python-chess {chess.__version__} + {engine_name} depth {args.depth}",
                "nodes": builder.nodes,
            }
            path = os.path.join(OUT, source["id"] + ".json")
            with open(path, "w", encoding="utf-8", newline="\n") as handle:
                json.dump(data, handle, ensure_ascii=False, indent=2)
                handle.write("\n")
            print(f"ok {source['id']}: {len(builder.nodes)} nodes -> {os.path.relpath(path, ROOT)}")


if __name__ == "__main__":
    main()
```
Only Easy and Normal are practical with this exhaustive search. For mate in 3 it needs minutes or more per puzzle. Do not add a Hard puzzle unless it finishes in under 5 minutes.

- [ ] **Step 4: Fetch candidates and choose the demo puzzles**

```bash
python tools/chess/fetch_candidates.py --theme mateIn2 --count 15
python tools/chess/fetch_candidates.py --theme mateIn1 --count 10 --min-rating 600 --max-rating 1200
python - <<'EOF'
import csv, chess
for theme in ("mateIn2", "mateIn1"):
    for row in csv.DictReader(open(f"tools/chess/cache/candidates-{theme}.csv", encoding="utf-8")):
        b = chess.Board(row["FEN"]); line = row["Moves"].split(); b.push_uci(line[0])
        print(theme, row["PuzzleId"], row["Rating"], " ".join(line[1:])); print(b, "\n")
EOF
```
Pick one `mateIn2` puzzle (and optionally one `mateIn1`) that a non-expert can read. Prefer few pieces, a first move that is not a quiet king move, and no promotion in the main line. Read the main line and write a one-sentence idea hint in Vietnamese that does not name the move. Examples: `Hy sinh để mở đường cho Xe.` or `Vua đen bị kẹt ở hàng cuối.`

Create `tools/chess/puzzle_sources.json` with the real Lichess ids you picked:
```json
[
  { "id": "m2_001", "lichessId": "<PuzzleId from candidates-mateIn2.csv>", "difficulty": "Normal",
    "ideaHint": "<one short Vietnamese sentence>" },
  { "id": "m1_001", "lichessId": "<PuzzleId from candidates-mateIn1.csv>", "difficulty": "Easy",
    "ideaHint": "<one short Vietnamese sentence>" }
]
```

- [ ] **Step 5: Generate and inspect the JSON**

Run: `python tools/chess/verify_puzzles.py`
Expected: `ok m2_001: <n> nodes -> Assets/_Project/Resources/Chess/Puzzles/m2_001.json` (and `m1_001`). If a source fails, choose another candidate. Never hand-edit the JSON.

- [ ] **Step 6: Create the Unity-side assembly and puzzle library**

`Assets/_Project/Scripts/Gameplay/Chess/KMA.Gameplay.Chess.asmdef`:
```json
{
    "name": "KMA.Gameplay.Chess",
    "rootNamespace": "KMA.Gameplay.Chess",
    "references": [
        "KMA.Gameplay.Chess.Core",
        "KMA.Gameplay",
        "KMA.Gameplay.Progression",
        "KMA.Gameplay.Core",
        "KMA.Gameplay.UI",
        "Unity.TextMeshPro",
        "UnityEngine.UI",
        "Unity.InputSystem",
        "KMA.Text"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```
Unity scopes an asmdef to its folder **minus** subfolders that have their own asmdef, so `Core/` stays in `KMA.Gameplay.Chess.Core`.

`Assets/_Project/Scripts/Gameplay/Chess/ChessPuzzleLibrary.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KMA.Gameplay.Chess
{
    public static class ChessPuzzleLibrary
    {
        public const string ResourceFolder = "Chess/Puzzles";

        public static IReadOnlyList<PuzzleDefinition> LoadAll() => Resources.LoadAll<TextAsset>(ResourceFolder)
            .OrderBy(asset => asset.name, StringComparer.Ordinal)
            .Select(asset => Parse(asset.name, asset.text))
            .ToList();

        public static PuzzleDefinition ForDifficulty(ChallengeDifficulty difficulty)
        {
            string name = difficulty.ToString();
            return LoadAll().FirstOrDefault(puzzle => puzzle.difficulty == name) ??
                throw new InvalidOperationException($"No {name} chess puzzle in Resources/{ResourceFolder}.");
        }

        public static PuzzleDefinition Parse(string name, string json)
        {
            PuzzleDefinition puzzle = JsonUtility.FromJson<PuzzleDefinition>(json);
            if (puzzle == null || !puzzle.TryValidateShape(out string error))
                throw new InvalidOperationException(
                    $"Chess puzzle {name} is invalid: {(puzzle == null ? "empty" : error)}");
            return puzzle;
        }
    }
}
```

- [ ] **Step 7: Write the shipped-data tests**

Add `"KMA.Gameplay"`, `"KMA.Gameplay.Chess"` and `"KMA.Gameplay.Progression"` to the `references` of `KMA.Gameplay.Chess.EditMode.Tests.asmdef`.

`Assets/Tests/EditMode/Gameplay/Chess/ShippedPuzzleTests.cs`:
```csharp
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ShippedPuzzleTests
    {
        [Test]
        public void TheJourneyPuzzleIsANormalWhiteMateInTwo()
        {
            PuzzleDefinition puzzle = ChessPuzzleLibrary.ForDifficulty(ChallengeDifficulty.Normal);
            Assert.That(puzzle.playerColor, Is.EqualTo("w"));
            Assert.That(puzzle.maxPlayerMoves, Is.EqualTo(2));
            Assert.That(puzzle.timeLimitSeconds, Is.EqualTo(90f));
            Assert.That(puzzle.maxRecoverableMistakes, Is.EqualTo(2));
        }

        [Test, Timeout(180000)]
        public void EveryShippedPuzzleIsExhaustiveAndSourced()
        {
            var puzzles = ChessPuzzleLibrary.LoadAll();
            Assert.That(puzzles, Is.Not.Empty);
            Assert.That(puzzles.Select(p => p.id).Distinct().Count(), Is.EqualTo(puzzles.Count));
            foreach (PuzzleDefinition puzzle in puzzles)
            {
                Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.True, $"{puzzle.id}: {error}");
                Assert.That(puzzle.sourcePuzzleId, Does.StartWith("lichess:"), puzzle.id);
                Assert.That(puzzle.ideaHint, Is.Not.Null.And.Not.Empty, puzzle.id);
                Assert.That(puzzle.ideaHint, Does.Not.Contain("—"), puzzle.id);
            }
        }
    }
}
```

- [ ] **Step 8: Run the tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-data`
Expected: all Chess tests pass. If `EveryShippedPuzzleIsExhaustiveAndSourced` fails while the Python script passed, the two implementations disagree. Treat that as a bug in one of them and investigate with superpowers:systematic-debugging; never weaken the test.

- [ ] **Step 9: Write `tools/chess/README.md`**

```markdown
# Chess puzzle authoring

The final exam grades moves only against verified JSON in
`Assets/_Project/Resources/Chess/Puzzles/`. Never hand-edit those files.

## Setup (Windows, once)

    python -m pip install -r tools/chess/requirements.txt
    curl -L -o tools/chess/stockfish.zip https://github.com/official-stockfish/Stockfish/releases/latest/download/stockfish-windows-x86-64-avx2.zip
    unzip tools/chess/stockfish.zip -d tools/chess/stockfish

## Add a puzzle

1. `python tools/chess/fetch_candidates.py --theme mateIn2 --count 15` (or `mateIn1`).
2. Pick a candidate. Add `{ id, lichessId, difficulty, ideaHint }` to `puzzle_sources.json`.
   `difficulty` is `Easy` (mate in 1) or `Normal` (mate in 2).
   `ideaHint` is one short Vietnamese sentence that does not name the move.
3. `python tools/chess/verify_puzzles.py` writes `<id>.json`. Exit code 1 means rejected.
4. Run the Unity EditMode filter `KMA.Tests.Gameplay.Chess` to re-check every file in C#.

The journey uses the first `Normal` puzzle by file name. Lichess puzzles are CC0.
```

- [ ] **Step 10: Commit**

```bash
git add .gitignore tools/chess/requirements.txt tools/chess/README.md tools/chess/fetch_candidates.py tools/chess/verify_puzzles.py tools/chess/puzzle_sources.json \
  Assets/_Project/Scripts/Gameplay/Chess/KMA.Gameplay.Chess.asmdef Assets/_Project/Scripts/Gameplay/Chess/KMA.Gameplay.Chess.asmdef.meta \
  Assets/_Project/Scripts/Gameplay/Chess/ChessPuzzleLibrary.cs Assets/_Project/Scripts/Gameplay/Chess/ChessPuzzleLibrary.cs.meta \
  Assets/_Project/Resources/Chess Assets/_Project/Resources/Chess.meta \
  Assets/Tests/EditMode/Gameplay/Chess/KMA.Gameplay.Chess.EditMode.Tests.asmdef Assets/Tests/EditMode/Gameplay/Chess/ShippedPuzzleTests.cs Assets/Tests/EditMode/Gameplay/Chess/ShippedPuzzleTests.cs.meta
git commit -m "feat(chess): add verified Lichess puzzles and authoring tools"
```

---

### Task 5: Think clock and level state machine

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/ThinkClock.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/Core/ChessFinalStateMachine.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/ThinkClockTests.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Chess/ChessFinalStateMachineTests.cs`

**Interfaces:**
- Consumes: `PuzzleDefinition`, `PuzzleGrader`, `PuzzleHints`, `GradeKind`.
- Produces:
  - `ThinkClock(float limitSeconds)`: `Limit`, `Elapsed`, `Remaining`, `Running`, `Expired`, `Start()`, `Stop()`, `Reset()`, `bool Tick(float dt)` (true once, when it expires)
  - `enum ChessFinalPhase { Intro, PlayerTurn, Validating, BossTurn, Paused, Completed, Failed }`
  - `enum ChessFailReason { None, TimeUp, TooManyMistakes }`
  - `ChessFinalStateMachine(PuzzleDefinition)`:
    - Properties: `Puzzle`, `Phase`, `Position`, `Node`, `PlayerMovesMade`, `Mistakes`, `MaxMistakes`, `MaxPlayerMoves`, `Clock`, `FailReason`, `LastMove`, `PendingBossReply`, `HintLevel`, `HintUsed`, `CurrentHint`, `CanMove`
    - Methods: `Begin()`, `GradeKind? Submit(ChessMove)`, `bool CompleteBossMove()`, `Pause()`, `Resume()`, `Tick(float)`, `string RevealNextHint()`, `Restart()`
    - Events: `PhaseChanged(ChessFinalPhase)`, `MoveCommitted(ChessMove move, bool byPlayer)`, `IllegalMoveRejected(ChessMove)`, `MistakeMade(int mistakes)`, `Finished(bool solved)`, `GradingFailed(string)`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Chess/ThinkClockTests.cs`:
```csharp
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ThinkClockTests
    {
        [Test]
        public void OnlyRunningTimeCountsAndExpiryFiresOnce()
        {
            var clock = new ThinkClock(90f);
            Assert.That(clock.Tick(10f), Is.False);
            Assert.That(clock.Elapsed, Is.EqualTo(0f));
            clock.Start();
            Assert.That(clock.Tick(30f), Is.False);
            clock.Stop();
            clock.Tick(100f);
            Assert.That(clock.Remaining, Is.EqualTo(60f));
            clock.Start();
            Assert.That(clock.Tick(60f), Is.True);
            Assert.That(clock.Running, Is.False);
            Assert.That(clock.Tick(1f), Is.False);
            Assert.That(clock.Remaining, Is.EqualTo(0f));
        }

        [Test]
        public void ResetRestoresTheFullLimit()
        {
            var clock = new ThinkClock(90f);
            clock.Start();
            clock.Tick(45f);
            clock.Reset();
            Assert.That(clock.Remaining, Is.EqualTo(90f));
            Assert.That(clock.Running, Is.False);
        }
    }
}
```

`Assets/Tests/EditMode/Gameplay/Chess/ChessFinalStateMachineTests.cs`:
```csharp
using System.Collections.Generic;
using KMA.Gameplay.Chess;
using NUnit.Framework;
using static KMA.Tests.Gameplay.Chess.TestPuzzles;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessFinalStateMachineTests
    {
        ChessFinalStateMachine machine;
        readonly List<bool> finished = new List<bool>();

        [SetUp]
        public void SetUp()
        {
            machine = new ChessFinalStateMachine(ScholarTwoMover());
            finished.Clear();
            machine.Finished += solved => finished.Add(solved);
        }

        [Test]
        public void IntroIgnoresMovesAndTheClock()
        {
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Intro));
            Assert.That(machine.Submit(Uci("d1h5")), Is.Null);
            machine.Tick(200f);
            Assert.That(machine.Clock.Elapsed, Is.EqualTo(0f));
            machine.Begin();
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
            Assert.That(machine.Clock.Running, Is.True);
        }

        [Test]
        public void AcceptedMoveHandsTheTurnToTheBossWithTheClockStopped()
        {
            machine.Begin();
            machine.Tick(5f);
            Assert.That(machine.Submit(Uci("d1h5")), Is.EqualTo(GradeKind.Accepted));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.BossTurn));
            Assert.That(machine.PlayerMovesMade, Is.EqualTo(1));
            Assert.That(machine.PendingBossReply.Value.ToUci(), Is.EqualTo("g8f6"));
            Assert.That(machine.Submit(Uci("h5f7")), Is.Null, "no move while the boss is to play");
            machine.Tick(30f);
            Assert.That(machine.Clock.Elapsed, Is.EqualTo(5f));

            Assert.That(machine.CompleteBossMove(), Is.True);
            Assert.That(machine.CompleteBossMove(), Is.False);
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
            Assert.That(machine.Position.ToFen(), Is.EqualTo(ScholarMateInOne));
            Assert.That(machine.Submit(Uci("h5f7")), Is.EqualTo(GradeKind.Solved));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Completed));
            Assert.That(finished, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void WrongMovesRollBackAndTheThirdFails()
        {
            machine.Begin();
            string before = machine.Position.ToFen();
            Assert.That(machine.Submit(Uci("d1f3")), Is.EqualTo(GradeKind.Wrong));
            Assert.That(machine.Position.ToFen(), Is.EqualTo(before));
            Assert.That(machine.Mistakes, Is.EqualTo(1));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
            Assert.That(machine.PlayerMovesMade, Is.EqualTo(0));
            machine.Submit(Uci("d1f3"));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
            machine.Submit(Uci("d1f3"));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Failed));
            Assert.That(machine.FailReason, Is.EqualTo(ChessFailReason.TooManyMistakes));
            Assert.That(machine.Submit(Uci("d1h5")), Is.Null);
            Assert.That(finished, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void WrongSecondMoveKeepsTheCorrectFirstMove()
        {
            machine.Begin();
            machine.Submit(Uci("d1h5"));
            machine.CompleteBossMove();
            Assert.That(machine.Submit(Uci("h5h7")), Is.EqualTo(GradeKind.Wrong));
            Assert.That(machine.Position.ToFen(), Is.EqualTo(ScholarMateInOne));
            Assert.That(machine.PlayerMovesMade, Is.EqualTo(1));
        }

        [Test]
        public void IllegalMoveCostsNothing()
        {
            machine.Begin();
            Assert.That(machine.Submit(Uci("e1e3")), Is.EqualTo(GradeKind.Illegal));
            Assert.That(machine.Mistakes, Is.EqualTo(0));
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
        }

        [Test]
        public void TimeRunsOutOnlyOnThePlayersTurn()
        {
            machine.Begin();
            machine.Tick(89f);
            machine.Pause();
            machine.Tick(60f);
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Paused));
            machine.Resume();
            machine.Tick(1f);
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Failed));
            Assert.That(machine.FailReason, Is.EqualTo(ChessFailReason.TimeUp));
            machine.Tick(1f);
            Assert.That(finished, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void PauseDuringTheBossTurnResumesTheBossTurn()
        {
            machine.Begin();
            machine.Submit(Uci("d1h5"));
            machine.Pause();
            machine.Resume();
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.BossTurn));
            Assert.That(machine.Clock.Running, Is.False);
        }

        [Test]
        public void HintsOpenInOrderAndRestartResetsEverything()
        {
            machine.Begin();
            Assert.That(machine.RevealNextHint(), Is.EqualTo("Nhắm vào ô f7."));
            machine.RevealNextHint();
            Assert.That(machine.RevealNextHint(), Is.EqualTo("Đi Hậu từ d1 đến h5."));
            Assert.That(machine.HintUsed, Is.True);
            machine.Submit(Uci("d1f3"));
            machine.Tick(20f);

            machine.Restart();
            Assert.That(machine.Phase, Is.EqualTo(ChessFinalPhase.Intro));
            Assert.That(machine.Mistakes, Is.EqualTo(0));
            Assert.That(machine.HintLevel, Is.EqualTo(0));
            Assert.That(machine.Clock.Remaining, Is.EqualTo(90f));
            Assert.That(machine.Position.ToFen(), Is.EqualTo(ScholarStart));
            Assert.That(machine.FailReason, Is.EqualTo(ChessFailReason.None));
        }

        [Test]
        public void DataErrorsReturnTheTurnWithoutAMistake()
        {
            PuzzleDefinition broken = ScholarTwoMover();
            broken.nodes[0] = Node(Move("d1f3", true));
            var faulty = new ChessFinalStateMachine(broken);
            string failure = null;
            faulty.GradingFailed += message => failure = message;
            faulty.Begin();
            Assert.That(faulty.Submit(Uci("d1f3")), Is.Null);
            Assert.That(failure, Is.Not.Null);
            Assert.That(faulty.Mistakes, Is.EqualTo(0));
            Assert.That(faulty.Phase, Is.EqualTo(ChessFinalPhase.PlayerTurn));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-machine`
Expected: compile errors for `ThinkClock` and `ChessFinalStateMachine`.

- [ ] **Step 3: Implement `ThinkClock.cs`**

```csharp
using System;

namespace KMA.Gameplay.Chess
{
    public sealed class ThinkClock
    {
        public ThinkClock(float limitSeconds)
        {
            if (!(limitSeconds > 0f)) throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            Limit = limitSeconds;
        }

        public float Limit { get; }
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public float Remaining => Math.Max(0f, Limit - Elapsed);
        public bool Expired => Elapsed >= Limit;

        public void Start()
        {
            if (!Expired) Running = true;
        }

        public void Stop() => Running = false;

        public void Reset()
        {
            Elapsed = 0f;
            Running = false;
        }

        /// Returns true on the tick that runs the clock out.
        public bool Tick(float dt)
        {
            if (!Running || !(dt > 0f)) return false;
            Elapsed = Math.Min(Limit, Elapsed + dt);
            if (!Expired) return false;
            Running = false;
            return true;
        }
    }
}
```

- [ ] **Step 4: Implement `ChessFinalStateMachine.cs`**

```csharp
using System;

namespace KMA.Gameplay.Chess
{
    public enum ChessFinalPhase { Intro, PlayerTurn, Validating, BossTurn, Paused, Completed, Failed }

    public enum ChessFailReason { None, TimeUp, TooManyMistakes }

    /// Gameplay state of the final exam. Presentation listens to the events and calls
    /// CompleteBossMove once the boss move has been animated.
    public sealed class ChessFinalStateMachine
    {
        readonly PuzzleGrader grader;
        readonly ChessPosition start;
        ChessFinalPhase pausedFrom;

        public ChessFinalStateMachine(PuzzleDefinition puzzle)
        {
            Puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            if (!puzzle.TryValidateShape(out string error)) throw new ArgumentException(error, nameof(puzzle));
            grader = new PuzzleGrader(puzzle);
            start = ChessPosition.FromFen(puzzle.startFen);
            Clock = new ThinkClock(puzzle.timeLimitSeconds);
            Reset();
        }

        public event Action<ChessFinalPhase> PhaseChanged;
        public event Action<ChessMove, bool> MoveCommitted;
        public event Action<ChessMove> IllegalMoveRejected;
        public event Action<int> MistakeMade;
        public event Action<bool> Finished;
        public event Action<string> GradingFailed;

        public PuzzleDefinition Puzzle { get; }
        public ThinkClock Clock { get; }
        public ChessFinalPhase Phase { get; private set; }
        public ChessPosition Position { get; private set; }
        public int Node { get; private set; }
        public int PlayerMovesMade { get; private set; }
        public int Mistakes { get; private set; }
        public int MaxMistakes => Puzzle.maxRecoverableMistakes;
        public int MaxPlayerMoves => Puzzle.maxPlayerMoves;
        public ChessFailReason FailReason { get; private set; }
        public ChessMove? LastMove { get; private set; }
        public ChessMove? PendingBossReply { get; private set; }
        public int HintLevel { get; private set; }
        public bool HintUsed => HintLevel > 0;
        public bool CanMove => Phase == ChessFinalPhase.PlayerTurn;
        public string CurrentHint => HintLevel == 0 ? null : PuzzleHints.For(Puzzle, Node, Position, HintLevel);

        public void Begin()
        {
            if (Phase != ChessFinalPhase.Intro) return;
            ReturnToPlayer();
        }

        /// Null when the move was not considered (wrong phase or a data error).
        public GradeKind? Submit(ChessMove move)
        {
            if (Phase != ChessFinalPhase.PlayerTurn) return null;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Validating);

            GradeResult grade;
            try
            {
                grade = grader.Grade(Position, Node, PlayerMovesMade, move);
            }
            catch (InvalidOperationException exception)
            {
                GradingFailed?.Invoke(exception.Message);
                ReturnToPlayer();
                return null;
            }

            switch (grade.Kind)
            {
                case GradeKind.Illegal:
                    IllegalMoveRejected?.Invoke(move);
                    ReturnToPlayer();
                    break;
                case GradeKind.Wrong:
                    Mistakes++;
                    MistakeMade?.Invoke(Mistakes);
                    if (Mistakes > MaxMistakes) Fail(ChessFailReason.TooManyMistakes);
                    else ReturnToPlayer();
                    break;
                case GradeKind.Solved:
                    PlayerMovesMade++;
                    Commit(grade.After, move, true);
                    SetPhase(ChessFinalPhase.Completed);
                    Finished?.Invoke(true);
                    break;
                case GradeKind.Accepted:
                    PlayerMovesMade++;
                    Node = grade.NextNode;
                    PendingBossReply = grade.BossReply;
                    Commit(grade.After, move, true);
                    SetPhase(ChessFinalPhase.BossTurn);
                    break;
            }
            return grade.Kind;
        }

        public bool CompleteBossMove()
        {
            if (Phase != ChessFinalPhase.BossTurn || !PendingBossReply.HasValue) return false;
            ChessMove reply = PendingBossReply.Value;
            PendingBossReply = null;
            Commit(Position.Apply(reply), reply, false);
            ReturnToPlayer();
            return true;
        }

        public void Pause()
        {
            if (Phase == ChessFinalPhase.Paused || Phase == ChessFinalPhase.Completed ||
                Phase == ChessFinalPhase.Failed) return;
            pausedFrom = Phase;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Paused);
        }

        public void Resume()
        {
            if (Phase != ChessFinalPhase.Paused) return;
            if (pausedFrom == ChessFinalPhase.PlayerTurn) Clock.Start();
            SetPhase(pausedFrom);
        }

        public void Tick(float dt)
        {
            if (Phase == ChessFinalPhase.PlayerTurn && Clock.Tick(dt)) Fail(ChessFailReason.TimeUp);
        }

        public string RevealNextHint()
        {
            if (Phase != ChessFinalPhase.PlayerTurn) return null;
            HintLevel = Math.Min(PuzzleHints.MaxLevel, HintLevel + 1);
            return CurrentHint;
        }

        public void Restart() => Reset();

        void Reset()
        {
            Position = start;
            Node = 0;
            PlayerMovesMade = 0;
            Mistakes = 0;
            HintLevel = 0;
            FailReason = ChessFailReason.None;
            LastMove = null;
            PendingBossReply = null;
            Clock.Reset();
            Phase = ChessFinalPhase.Intro;
            PhaseChanged?.Invoke(Phase);
        }

        void ReturnToPlayer()
        {
            Clock.Start();
            SetPhase(ChessFinalPhase.PlayerTurn);
        }

        void Fail(ChessFailReason reason)
        {
            FailReason = reason;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Failed);
            Finished?.Invoke(false);
        }

        void Commit(ChessPosition next, ChessMove move, bool byPlayer)
        {
            Position = next;
            LastMove = move;
            MoveCommitted?.Invoke(move, byPlayer);
        }

        void SetPhase(ChessFinalPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Chess" chess-machine`
Expected: all Chess tests pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess/Core/ThinkClock.cs Assets/_Project/Scripts/Gameplay/Chess/Core/ThinkClock.cs.meta \
  Assets/_Project/Scripts/Gameplay/Chess/Core/ChessFinalStateMachine.cs Assets/_Project/Scripts/Gameplay/Chess/Core/ChessFinalStateMachine.cs.meta \
  Assets/Tests/EditMode/Gameplay/Chess/ThinkClockTests.cs Assets/Tests/EditMode/Gameplay/Chess/ThinkClockTests.cs.meta \
  Assets/Tests/EditMode/Gameplay/Chess/ChessFinalStateMachineTests.cs Assets/Tests/EditMode/Gameplay/Chess/ChessFinalStateMachineTests.cs.meta
git commit -m "feat(chess): add think clock and final exam state machine"
```

---

### Task 6: `chess_final` in the journey, exempt from lives and frog jumps

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/SubjectId.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs` (enum `ChallengeKind`, helper `IsScored`)
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeCatalog.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs` (`ChallengeMetrics`)
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs`
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Core/JourneyControllerAdapter.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs`
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs`
- Generated: `Assets/_Project/ScriptableObjects/Journey/chess_final.asset`, `Assets/_Project/Resources/Journey/ChallengeCatalog.asset`, and the dialogue library asset under `Assets/_Project/Resources/Journey/`
- Test: `Assets/Tests/EditMode/Progression/ChessFinalProgressionTests.cs`
- Modify tests: `Assets/Tests/EditMode/Progression/JourneyTestData.cs`, `JourneyCatalogTests.cs`, `JourneyProgressTests.cs`; `Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs`, `JourneyRoutingFixture.cs`, `FullGameplayFlowTests.cs`, `StudentJourneyFlowTests.cs`; `Assets/Tests/PlayMode/Presentation/JourneyMapTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks (independent of the chess core).
- Produces:
  - `SubjectId.Chess = 8`, `ChallengeKind.Final`.
  - `static bool ChallengeDefinition.IsScored(ChallengeKind)`: true for Exam and Final, the kinds that carry a `MinigameResult` and update `SubjectRecord`.
  - `ChallengeCatalog.FinalChallengeId = "chess_final"`.
  - `ChallengeMetrics(..., int mistakes = 0, bool hintUsed = false, string detail = null)` with properties `Mistakes`, `HintUsed`, `Detail`.
  - `JourneyCommitOutcome(..., bool courseComplete, bool finalChallenge = false)` with property `FinalChallenge`.
  - `interface IChallengeMetricsSource { ChallengeMetrics BuildMetrics(ChallengeDefinition definition, MinigameResult result); }`
  - `static bool JourneyProgress.IsPenalizedKind(ChallengeKind)`: true for Practice and Exam only.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/ChessFinalProgressionTests.cs`:
```csharp
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChessFinalProgressionTests
    {
        [Test]
        public void CatalogEndsWithTheChessFinal()
        {
            ChallengeCatalog catalog = ChallengeCatalog.LoadDefault();
            Assert.That(catalog.Ordered.Count, Is.EqualTo(10));
            ChallengeDefinition final = catalog.Get(ChallengeCatalog.FinalChallengeId);
            Assert.That(catalog.Ordered[9], Is.SameAs(final));
            Assert.That(final.Subject, Is.EqualTo(SubjectId.Chess));
            Assert.That(final.Kind, Is.EqualTo(ChallengeKind.Final));
            Assert.That(final.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
            Assert.That(final.TimeLimit, Is.EqualTo(90f));
            Assert.That(final.TargetCount, Is.EqualTo(2));
        }

        [Test]
        public void ChessUnlocksOnlyAfterTheSoccerExam()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_practice");
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Chess), Is.False);
            Assert.That(session.TryStartChallenge("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);

            JourneyTestData.Play(session, "soccer_exam", true);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Chess), Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("chess_final"));
            Assert.That(session.Journey.CourseComplete, Is.False);
        }

        [Test]
        public void FailingTheFinalNeverCostsALifeOrOwesAFrogJump()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            int lives = session.Lives;
            for (int i = 0; i < 4; i++)
            {
                JourneyCommitOutcome outcome = JourneyTestData.Play(session, "chess_final", false);
                Assert.That(outcome.Accepted, Is.True);
                Assert.That(outcome.FinalChallenge, Is.True);
                Assert.That(outcome.FrogJumpRequired, Is.False);
                Assert.That(outcome.AttemptsRemaining, Is.EqualTo(lives));
            }
            Assert.That(session.Journey.FailCount("chess_final"), Is.EqualTo(0));
            Assert.That(session.PendingFrogJump, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(lives));
        }

        [Test]
        public void TheFinalCanStartWithNoLivesLeft()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            session.Journey.SetAttemptsRemaining(0);
            Assert.That(session.Journey.TryBegin("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            Assert.That(context.ChallengeId, Is.EqualTo("chess_final"));
        }

        [Test]
        public void WinningTheFinalCompletesTheCourseAndRecordsTheScore()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "chess_final", true);
            Assert.That(outcome.CourseComplete, Is.True);
            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(session.GetRecord(SubjectId.Chess).Passed, Is.True);
            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            Assert.That(session.Journey.ActiveAttempt.Mode, Is.EqualTo(ChallengeAttemptMode.FreePlay));
        }

        [Test]
        public void RestoredSavesDropFailCountsAndFrogJumpsForTheFinal()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            SaveData data = session.ToSaveData();
            data.journey.failCounts.Add(new JourneyFailCountData { challengeId = "chess_final", count = 2 });
            data.journey.pendingFrogJump = new JourneyFrogJumpData
            {
                id = "x", failedAttemptId = "y", failedChallengeId = "chess_final", savesLife = true
            };
            var restored = new GameSession();
            restored.Restore(data);
            Assert.That(restored.Journey.FailCount("chess_final"), Is.EqualTo(0));
            Assert.That(restored.PendingFrogJump, Is.Null);
        }
    }
}
```

In `Assets/Tests/EditMode/Progression/JourneyTestData.cs`, change the exam-result condition so the final gets a `MinigameResult`:
```csharp
                ChallengeDefinition.IsScored(definition.Kind)
                    ? new MinigameResult(pass, pass ? 8f : 0f, pass ? Rank.A : Rank.F)
                    : null);
```
Apply the same change in two PlayMode helpers:
- `Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs`: `MinigameResult exam = ChallengeDefinition.IsScored(definition.Kind) ? ... : null;`
- `JourneyRoutingFixture.FailActive`: `ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(false, 0f, Rank.F) : null`

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" journey-chess`
Expected: compile errors for `SubjectId.Chess`, `ChallengeKind.Final`, `FinalChallengeId`, `FinalChallenge` and `IsScored`.

- [ ] **Step 3: Add the enum members and helpers**

`SubjectId.cs`: add after `Volleyball = 7`:
```csharp
        Volleyball = 7,
        // The final exam: a chess puzzle against Cô Thể Chất. Kept last; never renumber.
        Chess = 8
```

`ChallengeDefinition.cs`: extend `ChallengeKind` and add the helper inside `ChallengeDefinition`:
```csharp
    public enum ChallengeKind
    {
        Learn,
        Practice,
        Exam,
        // The course's last challenge. It is scored like an exam but never costs a life.
        Final
    }
```
```csharp
        /// Kinds that carry a MinigameResult and update the subject record.
        public static bool IsScored(ChallengeKind kind) => kind == ChallengeKind.Exam || kind == ChallengeKind.Final;
```

`ChallengeAttempt.cs`: extend `ChallengeMetrics`:
```csharp
        public int Mistakes { get; }
        public bool HintUsed { get; }
        /// Short Vietnamese result line a controller wants shown, or null.
        public string Detail { get; }

        public ChallengeMetrics(float distance = 0f, float elapsed = 0f, int completedTargets = 0,
            int kicks = 0, float stamina = 0f, int placement = 0, int mistakes = 0, bool hintUsed = false,
            string detail = null)
        {
            Distance = distance;
            Elapsed = elapsed;
            CompletedTargets = completedTargets;
            Kicks = kicks;
            Stamina = stamina;
            Placement = placement;
            Mistakes = mistakes;
            HintUsed = hintUsed;
            Detail = detail;
        }
```

`ChallengeContracts.cs`: read the current file first, since the user edits it. Then add `FinalChallenge` to `JourneyCommitOutcome`:
```csharp
        public bool CourseComplete { get; }
        /// The committed challenge was the course final: it never costs a life or a frog jump.
        public bool FinalChallenge { get; }

        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool frogJumpRequired, bool frogJumpSavesLife, bool courseComplete, bool finalChallenge = false)
        {
            Accepted = accepted;
            NextChallengeId = nextChallengeId;
            AttemptsRemaining = attemptsRemaining;
            FrogJumpRequired = frogJumpRequired;
            FrogJumpSavesLife = frogJumpSavesLife;
            CourseComplete = courseComplete;
            FinalChallenge = finalChallenge;
        }
```
and add the metrics interface below `IChallengeController`:
```csharp
    /// A minigame that reports richer metrics than its MinigameResult (read by JourneyControllerAdapter).
    public interface IChallengeMetricsSource
    {
        ChallengeMetrics BuildMetrics(ChallengeDefinition definition, MinigameResult result);
    }
```

- [ ] **Step 4: Extend catalog validation**

In `ChallengeCatalog.cs`:
```csharp
        public const string FinalChallengeId = "chess_final";
        const int SubjectChallengeCount = 9;
        static readonly string[] ExpectedIds =
        {
            "sprint_learn", "sprint_practice", "sprint_exam",
            "volleyball_learn", "volleyball_practice", "volleyball_exam",
            "soccer_learn", "soccer_practice", "soccer_exam",
            FinalChallengeId
        };
```
In `Validate`, change the course-order loop bound from `ExpectedIds.Length` to `SubjectChallengeCount`. Add this before the Sprint time-limit check:
```csharp
            ChallengeDefinition final = challenges[SubjectChallengeCount];
            if (final.Subject != SubjectId.Chess || final.Kind != ChallengeKind.Final)
            {
                error = "The last challenge must be the chess final.";
                return false;
            }
```

- [ ] **Step 5: Exempt `Final` in `JourneyProgress`**

1. `IsSubjectUnlocked`: add `case SubjectId.Chess: return IsChallengeComplete("soccer_exam");`.
2. `TryBegin`, journey rule:
```csharp
                ChallengeAttemptMode.Journey => id == CheckpointChallengeId &&
                    (!IsPenalizedKind(definition.Kind) || attemptsRemaining > 0),
```
3. `TryBegin`, fixed difficulty:
```csharp
            if (definition.Subject == SubjectId.Football || definition.Kind == ChallengeKind.Final)
                difficulty = ChallengeDifficulty.Normal;
```
4. `Apply`: replace `else if (definition.Kind != ChallengeKind.Learn)` with `else if (IsPenalizedKind(definition.Kind))`. Change the final `return Outcome(true);` to `return Outcome(true, definition.Kind == ChallengeKind.Final);`.
5. Replace `Outcome` and `IsPenalized`:
```csharp
        JourneyCommitOutcome Outcome(bool accepted, bool finalChallenge = false) => new JourneyCommitOutcome(accepted,
            CheckpointChallengeId, attemptsRemaining, pendingFrogJump != null,
            pendingFrogJump?.SavesLife ?? false, CourseComplete, finalChallenge);

        public static bool IsPenalizedKind(ChallengeKind kind) =>
            kind == ChallengeKind.Practice || kind == ChallengeKind.Exam;

        bool IsPenalized(string id) => !string.IsNullOrEmpty(id) &&
            catalog.Ordered.Any(x => x.Id == id && IsPenalizedKind(x.Kind));
```

- [ ] **Step 6: Exempt `Final` in save normalization**

In `JourneySaveMigration.cs`:
- Rename the existing `CourseSubjects` array to `LegacyCourseSubjects` (it stays three subjects). It is used by `MigrateLegacy` only.
- Add `static readonly SubjectId[] CourseSubjects = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess };` for `SyncPassedFlags`.
- `SyncPassedFlags`: find the scored challenge with `x.Subject == subject && ChallengeDefinition.IsScored(x.Kind)`.
- Pending frog jump filter: replace `catalog.Get(checkpoint).Kind != ChallengeKind.Learn` with `JourneyProgress.IsPenalizedKind(catalog.Get(checkpoint).Kind)`.
- `NormalizeFailCounts`: replace `x.Kind != ChallengeKind.Learn` with `JourneyProgress.IsPenalizedKind(x.Kind)`.

- [ ] **Step 7: Scored results in `GameSession` and the adapter**

`GameSession`:
- `SubmitChallengeResult` and `SubmitResult`: change `definition.Kind == ChallengeKind.Exam` to `ChallengeDefinition.IsScored(definition.Kind)`.
- `ExamId`: add `SubjectId.Chess => ChallengeCatalog.FinalChallengeId,`.

`JourneyControllerAdapter.OnCompleted`:
```csharp
        void OnCompleted(MinigameResult result)
        {
            if (context == null || definition == null || result == null) return;
            bool scored = ChallengeDefinition.IsScored(definition.Kind);
            ChallengeMetrics metrics = source is IChallengeMetricsSource rich
                ? rich.BuildMetrics(definition, result)
                : scored
                    ? new ChallengeMetrics(completedTargets: definition.TargetCount)
                    : new ChallengeMetrics(elapsed: Mathf.Max(0f, result.Score),
                        completedTargets: definition.TargetCount);
            ChallengeCompleted?.Invoke(new ChallengeAttemptResult(context, result.Pass, metrics,
                scored ? result : null));
        }
```

- [ ] **Step 8: Builder data and dialogue**

In `StudentJourneyContentBuilder.BuildChallenges`, append after the `soccer_exam` spec:
```csharp
                new ChallengeSpec("chess_final", SubjectId.Chess, ChallengeKind.Final,
                    0f, 90f, 2, false, ChallengeDifficulty.Normal, false,
                    "Chiếu hết trong 2 nước, tối đa 90 giây.")
```
In the dialogue nodes, replace the `soccer_pass` node and add `chess_intro` right after it:
```csharp
                Node("soccer_pass",
                    Line(Co, DialoguePose.Cheer, "Đạt ba môn! Còn đúng một bài kiểm tra cuối với cô nữa thôi :eyes:", "ĐẠT!"),
                    Line(Mai, DialoguePose.Cheer, "QUA RỒI :sob::sob: Còn bài cuối, ông ráng nốt nha!")),
                Node("chess_intro",
                    Line(Co, DialoguePose.Idle, "Bài cuối không chạy, không nhảy. Cô đặt một thế cờ, em chiếu hết trong hai nước :eyes:", "BÀI CUỐI"),
                    Line(TanThu, DialoguePose.Hurt, "Thể chất mà thi cờ vua ạ? Em tưởng cô chỉ biết thổi còi :sob:"),
                    Line(Co, DialoguePose.Idle, "Còi vẫn mang theo đây. Đi sai là cô thổi. Em có 90 giây, sai tối đa hai lần :fire:", "90 GIÂY")),
```
In `JourneyDialoguePresenter.ShowJourney`, add to the checkpoint switch:
```csharp
                    case "chess_final": Add("soccer_pass"); Add("chess_intro"); break;
```
Regenerate both assets. First confirm the dialogue method name with `grep -n -A1 'Build Dialogue Library' Assets/Editor/StudentJourneyContentBuilder.cs`. Then run:
```bash
"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildChallenges -quit -logFile Builds/journey-challenges.log
"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildDialogueLibrary -quit -logFile Builds/journey-dialogue.log
```
Expected log lines: `[KMA] Student journey challenge catalog built.` and `[KMA] Student journey dialogue library built.`

- [ ] **Step 9: Update tests that assumed nine challenges**

- `JourneyCatalogTests` line 18: change `Is.EqualTo(9)` to `Is.EqualTo(10)`.
- For each test that expects `CourseComplete` after completing through `soccer_exam`, change the target id to `"chess_final"`. These are `JourneyProgressTests` (around lines 129 and 143), `JourneyMapTests` line 100, `FullGameplayFlowTests` line 75, and `StudentJourneyFlowTests` lines 34, 38 and 192. If a test plays challenges one by one, add one more `CompleteActiveChallenge` call.
- For `JourneyDialogueDataTests` and `JourneyDialogueValidationTests`: if a test pins the old `soccer_pass` text, update it to the new line. Do not change what the test checks.

- [ ] **Step 10: Run the progression suites**

Run:
```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" journey-chess
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression" journey-chess-play
```
Expected: `ChessFinalProgressionTests` pass, and no failures beyond the Task 0 baseline. Routing tests into chess are added in Task 8.

- [ ] **Step 11: Commit**

Stage only the files this task touched (check `git status` first; the user edits files in the same folders):
```bash
git add Assets/_Project/Scripts/Progression/SubjectId.cs Assets/_Project/Scripts/Progression/GameSession.cs \
  Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs Assets/_Project/Scripts/Progression/Journey/ChallengeCatalog.cs \
  Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs \
  Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs \
  Assets/_Project/Scripts/Core/JourneyControllerAdapter.cs Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs \
  Assets/Editor/StudentJourneyContentBuilder.cs Assets/_Project/ScriptableObjects/Journey Assets/_Project/Resources/Journey \
  Assets/Tests/EditMode/Progression/ChessFinalProgressionTests.cs Assets/Tests/EditMode/Progression/ChessFinalProgressionTests.cs.meta \
  Assets/Tests/EditMode/Progression/JourneyTestData.cs Assets/Tests/EditMode/Progression/JourneyCatalogTests.cs Assets/Tests/EditMode/Progression/JourneyProgressTests.cs \
  Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs \
  Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs Assets/Tests/PlayMode/Progression/StudentJourneyFlowTests.cs \
  Assets/Tests/PlayMode/Presentation/JourneyMapTests.cs
git commit -m "feat(journey): add the chess final as a life-free last challenge"
```

---

### Task 7: Save v9, chess best result and the celebration flag

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/SaveData.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs`
- Modify: `Assets/_Project/Scripts/Core/SaveSystem.cs`
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs`
- Test: `Assets/Tests/EditMode/Progression/ChessFinalSaveTests.cs`
- Modify test: `Assets/Tests/EditMode/Progression/SaveDataTests.cs` (version 9)

**Interfaces:**
- Consumes: Task 6 (`ChallengeKind.Final`, `ChallengeMetrics.Mistakes/HintUsed`).
- Produces:
  - `[Serializable] JourneyChessRecordData { bool recorded; float score; float thinkSeconds; int mistakes; bool hintUsed; Copy() }`
  - `JourneyStateData.chessBest`, `JourneyStateData.celebrationSeen`
  - `JourneyProgress.ChessBest` (copy), `JourneyProgress.CelebrationSeen`, `bool MarkCelebrationSeen()` (false unless `CourseComplete`), `void UnmarkCelebrationSeen()`
  - `GameManager.TryMarkCelebrationSeen(out string error)`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/ChessFinalSaveTests.cs`:
```csharp
using System;
using System.IO;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChessFinalSaveTests
    {
        static readonly string[] AllNine =
        {
            "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn", "volleyball_practice",
            "volleyball_exam", "soccer_learn", "soccer_practice", "soccer_exam"
        };

        string directory;
        SaveSystem saveSystem;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "KMA-ChessFinalSaveTests", Guid.NewGuid().ToString("N"));
            saveSystem = new SaveSystem(() => directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static SaveData Version8(params string[] completed)
        {
            var data = new SaveData
            {
                version = 8,
                lives = 3,
                subjects = new[]
                {
                    new SubjectRecordData { id = SubjectId.Sprint, passed = true, bestScore = 7f, bestRank = Rank.B },
                    new SubjectRecordData { id = SubjectId.Football, passed = completed.Length >= 9, bestScore = 8f, bestRank = Rank.A },
                    new SubjectRecordData { id = SubjectId.Volleyball, passed = completed.Length >= 6, bestScore = 6f, bestRank = Rank.C }
                },
                tutorialSeen = new[] { true, false, true },
                settings = Settings.CreateDefault(),
                journey = new JourneyStateData()
            };
            data.journey.completedChallengeIds.AddRange(completed);
            data.journey.seenDialogueIds.Add("opening");
            return data;
        }

        [Test]
        public void Load_Version8MidCourse_KeepsJourney()
        {
            SaveData old = Version8("sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn");
            old.journey.failCounts.Add(new JourneyFailCountData { challengeId = "volleyball_practice", count = 1 });
            saveSystem.Save(old);

            SaveData loaded = saveSystem.Load();

            Assert.That(saveSystem.HasLoadedValidSave, Is.True);
            Assert.That(loaded.version, Is.EqualTo(9));
            Assert.That(loaded.subjects.Length, Is.EqualTo(4));
            Assert.That(loaded.tutorialSeen.Length, Is.EqualTo(4));
            Assert.That(loaded.journey.completedChallengeIds, Is.EqualTo(new[]
                { "sprint_learn", "sprint_practice", "sprint_exam", "volleyball_learn" }));
            Assert.That(loaded.journey.failCounts[0].challengeId, Is.EqualTo("volleyball_practice"));
            Assert.That(loaded.journey.seenDialogueIds, Does.Contain("opening"));
            Assert.That(loaded.lives, Is.EqualTo(3));
        }

        [Test]
        public void Load_Version8CompletedCourse_ResumesAtTheChessFinal()
        {
            saveSystem.Save(Version8(AllNine));
            var session = new GameSession();
            session.Restore(saveSystem.Load());
            Assert.That(session.Journey.CourseComplete, Is.False);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("chess_final"));
            Assert.That(session.GetRecord(SubjectId.Football).BestScore, Is.EqualTo(8f));
            Assert.That(session.Journey.CelebrationSeen, Is.False);
        }

        [Test]
        public void ChessBestKeepsTheHighestWinAndRoundTrips()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            Assert.That(session.TryStartChallenge("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
            CommitActive(session, true, 7.5f, 50f, 1, true);
            Assert.That(session.Journey.CelebrationSeen, Is.False);
            Assert.That(session.Journey.MarkCelebrationSeen(), Is.True);

            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            CommitActive(session, true, 9.2f, 31f, 0, false);
            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            CommitActive(session, false, 0f, 90f, 3, false);

            var restored = new GameSession();
            restored.Restore(session.ToSaveData());
            JourneyChessRecordData best = restored.Journey.ChessBest;
            Assert.That(best.recorded, Is.True);
            Assert.That(best.score, Is.EqualTo(9.2f));
            Assert.That(best.thinkSeconds, Is.EqualTo(31f));
            Assert.That(best.mistakes, Is.EqualTo(0));
            Assert.That(best.hintUsed, Is.False);
            Assert.That(restored.Journey.CelebrationSeen, Is.True);
        }

        [Test]
        public void CelebrationCannotBeMarkedBeforeTheCourseIsComplete()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            Assert.That(session.Journey.MarkCelebrationSeen(), Is.False);
            SaveData data = session.ToSaveData();
            data.journey.celebrationSeen = true;
            var restored = new GameSession();
            restored.Restore(data);
            Assert.That(restored.Journey.CelebrationSeen, Is.False);
        }

        static void CommitActive(GameSession session, bool pass, float score, float seconds, int mistakes, bool hint)
        {
            ChallengeAttemptContext context = session.Journey.ActiveAttempt;
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(elapsed: seconds, completedTargets: 2, mistakes: mistakes, hintUsed: hint),
                new MinigameResult(pass, score, pass ? ScoreUtil.ToRank(score) : Rank.F)));
        }
    }
}
```
`SaveDataTests` line 19: change `Is.EqualTo(8)` to `Is.EqualTo(9)`.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.ChessFinalSaveTests" chess-save`
Expected: compile errors for `JourneyChessRecordData`, `ChessBest` and `MarkCelebrationSeen`.

- [ ] **Step 3: Data model**

`SaveData.cs`: set `public const int CurrentVersion = 9;`.

`JourneyStateData.cs`: add the fields to `JourneyStateData`:
```csharp
        public JourneyChessRecordData chessBest = new JourneyChessRecordData();
        public bool celebrationSeen;
```
and the record type:
```csharp
    [Serializable]
    public sealed class JourneyChessRecordData
    {
        public bool recorded;
        public float score;
        public float thinkSeconds;
        public int mistakes;
        public bool hintUsed;

        public JourneyChessRecordData Copy() => new JourneyChessRecordData
        {
            recorded = recorded, score = score, thinkSeconds = thinkSeconds, mistakes = mistakes, hintUsed = hintUsed
        };
    }
```

- [ ] **Step 4: `JourneyProgress`**

Add:
```csharp
        JourneyChessRecordData chessBest = new JourneyChessRecordData();
        bool celebrationSeen;

        public JourneyChessRecordData ChessBest => chessBest.Copy();
        public bool CelebrationSeen => celebrationSeen;

        /// The first celebration has played (or was skipped); later wins go to the map.
        public bool MarkCelebrationSeen()
        {
            if (!CourseComplete) return false;
            celebrationSeen = true;
            return true;
        }

        public void UnmarkCelebrationSeen() => celebrationSeen = false;

        void OfferChessResult(ChallengeMetrics metrics, float score)
        {
            if (chessBest.recorded && score <= chessBest.score) return;
            chessBest = new JourneyChessRecordData
            {
                recorded = true,
                score = score,
                thinkSeconds = Math.Max(0f, metrics.Elapsed),
                mistakes = Math.Max(0, metrics.Mistakes),
                hintUsed = metrics.HintUsed
            };
        }
```
In `Apply`, after the `if (activeAttempt.Mode == ChallengeAttemptMode.Journey) { ... }` block and before `lastCommittedAttemptId = ...`:
```csharp
            if (definition.Kind == ChallengeKind.Final && result.Pass && result.ExamResult != null &&
                result.ExamResult.Pass)
                OfferChessResult(result.Metrics, result.ExamResult.Score);
```
In `ToData()` add `chessBest = chessBest.Copy(), celebrationSeen = celebrationSeen,`. At the end of `Restore`, after the `activeAttempt` handling:
```csharp
            JourneyChessRecordData best = data?.chessBest;
            chessBest = best == null || !best.recorded ? new JourneyChessRecordData() : new JourneyChessRecordData
            {
                recorded = true,
                score = Math.Max(0f, best.score),
                thinkSeconds = Math.Max(0f, best.thinkSeconds),
                mistakes = Math.Max(0, best.mistakes),
                hintUsed = best.hintUsed
            };
            celebrationSeen = (data?.celebrationSeen ?? false) && CourseComplete;
```

- [ ] **Step 5: Keep the journey on v7/v8 → v9**

`JourneySaveMigration.Normalize`: inside `if (!normalized.settingsOnly && source != null)`, add:
```csharp
                journey.chessBest = source.chessBest?.Copy() ?? new JourneyChessRecordData();
                journey.celebrationSeen = source.celebrationSeen;
```

`SaveSystem.Migrate`: insert right after the `if (data.version == SaveData.CurrentVersion) { ... }` block:
```csharp
            // Journey-era saves (v7+) already hold the course state; Normalize pads the subject
            // and tutorial arrays for new subjects. Only older saves rebuild the journey.
            if (data.version >= 7)
                return JourneySaveMigration.Normalize(data, ChallengeCatalog.LoadDefault());
```

- [ ] **Step 6: `GameManager.TryMarkCelebrationSeen`**

Add next to `TryMarkJourneyDialogueSeen`:
```csharp
        public bool TryMarkCelebrationSeen(out string error)
        {
            error = null;
            if (!initialized || session == null)
            {
                error = "The celebration cannot be saved right now.";
                return false;
            }
            if (session.Journey.CelebrationSeen) return true;
            if (!session.Journey.MarkCelebrationSeen())
            {
                error = "The course is not complete.";
                return false;
            }
            if (TryPersistSession(out error)) return true;
            session.Journey.UnmarkCelebrationSeen();
            return false;
        }
```

- [ ] **Step 7: Run the tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" chess-save`
Expected: `ChessFinalSaveTests` pass, and nothing fails beyond the baseline. Check `LifeRegenSessionTests` (it builds a version 7 save) and every `SaveSystemTests` migration case.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/Progression/SaveData.cs Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs \
  Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs \
  Assets/_Project/Scripts/Core/SaveSystem.cs Assets/_Project/Scripts/Core/GameManager.cs \
  Assets/Tests/EditMode/Progression/ChessFinalSaveTests.cs Assets/Tests/EditMode/Progression/ChessFinalSaveTests.cs.meta \
  Assets/Tests/EditMode/Progression/SaveDataTests.cs
git commit -m "feat(save): v9 keeps journey saves and stores the chess best and celebration flag"
```

---

### Task 8: Routes, result panel and the celebration summary

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs` (`SessionRoute.Celebration`)
- Create: `Assets/_Project/Scripts/Progression/Journey/CelebrationSummary.cs`
- Create: `Assets/_Project/Scripts/Core/SceneRouter.Celebration.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs` (`TryGetSceneName`, `PrepareSceneBinding`, `DefaultSubjectScenes`)
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.Journey.cs` (`HandleChallengeAction`)
- Modify: `Assets/_Project/Scripts/Core/S5RouteBootstrap.cs`
- Modify: `Assets/_Project/Scripts/UI/ResultPanel.cs` (`ShowChallengeCore`)
- Test: `Assets/Tests/EditMode/Progression/CelebrationSummaryTests.cs`
- Test: `Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs` (add `PassActive`)

**Interfaces:**
- Consumes: Task 6 (`FinalChallenge`, `IsScored`, `ChallengeMetrics.Detail`) and Task 7 (`ChessBest`, `CelebrationSeen`).
- Produces:
  - `SessionRoute.Celebration` (last member)
  - `SceneRouter.CelebrationScene` (default `"Celebration"`), `bool SceneRouter.RouteToCelebration()`
  - `readonly struct CelebrationRow { string Title; bool Completed; bool HasScore; float Score; Rank Rank; }`
  - `sealed class CelebrationSummary`:
    - `IReadOnlyList<CelebrationRow> Subjects` (Sprint, Volleyball, Football)
    - Chess fields `ChessRecorded`, `ChessThinkSeconds`, `ChessMistakes`, `ChessHintUsed`
    - `SupplementaryRounds`, `IsSample`
    - `static CelebrationSummary From(GameSession)`, `static CelebrationSummary Sample()`, `static string FormatClock(float)`
  - Subject scene mapping `Chess → "MG_ChessFinal"`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/CelebrationSummaryTests.cs`:
```csharp
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class CelebrationSummaryTests
    {
        [Test]
        public void SummaryReadsRecordsAndTheChessBest()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "chess_final");
            CelebrationSummary summary = CelebrationSummary.From(session);

            Assert.That(summary.IsSample, Is.False);
            Assert.That(summary.Subjects.Count, Is.EqualTo(3));
            Assert.That(summary.Subjects[0].Title, Is.EqualTo("Chạy nước rút"));
            Assert.That(summary.Subjects[2].Title, Is.EqualTo("Bóng đá"));
            foreach (CelebrationRow row in summary.Subjects)
            {
                Assert.That(row.Completed, Is.True);
                Assert.That(row.HasScore, Is.True);
                Assert.That(row.Score, Is.EqualTo(8f));
            }
            Assert.That(summary.ChessRecorded, Is.True);
            Assert.That(summary.ChessThinkSeconds, Is.EqualTo(90f));
            Assert.That(summary.ChessMistakes, Is.EqualTo(0));
        }

        [Test]
        public void SampleIsMarkedAndClockFormatsMinutes()
        {
            Assert.That(CelebrationSummary.Sample().IsSample, Is.True);
            Assert.That(CelebrationSummary.FormatClock(42.3f), Is.EqualTo("00:43"));
            Assert.That(CelebrationSummary.FormatClock(90f), Is.EqualTo("01:30"));
        }
    }
}
```

The router-level tests below cannot run yet. `SceneRouter.TryStartChallenge` and `Route` call `CanLoadScene`, which in the Editor requires the scene to be enabled in Build Settings. `MG_ChessFinal` arrives in Task 10 and `Celebration` in Task 11. So this task only **writes** `ChessFinalRoutingTests.cs` and the fixture helper; Task 11 Step 9 runs them. Keep them out of the compile for now: wrap the whole test file in `#if KMA_CHESS_ROUTING_READY` … `#endif`, and Task 11 removes the guard.

Add a `PassActive` helper to `JourneyRoutingFixture` (next to `FailActive`):
```csharp
        public static void PassActive(SceneRouter router, ResultPanel panel, string id, ChallengeAttemptMode mode =
            ChallengeAttemptMode.Journey)
        {
            ChallengeDefinition definition = router.Session.Journey.Catalog.Get(id);
            Assert.That(router.TryStartChallenge(id, mode, definition.Difficulty), Is.True, id);
            router.ReportChallengeResultForTests(new ChallengeAttemptResult(router.Session.Journey.ActiveAttempt,
                true, new ChallengeMetrics(elapsed: 40f, completedTargets: definition.TargetCount),
                ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null));
            Assert.That(panel.CurrentResult, Is.Not.Null);
        }
```

`Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs` (first line `#if KMA_CHESS_ROUTING_READY`, last line `#endif`):
```csharp
using System.Collections.Generic;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChessFinalRoutingTests
    {
        readonly List<SceneRouteTransition> routes = new List<SceneRouteTransition>();
        SceneRouter router;
        ResultPanel panel;

        [SetUp]
        public void SetUp()
        {
            routes.Clear();
            router = JourneyRoutingFixture.CreateRouter(routes);
            panel = JourneyRoutingFixture.CreateResultPanel();
        }

        [TearDown]
        public void TearDown() => JourneyRoutingFixture.Destroy(router, panel);

        [Test]
        public void TheChessFinalLoadsItsScene()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "soccer_exam");
            Assert.That(router.TryStartChallenge("chess_final"), Is.True);
            Assert.That(routes.Last().SceneName, Is.EqualTo("MG_ChessFinal"));
        }

        [Test]
        public void FailedFinalOffersReplayEvenAtZeroLivesAndNeverTheFrogJump()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "soccer_exam");
            router.Session.Journey.SetAttemptsRemaining(0);
            JourneyRoutingFixture.FailActive(router, panel, "chess_final");

            Assert.That(router.Session.PendingFrogJump, Is.Null);
            Assert.That(panel.RetryAvailable, Is.True);
            Assert.That(panel.ContinueLabel, Is.EqualTo(VietText.Fix("TIẾP TỤC")));
            panel.Retry();
            Assert.That(routes.Last().Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("chess_final"));
        }

        [Test]
        public void FirstWinRoutesToTheCelebrationOnce()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "soccer_exam");
            JourneyRoutingFixture.PassActive(router, panel, "chess_final");
            panel.Continue();
            panel.Continue();
            Assert.That(routes.Count(r => r.Route == SessionRoute.Celebration), Is.EqualTo(1));
            Assert.That(routes.Last().SceneName, Is.EqualTo("Celebration"));
        }

        [Test]
        public void WinAfterTheCelebrationWasSeenGoesToTheMap()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "chess_final");
            Assert.That(router.Session.Journey.MarkCelebrationSeen(), Is.True);
            JourneyRoutingFixture.PassActive(router, panel, "chess_final", ChallengeAttemptMode.FreePlay);
            panel.Continue();
            Assert.That(routes.Last().Route, Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void CelebrationRouteIsRefusedBeforeTheCourseIsComplete()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "soccer_exam");
            Assert.That(router.RouteToCelebration(), Is.False);
            JourneyRoutingFixture.CompleteThrough(router.Session, "chess_final");
            Assert.That(router.RouteToCelebration(), Is.True);
        }
    }
}
```
- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.CelebrationSummaryTests" celebration-summary`
Expected: compile error, because `CelebrationSummary` is not defined.

- [ ] **Step 3: `SessionRoute.Celebration` and the summary**

`GameSession.cs`: add `Celebration` as the last member of `SessionRoute` (after `FrogJump`).

`Assets/_Project/Scripts/Progression/Journey/CelebrationSummary.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace KMA.Gameplay
{
    public readonly struct CelebrationRow
    {
        public string Title { get; }
        public bool Completed { get; }
        public bool HasScore { get; }
        public float Score { get; }
        public Rank Rank { get; }

        public CelebrationRow(string title, bool completed, bool hasScore, float score, Rank rank)
        {
            Title = title;
            Completed = completed;
            HasScore = hasScore;
            Score = score;
            Rank = rank;
        }
    }

    /// Read-only course recap for the celebration scene. Built from saved data only.
    public sealed class CelebrationSummary
    {
        static readonly (SubjectId Subject, string Title)[] Subjects3 =
        {
            (SubjectId.Sprint, "Chạy nước rút"), (SubjectId.Volleyball, "Bóng chuyền"), (SubjectId.Football, "Bóng đá")
        };

        public const string ChessTitle = "Bài kiểm tra cuối";

        public IReadOnlyList<CelebrationRow> Subjects { get; private set; }
        public bool ChessRecorded { get; private set; }
        public float ChessThinkSeconds { get; private set; }
        public int ChessMistakes { get; private set; }
        public bool ChessHintUsed { get; private set; }
        public int SupplementaryRounds { get; private set; }
        public bool IsSample { get; private set; }

        public static CelebrationSummary From(GameSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var rows = new List<CelebrationRow>();
            foreach ((SubjectId subject, string title) in Subjects3)
            {
                SubjectRecord record = session.GetRecord(subject);
                MinigameResult best = record.BestResult;
                rows.Add(new CelebrationRow(title, record.Passed, best != null,
                    best?.Score ?? 0f, best?.Rank ?? Rank.F));
            }
            JourneyChessRecordData chess = session.Journey.ChessBest;
            return new CelebrationSummary
            {
                Subjects = rows,
                ChessRecorded = chess.recorded,
                ChessThinkSeconds = chess.thinkSeconds,
                ChessMistakes = chess.mistakes,
                ChessHintUsed = chess.hintUsed,
                SupplementaryRounds = session.Journey.SupplementaryRounds
            };
        }

        /// Editor preview data. Scenes must never write a save while showing a sample.
        public static CelebrationSummary Sample() => new CelebrationSummary
        {
            Subjects = new[]
            {
                new CelebrationRow("Chạy nước rút", true, true, 8.4f, Rank.A),
                new CelebrationRow("Bóng chuyền", true, true, 7.1f, Rank.B),
                new CelebrationRow("Bóng đá", true, true, 9.0f, Rank.S)
            },
            ChessRecorded = true,
            ChessThinkSeconds = 42f,
            ChessMistakes = 1,
            ChessHintUsed = false,
            IsSample = true
        };

        public static string FormatClock(float seconds)
        {
            int total = (int)Math.Ceiling(Math.Max(0f, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
```

- [ ] **Step 4: Router**

`Assets/_Project/Scripts/Core/SceneRouter.Celebration.cs`:
```csharp
using UnityEngine;

namespace KMA.Gameplay.Core
{
    public sealed partial class SceneRouter
    {
        [SerializeField] string celebrationScene = "Celebration";

        public string CelebrationScene => celebrationScene;

        /// The course-complete celebration. Allowed only once the course is complete.
        public bool RouteToCelebration()
        {
            lastRouteError = null;
            if (IsTransitioning || session.ActiveSubject.HasValue || !session.Journey.CourseComplete)
                return false;
            return Route(SessionRoute.Celebration);
        }
    }
}
```
`SceneRouter.cs`:
- `TryGetSceneName`: add `SessionRoute.Celebration => celebrationScene,`.
- `PrepareSceneBinding`: add `case SessionRoute.Celebration:` to the group with `Map`, `GameOver` and `FrogJump`.
- `DefaultSubjectScenes()`: append `new SubjectScene { Subject = SubjectId.Chess, SceneName = "MG_ChessFinal" }`.

`S5RouteBootstrap.cs`: append `new SceneRouter.SubjectScene { Subject = SubjectId.Chess, SceneName = "MG_ChessFinal" }` to `Routes`.

`SceneRouter.Journey.cs`: **read the current file first; the user has local edits in it.** In `HandleChallengeAction`, replace only the final `else` branch (the one that routes to the Map) with:
```csharp
            else
            {
                // The first win of the final opens the celebration; later wins go back to the map.
                bool celebrate = displayedResult.Pass && displayedOutcome.HasValue &&
                    displayedOutcome.Value.FinalChallenge && displayedOutcome.Value.CourseComplete &&
                    !session.Journey.CelebrationSeen;
                bool routed = Route(celebrate ? SessionRoute.Celebration : SessionRoute.Map);
                if (!routed)
                    challengePanel?.ShowChallenge(displayedChallenge, displayedResult, displayedOutcome,
                        lastRouteError ?? "Không thể chuyển cảnh. Hãy thử lại.");
            }
```

- [ ] **Step 5: Result panel for the final**

`ResultPanel.cs` (read the current file first; the user has local edits). In `ShowChallengeCore`, right after `bool frogJump = ...`, add `bool final = outcome.HasValue && outcome.Value.FinalChallenge;`. Then:
- `SetDetail(...)`: when `saveError == null && final && !string.IsNullOrEmpty(result.Metrics.Detail)`, show `result.Metrics.Detail`; otherwise keep the existing expression. Write it as an outer conditional around the existing expression.
- `retryAvailable = result.ExamResult != null && !result.Pass && outcome.HasValue && (final || outcome.Value.AttemptsRemaining > 0) && !frogJump;`
- `SetButtonLabel(retryButton, final ? "CHƠI LẠI" : "THI LẠI");`
- `livesLabel.gameObject.SetActive(!final && (result.ExamResult != null || frogJump));`

- [ ] **Step 6: Run the tests**

Run:
```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" celebration-summary
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression" chess-routing
```
Expected: `CelebrationSummaryTests` pass. `ChessFinalRoutingTests` does not compile in yet. All other routing and frog-jump tests still pass, in particular `FrogJumpRoutingTests` and `FootballResultRoutingTests`.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Progression/GameSession.cs Assets/_Project/Scripts/Progression/Journey/CelebrationSummary.cs Assets/_Project/Scripts/Progression/Journey/CelebrationSummary.cs.meta \
  Assets/_Project/Scripts/Core/SceneRouter.Celebration.cs Assets/_Project/Scripts/Core/SceneRouter.Celebration.cs.meta \
  Assets/_Project/Scripts/Core/SceneRouter.cs Assets/_Project/Scripts/Core/SceneRouter.Journey.cs Assets/_Project/Scripts/Core/S5RouteBootstrap.cs \
  Assets/_Project/Scripts/UI/ResultPanel.cs \
  Assets/Tests/EditMode/Progression/CelebrationSummaryTests.cs Assets/Tests/EditMode/Progression/CelebrationSummaryTests.cs.meta \
  Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs.meta \
  Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs
git commit -m "feat(router): route the chess final and the course celebration"
```

---

### Task 9: Boss art, chess piece art and the dialogue portrait

**Files:**
- Create (from zip): `Assets/_Project/Art/Characters/BossPE/BossPE_*.png` (30 files)
- Modify: `Assets/Editor/CharacterArt.cs`
- Create: `tools/render-chess-pieces.js`
- Create (generated): `Assets/_Project/Art/Chess/Pieces/{w,b}{P,N,B,R,Q,K}.png`, `Assets/_Project/Resources/Icons/SportIcon_Chess.png`
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs` (Cô Thể Chất sprite set)
- Test: `Assets/Tests/EditMode/EditorTools/BossArtImportTests.cs`

**Interfaces:**
- Produces:
  - `CharacterArt.Boss = "BossPE"`
  - `CharacterArt.BossPoses`: the 18 base poses plus `idleBoss, whistle0, whistle1, command, ready, taunt, chessThink, chessMove, strictLook, penalty0, penalty1, count`
  - `CharacterArt.ImportAll()` now also imports BossPE
  - Piece sprite paths `Assets/_Project/Art/Chess/Pieces/<w|b><P|N|B|R|Q|K>.png`
  - `Resources/Icons/SportIcon_Chess` (white glyph, loaded by `MapPresentationBuilder.SportIconSprite(SubjectId.Chess)`)

- [ ] **Step 1: Extract only the boss folder**

```bash
unzip -o boss-final-level-assets.zip 'Assets/_Project/Art/Characters/BossPE/*' -d .
ls Assets/_Project/Art/Characters/BossPE | wc -l
python - <<'EOF'
import struct, glob
for p in sorted(glob.glob("Assets/_Project/Art/Characters/BossPE/*.png")):
    w, h = struct.unpack(">II", open(p, "rb").read()[16:24])
    assert (w, h) == (192, 256), (p, w, h)
print("all 192x256")
EOF
```
Expected: `30` and `all 192x256`. Do not extract `StudentPenalty/`, `PROMPTS.md` or `CHESS-AND-FROG-PROMPTS.md`.

- [ ] **Step 2: Write the failing import test**

`Assets/Tests/EditMode/EditorTools/BossArtImportTests.cs`:
```csharp
using KMA.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class BossArtImportTests
    {
        [OneTimeSetUp]
        public void ImportOnce() => CharacterArt.ImportAll();

        [TestCase("idleBoss")]
        [TestCase("chessThink")]
        [TestCase("chessMove")]
        [TestCase("whistle0")]
        [TestCase("whistle1")]
        [TestCase("strictLook")]
        [TestCase("taunt")]
        [TestCase("cheer0")]
        public void BossPosesImportAsBottomCenterSprites(string pose)
        {
            Sprite sprite = CharacterArt.Load(CharacterArt.Boss, pose);
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(CharacterArt.PixelsPerUnit));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)));
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(192f, 256f)));
        }

        [Test]
        public void ChessPiecesAndIconAreUiSprites()
        {
            foreach (string name in new[] { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" })
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Chess/Pieces/{name}.png"),
                    Is.Not.Null, name);
            Assert.That(Resources.Load<Sprite>("Icons/SportIcon_Chess"), Is.Not.Null);
        }
    }
}
```

- [ ] **Step 3: Extend `CharacterArt`**

```csharp
        public const string Boss = "BossPE";

        /// BossPE has the 18 shared poses plus 12 of its own.
        public static readonly string[] BossPoses = Poses.Concat(new[]
        {
            "idleBoss", "whistle0", "whistle1", "command", "ready", "taunt",
            "chessThink", "chessMove", "strictLook", "penalty0", "penalty1", "count"
        }).ToArray();
```
Declare `BossPoses` **after** `Poses` in the file, so the static initializer sees `Poses` already set. In `ImportAll`, append:
```csharp
            foreach (string pose in BossPoses)
                Import(PosePath(Boss, pose));
```
Do not add `Boss` to `Characters`; other configurators iterate that list as the student and rival cast.

- [ ] **Step 4: Write `tools/render-chess-pieces.js`**

```javascript
// Flat cartoon chess pieces (cream White, navy Black, dark outline) and a white knight
// map icon. Usage:
//   npm install --prefix "$TMP_NODE" @resvg/resvg-js
//   NODE_PATH="$TMP_NODE/node_modules" node tools/render-chess-pieces.js
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');

const ROOT = path.resolve(__dirname, '..');
const PIECES = path.join(ROOT, 'Assets/_Project/Art/Chess/Pieces');
const ICON = path.join(ROOT, 'Assets/_Project/Resources/Icons/SportIcon_Chess.png');
const INK = '#1c2546';
const FILL = { w: '#fbf4e2', b: '#2b3350' };
const SHINE = { w: '#ffffff', b: '#4a5680' };
const W = 5;

const base = 'M22 88 H78 V81 Q78 74 71 74 H29 Q22 74 22 81 Z';
const shapes = {
  P: ['M37 74 Q38 54 45 46 H55 Q62 54 63 74 Z', '<circle cx="50" cy="32" r="13"/>'],
  R: ['M31 74 L34 42 H66 L69 74 Z', 'M28 42 V22 H37 V30 H45 V22 H55 V30 H63 V22 H72 V42 Z'],
  N: ['M30 74 Q29 55 42 45 L33 41 Q28 31 39 22 L50 13 L55 22 Q73 31 70 54 L68 74 Z'],
  B: ['M38 74 Q40 57 44 50 H56 Q60 57 62 74 Z', 'M50 15 Q67 31 60 47 H40 Q33 31 50 15 Z', '<circle cx="50" cy="12" r="5"/>'],
  Q: ['M34 74 L29 33 L42 51 L50 25 L58 51 L71 33 L66 74 Z',
      '<circle cx="29" cy="31" r="5"/>', '<circle cx="50" cy="22" r="5"/>', '<circle cx="71" cy="31" r="5"/>'],
  K: ['M34 74 Q30 51 40 43 H60 Q70 51 66 74 Z', 'M38 43 H62 V36 H38 Z',
      'M47 9 H53 V17 H61 V23 H53 V36 H47 V23 H39 V17 H47 Z'],
};
const details = {
  N: '<circle cx="50" cy="31" r="3" fill="' + INK + '"/>',
  B: '<path d="M54 25 L46 36" stroke="' + INK + '" stroke-width="4" stroke-linecap="round"/>',
};

function part(d, fill) {
  return d.startsWith('<')
    ? d.replace('/>', ` fill="${fill}" stroke="${INK}" stroke-width="${W}"/>`)
    : `<path d="${d}" fill="${fill}" stroke="${INK}" stroke-width="${W}" stroke-linejoin="round"/>`;
}

function pieceSvg(color, type) {
  const fill = FILL[color];
  const body = [base, ...shapes[type]].map(d => part(d, fill)).join('');
  const shine = `<path d="M31 80 H45" stroke="${SHINE[color]}" stroke-width="3" stroke-linecap="round" opacity=".7"/>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="128" height="128">${body}${shine}${details[type] || ''}</svg>`;
}

function iconSvg() {
  const d = [base, ...shapes.N].map(p => `<path d="${p}" fill="#ffffff"/>`).join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="256" height="256">${d}</svg>`;
}

function write(svg, file, width) {
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: width } }).render().asPng();
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, png);
  console.log('wrote', path.relative(ROOT, file));
}

for (const color of ['w', 'b'])
  for (const type of Object.keys(shapes))
    write(pieceSvg(color, type), path.join(PIECES, `${color}${type}.png`), 128);
write(iconSvg(), ICON, 256);
```

- [ ] **Step 5: Render the art and add the sprite importer**

```bash
TMP_NODE="$(mktemp -d)"
npm install --prefix "$TMP_NODE" @resvg/resvg-js
NODE_PATH="$TMP_NODE/node_modules" node tools/render-chess-pieces.js
```
Expected: 12 `wrote Assets/_Project/Art/Chess/Pieces/...` lines and one for `SportIcon_Chess.png`. Open `wP.png` and `bN.png` with the Read tool and check that they read as a pawn and a knight.

Create `Assets/Editor/ChessArtImporter.cs`. Its `[OneTimeSetUp]` caller (the test) and Task 10's configurator both use it:
```csharp
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class ChessArtImporter
    {
        public const string PieceFolder = "Assets/_Project/Art/Chess/Pieces";
        public const string IconPath = "Assets/_Project/Resources/Icons/SportIcon_Chess.png";
        /// Order matches ChessBoardView.pieceSprites: White P N B R Q K, then Black.
        public static readonly string[] PieceNames =
            { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" };

        [MenuItem("KMA/Chess Final/Import Art")]
        public static void ImportAll()
        {
            CharacterArt.ImportAll();
            foreach (string name in PieceNames) EnsureUiSprite($"{PieceFolder}/{name}.png");
            EnsureUiSprite(IconPath);
        }

        public static Sprite[] LoadPieces() => PieceNames
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>($"{PieceFolder}/{name}.png") ??
                throw new InvalidOperationException($"[KMA] Missing chess piece sprite {name}."))
            .ToArray();

        static void EnsureUiSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("[KMA] Missing texture " + path);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled &&
                importer.filterMode == FilterMode.Bilinear && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.alphaIsTransparency)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
```
In `BossArtImportTests`, change the `[OneTimeSetUp]` body to `ChessArtImporter.ImportAll();`.

- [ ] **Step 6: Switch the teacher's dialogue portrait**

In `StudentJourneyContentBuilder`, change `Character(Co, "Cô Thể Chất", "#B9F27C", false, "FemaleAdventurer")` to use `"BossPE"`. Then rebuild the dialogue library with the same `-executeMethod` as Task 6 Step 8.

- [ ] **Step 7: Run the tests**

Run:
```bash
"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.ChessArtImporter.ImportAll -quit -logFile Builds/chess-art-import.log
tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.BossArtImportTests" boss-art
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" dialogue-portrait
```
Expected: all `BossArtImportTests` pass, and dialogue validation tests pass.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Art/Characters/BossPE Assets/_Project/Art/Characters/BossPE.meta Assets/_Project/Art/Chess Assets/_Project/Art/Chess.meta \
  Assets/_Project/Resources/Icons/SportIcon_Chess.png Assets/_Project/Resources/Icons/SportIcon_Chess.png.meta \
  Assets/Editor/CharacterArt.cs Assets/Editor/ChessArtImporter.cs Assets/Editor/ChessArtImporter.cs.meta \
  Assets/Editor/StudentJourneyContentBuilder.cs Assets/_Project/Resources/Journey \
  tools/render-chess-pieces.js Assets/Tests/EditMode/EditorTools/BossArtImportTests.cs Assets/Tests/EditMode/EditorTools/BossArtImportTests.cs.meta
git commit -m "feat(art): import the PE teacher boss and render chess pieces"
```

---

### Task 10: The chess level scene `MG_ChessFinal`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessBoardView.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessCastView.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessFinalHud.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/PromotionPicker.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Chess/ChessFinalController.cs`
- Create: `Assets/Editor/ChessFinalSceneConfigurator.cs`
- Modify: `Assets/Editor/KMA.EditorTools.asmdef` (add `KMA.Gameplay.Chess`, `KMA.Gameplay.Chess.Core`)
- Create (generated): `Assets/_Project/Scenes/MG_ChessFinal.unity`; `ProjectSettings/EditorBuildSettings.asset` gains it
- Test: `Assets/Tests/PlayMode/Gameplay/Chess/KMA.Gameplay.Chess.PlayMode.Tests.asmdef`
- Test: `Assets/Tests/PlayMode/Gameplay/Chess/ChessFinalSceneTests.cs`

**Interfaces:**
- Consumes: Tasks 1–5 (core), Task 4 (`ChessPuzzleLibrary`), Task 6 (`IChallengeMetricsSource`, `ChallengeMetrics`), Task 9 (`ChessArtImporter`, `CharacterArt.Boss`).
- Produces:
  - `ChessBoardView`:
    - `event Action<int,int> MoveRequested`
    - `Configure(Sprite[] pieces, Sprite dot, Sprite ring)`
    - `Render(ChessPosition, ChessMove? lastMove)`
    - `SetInteractable(bool, PieceColor, List<ChessMove>)`
    - `ClickSquare(int)` (also the test hook)
    - `IEnumerator AnimateMove(ChessMove, ChessPosition after, float seconds)`
    - Properties: `Interactable`, `SelectedSquare`, `PieceSpriteAt(int)`
  - `ChessCastView`: `Configure(...)`, `SetStudent(string)`, `SetTeacher(string)`, `PlayTeacher(params string[])`, `Say(string, float)`, `StudentPose`, `TeacherPose`, `BubbleText`.
  - `ChessFinalHud`: `Configure(...)`, `StartButton`, `HintButton`, `ShowIntro(bool)`, `SetObjective(int)`, `SetClock(float)`, `SetMistakes(int,int)`, `SetTurn(ChessFinalPhase)`, `SetTurnText(string)`, `SetHintAvailable(bool)`, `Toast(string)`, `static string FormatClock(float)`.
  - `PromotionPicker`: `Configure(Button q, Button r, Button b, Button n)`, `IsOpen`, `Open(Action<int>)`, `Choose(int)`, `Close()`.
  - `ChessFinalController : MinigameBase, IChallengeMetricsSource`: `Machine`, `Configure(board, hud, cast, promotion)`, `BeginAttempt()`, `RequestMove(int from, int to)`, `RevealHint()`, `BuildResult(bool solved)`.

Pose names used. Student (`MaleAdventurer`): `idle`, `hurt`, `cheer0`, `cheer1`. Teacher (`BossPE`): `idleBoss`, `strictLook`, `chessThink`, `chessMove`, `whistle0`, `whistle1`, `taunt`, `cheer0`. No `penalty*`, `count` or frog sprites.

- [ ] **Step 1: Write the failing PlayMode test**

`Assets/Tests/PlayMode/Gameplay/Chess/KMA.Gameplay.Chess.PlayMode.Tests.asmdef`:
```json
{
    "name": "KMA.Gameplay.Chess.PlayMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.Chess",
    "references": [
        "UnityEngine.TestRunner",
        "KMA.Gameplay",
        "KMA.Gameplay.Chess",
        "KMA.Gameplay.Chess.Core",
        "KMA.Gameplay.Progression",
        "KMA.Gameplay.UI",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": true,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Tests/PlayMode/Gameplay/Chess/ChessFinalSceneTests.cs`:
```csharp
using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Chess;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessFinalSceneTests
    {
        ChessFinalController controller;
        ChessBoardView board;
        MinigameResult result;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MG_ChessFinal", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindFirstObjectByType<ChessFinalController>();
            board = Object.FindFirstObjectByType<ChessBoardView>();
            result = null;
            controller.Completed += r => result = r;
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardIsLockedAndClockStillUntilStart()
        {
            Assert.That(controller.Machine.Phase, Is.EqualTo(ChessFinalPhase.Intro));
            Assert.That(board.Interactable, Is.False);
            yield return new WaitForSeconds(.3f);
            Assert.That(controller.Machine.Clock.Elapsed, Is.EqualTo(0f));
            controller.BeginAttempt();
            yield return null;
            yield return null;
            Assert.That(board.Interactable, Is.True);
            Assert.That(controller.Machine.Clock.Running, Is.True);
        }

        [UnityTest]
        public IEnumerator PlayingTheMainLineSolvesAndReportsMetrics()
        {
            controller.BeginAttempt();
            yield return null;
            yield return null;
            PuzzleDefinition puzzle = controller.Machine.Puzzle;
            yield return Play(puzzle.nodes[0].moves[0].uci);
            if (controller.Machine.Phase != ChessFinalPhase.Completed)
            {
                Assert.That(board.Interactable, Is.False, "board locked during the boss turn");
                yield return new WaitUntil(() => controller.Machine.Phase == ChessFinalPhase.PlayerTurn);
                yield return Play(puzzle.nodes[controller.Machine.Node].moves[0].uci);
            }
            yield return new WaitUntil(() => result != null);
            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.GreaterThanOrEqualTo(6f));
            ChallengeMetrics metrics = controller.BuildMetrics(null, result);
            Assert.That(metrics.Mistakes, Is.EqualTo(0));
            Assert.That(metrics.HintUsed, Is.False);
            Assert.That(metrics.Detail, Does.StartWith("Thời gian"));
        }

        [UnityTest]
        public IEnumerator ThreeWrongMovesFailAndPauseFreezesTheClock()
        {
            controller.BeginAttempt();
            yield return null;
            yield return null;
            ChessPosition start = controller.Machine.Position;
            string[] tree = controller.Machine.Puzzle.nodes[0].moves.Select(m => m.uci).ToArray();
            ChessMove wrong = MoveGenerator.LegalMoves(start).First(m => !tree.Contains(m.ToUci()) &&
                !MoveGenerator.IsCheckmate(start.Apply(m)) && m.Promotion == 0);

            Time.timeScale = 0f;
            yield return null;
            float frozen = controller.Machine.Clock.Elapsed;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(controller.Machine.Clock.Elapsed, Is.EqualTo(frozen));
            Assert.That(controller.Machine.Phase, Is.EqualTo(ChessFinalPhase.Paused));
            Time.timeScale = 1f;
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                yield return Play(wrong.ToUci());
                Assert.That(controller.Machine.Position.ToFen(), Is.EqualTo(start.ToFen()));
            }
            yield return new WaitUntil(() => result != null);
            Assert.That(result.Pass, Is.False);
            Assert.That(controller.BuildMetrics(null, result).Detail, Is.EqualTo("Sai quá 2 lần"));
        }

        IEnumerator Play(string uci)
        {
            ChessMove.TryParseUci(uci, out ChessMove move);
            board.ClickSquare(move.From);
            board.ClickSquare(move.To);
            yield return null;
        }
    }
}
```
The main-line test handles promotion moves only if the puzzle's main line has none, and Task 4 Step 4 picks such a puzzle. If it does contain one, extend `Play` to call `Object.FindFirstObjectByType<PromotionPicker>().Choose(move.Promotion)` when `move.Promotion != 0`.

- [ ] **Step 2: Run the test and confirm it fails**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Chess" chess-scene`
Expected: compile errors (types missing).

- [ ] **Step 3: `ChessBoardView.cs`**

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Chess
{
    /// 64 squares, a1 bottom-left, White always at the bottom. Mirrors the model; never infers rules.
    public sealed class ChessBoardView : MonoBehaviour
    {
        public static readonly Color LightSquare = new Color32(0xF3, 0xE6, 0xC4, 0xFF);
        public static readonly Color DarkSquare = new Color32(0xA9, 0xD3, 0xEA, 0xFF);
        static readonly Color SelectedFrame = new Color32(0xFF, 0xC9, 0x28, 0xFF);
        static readonly Color LastMoveTint = new Color32(0xFF, 0xD8, 0x5C, 0x66);
        static readonly Color CheckTint = new Color32(0xE8, 0x5A, 0x48, 0x8C);
        static readonly Color MarkerColor = new Color32(0x1C, 0x25, 0x46, 0x66);

        [SerializeField] Sprite[] pieceSprites = new Sprite[12];
        [SerializeField] Sprite dotSprite;
        [SerializeField] Sprite ringSprite;

        readonly Image[] tints = new Image[64];
        readonly Image[] pieces = new Image[64];
        readonly Image[] markers = new Image[64];
        readonly GameObject[] frames = new GameObject[64];
        readonly RectTransform[] cells = new RectTransform[64];
        List<ChessMove> legal = new List<ChessMove>();
        ChessPosition position;
        PieceColor player = PieceColor.White;

        public event Action<int, int> MoveRequested;
        public bool Interactable { get; private set; }
        public int SelectedSquare { get; private set; } = -1;
        public Sprite PieceSpriteAt(int square) => pieces[square] != null && pieces[square].enabled ? pieces[square].sprite : null;

        public void Configure(Sprite[] pieceSet, Sprite dot, Sprite ring)
        {
            pieceSprites = pieceSet;
            dotSprite = dot;
            ringSprite = ring;
        }

        void Awake() => Build();

        void Build()
        {
            if (cells[0] != null) return;
            for (int sq = 0; sq < 64; sq++)
            {
                int file = Square.File(sq), rank = Square.Rank(sq);
                RectTransform cell = Child(transform, Square.Name(sq),
                    new Vector2(file / 8f, rank / 8f), new Vector2((file + 1) / 8f, (rank + 1) / 8f));
                var background = cell.gameObject.AddComponent<Image>();
                background.color = (file + rank) % 2 == 0 ? DarkSquare : LightSquare;
                var button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                int captured = sq;
                button.onClick.AddListener(() => ClickSquare(captured));
                cells[sq] = cell;

                tints[sq] = Overlay(cell, "Tint", Vector2.zero, Vector2.one);
                frames[sq] = BuildFrame(cell);
                pieces[sq] = Overlay(cell, "Piece", new Vector2(.06f, .06f), new Vector2(.94f, .94f));
                pieces[sq].preserveAspect = true;
                markers[sq] = Overlay(cell, "Marker", new Vector2(.35f, .35f), new Vector2(.65f, .65f));
                markers[sq].color = MarkerColor;
                markers[sq].enabled = false;
            }
        }

        public void Render(ChessPosition current, ChessMove? lastMove)
        {
            Build();
            position = current;
            int checkedKing = MoveGenerator.IsInCheck(current) ? current.KingSquare(current.SideToMove) : -1;
            for (int sq = 0; sq < 64; sq++)
            {
                Sprite sprite = SpriteFor(current[sq]);
                pieces[sq].sprite = sprite;
                pieces[sq].enabled = sprite != null;
                bool moved = lastMove.HasValue && (lastMove.Value.From == sq || lastMove.Value.To == sq);
                tints[sq].color = sq == checkedKing ? CheckTint : moved ? LastMoveTint : Color.clear;
            }
            ClearSelection();
        }

        public void SetInteractable(bool value, PieceColor side, List<ChessMove> legalMoves)
        {
            Interactable = value;
            player = side;
            legal = legalMoves ?? new List<ChessMove>();
            if (!value) ClearSelection();
        }

        public void ClickSquare(int sq)
        {
            if (!Interactable || position == null) return;
            if (SelectedSquare >= 0 && legal.Any(m => m.From == SelectedSquare && m.To == sq))
            {
                int from = SelectedSquare;
                ClearSelection();
                MoveRequested?.Invoke(from, sq);
                return;
            }
            sbyte piece = position[sq];
            if (piece != 0 && Piece.ColorOf(piece) == player && legal.Any(m => m.From == sq)) Select(sq);
            else ClearSelection();
        }

        public void ClearSelection()
        {
            if (SelectedSquare >= 0 && frames[SelectedSquare] != null) frames[SelectedSquare].SetActive(false);
            SelectedSquare = -1;
            foreach (Image marker in markers)
                if (marker != null) marker.enabled = false;
        }

        /// Slides the piece, then renders `after`. Uses scaled time, so a pause freezes it.
        public IEnumerator AnimateMove(ChessMove move, ChessPosition after, float seconds)
        {
            Build();
            Image ghost = Overlay(transform, "MovingPiece", cells[move.From].anchorMin, cells[move.From].anchorMax);
            ghost.sprite = pieces[move.From].sprite;
            ghost.preserveAspect = true;
            pieces[move.From].enabled = false;
            Vector2 fromMin = cells[move.From].anchorMin, toMin = cells[move.To].anchorMin;
            Vector2 size = cells[move.From].anchorMax - fromMin;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                Vector2 min = Vector2.Lerp(fromMin, toMin, k);
                ghost.rectTransform.anchorMin = min;
                ghost.rectTransform.anchorMax = min + size;
                yield return null;
            }
            Destroy(ghost.gameObject);
            Render(after, move);
        }

        void Select(int sq)
        {
            ClearSelection();
            SelectedSquare = sq;
            frames[sq].SetActive(true);
            foreach (ChessMove move in legal.Where(m => m.From == sq))
            {
                bool capture = position[move.To] != 0 ||
                    (Piece.TypeOf(position[sq]) == Piece.Pawn && Square.File(move.From) != Square.File(move.To));
                Image marker = markers[move.To];
                marker.sprite = capture ? ringSprite : dotSprite;
                RectTransform rect = marker.rectTransform;
                rect.anchorMin = capture ? new Vector2(.04f, .04f) : new Vector2(.36f, .36f);
                rect.anchorMax = capture ? new Vector2(.96f, .96f) : new Vector2(.64f, .64f);
                marker.enabled = true;
            }
        }

        Sprite SpriteFor(sbyte piece) => piece == 0
            ? null
            : pieceSprites[(Piece.ColorOf(piece) == PieceColor.White ? 0 : 6) + Piece.TypeOf(piece) - 1];

        static GameObject BuildFrame(RectTransform cell)
        {
            RectTransform frame = Child(cell, "Selected", Vector2.zero, Vector2.one);
            const float t = .08f;
            foreach ((Vector2 min, Vector2 max) in new[]
            {
                (new Vector2(0f, 0f), new Vector2(1f, t)), (new Vector2(0f, 1f - t), new Vector2(1f, 1f)),
                (new Vector2(0f, 0f), new Vector2(t, 1f)), (new Vector2(1f - t, 0f), new Vector2(1f, 1f))
            })
                Overlay(frame, "Edge", min, max).color = SelectedFrame;
            frame.gameObject.SetActive(false);
            return frame.gameObject;
        }

        static Image Overlay(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var image = Child(parent, name, min, max).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        static RectTransform Child(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
```
The view builds its 64 cells at runtime in `Awake`, so the scene file only stores the board root. That keeps `MG_ChessFinal.unity` small and lets the configurator stay simple.

- [ ] **Step 4: `ChessCastView.cs`, `ChessFinalHud.cs`, `PromotionPicker.cs`**

`ChessCastView.cs`:
```csharp
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Chess
{
    public sealed class ChessCastView : MonoBehaviour
    {
        [Serializable]
        public struct Pose
        {
            public string name;
            public Sprite sprite;
        }

        [SerializeField] Image student;
        [SerializeField] Image teacher;
        [SerializeField] Pose[] studentPoses;
        [SerializeField] Pose[] teacherPoses;
        [SerializeField] GameObject bubble;
        [SerializeField] TMP_Text bubbleText;
        [SerializeField] float stepSeconds = .3f;
        float bubbleUntil;
        Coroutine teacherSequence;

        public string StudentPose { get; private set; }
        public string TeacherPose { get; private set; }
        public string BubbleText => bubble != null && bubble.activeSelf ? bubbleText.text : string.Empty;

        public void Configure(Image studentImage, Image teacherImage, Pose[] studentSet, Pose[] teacherSet,
            GameObject speechBubble, TMP_Text speechText)
        {
            student = studentImage;
            teacher = teacherImage;
            studentPoses = studentSet;
            teacherPoses = teacherSet;
            bubble = speechBubble;
            bubbleText = speechText;
        }

        public void SetStudent(string pose)
        {
            StudentPose = pose;
            student.sprite = Find(studentPoses, pose);
        }

        public void SetTeacher(string pose)
        {
            if (teacherSequence != null) StopCoroutine(teacherSequence);
            teacherSequence = null;
            ApplyTeacher(pose);
        }

        /// Plays the poses one step apart; the last one stays.
        public void PlayTeacher(params string[] poses)
        {
            if (teacherSequence != null) StopCoroutine(teacherSequence);
            teacherSequence = StartCoroutine(Sequence(poses));
        }

        public void Say(string text, float seconds = 2.5f)
        {
            bubble.SetActive(true);
            bubbleText.text = VietText.Fix(text);
            bubbleUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (bubble != null && bubble.activeSelf && Time.unscaledTime > bubbleUntil) bubble.SetActive(false);
        }

        IEnumerator Sequence(string[] poses)
        {
            for (int i = 0; i < poses.Length; i++)
            {
                ApplyTeacher(poses[i]);
                if (i + 1 < poses.Length) yield return new WaitForSeconds(stepSeconds);
            }
            teacherSequence = null;
        }

        void ApplyTeacher(string pose)
        {
            TeacherPose = pose;
            teacher.sprite = Find(teacherPoses, pose);
        }

        static Sprite Find(Pose[] set, string pose) =>
            set.FirstOrDefault(p => p.name == pose).sprite ??
            throw new InvalidOperationException($"[KMA] Pose {pose} is not configured.");
    }
}
```

`ChessFinalHud.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Chess
{
    public sealed class ChessFinalHud : MonoBehaviour
    {
        [SerializeField] TMP_Text objective;
        [SerializeField] TMP_Text clock;
        [SerializeField] TMP_Text mistakes;
        [SerializeField] TMP_Text turn;
        [SerializeField] TMP_Text toast;
        [SerializeField] GameObject introCard;
        [SerializeField] Button startButton;
        [SerializeField] Button hintButton;
        float toastUntil;

        public Button StartButton => startButton;
        public Button HintButton => hintButton;
        public string ClockText => clock.text;
        public string MistakesText => mistakes.text;
        public string TurnText => turn.text;

        public void Configure(TMP_Text objectiveLabel, TMP_Text clockLabel, TMP_Text mistakesLabel, TMP_Text turnLabel,
            TMP_Text toastLabel, GameObject intro, Button start, Button hint)
        {
            objective = objectiveLabel;
            clock = clockLabel;
            mistakes = mistakesLabel;
            turn = turnLabel;
            toast = toastLabel;
            introCard = intro;
            startButton = start;
            hintButton = hint;
        }

        public void ShowIntro(bool visible) => introCard.SetActive(visible);
        public void SetObjective(int moves) => objective.text = VietText.Fix($"Chiếu hết trong {moves} nước");
        public void SetClock(float remaining) => clock.text = FormatClock(remaining);
        public void SetMistakes(int made, int max) => mistakes.text = VietText.Fix($"Sai: {made}/{max}");
        public void SetTurnText(string text) => turn.text = VietText.Fix(text);
        public void SetHintAvailable(bool available) => hintButton.interactable = available;

        public void SetTurn(ChessFinalPhase phase) => SetTurnText(phase switch
        {
            ChessFinalPhase.PlayerTurn => "Lượt của bạn",
            ChessFinalPhase.Validating => "Lượt của bạn",
            ChessFinalPhase.BossTurn => "Lượt giảng viên",
            ChessFinalPhase.Completed => "Hoàn thành",
            ChessFinalPhase.Paused => "Tạm dừng",
            ChessFinalPhase.Failed => turn.text,
            _ => "Đọc đề rồi bấm Bắt đầu"
        });

        public void Toast(string text, float seconds = 1.6f)
        {
            toast.text = VietText.Fix(text);
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil)
                toast.gameObject.SetActive(false);
        }

        public static string FormatClock(float seconds) => CelebrationSummary.FormatClock(seconds);
    }
}
```

`PromotionPicker.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Chess
{
    public sealed class PromotionPicker : MonoBehaviour
    {
        [SerializeField] Button queen;
        [SerializeField] Button rook;
        [SerializeField] Button bishop;
        [SerializeField] Button knight;
        Action<int> pending;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(Button q, Button r, Button b, Button n)
        {
            queen = q;
            rook = r;
            bishop = b;
            knight = n;
        }

        void Awake()
        {
            queen.onClick.AddListener(() => Choose(Piece.Queen));
            rook.onClick.AddListener(() => Choose(Piece.Rook));
            bishop.onClick.AddListener(() => Choose(Piece.Bishop));
            knight.onClick.AddListener(() => Choose(Piece.Knight));
        }

        public void Open(Action<int> onChosen)
        {
            pending = onChosen;
            gameObject.SetActive(true);
        }

        public void Choose(int pieceType)
        {
            Action<int> callback = pending;
            Close();
            callback?.Invoke(pieceType);
        }

        public void Close()
        {
            pending = null;
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 5: `ChessFinalController.cs`**

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Chess
{
    public sealed class ChessFinalController : MinigameBase, IChallengeMetricsSource
    {
        [SerializeField] ChessBoardView board;
        [SerializeField] ChessFinalHud hud;
        [SerializeField] ChessCastView cast;
        [SerializeField] PromotionPicker promotion;
        [SerializeField] float bossThinkSeconds = .5f;
        [SerializeField] float bossMoveSeconds = .6f;
        [SerializeField] float resultDelaySeconds = .9f;
        bool resolving;

        public ChessFinalStateMachine Machine { get; private set; }
        public override bool UsesSharedTutorial => false;
        public override bool UsesSharedCountdown => false;
        public override bool OwnsStartGate => true;
        public override bool OwnsCameraBackground => true;

        public void Configure(ChessBoardView boardView, ChessFinalHud hudView, ChessCastView castView,
            PromotionPicker picker)
        {
            board = boardView;
            hud = hudView;
            cast = castView;
            promotion = picker;
        }

        protected override void Awake()
        {
            base.Awake();
            SetTutorialGate(true);
            Machine = new ChessFinalStateMachine(ChessPuzzleLibrary.ForDifficulty(ChallengeDifficulty.Normal));
            Machine.PhaseChanged += OnPhaseChanged;
            Machine.MoveCommitted += OnMoveCommitted;
            Machine.MistakeMade += OnMistake;
            Machine.IllegalMoveRejected += _ => hud.Toast("Nước này không hợp lệ.");
            Machine.GradingFailed += OnGradingFailed;
            Machine.Finished += OnFinished;
        }

        void Start()
        {
            board.MoveRequested += RequestMove;
            hud.StartButton.onClick.AddListener(BeginAttempt);
            hud.HintButton.onClick.AddListener(RevealHint);
            var panel = FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            if (panel != null) panel.SetTitles("CHIẾU HẾT!", "CHƯA ĐẠT");
            hud.SetObjective(Machine.MaxPlayerMoves);
            hud.SetMistakes(0, Machine.MaxMistakes);
            hud.SetClock(Machine.Clock.Remaining);
            hud.SetTurn(Machine.Phase);
            hud.ShowIntro(true);
            promotion.Close();
            board.Render(Machine.Position, null);
            cast.SetStudent("idle");
            cast.SetTeacher("idleBoss");
            cast.Say($"Chiếu hết trong {CountWord(Machine.MaxPlayerMoves)} nước. Em có {TimeWords(Machine.Clock.Limit)}.", 6f);
            RefreshInput();
        }

        public void BeginAttempt()
        {
            if (Machine.Phase != ChessFinalPhase.Intro) return;
            hud.ShowIntro(false);
            SetTutorialGate(false);
            Machine.Begin();
        }

        protected override void Update()
        {
            base.Update();
            bool paused = Time.timeScale == 0f;
            if (paused) Machine.Pause();
            else if (Machine.Phase == ChessFinalPhase.Paused) Machine.Resume();
            Machine.Tick(Time.deltaTime);
            hud.SetClock(Machine.Clock.Remaining);
        }

        protected override void TickPlay(float dt) { }

        public void RequestMove(int from, int to)
        {
            if (!Machine.CanMove || promotion.IsOpen) return;
            List<ChessMove> options = MoveGenerator.LegalMoves(Machine.Position)
                .Where(m => m.From == from && m.To == to).ToList();
            if (options.Count <= 1)
            {
                Submit(options.Count == 1 ? options[0] : new ChessMove(from, to));
                return;
            }
            board.SetInteractable(false, Machine.Puzzle.PlayerColor, null);
            promotion.Open(type =>
            {
                RefreshInput();
                if (Machine.CanMove) Submit(new ChessMove(from, to, type));
            });
        }

        public void RevealHint()
        {
            string text = Machine.RevealNextHint();
            if (text == null) return;
            cast.SetTeacher("taunt");
            cast.Say(text, 8f);
            hud.SetHintAvailable(Machine.HintLevel < PuzzleHints.MaxLevel);
        }

        void Submit(ChessMove move)
        {
            if (Machine.Submit(move) == GradeKind.Accepted) StartCoroutine(BossTurn());
        }

        IEnumerator BossTurn()
        {
            cast.SetTeacher("chessThink");
            yield return new WaitForSeconds(bossThinkSeconds);
            if (Machine.Phase != ChessFinalPhase.BossTurn && Machine.Phase != ChessFinalPhase.Paused) yield break;
            yield return new WaitUntil(() => Machine.Phase == ChessFinalPhase.BossTurn);
            ChessMove reply = Machine.PendingBossReply.Value;
            ChessPosition before = Machine.Position;
            // The chessMove pose holds a rook, so it is only shown for rook moves.
            cast.SetTeacher(Piece.TypeOf(before[reply.From]) == Piece.Rook ? "chessMove" : "chessThink");
            yield return board.AnimateMove(reply, before.Apply(reply), bossMoveSeconds);
            Machine.CompleteBossMove();
            cast.SetTeacher("strictLook");
        }

        void OnPhaseChanged(ChessFinalPhase phase)
        {
            if (hud == null) return;
            hud.SetTurn(phase);
            hud.SetHintAvailable(phase == ChessFinalPhase.PlayerTurn && Machine.HintLevel < PuzzleHints.MaxLevel);
            RefreshInput();
        }

        void OnMoveCommitted(ChessMove move, bool byPlayer)
        {
            if (byPlayer) board.Render(Machine.Position, move);
        }

        void OnMistake(int mistakes)
        {
            hud.SetMistakes(Mathf.Min(mistakes, Machine.MaxMistakes), Machine.MaxMistakes);
            board.Render(Machine.Position, Machine.LastMove);
            cast.SetStudent("hurt");
            GameAudio.Play(GameSound.Whistle);
            int left = Machine.MaxMistakes - mistakes;
            if (left < 0) return;
            cast.PlayTeacher("whistle0", "whistle1", "strictLook");
            cast.Say(left == 1 ? "Còn một lần sửa." : "Chưa đúng. Em xem lại.");
        }

        void OnGradingFailed(string message)
        {
            Debug.LogError("[KMA] Chess puzzle data error: " + message);
            hud.Toast("Đề có lỗi. Em đi lại nước khác nhé.");
            board.Render(Machine.Position, Machine.LastMove);
        }

        void OnFinished(bool solved)
        {
            if (resolving) return;
            resolving = true;
            StartCoroutine(Resolve(solved));
        }

        IEnumerator Resolve(bool solved)
        {
            board.SetInteractable(false, Machine.Puzzle.PlayerColor, null);
            if (solved)
            {
                cast.SetStudent("cheer0");
                cast.SetTeacher("cheer0");
                cast.Say("Được, em qua.");
            }
            else
            {
                bool timeUp = Machine.FailReason == ChessFailReason.TimeUp;
                hud.SetTurnText(timeUp ? "Hết giờ" : "Hết lượt sửa");
                cast.SetStudent("hurt");
                cast.SetTeacher("strictLook");
                cast.Say(timeUp ? "Hết giờ." : "Sai quá số lần rồi.");
            }
            yield return new WaitForSeconds(resultDelaySeconds);
            Finish(BuildResult(solved));
        }

        public MinigameResult BuildResult(bool solved) => ScoreUtil.Build(solved,
            accuracy: 2f - Machine.Mistakes,
            efficiency: Machine.Clock.Remaining / Machine.Clock.Limit,
            mastery: Machine.HintUsed ? 0f : 1f);

        public ChallengeMetrics BuildMetrics(ChallengeDefinition definition, MinigameResult result) =>
            new ChallengeMetrics(elapsed: Machine.Clock.Elapsed, completedTargets: Machine.PlayerMovesMade,
                mistakes: Machine.Mistakes, hintUsed: Machine.HintUsed, detail: Detail(result != null && result.Pass));

        string Detail(bool solved)
        {
            if (solved)
                return $"Thời gian {ChessFinalHud.FormatClock(Machine.Clock.Elapsed)} · " +
                       $"Sai: {Machine.Mistakes}/{Machine.MaxMistakes}" + (Machine.HintUsed ? " · Có dùng gợi ý" : string.Empty);
            return Machine.FailReason == ChessFailReason.TimeUp ? "Hết giờ" : $"Sai quá {Machine.MaxMistakes} lần";
        }

        void RefreshInput()
        {
            if (board == null || promotion == null) return;
            bool open = Machine.CanMove && !promotion.IsOpen;
            board.SetInteractable(open, Machine.Puzzle.PlayerColor,
                open ? MoveGenerator.LegalMoves(Machine.Position) : null);
        }

        static string CountWord(int n) => n switch { 1 => "một", 2 => "hai", 3 => "ba", _ => n.ToString(CultureInfo.InvariantCulture) };

        static string TimeWords(float seconds) => Mathf.RoundToInt(seconds) switch
        {
            60 => "một phút",
            90 => "một phút rưỡi",
            120 => "hai phút",
            int s => $"{s} giây"
        };
    }
}
```
`OnMistake` caps the HUD at `Sai: 2/2` on the failing mistake. The result screen then explains the failure.

- [ ] **Step 6: Let the Editor assembly see the chess types**

Add `"KMA.Gameplay.Chess"` and `"KMA.Gameplay.Chess.Core"` to `references` in `Assets/Editor/KMA.EditorTools.asmdef`.

- [ ] **Step 7: `ChessFinalSceneConfigurator.cs`**

```csharp
#if UNITY_EDITOR
using System;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Chess;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    public static class ChessFinalSceneConfigurator
    {
        public const string ScenePath = "Assets/_Project/Scenes/MG_ChessFinal.unity";
        const string HudRootName = "S2_HUD_Minigame";
        const string SkyPath = "Assets/_Project/Art/Environments/Sprint/Sky.png";
        const string CampusPath = "Assets/_Project/Art/Environments/Sprint/Campus.png";
        const float BoardSize = 740f;
        static readonly string[] StudentPoses = { "idle", "hurt", "cheer0", "cheer1" };
        static readonly string[] TeacherPoses =
            { "idleBoss", "strictLook", "chessThink", "chessMove", "whistle0", "whistle1", "taunt", "cheer0" };

        [MenuItem("KMA/Chess Final/Build Scene")]
        public static void BuildScene()
        {
            ChessArtImporter.ImportAll();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var controllerObject = new GameObject("ChessFinalController");
            controllerObject.AddComponent<ChessFinalController>();
            var serialized = new SerializedObject(controllerObject.GetComponent<ChessFinalController>());
            serialized.FindProperty("tutorialSeconds").floatValue = 0f;
            serialized.FindProperty("countdownSeconds").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);

            MinigameUIAssembler.AssembleScenePath(ScenePath);
            BuildLayout();
            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] MG_ChessFinal built.");
        }

        static void BuildLayout()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            var parent = (RectTransform)(hudRoot.transform.Find("SafeAreaRoot") ?? hudRoot.transform);
            foreach (Transform child in hudRoot.GetComponentsInChildren<Transform>(true)
                         .Where(t => new[] { "Phase", "Stamina", "Score", "HeartBar", "Progress", "Status", "Timer" }
                             .Contains(t.name)).ToArray())
                child.gameObject.SetActive(false);
            Vector2 top = new Vector2(.5f, 1f), centre = new Vector2(.5f, .5f), bottom = new Vector2(.5f, 0f);

            // Backdrop: the campus art under a cream wash, so the board stays the focus.
            RectTransform backdrop = Rect(parent, "Backdrop", Vector2.zero, Vector2.one);
            backdrop.SetAsFirstSibling();
            Picture(backdrop, "Sky", SkyPath, Vector2.zero, Vector2.one, Color.white);
            Picture(backdrop, "Campus", CampusPath, Vector2.zero, new Vector2(1f, .55f), Color.white);
            Rect(backdrop, "Wash", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>().color =
                MinigameUiTheme.WithAlpha(UITheme.Shared.Background, .62f);

            TMP_Text title = UiKit.Label(parent, "Title", "Bài kiểm tra cuối", MinigameUiTheme.Title,
                MinigameUiTheme.TextPrimary);
            UiKit.Place(title.rectTransform, top, top, new Vector2(0f, -18f), new Vector2(900f, 70f));
            ChipHandle objective = UiKit.Chip(parent, "Objective", "Chiếu hết trong 2 nước");
            UiKit.Place(objective.Background.rectTransform, top, top, new Vector2(-150f, -96f), new Vector2(480f, 64f));
            ChipHandle clock = UiKit.Chip(parent, "Clock", "01:30");
            UiKit.Place(clock.Background.rectTransform, top, top, new Vector2(230f, -96f), new Vector2(200f, 64f));

            Image frame = UiKit.Panel(parent, "BoardFrame");
            UiKit.Place(frame.rectTransform, centre, centre, new Vector2(0f, -30f), Vector2.one * (BoardSize + 64f));
            RectTransform boardRect = Rect(frame.transform, "Board", centre, centre);
            boardRect.sizeDelta = Vector2.one * BoardSize;
            var board = boardRect.gameObject.AddComponent<ChessBoardView>();
            board.Configure(ChessArtImporter.LoadPieces(), UiKitAssets.Load().Circle, UiKitAssets.Load().Ring);
            for (int i = 0; i < 8; i++)
            {
                TMP_Text file = UiKit.Label(frame.transform, "File" + (char)('a' + i), ((char)('a' + i)).ToString(),
                    MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary);
                UiKit.Place(file.rectTransform, new Vector2(.5f, 0f), centre,
                    new Vector2((i - 3.5f) * BoardSize / 8f, 16f), new Vector2(40f, 30f));
                TMP_Text rank = UiKit.Label(frame.transform, "Rank" + (i + 1), (i + 1).ToString(),
                    MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary);
                UiKit.Place(rank.rectTransform, new Vector2(0f, .5f), centre,
                    new Vector2(16f, (i - 3.5f) * BoardSize / 8f), new Vector2(30f, 40f));
            }

            TMP_Text turn = UiKit.Label(parent, "Turn", "Đọc đề rồi bấm Bắt đầu", MinigameUiTheme.Body,
                MinigameUiTheme.TextPrimary);
            UiKit.Place(turn.rectTransform, bottom, bottom, new Vector2(-170f, 28f), new Vector2(520f, 60f));
            ButtonHandle hint = UiKit.Button(parent, "HintButton", "Gợi ý", ButtonVariant.Secondary);
            UiKit.Place((RectTransform)hint.Button.transform, bottom, bottom, new Vector2(220f, 14f),
                new Vector2(220f, MinigameUiTheme.ButtonHeight));
            TMP_Text toast = UiKit.Label(parent, "Toast", string.Empty, MinigameUiTheme.Body,
                MinigameUiTheme.Accent, outline: true);
            UiKit.Place(toast.rectTransform, centre, centre, new Vector2(0f, 420f), new Vector2(760f, 56f));
            toast.gameObject.SetActive(false);

            // Side columns: 18 % each, avatars bottom-anchored below the name and counters.
            Image student = Avatar(parent, "Student", .09f, CharacterArt.Load(CharacterArt.Hero, "idle"));
            Image teacher = Avatar(parent, "Teacher", .91f, CharacterArt.Load(CharacterArt.Boss, "idleBoss"));
            NameTag(parent, "StudentName", "Tân Thủ", .09f);
            NameTag(parent, "TeacherName", "Cô Thể Chất", .91f);
            ChipHandle mistakes = UiKit.Chip(parent, "Mistakes", "Sai: 0/2");
            UiKit.Place(mistakes.Background.rectTransform, new Vector2(.09f, .80f), centre, Vector2.zero, new Vector2(240f, 60f));
            Image bubble = UiKit.Panel(parent, "SpeechBubble");
            UiKit.Place(bubble.rectTransform, new Vector2(.91f, .72f), centre, Vector2.zero, new Vector2(320f, 190f));
            TMP_Text bubbleText = UiKit.Label(bubble.transform, "Text", string.Empty, MinigameUiTheme.Caption,
                MinigameUiTheme.TextPrimary);
            UiKit.Stretch(bubbleText.rectTransform, new Vector2(18f, 14f), new Vector2(-18f, -14f));
            bubbleText.enableWordWrapping = true;
            bubble.gameObject.SetActive(false);

            Image intro = UiKit.Panel(parent, "IntroCard");
            UiKit.Place(intro.rectTransform, centre, centre, new Vector2(0f, -30f), new Vector2(640f, 380f));
            TMP_Text introText = UiKit.Label(intro.transform, "Text",
                "Chiếu hết trong 2 nước.\nThời gian suy nghĩ 90 giây.\nĐược sửa sai 2 lần.",
                MinigameUiTheme.BodyLarge, MinigameUiTheme.TextPrimary);
            UiKit.Place(introText.rectTransform, top, top, new Vector2(0f, -36f), new Vector2(580f, 200f));
            ButtonHandle start = UiKit.Button(intro.transform, "StartButton", "Bắt đầu", ButtonVariant.Primary);
            UiKit.Place((RectTransform)start.Button.transform, bottom, bottom, new Vector2(0f, 36f),
                new Vector2(320f, MinigameUiTheme.ButtonHeight));

            Image picker = UiKit.Panel(parent, "PromotionPicker");
            UiKit.Place(picker.rectTransform, centre, centre, new Vector2(0f, -30f), new Vector2(640f, 180f));
            Button[] options = new[] { "Hậu", "Xe", "Tượng", "Mã" }.Select((label, i) =>
            {
                ButtonHandle option = UiKit.Button(picker.transform, "Promote" + i, label, ButtonVariant.Primary);
                UiKit.Place((RectTransform)option.Button.transform, new Vector2((i + .5f) / 4f, .5f), centre,
                    Vector2.zero, new Vector2(136f, MinigameUiTheme.ButtonHeight));
                return option.Button;
            }).ToArray();
            var promotion = picker.gameObject.AddComponent<PromotionPicker>();
            promotion.Configure(options[0], options[1], options[2], options[3]);

            var hud = parent.gameObject.AddComponent<ChessFinalHud>();
            hud.Configure(objective.Label, clock.Label, mistakes.Label, turn, toast, intro.gameObject, start.Button,
                hint.Button);
            var cast = parent.gameObject.AddComponent<ChessCastView>();
            cast.Configure(student, teacher, Poses(CharacterArt.Hero, StudentPoses),
                Poses(CharacterArt.Boss, TeacherPoses), bubble.gameObject, bubbleText);

            var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            if (pause != null)
            {
                pause.transform.SetParent(parent, false);
                UiKit.Place((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-22f, -18f),
                    Vector2.one * MinigameUiTheme.ButtonHeight);
            }
            // Popups stay on top of the board.
            intro.transform.SetAsLastSibling();
            picker.transform.SetAsLastSibling();
            picker.gameObject.SetActive(false);

            var controller = Object.FindFirstObjectByType<ChessFinalController>();
            controller.Configure(board, hud, cast, promotion);
            foreach (Object dirty in new Object[] { controller, board, hud, cast, promotion, hudRoot })
                EditorUtility.SetDirty(dirty);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static ChessCastView.Pose[] Poses(string character, string[] names) => names
            .Select(name => new ChessCastView.Pose { name = name, sprite = CharacterArt.Load(character, name) })
            .ToArray();

        static Image Avatar(RectTransform parent, string name, float x, Sprite sprite)
        {
            RectTransform rect = Rect(parent, name, new Vector2(x, .06f), new Vector2(x, .06f));
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(300f, 400f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static void NameTag(RectTransform parent, string name, string text, float x)
        {
            TMP_Text label = UiKit.Label(parent, name, text, MinigameUiTheme.BodyLarge, MinigameUiTheme.TextPrimary,
                outline: true);
            UiKit.Place(label.rectTransform, new Vector2(x, .89f), new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(320f, 60f));
        }

        static void Picture(RectTransform parent, string name, string path, Vector2 min, Vector2 max, Color color)
        {
            var image = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
                throw new InvalidOperationException("[KMA] Missing backdrop " + path);
            image.color = color;
            image.raycastTarget = false;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
```
If `Sprint/Sky.png` or `Campus.png` are not imported as Sprites (`LoadAssetAtPath<Sprite>` returns null), check how `SprintSceneConfigurator` loads them and use the same call. Do not change their import settings.

- [ ] **Step 8: Build the scene and run the tests**

```bash
"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.ChessFinalSceneConfigurator.BuildScene -quit -logFile Builds/chess-scene.log
grep -n "MG_ChessFinal built\|error\|Exception" Builds/chess-scene.log | head
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Chess" chess-scene
```
Expected: `[KMA] MG_ChessFinal built.`, and `ChessFinalSceneTests` 3/3 pass.

- [ ] **Step 9: Look at it**

Use the `testing-unity-ui-with-screenshots` skill to capture `MG_ChessFinal` at 1920×1080 in two states: the intro card, and after `BeginAttempt` with a piece selected. Read both PNGs. Check that:
- the board is centred and square;
- the coordinates sit outside the board;
- neither avatar overlaps the board, title or chips;
- the hat and ponytail are not cropped;
- Vietnamese diacritics render;
- the pieces are readable on both square colours.

Fix layout constants in the configurator, then rebuild, until the screenshots look right. Save the two captures as `docs/qa/images/chess-final-1-intro.png` and `chess-final-2-select.png`.

- [ ] **Step 10: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Chess/*.cs Assets/_Project/Scripts/Gameplay/Chess/*.cs.meta \
  Assets/Editor/ChessFinalSceneConfigurator.cs Assets/Editor/ChessFinalSceneConfigurator.cs.meta Assets/Editor/KMA.EditorTools.asmdef \
  Assets/_Project/Scenes/MG_ChessFinal.unity Assets/_Project/Scenes/MG_ChessFinal.unity.meta ProjectSettings/EditorBuildSettings.asset \
  Assets/Tests/PlayMode/Gameplay/Chess Assets/Tests/PlayMode/Gameplay/Chess.meta \
  docs/qa/images/chess-final-1-intro.png docs/qa/images/chess-final-2-select.png
git commit -m "feat(chess): add the final exam scene with board, cast and HUD"
```

---

### Task 11: Celebration scene

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Celebration/KMA.Gameplay.Celebration.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/Celebration/CelebrationTimeline.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Celebration/UiConfetti.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Celebration/CelebrationSceneController.cs`
- Create: `Assets/Editor/CelebrationSceneConfigurator.cs`
- Modify: `Assets/Editor/KMA.EditorTools.asmdef` (add `KMA.Gameplay.Celebration`)
- Create (generated): `Assets/_Project/Scenes/Celebration.unity` (+ Build Settings)
- Test: `Assets/Tests/EditMode/Gameplay/Celebration/KMA.Gameplay.Celebration.EditMode.Tests.asmdef`, `CelebrationTimelineTests.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Celebration/KMA.Gameplay.Celebration.PlayMode.Tests.asmdef`, `CelebrationSceneTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs` (remove the `#if` guard)

**Interfaces:**
- Consumes: Task 7 (`GameManager.TryMarkCelebrationSeen`), Task 8 (`CelebrationSummary`, `SceneRouter.RouteToCelebration`), Task 9 (BossPE poses).
- Produces:
  - `enum CelebrationBeat { Arrive, Cheer, Teacher, Summary }`
  - `CelebrationTimeline`:
    - Constants: `CheerAt = 2f`, `TeacherAt = 5f`, `SummaryAt = 8f`, `Duration = 11f`
    - Members: `Time`, `Beat`, `SummaryShown`, `Skipped`, `event Action SummaryRequested`, `Tick(float)`, `Skip()`
  - `CelebrationSceneController`: `Timeline`, `Summary`, `SummaryVisible`, `RowTexts`, `Skip()`, `GoToMenu()`, `GoToMap()`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/Celebration/KMA.Gameplay.Celebration.EditMode.Tests.asmdef`: same shape as the Chess EditMode test asmdef, with `"name": "KMA.Gameplay.Celebration.EditMode.Tests"`, `"rootNamespace": "KMA.Tests.Gameplay.Celebration"` and references `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `KMA.Gameplay.Celebration`.

`CelebrationTimelineTests.cs`:
```csharp
using KMA.Gameplay.Celebration;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Celebration
{
    public sealed class CelebrationTimelineTests
    {
        int requested;
        CelebrationTimeline timeline;

        [SetUp]
        public void SetUp()
        {
            requested = 0;
            timeline = new CelebrationTimeline();
            timeline.SummaryRequested += () => requested++;
        }

        [Test]
        public void BeatsFollowTheScheduleAndTheSummaryOpensOnce()
        {
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Arrive));
            timeline.Tick(2.1f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Cheer));
            timeline.Tick(3f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Teacher));
            timeline.Tick(3f);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Summary));
            timeline.Tick(10f);
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(timeline.Time, Is.EqualTo(CelebrationTimeline.Duration));
        }

        [TestCase(0f)]
        [TestCase(10.9f)]
        public void SkipConvergesOnTheSameSummary(float at)
        {
            timeline.Tick(at);
            timeline.Skip();
            timeline.Skip();
            timeline.Tick(5f);
            Assert.That(timeline.SummaryShown, Is.True);
            Assert.That(timeline.Beat, Is.EqualTo(CelebrationBeat.Summary));
            Assert.That(requested, Is.EqualTo(1));
        }
    }
}
```

`Assets/Tests/PlayMode/Gameplay/Celebration/KMA.Gameplay.Celebration.PlayMode.Tests.asmdef`: same shape as the Chess PlayMode test asmdef, with references `UnityEngine.TestRunner`, `KMA.Gameplay`, `KMA.Gameplay.Celebration`, `KMA.Gameplay.Progression`, `KMA.Gameplay.Core`, `Unity.TextMeshPro`.

`CelebrationSceneTests.cs`:
```csharp
using System.Collections;
using KMA.Gameplay.Celebration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Celebration
{
    public sealed class CelebrationSceneTests
    {
        [UnityTest]
        public IEnumerator OpenedWithoutASessionShowsSampleDataAndSkipReachesTheSummary()
        {
            yield return SceneManager.LoadSceneAsync("Celebration", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<CelebrationSceneController>();
            Assert.That(controller.Summary.IsSample, Is.True);
            Assert.That(controller.SummaryVisible, Is.False);
            controller.Skip();
            yield return new WaitForSeconds(.6f);
            Assert.That(controller.SummaryVisible, Is.True);
            Assert.That(controller.RowTexts.Length, Is.EqualTo(4));
            Assert.That(controller.RowTexts[3], Does.Contain("Bài kiểm tra cuối"));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Celebration" celebration-timeline`
Expected: compile errors.

- [ ] **Step 3: Assembly and timeline**

`KMA.Gameplay.Celebration.asmdef`: references `KMA.Gameplay`, `KMA.Gameplay.Progression`, `KMA.Gameplay.Core`, `KMA.Gameplay.UI`, `Unity.TextMeshPro`, `UnityEngine.UI`, `KMA.Text`; `rootNamespace` `KMA.Gameplay.Celebration`; other fields as in `KMA.Gameplay.Chess.asmdef`.

`CelebrationTimeline.cs`:
```csharp
using System;

namespace KMA.Gameplay.Celebration
{
    public enum CelebrationBeat { Arrive, Cheer, Teacher, Summary }

    /// Spec section 5.3: arrive 0-2 s, cheer 2-5 s, teacher 5-8 s, summary from 8 s. Skip jumps to the
    /// summary through the same path the timeline takes.
    public sealed class CelebrationTimeline
    {
        public const float CheerAt = 2f, TeacherAt = 5f, SummaryAt = 8f, Duration = 11f;

        public event Action SummaryRequested;

        public float Time { get; private set; }
        public bool SummaryShown { get; private set; }
        public bool Skipped { get; private set; }

        public CelebrationBeat Beat => Time >= SummaryAt ? CelebrationBeat.Summary
            : Time >= TeacherAt ? CelebrationBeat.Teacher
            : Time >= CheerAt ? CelebrationBeat.Cheer
            : CelebrationBeat.Arrive;

        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            Time = Math.Min(Duration, Time + dt);
            if (Beat == CelebrationBeat.Summary) ShowSummary();
        }

        public void Skip()
        {
            if (SummaryShown) return;
            Skipped = true;
            Time = Math.Max(Time, SummaryAt);
            ShowSummary();
        }

        void ShowSummary()
        {
            if (SummaryShown) return;
            SummaryShown = true;
            SummaryRequested?.Invoke();
        }
    }
}
```

- [ ] **Step 4: `UiConfetti.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Celebration
{
    /// A few flat paper bits falling over the scene. Deterministic, UI-only, no particle system.
    public sealed class UiConfetti : MonoBehaviour
    {
        [SerializeField] Color[] colors =
        {
            new Color32(0xFF, 0xC9, 0x28, 0xFF), new Color32(0xE8, 0x5A, 0x48, 0xFF),
            new Color32(0x4F, 0xB3, 0xF0, 0xFF), new Color32(0x62, 0xB5, 0x4A, 0xFF)
        };
        [SerializeField] int count = 48;
        readonly List<(RectTransform rect, float speed, float spin, float sway)> bits =
            new List<(RectTransform, float, float, float)>();

        public bool Playing { get; private set; }

        public void Play()
        {
            if (Playing) return;
            Playing = true;
            var random = new System.Random(20261006);
            var area = (RectTransform)transform;
            for (int i = 0; i < count; i++)
            {
                var rect = (RectTransform)new GameObject("Bit" + i, typeof(RectTransform), typeof(Image)).transform;
                rect.SetParent(transform, false);
                rect.sizeDelta = new Vector2(14f + random.Next(10), 22f + random.Next(12));
                rect.anchorMin = rect.anchorMax = new Vector2((float)random.NextDouble(), 1f);
                rect.anchoredPosition = new Vector2(0f, 40f + random.Next((int)Mathf.Max(1f, area.rect.height)));
                var image = rect.GetComponent<Image>();
                image.color = colors[i % colors.Length];
                image.raycastTarget = false;
                bits.Add((rect, 160f + random.Next(140), random.Next(-180, 180), (float)random.NextDouble() * 6f));
            }
        }

        void Update()
        {
            if (!Playing) return;
            float height = ((RectTransform)transform).rect.height;
            foreach ((RectTransform rect, float speed, float spin, float sway) in bits)
            {
                Vector2 p = rect.anchoredPosition;
                p.y -= speed * Time.deltaTime;
                p.x = Mathf.Sin(Time.time * 2f + sway) * 24f;
                if (p.y < -height - 40f) p.y = 40f;
                rect.anchoredPosition = p;
                rect.Rotate(0f, 0f, spin * Time.deltaTime);
            }
        }
    }
}
```

- [ ] **Step 5: `CelebrationSceneController.cs`**

```csharp
using System;
using System.Globalization;
using System.Linq;
using KMA.Gameplay.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.Gameplay.Celebration
{
    public sealed class CelebrationSceneController : MonoBehaviour
    {
        static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        [SerializeField] Image student;
        [SerializeField] Image classmate;
        [SerializeField] Image teacher;
        [SerializeField] Sprite[] studentFrames;   // idle, cheer0, cheer1
        [SerializeField] Sprite[] classmateFrames; // idle, cheer0, cheer1
        [SerializeField] Sprite[] teacherFrames;   // idleBoss, taunt, cheer0
        [SerializeField] GameObject bubble;
        [SerializeField] TMP_Text bubbleText;
        [SerializeField] UiConfetti confetti;
        [SerializeField] CanvasGroup summaryGroup;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text[] rows;
        [SerializeField] TMP_Text footnote;
        [SerializeField] Button skipButton;
        [SerializeField] Button menuButton;
        [SerializeField] Button replayButton;
        float frameClock;
        bool leaving;

        public CelebrationTimeline Timeline { get; } = new CelebrationTimeline();
        public CelebrationSummary Summary { get; private set; }
        public bool SummaryVisible => summaryGroup.alpha >= .99f;
        public string[] RowTexts => rows.Select(row => row.text).ToArray();

        public void Configure(Image studentImage, Image classmateImage, Image teacherImage, Sprite[] studentSet,
            Sprite[] classmateSet, Sprite[] teacherSet, GameObject speech, TMP_Text speechText, UiConfetti paper,
            CanvasGroup summaryPanel, TMP_Text titleLabel, TMP_Text[] rowLabels, TMP_Text note, Button skip,
            Button menu, Button replay)
        {
            student = studentImage; classmate = classmateImage; teacher = teacherImage;
            studentFrames = studentSet; classmateFrames = classmateSet; teacherFrames = teacherSet;
            bubble = speech; bubbleText = speechText; confetti = paper; summaryGroup = summaryPanel;
            title = titleLabel; rows = rowLabels; footnote = note;
            skipButton = skip; menuButton = menu; replayButton = replay;
        }

        void Start()
        {
            GameSession session = SceneRouter.Instance != null ? SceneRouter.Instance.Session : GameManager.Instance?.Session;
            bool real = session != null && session.Journey.CourseComplete;
            Summary = real ? CelebrationSummary.From(session) : CelebrationSummary.Sample();
            // Progress is already saved by the result commit; this only records that the first celebration ran.
            if (real && GameManager.Instance != null && !GameManager.Instance.TryMarkCelebrationSeen(out string error))
                Debug.LogWarning("[KMA] Could not save the celebration flag: " + error);

            Timeline.SummaryRequested += OnSummaryRequested;
            skipButton.onClick.AddListener(Skip);
            menuButton.onClick.AddListener(GoToMenu);
            replayButton.onClick.AddListener(GoToMap);
            FillSummary();
            summaryGroup.alpha = 0f;
            summaryGroup.interactable = summaryGroup.blocksRaycasts = false;
            bubble.SetActive(false);
        }

        public void Skip() => Timeline.Skip();

        void Update()
        {
            Timeline.Tick(Time.deltaTime);
            frameClock += Time.deltaTime;
            int cheer = Mathf.FloorToInt(frameClock / .35f) % 2 == 0 ? 1 : 2;
            CelebrationBeat beat = Timeline.Beat;
            student.sprite = studentFrames[beat == CelebrationBeat.Arrive ? 0 : cheer];
            classmate.sprite = classmateFrames[beat == CelebrationBeat.Arrive ? 0 : 3 - cheer];
            teacher.sprite = teacherFrames[beat == CelebrationBeat.Arrive || beat == CelebrationBeat.Cheer ? 0
                : Timeline.Time < CelebrationTimeline.TeacherAt + 1f ? 1 : 2];
            if (beat != CelebrationBeat.Arrive) confetti.Play();
            if (beat == CelebrationBeat.Teacher && !bubble.activeSelf && !Timeline.Skipped)
            {
                bubble.SetActive(true);
                bubbleText.text = VietText.Fix("Được, em qua.");
            }
            if (Timeline.SummaryShown)
                summaryGroup.alpha = Mathf.MoveTowards(summaryGroup.alpha, 1f, Time.deltaTime * 3f);
        }

        void OnSummaryRequested()
        {
            skipButton.gameObject.SetActive(false);
            bubble.SetActive(false);
            summaryGroup.interactable = summaryGroup.blocksRaycasts = true;
        }

        void FillSummary()
        {
            title.text = VietText.Fix("Đã qua thể chất!");
            for (int i = 0; i < Summary.Subjects.Count && i < 3; i++)
            {
                CelebrationRow row = Summary.Subjects[i];
                string score = row.HasScore
                    ? $"  ·  {row.Score.ToString("0.0", Vietnamese)} điểm  ·  Hạng {row.Rank}" : string.Empty;
                rows[i].text = VietText.Fix((row.Completed ? "Đạt  " : "Chưa đạt  ") + row.Title + score);
            }
            rows[3].text = VietText.Fix(Summary.ChessRecorded
                ? $"Đạt  {CelebrationSummary.ChessTitle}  ·  {CelebrationSummary.FormatClock(Summary.ChessThinkSeconds)}" +
                  $"  ·  Sai: {Summary.ChessMistakes}  ·  {(Summary.ChessHintUsed ? "Có dùng gợi ý" : "Không dùng gợi ý")}"
                : $"Đạt  {CelebrationSummary.ChessTitle}");
            footnote.text = VietText.Fix(Summary.IsSample ? "Dữ liệu mẫu"
                : Summary.SupplementaryRounds > 0 ? $"Thi bổ sung: {Summary.SupplementaryRounds} đợt" : string.Empty);
        }

        public void GoToMenu()
        {
            if (leaving) return;
            leaving = true;
            if (SceneRouter.Instance != null && SceneRouter.Instance.RouteToMenu()) return;
            if (SceneRouter.Instance == null) SceneManager.LoadScene("Menu");
            else leaving = false;
        }

        public void GoToMap()
        {
            if (leaving) return;
            leaving = true;
            if (SceneRouter.Instance != null && SceneRouter.Instance.Route(SessionRoute.Map)) return;
            if (SceneRouter.Instance == null) SceneManager.LoadScene("Map");
            else leaving = false;
        }
    }
}
```
`GameManager` lives in `KMA.Gameplay.Core`. Confirm the namespace with `grep -n "^namespace" Assets/_Project/Scripts/Core/GameManager.cs` and fix the `using` if it differs.

- [ ] **Step 6: `CelebrationSceneConfigurator.cs`**

Model it on `ChessFinalSceneConfigurator`, reusing its `Rect`, `Picture` and `EnsureInBuildSettings` patterns (copy the helpers; the two configurators do not share a base). `BuildScene()` steps:
1. `ChessArtImporter.ImportAll()`.
2. New empty scene. Add an `EventSystem` + `InputSystemUIInputModule`, and a `Main Camera` (orthographic, `clearFlags = SolidColor`, `backgroundColor = UITheme.Shared.Background`, tag `MainCamera`).
3. A `Canvas` (`ScreenSpaceOverlay`), `CanvasScaler` (`ScaleWithScreenSize`, 1920×1080, match 0.5) and `GraphicRaycaster` named `CelebrationCanvas`, with a stretched child `SafeAreaRoot` carrying `SafeAreaFitter`.
4. Backdrop: `Sky.png` stretched and `Campus.png` on the lower 60 %, no wash.
5. Characters as bottom-anchored `Image`s (`preserveAspect`, 300×400):
   - Tân Thủ (`MaleAdventurer`) at x = .50;
   - Mai Toang (`FemalePerson`) at x = .33, scale .9;
   - Cô Thể Chất (`BossPE`) at x = .78.
   Frames: student `idle, cheer0, cheer1`; classmate `idle, cheer0, cheer1`; teacher `idleBoss, taunt, cheer0`.
6. A speech bubble (`UiKit.Panel` 320×150 at (.78, .66), caption label, inactive).
7. A stretched `Confetti` rect with `UiConfetti`, placed above the characters and below the summary.
8. `UiKit.Button(..., "SkipButton", "Bỏ qua", Secondary)`, top-right (anchor (1,1), offset (-24,-24), 220×88).
9. The summary: a `UiKit.Panel` 1100×620 centred, with a `CanvasGroup`.
   - Title `Đã qua thể chất!` (`MinigameUiTheme.Display * .5f`, Accent, outline).
   - Four row labels (`Body`, left aligned, 980×60, stacked from y = 160 down by 76).
   - A footnote label (`Caption`).
   - `Về menu` Primary (anchor bottom, x +170) and `Chơi lại` Secondary (x −170), 300×88.
10. Add `CelebrationSceneController` on the canvas and call `Configure(...)` with every reference above.
11. Save to `Assets/_Project/Scenes/Celebration.unity` and add it to Build Settings.

Add `"KMA.Gameplay.Celebration"` to `Assets/Editor/KMA.EditorTools.asmdef` references, then run:
```bash
"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.CelebrationSceneConfigurator.BuildScene -quit -logFile Builds/celebration-scene.log
```
Expected: the log ends with `[KMA] Celebration built.` (log that line at the end of `BuildScene`).

- [ ] **Step 7: Run the tests**

```bash
tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Celebration" celebration-timeline
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Celebration" celebration-scene
```
Expected: 3 EditMode cases and 1 PlayMode test pass.

- [ ] **Step 8: Look at it**

Capture `Celebration` with the screenshot skill at 3 s (cheer), 6 s (teacher and bubble) and after `Skip()` (summary). Check that:
- no character covers the summary panel's text;
- the teacher's cap and ponytail are fully inside the frame;
- the title and the row text fit without overflow.

Save the three captures as `docs/qa/images/celebration-1-cheer.png`, `celebration-2-teacher.png` and `celebration-3-summary.png`.

- [ ] **Step 9: Enable the chess routing tests**

Remove the `#if KMA_CHESS_ROUTING_READY` / `#endif` lines from `Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs` (written in Task 8). Then run:
```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression" chess-routing
```
Expected: all 5 `ChessFinalRoutingTests` pass, along with the rest of the Progression PlayMode suite. If `FailedFinalOffersReplayEvenAtZeroLivesAndNeverTheFrogJump` fails because `TryStartChallenge` refreshes lives through the regen clock, assert on `session.Journey.AttemptsRemaining` only after `FailActive`. Never loosen the frog-jump assertions.

- [ ] **Step 10: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Celebration Assets/_Project/Scripts/Gameplay/Celebration.meta \
  Assets/Editor/CelebrationSceneConfigurator.cs Assets/Editor/CelebrationSceneConfigurator.cs.meta Assets/Editor/KMA.EditorTools.asmdef \
  Assets/_Project/Scenes/Celebration.unity Assets/_Project/Scenes/Celebration.unity.meta ProjectSettings/EditorBuildSettings.asset \
  Assets/Tests/EditMode/Gameplay/Celebration Assets/Tests/EditMode/Gameplay/Celebration.meta \
  Assets/Tests/PlayMode/Gameplay/Celebration Assets/Tests/PlayMode/Gameplay/Celebration.meta \
  Assets/Tests/PlayMode/Progression/ChessFinalRoutingTests.cs \
  docs/qa/images/celebration-1-cheer.png docs/qa/images/celebration-2-teacher.png docs/qa/images/celebration-3-summary.png
git commit -m "feat(celebration): add the skippable course celebration scene"
```

---

### Task 12: Map: fourth stop, final lesson card and the replay button

**Files:**
- Modify: `Assets/_Project/Scripts/UI/UITheme.cs` (`LessonJourneyStyle.chess`, 4-entry `stopX`/`stopY` defaults)
- Modify: `Assets/_Project/Settings/UI/UITheme.asset` (`stopX`, `stopY` with 4 entries)
- Modify: `Assets/_Project/Scripts/UI/MapJourneyPathLayout.cs`
- Modify: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs`
- Modify: `Assets/_Project/Scripts/UI/MapScreen.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyLessonList.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs`
- Modify: `Assets/_Project/Scripts/UI/JourneyCourseSummary.cs`
- Modify: `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs`
- Test: `Assets/Tests/PlayMode/Presentation/JourneyMapChessTests.cs`

**Interfaces:**
- Consumes: Task 6 (`SubjectId.Chess`, `IsPenalizedKind`), Task 8 (`RouteToCelebration`), Task 9 (`SportIcon_Chess`).
- Produces:
  - `MapScreen.CelebrationRequested` (`event Action`)
  - `JourneyCourseSummary.ReplayButton` and `JourneyCourseSummary.ReplayRequested` (`event Action`)
  - `UITheme.LessonJourneyStyle.chess` (Color)

Read every file in this list right before editing it. `MapScreen.cs`, `MapPresentationBuilder.cs` and `JourneyLessonList.cs` had user edits when this plan was written.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/PlayMode/Presentation/JourneyMapChessTests.cs`:
```csharp
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class JourneyMapChessTests
    {
        GameObject root;
        MapScreen screen;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("JourneyMap", typeof(RectTransform));
            screen = root.AddComponent<MapScreen>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void MapShowsFourStopsWithTheFinalLockedUntilTheSoccerExam()
        {
            var session = new GameSession();
            CompleteThrough(session, "soccer_practice");
            MapPresentationBuilder.Build(screen, session);
            Assert.That(screen.Nodes.Select(n => n.SubjectId), Is.EqualTo(new[]
                { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess }));
            Assert.That(screen.Nodes[3].IsInteractable, Is.False);

            CompleteThrough(session, "soccer_exam");
            screen.RefreshJourney(session);
            Assert.That(screen.Nodes[3].IsInteractable, Is.True);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("chess_final"));
            Assert.That(screen.LessonList.LessonIds, Is.EqualTo(new[] { "chess_final" }));
        }

        [Test]
        public void TheFinalCardStaysPlayableAtZeroLives()
        {
            var session = new GameSession();
            CompleteThrough(session, "soccer_exam");
            session.Journey.SetAttemptsRemaining(0);
            MapPresentationBuilder.Build(screen, session);
            screen.RefreshJourney(session);
            Assert.That(screen.LessonList.CurrentChallengeId, Is.EqualTo("chess_final"));
            Button card = screen.LessonList.GetComponentsInChildren<Button>()
                .First(b => b.gameObject.activeInHierarchy && b.name.StartsWith("Lesson"));
            Assert.That(card.interactable, Is.True);
        }

        [Test]
        public void CompletedCourseOffersTheCelebrationReplay()
        {
            var session = new GameSession();
            CompleteThrough(session, "chess_final");
            MapPresentationBuilder.Build(screen, session);
            screen.RefreshJourney(session);
            int requests = 0;
            screen.CelebrationRequested += () => requests++;
            Assert.That(screen.CourseSummary.gameObject.activeSelf, Is.True);
            screen.CourseSummary.ReplayButton.onClick.Invoke();
            Assert.That(requests, Is.EqualTo(1));
        }

        static void CompleteThrough(GameSession session, string id)
        {
            foreach (ChallengeDefinition definition in session.Journey.Catalog.Ordered)
            {
                if (session.Journey.IsChallengeComplete(definition.Id))
                {
                    if (definition.Id == id) return;
                    continue;
                }
                session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey, definition.Difficulty,
                    out ChallengeAttemptContext context);
                session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                    new ChallengeMetrics(completedTargets: definition.TargetCount),
                    ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null));
                if (definition.Id == id) return;
            }
        }
    }
}
```
Add `using UnityEngine.UI;` at the top for `Button`. Lesson card button names come from `JourneyLessonPresentation`. If they are not prefixed `Lesson`, look up the real name with `grep -n "Button" Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs` and use it.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation.JourneyMapChessTests" map-chess`
Expected: fail; the map has 3 nodes and `CelebrationRequested` does not exist.

- [ ] **Step 3: Theme**

`UITheme.cs`, `LessonJourneyStyle`:
```csharp
            public Color chess = new Color32(232, 90, 72, 255);
            public float[] stopX = { .12f, .37f, .63f, .88f };
            public float[] stopY = { .45f, .70f, .45f, .70f };
```
`UITheme.asset` (serialized values win over C# defaults), edit the two lists:
```yaml
    stopX:
    - 0.12
    - 0.37
    - 0.63
    - 0.88
    stopY:
    - 0.45
    - 0.7
    - 0.45
    - 0.7
```

- [ ] **Step 4: Path layout for four stops**

`MapJourneyPathLayout.cs`:
- Change `centers` to `new Vector2[4]` and the list capacities to 4/3.
- `int count = Mathf.Min(nodes.Count, Mathf.Min(style.stopX.Length, style.stopY.Length), centers.Length);`
- Add `SubjectId.Chess => 3,` to `CourseOrder`.
- Update the header comment from "three stops" to "the course stops".

- [ ] **Step 5: Builder, screen and summary button**

`MapPresentationBuilder.cs`:
- Append `new Entry(SubjectId.Chess, "Bài kiểm tra cuối", UITheme.Shared.LessonJourney.chess, true)` to `Entries`.
- In the `existing != null` branch (scenes baked with three stops), right after `existingNodes` is read, add:
```csharp
                if (existingNodes.Length > 0 && existingNodes.All(n => n.SubjectId != SubjectId.Chess))
                {
                    Entry final = Entries.First(e => e.Subject == SubjectId.Chess);
                    MapNodeView chess = MapStopBuilder.Build(existingNodes[0].transform.parent, screen,
                        final.Subject, final.Label, final.Color, Entries.Length - 1);
                    existingNodes = existingNodes.Append(chess).ToArray();
                }
```
Add `using System.Linq;` if it is missing. Check that the fresh-build branch builds nodes from `Entries`; if it loops over a hard-coded count of 3, change it to `Entries.Length`.

`JourneyCourseSummary.cs`: add a replay button that is created when missing, so baked scenes get it too:
```csharp
        [SerializeField] Button replayButton;
        public Button ReplayButton => EnsureReplayButton();
        public event System.Action ReplayRequested;

        Button EnsureReplayButton()
        {
            if (replayButton == null)
            {
                KMA.UI.Kit.ButtonHandle handle = KMA.UI.Kit.UiKit.Button(transform, "ReplayCelebration",
                    "Xem lại lễ mừng", KMA.UI.Kit.ButtonVariant.Primary);
                KMA.UI.Kit.UiKit.Place((RectTransform)handle.Button.transform, new Vector2(1f, .5f),
                    new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(300f, KMA.UI.Kit.MinigameUiTheme.ButtonHeight));
                replayButton = handle.Button;
            }
            replayButton.onClick.RemoveListener(OnReplay);
            replayButton.onClick.AddListener(OnReplay);
            return replayButton;
        }

        void OnReplay() => ReplayRequested?.Invoke();
```
Call `EnsureReplayButton()` at the end of `Show(...)`. Extend `CourseOrder` and `Labels` with `SubjectId.Chess` / `"Bài kiểm tra cuối"`, and shorten the summary label so it does not run under the button. Use `"HOÀN TẤT HỌC PHẦN"` followed by the four scores on a second line. Shrink its right edge by 320 px.

`MapScreen.cs`:
- Add `public event Action CelebrationRequested;`.
- In `BindPresentation`, after `CourseSummary` is assigned, subscribe once: `CourseSummary.ReplayRequested -= RaiseCelebration; CourseSummary.ReplayRequested += RaiseCelebration;` with `void RaiseCelebration() => CelebrationRequested?.Invoke();`. Find where `CourseSummary` is set; if `BindPresentation` doesn't set it, do this where it is set.
- Add `SubjectId.Chess` to `CourseOrder` and `SubjectId.Chess => "Bài kiểm tra cuối",` to the title switch.

`S5ShellSceneController.cs`: next to `map.ChallengeRequested += StartChallenge;` add `map.CelebrationRequested += ReplayCelebration;`, and the matching `-=` where it unsubscribes. Then add:
```csharp
        void ReplayCelebration() => KMA.Gameplay.Core.SceneRouter.Instance?.RouteToCelebration();
```

- [ ] **Step 6: Lesson list for a one-lesson subject**

`JourneyLessonList.cs`:
- `CourseOrder` gets `SubjectId.Chess` and `CourseTitles` gets `"Bài kiểm tra cuối"`. Add `SubjectId.Chess => UITheme.Shared.LessonJourney.chess,` to the chapter colour switch.
- Stage label: `ChallengeKind.Final => "CUỐI",` before the default.
- Lives lock: `bool outOfLives = checkpoint && !complete && !session.Journey.CourseComplete && JourneyProgress.IsPenalizedKind(challenge.Kind) && session.Lives == 0;`
- Connectors: in the `for (int index = 0; index < 2; index++)` loop, hide connectors that join nothing:
```csharp
                bool used = index + 1 < challenges.Length;
                transform.Find($"LessonConnector{index + 1}").gameObject.SetActive(used);
                transform.Find($"LessonArrow{index + 1}").gameObject.SetActive(used);
```
- Course icon: when `courseIcon.childCount <= subjectIndex`, create the missing glyph first, so baked panels show the chess icon:
```csharp
            if (courseIcon.childCount <= subjectIndex)
            {
                var glyph = new GameObject(selectedSubject + "Glyph", typeof(RectTransform), typeof(Image));
                glyph.transform.SetParent(courseIcon, false);
                KMA.UI.Kit.UiKit.Stretch((RectTransform)glyph.transform, new Vector2(14f, 14f), new Vector2(-14f, -14f));
                var image = glyph.GetComponent<Image>();
                image.sprite = MapPresentationBuilder.SportIconSprite(selectedSubject);
                image.color = UITheme.Shared.Surface;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
```
Use the same insets as the glyphs `JourneyLessonPresentation` builds; read them there and copy the values.

`JourneyLessonPresentation.cs`: add `SubjectId.Chess` to the `subjects` array that builds the course-icon glyphs. The `CourtPattern` children stay three; with no fourth pattern, the chess page shows the plain panel.

- [ ] **Step 7: Run the tests**

```bash
tools/run-unity-tests.sh PlayMode "KMA.Tests.Presentation" map-chess
```
Expected: the 3 `JourneyMapChessTests` pass, and the existing `JourneyMapTests` and other Presentation tests stay at baseline. `CompletedCourseShowsSummary…` must still pass after its Task 6 retarget.

- [ ] **Step 8: Look at it**

Capture the Map (screenshot skill, map states in `PlayModeScreenshotQaStates`). Add the states `map-chess-locked` (complete through `soccer_practice`), `map-chess-open` (through `soccer_exam`) and `map-complete` (through `chess_final`), following how `map-lives-3` builds its session. Check that:
- four stops fit without overlapping labels;
- the road joins all four stops;
- the chess lesson panel has one card and no dangling connector;
- the replay button does not overlap the summary text.

Save the captures as `docs/qa/images/chess-final-map-locked.png`, `chess-final-map-open.png` and `chess-final-map-complete.png`.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Scripts/UI/UITheme.cs Assets/_Project/Settings/UI/UITheme.asset Assets/_Project/Scripts/UI/MapJourneyPathLayout.cs \
  Assets/_Project/Scripts/UI/MapPresentationBuilder.cs Assets/_Project/Scripts/UI/MapScreen.cs Assets/_Project/Scripts/UI/JourneyLessonList.cs \
  Assets/_Project/Scripts/UI/JourneyLessonPresentation.cs Assets/_Project/Scripts/UI/JourneyCourseSummary.cs \
  Assets/_Project/Scripts/Shell/S5ShellSceneController.cs Assets/Editor/PlayModeScreenshotQaStates.cs \
  Assets/Tests/PlayMode/Presentation/JourneyMapChessTests.cs Assets/Tests/PlayMode/Presentation/JourneyMapChessTests.cs.meta \
  docs/qa/images/chess-final-map-locked.png docs/qa/images/chess-final-map-open.png docs/qa/images/chess-final-map-complete.png
git commit -m "feat(map): add the final exam stop and the celebration replay"
```

---

### Task 13: Full verification, QA evidence and hand-off docs

**Files:**
- Modify: `Assets/Editor/PlayModeScreenshotQaStates.cs` (chess level states)
- Create: `docs/qa/chess-final-celebration.md`
- Modify: `README.md`, `PLAN.md`

- [ ] **Step 1: Capture the remaining chess states**

Add QA states to `PlayModeScreenshotQaStates`, following an existing state such as `exam-fail-1`. Each state loads `MG_ChessFinal` and drives `ChessFinalController` through its public API (`BeginAttempt`, `RequestMove`, `RevealHint`). The states are:
- `chess-wrong`: one wrong move, mid-whistle;
- `chess-boss`: during the boss animation;
- `chess-hint`: hint level 2 shown;
- `chess-promotion`: picker open. Use a scene-loaded controller and call `promotion.Open` directly, because the demo puzzle has no promotion;
- `chess-win`: the result panel after solving;
- `chess-timeout`: the result after setting time to 89.9 s and ticking.

Capture each with the screenshot skill and save it under `docs/qa/images/chess-final-<state>.png`. Read every image: Vietnamese text, no overlaps, and labels `Sai: x/2`, `Lượt giảng viên`, `Hết giờ`.

- [ ] **Step 2: Run every suite**

```bash
tools/run-unity-tests.sh EditMode "" final-edit
tools/run-unity-tests.sh PlayMode "" final-play
```
Compare with the Task 0 baseline. Every new test must pass, and no old test may newly fail. Report the exact totals from both XML files.

- [ ] **Step 3: Play the whole loop in the Editor**

The user, or an agent with the Unity MCP, plays `Bootstrap` → Menu → New Game. Use a debug save made with `JourneyGameplayDriver.CompleteThrough(session, "soccer_exam")` (the existing QA save tooling, if any). Then go Map → stop 4, lose once, retry, win, watch the celebration, skip on the second visit, and go back to the Menu. Write down what was observed. Anything not actually played must be listed in the report as **not verified**, not as working.

- [ ] **Step 4: Build the Android APK**

Run `tools/build-apk.sh` (or `.ps1`), as documented in `README.md`. Expected: an APK in `Builds/`. If the SDK, licence or device is unavailable, record the exact error and do not claim an on-device check.

- [ ] **Step 5: Write `docs/qa/chess-final-celebration.md`**

Sections, all in Vietnamese to match the other QA docs:
1. Final behaviour, and the assumptions from spec §1.
2. Files created or changed, with their purpose (taken from this plan's File Structure).
3. How to run the level directly (`MG_ChessFinal`) and through the journey; the `KMA/Chess Final/*` and `KMA/Journey/*` menu items.
4. The demo puzzle: Lichess id, start FEN, main line, the `verifiedBy` string, and how it was verified (Python exhaustive tree + Stockfish ordering + C# `PuzzleVerifier`).
5. Test totals (Editor/automatic), the screenshots list, emulator/device status, and known limits. Examples of limits: Hard puzzles are not generated; `cheer0` stands in for a nod pose; the hint level carries over after the boss reply.
6. Adding a puzzle (pointer to `tools/chess/README.md`) and swapping boss or piece art (re-render or replace the PNGs, then run `KMA/Chess Final/Import Art`).
7. Testing the celebration gate: missing one subject, not yet winning the chess level, first win, replay from the Map, skip at 0 s, skip near the end, Về menu.
8. The extra punishments from spec §6 are **proposals only**; nothing was built for them. Frog jump is unchanged and never called from chess.

- [ ] **Step 6: Update `README.md` and `PLAN.md`**

- README: add `MG_ChessFinal` and `Celebration` to the Scenes table. In the progression loop, state that the course ends with the chess final (no lives, no frog jump) and that a first win opens the celebration.
- PLAN: add the fourth stop to the subject table and mention save v9.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editor/PlayModeScreenshotQaStates.cs docs/qa/chess-final-celebration.md docs/qa/images/chess-final-*.png README.md PLAN.md
git commit -m "docs(qa): chess final and celebration evidence"
```
