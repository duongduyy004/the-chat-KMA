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
