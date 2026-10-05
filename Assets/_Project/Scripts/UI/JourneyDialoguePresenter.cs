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
        Image backgroundImage;
        Image portrait;
        TMP_Text speaker, body, errorText, continueLabel;
        Button continueButton, skipButton;
        IReadOnlyList<JourneyDialogueLine> activeLines;
        string activeSeenKey;
        int lineIndex;
        Action closed;

        public bool IsShowing => overlay != null && overlay.gameObject.activeSelf;
        public int CurrentLineIndex => lineIndex;

        public void Configure(JourneyDialogueLibrary dialogueLibrary, Func<string, bool> saveSeen,
            Sprite sharedBackground = null)
        {
            library = dialogueLibrary;
            persistSeen = saveSeen;
            EnsureView();
            if (backgroundImage != null && sharedBackground != null)
                backgroundImage.sprite = sharedBackground;
        }

        public void Show(string nodeId, Action onClosed)
        {
            Show(nodeId, nodeId, onClosed);
        }

        public void ShowJourney(GameSession session, GameManager manager, Sprite sharedBackground = null)
        {
            if (session == null || manager == null) return;
            Configure(JourneyDialogueLibrary.LoadDefault(), key => manager.TryMarkJourneyDialogueSeen(key, out _),
                sharedBackground);
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
            veil.color = new Color(0f, 0f, 0f, 0f);
            veil.raycastTarget = true;

            var backgroundObject = new GameObject("SharedMenuBackground", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            backgroundObject.transform.SetParent(overlay, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(.5f, .5f);
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            AspectRatioFitter backgroundFit = backgroundObject.GetComponent<AspectRatioFitter>();
            backgroundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFit.aspectRatio = 1928f / 816f;

            RectTransform dimmerRect = new GameObject("BackgroundDimmer", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            dimmerRect.SetParent(overlay, false);
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = dimmerRect.offsetMax = Vector2.zero;
            Image dimmer = dimmerRect.GetComponent<Image>();
            dimmer.color = new Color(HomeMenuStyle.Navy.r, HomeMenuStyle.Navy.g, HomeMenuStyle.Navy.b, .36f);
            dimmer.raycastTarget = false;

            var card = new GameObject("DialogueCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(overlay, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, 0f);
            cardRect.pivot = new Vector2(.5f, 0f);
            cardRect.anchoredPosition = new Vector2(0f, 54f);
            cardRect.sizeDelta = new Vector2(1500f, 310f);
            card.GetComponent<Image>().color = HomeMenuStyle.Glass;
            Outline cardOutline = card.GetComponent<Outline>();
            cardOutline.effectColor = HomeMenuStyle.Gold;
            cardOutline.effectDistance = new Vector2(5f, -5f);

            portrait = MakeImage(card.transform, "Portrait", new Vector2(-608f, 164f), new Vector2(230f, 330f));
            portrait.color = Color.white;
            Outline portraitOutline = portrait.gameObject.AddComponent<Outline>();
            portraitOutline.effectColor = HomeMenuStyle.Gold;
            portraitOutline.effectDistance = new Vector2(4f, -4f);
            speaker = MakeText(card.transform, "Speaker", new Vector2(-210f, 111f), new Vector2(500f, 48f), 30,
                HomeMenuStyle.Gold, TextAlignmentOptions.Left);
            body = MakeText(card.transform, "Dialogue", new Vector2(105f, 28f), new Vector2(880f, 112f), 31,
                HomeMenuStyle.White, TextAlignmentOptions.TopLeft);
            errorText = MakeText(card.transform, "SaveError", new Vector2(220f, -63f), new Vector2(650f, 38f), 18,
                HomeMenuStyle.GoldLight, TextAlignmentOptions.Left);
            continueButton = MakeButton(card.transform, "TIẾP", new Vector2(630f, -108f), new Vector2(220f, 66f));
            skipButton = MakeButton(card.transform, "BỎ QUA", new Vector2(385f, -108f), new Vector2(220f, 66f));
            continueButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(FinishNode);
            overlay.gameObject.SetActive(false);
        }

        void RenderLine()
        {
            JourneyDialogueLine line = activeLines[lineIndex];
            JourneyCharacter character = library.GetCharacter(line.CharacterId);
            speaker.text = VietText.Fix(character.DisplayName);
            body.text = VietText.Fix(line.Text);
            portrait.sprite = character.GetPose(line.Pose);
            portrait.enabled = portrait.sprite != null;
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
            VietTypography.Apply(text);
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
            bool primary = label == "TIẾP";
            go.GetComponent<Image>().color = primary ? HomeMenuStyle.Gold : HomeMenuStyle.Navy;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = primary ? HomeMenuStyle.White : HomeMenuStyle.Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            Button button = go.GetComponent<Button>();
            TMP_Text text = MakeText(go.transform, "Label", Vector2.zero, size, 23,
                primary ? HomeMenuStyle.Navy : HomeMenuStyle.White, TextAlignmentOptions.Center);
            if (label == "TIẾP") continueLabel = text;
            else text.text = VietText.Fix(label);
            return button;
        }
    }
}
