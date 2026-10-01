using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class ResponsiveGridLayout : MonoBehaviour
    {
        GridLayoutGroup grid;
        RectTransform rect;
        float lastWidth = -1f;
        int lastScreenWidth = -1;

        void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rect = GetComponent<RectTransform>();
        }

        void OnEnable() => Refresh();
        void OnRectTransformDimensionsChange() => Refresh();

        void LateUpdate()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            float width = rect == null ? 0f : rect.rect.width;
            if (!Mathf.Approximately(width, lastWidth) || Screen.width != lastScreenWidth)
            {
                lastWidth = width;
                lastScreenWidth = Screen.width;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (grid == null || rect == null) return;
            int columns = Mathf.Max(1, grid.constraintCount);
            float width = rect.rect.width;
            if (width <= 0f) width = Mathf.Max(1f, Screen.width - 144f);
            float cellWidth = Mathf.Max(1f, (width - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1)) / columns);
            float cellHeight = Mathf.Clamp(cellWidth * 0.56f, 220f, 264f);
            if (rect.rect.height > 0f)
                cellHeight = Mathf.Min(cellHeight, Mathf.Max(1f, (rect.rect.height - grid.spacing.y) / 2f));
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            int titleSize = cellWidth < 350f ? 30 : cellWidth < 420f ? 32 : 36;
            foreach (MapNodeView node in GetComponentsInChildren<MapNodeView>(true))
            {
                Transform title = node.transform.Find("CardHeader/TitleContainer/Title");
                if (title == null) continue;
                TMP_Text tmp = title.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                tmp.fontSize = titleSize;
                UiKit.FitLabel(tmp, titleSize);
            }
            LayoutElement element = GetComponent<LayoutElement>();
            if (element == null) return;
            element.preferredHeight = cellHeight * 2f + grid.spacing.y;
            element.flexibleHeight = 0f;
        }
    }
}
