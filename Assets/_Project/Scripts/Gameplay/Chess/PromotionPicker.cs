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
