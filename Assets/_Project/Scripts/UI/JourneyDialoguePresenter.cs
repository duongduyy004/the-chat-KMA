using System;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class JourneyDialoguePresenter : MonoBehaviour
    {
        readonly struct DialogueRequest
        {
            public readonly string NodeId, SeenKey;
            public DialogueRequest(string nodeId, string seenKey) { NodeId = nodeId; SeenKey = seenKey; }
        }

        JourneyDialogueLibrary library;
        Func<string, bool> persistSeen;
        readonly Queue<DialogueRequest> pending = new Queue<DialogueRequest>();
        RectTransform overlay;
        Image portrait;
        TMP_Text speaker, body, errorText, continueLabel;
        Button continueButton, skipButton;
        IReadOnlyList<JourneyDialogueLine> activeLines;
        string activeSeenKey;
        int lineIndex;
        Action closed;

        public bool IsShowing => overlay != null && overlay.gameObject.activeSelf;
        public int CurrentLineIndex => lineIndex;

        public void Configure(JourneyDialogueLibrary dialogueLibrary, Func<string, bool> saveSeen)
        {
            library = dialogueLibrary;
            persistSeen = saveSeen;
            EnsureView();
        }

        public void Show(string nodeId, Action onClosed)
        {
            Show(nodeId, nodeId, onClosed);
        }

        public void ShowJourney(GameSession session, GameManager manager)
        {
            if (session == null || manager == null) return;
            Configure(JourneyDialogueLibrary.LoadDefault(), key => manager.TryMarkJourneyDialogueSeen(key, out _));
            pending.Clear();
            JourneyProgress journey = session.Journey;
            void Add(string node, string key = null)
            {
                string seenKey = key ?? node;
                if (!journey.IsDialogueSeen(seenKey)) pending.Enqueue(new DialogueRequest(node, seenKey));
            }

            Add("opening");
            if (journey.AwaitingSupplementary)
            {
                Add("supplementary", $"supplementary_{journey.SupplementaryRounds + 1}");
            }
            else if (journey.CourseComplete)
            {
                Add("soccer_pass");
                Add("course_complete");
            }
            else
            {
                string checkpoint = journey.CheckpointChallengeId;
                switch (checkpoint)
                {
                    case "sprint_learn": Add("sprint_intro"); break;
                    case "sprint_exam": Add("sprint_exam"); break;
                    case "volleyball_learn": Add("sprint_pass"); Add("volleyball_intro"); break;
                    case "volleyball_exam": Add("volleyball_exam"); break;
                    case "soccer_learn": Add("volleyball_pass"); Add("soccer_intro"); break;
                    case "soccer_exam": Add("soccer_exam"); break;
                }
            }
            ShowNext();
        }

        void Show(string nodeId, string seenKey, Action onClosed)
        {
            if (library == null) library = JourneyDialogueLibrary.LoadDefault();
            if (library == null) throw new InvalidOperationException("Journey dialogue Resources asset is missing.");
            JourneyDialogueNode node = library.Get(nodeId);
            activeLines = node.Lines;
            activeSeenKey = seenKey;
            closed = onClosed;
            lineIndex = 0;
            errorText.text = string.Empty;
            overlay.gameObject.SetActive(true);
            RenderLine();
        }

        void EnsureView()
        {
            if (overlay != null) return;
            overlay = new GameObject("JourneyDialogueOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup))
                .GetComponent<RectTransform>();
            overlay.SetParent(transform, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            Image veil = overlay.GetComponent<Image>();
            veil.color = new Color(3f / 255f, 19f / 255f, 34f / 255f, .78f);
            veil.raycastTarget = true;

            var card = new GameObject("DialogueCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(overlay, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
            cardRect.sizeDelta = new Vector2(900f, 430f);
            card.GetComponent<Image>().color = new Color32(255, 249, 231, 255);
            card.GetComponent<Outline>().effectColor = new Color32(3, 18, 33, 255);

            portrait = MakeImage(card.transform, "Portrait", new Vector2(-325f, 0f), new Vector2(230f, 320f));
            speaker = MakeText(card.transform, "Speaker", new Vector2(155f, 132f), new Vector2(500f, 52f), 28,
                MinigameUiTheme.Accent, TextAlignmentOptions.Left);
            body = MakeText(card.transform, "Dialogue", new Vector2(155f, 28f), new Vector2(500f, 165f), 25,
                new Color32(8, 35, 61, 255), TextAlignmentOptions.TopLeft);
            errorText = MakeText(card.transform, "SaveError", new Vector2(155f, -103f), new Vector2(500f, 38f), 16,
                MinigameUiTheme.Energy, TextAlignmentOptions.Left);
            continueButton = MakeButton(card.transform, "CONTINUE", new Vector2(265f, -165f), new Vector2(220f, 60f));
            skipButton = MakeButton(card.transform, "BỎ QUA", new Vector2(30f, -165f), new Vector2(180f, 60f));
            continueButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(FinishNode);
            overlay.gameObject.SetActive(false);
        }

        void RenderLine()
        {
            JourneyDialogueLine line = activeLines[lineIndex];
            speaker.text = VietText.Fix(line.SpeakerRole);
            body.text = VietText.Fix(line.Text);
            portrait.sprite = line.Portrait;
            portrait.enabled = line.Portrait != null;
            continueLabel.text = VietText.Fix(lineIndex + 1 >= activeLines.Count ? "ĐÓNG" : "TIẾP");
        }

        void Advance()
        {
            errorText.text = string.Empty;
            if (lineIndex + 1 < activeLines.Count)
            {
                lineIndex++;
                RenderLine();
                return;
            }
            FinishNode();
        }

        void FinishNode()
        {
            errorText.text = string.Empty;
            if (persistSeen == null || !persistSeen(activeSeenKey))
            {
                errorText.text = VietText.Fix("Không lưu được mốc hội thoại. Chạm ĐÓNG để thử lại.");
                return;
            }
            overlay.gameObject.SetActive(false);
            Action callback = closed;
            closed = null;
            callback?.Invoke();
        }

        void ShowNext()
        {
            if (pending.Count == 0) return;
            DialogueRequest request = pending.Dequeue();
            Show(request.NodeId, request.SeenKey, ShowNext);
        }

        static TMP_Text MakeText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize,
            Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.enableWordWrapping = true;
            UiKit.StyleLabel(text, fontSize, color);
            return text;
        }

        static Image MakeImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.preserveAspect = true;
            return image;
        }

        Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = MinigameUiTheme.Accent;
            Button button = go.GetComponent<Button>();
            TMP_Text text = MakeText(go.transform, "Label", Vector2.zero, size, 20,
                Color.white, TextAlignmentOptions.Center);
            if (label == "CONTINUE") continueLabel = text;
            else text.text = VietText.Fix(label);
            return button;
        }
    }
}
