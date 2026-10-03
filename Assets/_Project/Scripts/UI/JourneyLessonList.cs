using System;
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
        readonly List<Button> lessonButtons = new List<Button>();
        readonly List<TMP_Text> lessonLabels = new List<TMP_Text>();
        readonly List<string> lessonIds = new List<string>();
        TMP_Text heading;
        Button continueButton;
        GameSession session;
        Action<string, ChallengeAttemptMode> onSelected;
        SubjectId selectedSubject;
        bool hasBound;
        string lastCheckpointId;

        public IReadOnlyList<string> LessonIds => lessonIds;
        public string CurrentChallengeId { get; private set; }

        public static JourneyLessonList Create(Transform parent)
        {
            var panelObject = new GameObject("JourneyLessons", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(.02f, .015f);
            panel.anchorMax = new Vector2(.98f, .30f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = MinigameUiTheme.WithAlpha(UITheme.Shared.Surface, .96f);
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = UITheme.Shared.Border;
            outline.effectDistance = new Vector2(2f, -2f);

            var layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 5, 5);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            JourneyLessonList list = panelObject.AddComponent<JourneyLessonList>();
            list.heading = CreateLabel(panel, "CourseTitle", 23, UITheme.Shared.TextPrimary,
                FontStyles.Bold, 28f);
            foreach (int i in Enumerable.Range(0, 3))
            {
                Button button = CreateLessonButton(panel, i);
                list.lessonButtons.Add(button);
                list.lessonLabels.Add(button.GetComponentInChildren<TMP_Text>(true));
            }
            list.continueButton = CreateActionButton(panel, "ContinueCheckpoint", "TIẾP TỤC BÀI ĐANG HỌC");
            list.continueButton.onClick.AddListener(list.ContinueCheckpoint);
            return list;
        }

        public void Bind(GameSession session, Action<string, ChallengeAttemptMode> onSelected)
        {
            CacheChildReferences();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.onSelected = onSelected;
            string checkpoint = session.Journey.CheckpointChallengeId;
            if ((!hasBound || checkpoint != lastCheckpointId) && !string.IsNullOrEmpty(checkpoint))
                selectedSubject = session.Journey.Catalog.Get(checkpoint).Subject;
            else if (!session.Journey.IsSubjectUnlocked(selectedSubject) && !string.IsNullOrEmpty(checkpoint))
                selectedSubject = session.Journey.Catalog.Get(checkpoint).Subject;
            hasBound = true;
            lastCheckpointId = checkpoint;
            Refresh();
        }

        public void ShowSubject(SubjectId subject)
        {
            if (session == null || !session.Journey.IsSubjectUnlocked(subject))
                return;
            selectedSubject = subject;
            Refresh();
        }

        public void Refresh()
        {
            CacheChildReferences();
            if (session == null) return;
            int subjectIndex = Array.IndexOf(CourseOrder, selectedSubject);
            if (subjectIndex < 0) subjectIndex = 0;
            if (heading != null) heading.text = VietText.Fix($"{CourseTitles[subjectIndex]} · BA BÀI HỌC");
            ChallengeDefinition[] challenges = session.Journey.Catalog.Ordered
                .Where(challenge => challenge.Subject == selectedSubject).ToArray();
            lessonIds.Clear();
            for (int index = 0; index < lessonButtons.Count; index++)
            {
                Button button = lessonButtons[index];
                if (index >= challenges.Length)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }
                ChallengeDefinition challenge = challenges[index];
                lessonIds.Add(challenge.Id);
                bool complete = session.Journey.IsChallengeComplete(challenge.Id);
                bool checkpoint = challenge.Id == session.Journey.CheckpointChallengeId;
                bool unlocked = complete || checkpoint || session.Journey.CourseComplete;
                string label = challenge.Kind switch
                {
                    ChallengeKind.Learn => "HỌC",
                    ChallengeKind.Practice => "LUYỆN",
                    _ => "THI"
                };
                lessonLabels[index].text = VietText.Fix($"{label}  ·  {challenge.Objective}");
                button.interactable = unlocked;
                ChallengeAttemptMode mode = session.Journey.CourseComplete
                    ? ChallengeAttemptMode.FreePlay
                    : checkpoint && session.Journey.AwaitingSupplementary
                    ? ChallengeAttemptMode.Supplementary
                    : complete ? ChallengeAttemptMode.Review
                        : checkpoint ? ChallengeAttemptMode.Journey : ChallengeAttemptMode.Review;
                button.onClick.RemoveAllListeners();
                string id = challenge.Id;
                button.onClick.AddListener(() => onSelected?.Invoke(id, mode));
            }
            string current = session.Journey.CheckpointChallengeId;
            CurrentChallengeId = !string.IsNullOrEmpty(current) &&
                session.Journey.Catalog.Get(current).Subject == selectedSubject ? current : null;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(!string.IsNullOrEmpty(current));
                continueButton.interactable = !string.IsNullOrEmpty(current);
            }
        }

        void ContinueCheckpoint()
        {
            if (session == null || string.IsNullOrEmpty(session.Journey.CheckpointChallengeId)) return;
            string id = session.Journey.CheckpointChallengeId;
            ChallengeDefinition definition = session.Journey.Catalog.Get(id);
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
            for (int index = 0; index < 3; index++)
            {
                if (lessonButtons.Count > index && lessonLabels.Count > index) continue;
                Button button = transform.Find($"Lesson{index + 1}")?.GetComponent<Button>();
                if (button == null) continue;
                lessonButtons.Add(button);
                lessonLabels.Add(button.GetComponentInChildren<TMP_Text>(true));
            }
        }

        static TMP_Text CreateLabel(Transform parent, string name, float fontSize, Color color,
            FontStyles style, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
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

        static Button CreateLessonButton(Transform parent, int index)
        {
            Button button = CreateActionButton(parent, $"Lesson{index + 1}", "");
            button.GetComponent<LayoutElement>().preferredHeight = 34f;
            button.GetComponent<Image>().color = UITheme.Shared.Card;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 16f;
            text.color = UITheme.Shared.TextPrimary;
            return button;
        }

        static Button CreateActionButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UITheme.Shared.Accent;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = ColorBlock.defaultColorBlock;
            TMP_Text text = CreateLabel(go.transform, "Label", 17f, Color.white, FontStyles.Bold, 24f);
            text.text = VietText.Fix(label);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 40f;
            return button;
        }
    }
}
