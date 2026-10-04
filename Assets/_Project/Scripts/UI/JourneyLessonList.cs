using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KMA.Gameplay;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class JourneyLessonList : MonoBehaviour
    {
        static readonly SubjectId[] CourseOrder = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football };
        static readonly string[] CourseTitles = { "Chạy nước rút", "Bóng chuyền", "Bóng đá" };
        readonly List<LessonCard> lessonCards = new List<LessonCard>(3);
        TMP_Text heading;
        Button continueButton;
        GameSession session;
        Action<string, ChallengeAttemptMode> onSelected;
        SubjectId selectedSubject;
        bool hasBound;
        string lastCheckpointId;
        Coroutine revealRoutine;
        int currentCardIndex = -1;

        readonly List<string> lessonIds = new List<string>(3);
        public IReadOnlyList<string> LessonIds => lessonIds;
        public string CurrentChallengeId { get; private set; }

        public static JourneyLessonList Create(Transform parent)
        {
            var panelObject = new GameObject("JourneyLessons", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(.02f, .015f);
            panel.anchorMax = new Vector2(.98f, .34f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = MinigameUiTheme.WithAlpha(UITheme.Shared.Surface, .96f);
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = UITheme.Shared.Border;
            outline.effectDistance = new Vector2(2f, -2f);

            HorizontalLayoutGroup layout = panelObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 44, 42);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            JourneyLessonList list = panelObject.AddComponent<JourneyLessonList>();
            list.heading = CreateLabel(panel, "CourseTitle", 23, UITheme.Shared.TextPrimary,
                FontStyles.Bold, 26f);
            LayoutElement headingLayout = list.heading.GetComponent<LayoutElement>();
            headingLayout.ignoreLayout = true;
            list.heading.rectTransform.anchorMin = new Vector2(.02f, .77f);
            list.heading.rectTransform.anchorMax = new Vector2(.98f, 1f);
            list.heading.rectTransform.offsetMin = list.heading.rectTransform.offsetMax = Vector2.zero;
            list.CreateCards(panel);
            list.continueButton = CreateActionButton(panel, "ContinueCheckpoint", "TIẾP TỤC BÀI ĐANG HỌC");
            LayoutElement continueLayout = list.continueButton.GetComponent<LayoutElement>();
            continueLayout.preferredHeight = 28f;
            continueLayout.ignoreLayout = true;
            RectTransform continueRect = (RectTransform)list.continueButton.transform;
            continueRect.anchorMin = new Vector2(.02f, .02f);
            continueRect.anchorMax = new Vector2(.98f, .19f);
            continueRect.offsetMin = continueRect.offsetMax = Vector2.zero;
            list.continueButton.onClick.AddListener(list.ContinueCheckpoint);
            return list;
        }

        public void Bind(GameSession session, Action<string, ChallengeAttemptMode> onSelected)
        {
            CacheChildReferences();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.onSelected = onSelected;
            string checkpoint = session.Journey.CheckpointChallengeId;
            bool reveal = !hasBound || checkpoint != lastCheckpointId;
            if (reveal && !string.IsNullOrEmpty(checkpoint))
                selectedSubject = session.Journey.Catalog.Get(checkpoint).Subject;
            else if (!session.Journey.IsSubjectUnlocked(selectedSubject) && !string.IsNullOrEmpty(checkpoint))
                selectedSubject = session.Journey.Catalog.Get(checkpoint).Subject;
            hasBound = true;
            lastCheckpointId = checkpoint;
            Refresh(reveal);
        }

        public void ShowSubject(SubjectId subject)
        {
            if (session == null || !session.Journey.IsSubjectUnlocked(subject) || selectedSubject == subject)
                return;
            selectedSubject = subject;
            Refresh(true);
        }

        public void Refresh() => Refresh(false);

        void Refresh(bool reveal)
        {
            CacheChildReferences();
            if (session == null) return;
            int subjectIndex = Array.IndexOf(CourseOrder, selectedSubject);
            if (subjectIndex < 0) subjectIndex = 0;
            if (heading != null) heading.text = VietText.Fix($"{CourseTitles[subjectIndex]} · BA BÀI HỌC");

            ChallengeDefinition[] challenges = session.Journey.Catalog.Ordered
                .Where(challenge => challenge.Subject == selectedSubject).ToArray();
            lessonIds.Clear();
            currentCardIndex = -1;
            for (int index = 0; index < lessonCards.Count; index++)
            {
                LessonCard card = lessonCards[index];
                if (index >= challenges.Length)
                {
                    card.Button.gameObject.SetActive(false);
                    continue;
                }

                ChallengeDefinition challenge = challenges[index];
                card.Button.gameObject.SetActive(true);
                lessonIds.Add(challenge.Id);
                bool complete = session.Journey.IsChallengeComplete(challenge.Id);
                bool checkpoint = challenge.Id == session.Journey.CheckpointChallengeId;
                bool unlocked = complete || checkpoint || session.Journey.CourseComplete;
                string objective = challenge.Objective;
                string stage = challenge.Kind switch
                {
                    ChallengeKind.Learn => "HỌC",
                    ChallengeKind.Practice => "LUYỆN",
                    _ => "THI"
                };
                card.Title.text = VietText.Fix($"{index + 1:00}  ·  {stage}");
                card.Objective.text = VietText.Fix(objective);
                card.Button.interactable = unlocked;
                ApplyState(card, complete, checkpoint, unlocked);

                if (checkpoint) currentCardIndex = index;
                ChallengeAttemptMode mode = session.Journey.CourseComplete
                    ? ChallengeAttemptMode.FreePlay
                    : checkpoint && session.Journey.AwaitingSupplementary
                    ? ChallengeAttemptMode.Supplementary
                    : complete ? ChallengeAttemptMode.Review
                        : checkpoint ? ChallengeAttemptMode.Journey : ChallengeAttemptMode.Review;
                card.Button.onClick.RemoveAllListeners();
                string id = challenge.Id;
                card.Button.onClick.AddListener(() => onSelected?.Invoke(id, mode));
            }

            string current = session.Journey.CheckpointChallengeId;
            CurrentChallengeId = !string.IsNullOrEmpty(current) &&
                session.Journey.Catalog.Get(current).Subject == selectedSubject ? current : null;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(CurrentChallengeId != null);
                continueButton.interactable = CurrentChallengeId != null;
            }

            if (reveal) PlayReveal();
        }

        void CreateCards(Transform panel)
        {
            for (int index = 0; index < 3; index++)
                lessonCards.Add(CreateLessonCard(panel, index));
        }

        static LessonCard CreateLessonCard(Transform parent, int index)
        {
            var root = new GameObject($"Lesson{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            root.AddComponent<LayoutElement>().preferredHeight = 82f;
            Image background = root.GetComponent<Image>();
            background.color = UITheme.Shared.MapLockedCard;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = UITheme.Shared.MapLockedBorder;
            outline.effectDistance = new Vector2(2f, -2f);
            CanvasGroup group = root.AddComponent<CanvasGroup>();

            var accentObject = new GameObject("StageIcon", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(root.transform, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = accentRect.anchorMax = new Vector2(0f, .76f);
            accentRect.pivot = new Vector2(0f, .5f);
            accentRect.anchoredPosition = new Vector2(12f, 0f);
            accentRect.sizeDelta = new Vector2(32f, 32f);
            Image iconBackground = accentObject.GetComponent<Image>();
            iconBackground.sprite = UiKitAssets.Load().Circle;
            iconBackground.color = UITheme.Shared.Accent;
            iconBackground.raycastTarget = false;

            var markObject = new GameObject("StageMark", typeof(RectTransform), typeof(TextMeshProUGUI));
            markObject.transform.SetParent(accentObject.transform, false);
            TMP_Text mark = markObject.GetComponent<TMP_Text>();
            mark.text = new[] { "H", "L", "T" }[index];
            mark.fontSize = 16f;
            mark.fontStyle = FontStyles.Bold;
            mark.alignment = TextAlignmentOptions.Center;
            mark.color = UITheme.Shared.Surface;
            mark.raycastTarget = false;
            mark.rectTransform.anchorMin = Vector2.zero;
            mark.rectTransform.anchorMax = Vector2.one;
            mark.rectTransform.offsetMin = mark.rectTransform.offsetMax = Vector2.zero;
            VietTypography.Apply(mark);

            TMP_Text title = CreateCardLabel(root.transform, "StageTitle", 19f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.rectTransform.anchorMin = new Vector2(0f, .52f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(54f, 0f);
            title.rectTransform.offsetMax = new Vector2(-8f, -6f);

            TMP_Text objective = CreateCardLabel(root.transform, "Objective", 16f, FontStyles.Normal);
            objective.alignment = TextAlignmentOptions.TopLeft;
            objective.enableWordWrapping = true;
            objective.overflowMode = TextOverflowModes.Ellipsis;
            objective.rectTransform.anchorMin = new Vector2(0f, .20f);
            objective.rectTransform.anchorMax = new Vector2(1f, .58f);
            objective.rectTransform.offsetMin = new Vector2(12f, 2f);
            objective.rectTransform.offsetMax = new Vector2(-8f, 0f);

            TMP_Text status = CreateCardLabel(root.transform, "Status", 16f, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.MidlineLeft;
            status.rectTransform.anchorMin = Vector2.zero;
            status.rectTransform.anchorMax = new Vector2(1f, .24f);
            status.rectTransform.offsetMin = new Vector2(12f, 4f);
            status.rectTransform.offsetMax = new Vector2(-8f, 0f);

            return new LessonCard(button, background, outline, group, iconBackground,
                title, objective, status);
        }

        static TMP_Text CreateCardLabel(Transform parent, string name, float fontSize, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = UITheme.Shared.TextPrimary;
            text.raycastTarget = false;
            VietTypography.Apply(text);
            return text;
        }

        static void ApplyState(LessonCard card, bool complete, bool checkpoint, bool unlocked)
        {
            Color background;
            Color ink;
            Color accent;
            string status;
            if (checkpoint)
            {
                background = UITheme.Shared.TextPrimary;
                ink = UITheme.Shared.Surface;
                accent = UITheme.Shared.Accent;
                status = "ĐANG HỌC  ›";
            }
            else if (complete)
            {
                background = new Color32(225, 244, 222, 255);
                ink = UITheme.Shared.Surface;
                accent = UITheme.Shared.Success;
                status = "HOÀN THÀNH";
            }
            else if (unlocked)
            {
                background = UITheme.Shared.Card;
                ink = UITheme.Shared.Surface;
                accent = UITheme.Shared.Accent;
                status = "SẴN SÀNG";
            }
            else
            {
                background = UITheme.Shared.MapLockedCard;
                ink = UITheme.Shared.MapLockedText;
                accent = UITheme.Shared.MapLockedIcon;
                status = "CHƯA MỞ";
            }

            card.Background.color = background;
            card.Outline.effectColor = checkpoint ? UITheme.Shared.Accent
                : complete ? UITheme.Shared.Success : UITheme.Shared.MapLockedBorder;
            card.StageAccent.color = accent;
            card.Title.color = ink;
            card.Objective.color = ink;
            card.Status.color = checkpoint ? UITheme.Shared.MapActionText
                : complete ? UITheme.Shared.MapReadyText : ink;
            card.Status.text = VietText.Fix(status);
        }

        void PlayReveal()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            if (!Application.isPlaying)
            {
                foreach (LessonCard card in lessonCards)
                {
                    card.Group.alpha = 1f;
                    card.Group.interactable = true;
                    card.Group.blocksRaycasts = true;
                    card.Button.transform.localScale = Vector3.one;
                }
                revealRoutine = null;
                return;
            }
            revealRoutine = StartCoroutine(RevealCards());
        }

        IEnumerator RevealCards()
        {
            for (int i = 0; i < lessonCards.Count; i++)
            {
                LessonCard card = lessonCards[i];
                if (!card.Button.gameObject.activeSelf) continue;
                card.Group.alpha = 0f;
                card.Group.interactable = false;
                card.Group.blocksRaycasts = false;
                card.Button.transform.localScale = Vector3.one * .88f;
                yield return new WaitForSecondsRealtime(i * .045f);
                float elapsed = 0f;
                const float duration = .18f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    card.Group.alpha = eased;
                    card.Button.transform.localScale = Vector3.one * Mathf.Lerp(.88f, 1f, eased);
                    yield return null;
                }
                card.Group.alpha = 1f;
                card.Group.interactable = true;
                card.Group.blocksRaycasts = true;
                card.Button.transform.localScale = Vector3.one;
            }
            revealRoutine = null;
        }

        void Update()
        {
            if (revealRoutine != null || currentCardIndex < 0 || currentCardIndex >= lessonCards.Count)
                return;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * .012f;
            lessonCards[currentCardIndex].Button.transform.localScale = Vector3.one * pulse;
        }

        void ContinueCheckpoint()
        {
            if (session == null || string.IsNullOrEmpty(CurrentChallengeId)) return;
            string id = CurrentChallengeId;
            ChallengeAttemptMode mode = session.Journey.AwaitingSupplementary
                ? ChallengeAttemptMode.Supplementary : ChallengeAttemptMode.Journey;
            onSelected?.Invoke(id, mode);
        }

        void CacheChildReferences()
        {
            if (heading == null) heading = transform.Find("CourseTitle")?.GetComponent<TMP_Text>();
            if (continueButton == null)
            {
                continueButton = transform.Find("ContinueCheckpoint")?.GetComponent<Button>();
                if (continueButton != null)
                {
                    continueButton.onClick.RemoveListener(ContinueCheckpoint);
                    continueButton.onClick.AddListener(ContinueCheckpoint);
                }
            }
            if (lessonCards.Count == 0)
            {
                for (int index = 0; index < 3; index++)
                {
                    Transform item = transform.Find($"Lesson{index + 1}");
                    if (item == null) continue;
                    lessonCards.Add(new LessonCard(item.GetComponent<Button>(), item.GetComponent<Image>(),
                        item.GetComponent<Outline>(), item.GetComponent<CanvasGroup>(),
                        item.Find("StageIcon")?.GetComponent<Image>(), item.Find("StageTitle")?.GetComponent<TMP_Text>(),
                        item.Find("Objective")?.GetComponent<TMP_Text>(), item.Find("Status")?.GetComponent<TMP_Text>()));
                }
            }
        }

        static TMP_Text CreateLabel(Transform parent, string name, float fontSize, Color color,
            FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.enableWordWrapping = false;
            VietTypography.Apply(text);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        static Button CreateActionButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = UITheme.Shared.Accent;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = ColorBlock.defaultColorBlock;
            TMP_Text text = CreateLabel(go.transform, "Label", 17f, Color.white, FontStyles.Bold, 24f);
            text.text = VietText.Fix(label);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            go.AddComponent<LayoutElement>().preferredHeight = 40f;
            return button;
        }

        sealed class LessonCard
        {
            public readonly Button Button;
            public readonly Image Background;
            public readonly Outline Outline;
            public readonly CanvasGroup Group;
            public readonly Image StageAccent;
            public readonly TMP_Text Title;
            public readonly TMP_Text Objective;
            public readonly TMP_Text Status;

            public LessonCard(Button button, Image background, Outline outline, CanvasGroup group,
                Image stageAccent, TMP_Text title, TMP_Text objective, TMP_Text status)
            {
                Button = button;
                Background = background;
                Outline = outline;
                Group = group;
                StageAccent = stageAccent;
                Title = title;
                Objective = objective;
                Status = status;
            }
        }
    }
}
