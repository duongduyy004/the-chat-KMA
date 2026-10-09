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
        static readonly SubjectId[] CourseOrder =
            { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess };
        static readonly string[] CourseTitles = { "Chạy nước rút", "Bóng chuyền", "Bóng đá", "Bài kiểm tra cuối" };
        readonly List<LessonCard> lessonCards = new List<LessonCard>(3);
        TMP_Text heading;
        TMP_Text progress;
        TMP_Text hint;
        Button continueButton;
        Button closeButton;
        Button scrim;
        GameSession session;
        Action<string, ChallengeAttemptMode> onSelected;
        SubjectId selectedSubject;
        bool hasBound;
        string lastCheckpointId;
        Coroutine revealRoutine;
        int currentCardIndex = -1;
        bool currentOutOfLives;

        readonly List<string> lessonIds = new List<string>(3);
        public IReadOnlyList<string> LessonIds => lessonIds;
        public string CurrentChallengeId { get; private set; }
        public SubjectId SelectedSubject => selectedSubject;
        public bool IsOpen => gameObject.activeSelf;

        public static JourneyLessonList Create(Transform parent)
        {
            RectTransform panel = JourneyLessonPresentation.Create(parent);
            JourneyLessonList list = panel.gameObject.AddComponent<JourneyLessonList>();
            list.CacheChildReferences();
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

        /// Pops the lesson menu up over the map for an unlocked subject; locked stops stay closed.
        public void Open(SubjectId subject)
        {
            if (session == null || !session.Journey.IsSubjectUnlocked(subject))
                return;
            selectedSubject = subject;
            SetOpen(true);
            Refresh(true);
        }

        public void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            CacheChildReferences();
            if (!open && revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
            if (scrim != null) scrim.gameObject.SetActive(open);
            gameObject.SetActive(open);
        }

        public void Refresh() => Refresh(false);

        void Refresh(bool reveal)
        {
            CacheChildReferences();
            if (session == null) return;
            int subjectIndex = Array.IndexOf(CourseOrder, selectedSubject);
            if (subjectIndex < 0) subjectIndex = 0;
            if (heading != null)
                heading.text = VietText.Fix(CourseTitles[subjectIndex]);
            Color chapterColor = selectedSubject switch
            {
                SubjectId.Volleyball => UITheme.Shared.LessonJourney.volleyball,
                SubjectId.Football => UITheme.Shared.LessonJourney.football,
                SubjectId.Chess => UITheme.Shared.LessonJourney.chess,
                _ => UITheme.Shared.LessonJourney.sprint
            };
            transform.Find("ChapterAccent").GetComponent<Image>().color = chapterColor;
            transform.Find("CourseIcon").GetComponent<Image>().color = chapterColor;
            Transform courseIcon = transform.Find("CourseIcon");
            // Panels baked before the final exam have no chess glyph yet.
            if (courseIcon.childCount <= subjectIndex)
            {
                var glyph = new GameObject(selectedSubject + "Glyph", typeof(RectTransform), typeof(Image));
                glyph.transform.SetParent(courseIcon, false);
                var glyphRect = (RectTransform)glyph.transform;
                glyphRect.anchorMin = Vector2.one * .2f;
                glyphRect.anchorMax = Vector2.one * .8f;
                glyphRect.offsetMin = glyphRect.offsetMax = Vector2.zero;
                var image = glyph.GetComponent<Image>();
                image.sprite = MapPresentationBuilder.SportIconSprite(selectedSubject);
                image.color = UITheme.Shared.Surface;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            for (int i = 0; i < courseIcon.childCount; i++)
                courseIcon.GetChild(i).gameObject.SetActive(i == subjectIndex);
            Transform patterns = transform.Find("CourtPattern");
            for (int i = 0; i < patterns.childCount; i++)
                patterns.GetChild(i).gameObject.SetActive(i == subjectIndex);

            ChallengeDefinition[] challenges = session.Journey.Catalog.Ordered
                .Where(challenge => challenge.Subject == selectedSubject).ToArray();
            lessonIds.Clear();
            currentCardIndex = -1;
            currentOutOfLives = false;
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
                    ChallengeKind.Final => "CUỐI",
                    _ => "THI"
                };
                card.Title.text = VietText.Fix(stage);
                bool outOfLives = checkpoint && !complete && !session.Journey.CourseComplete &&
                    JourneyProgress.IsPenalizedKind(challenge.Kind) && session.Lives == 0;
                card.Objective.text = VietText.Fix(outOfLives ? "Hết lượt thi" : objective);
                card.Button.interactable = unlocked && !outOfLives;
                ApplyState(card, index, challenge.Kind, complete, checkpoint, unlocked, chapterColor, outOfLives);

                if (checkpoint)
                {
                    currentCardIndex = index;
                    currentOutOfLives = outOfLives;
                }
                ChallengeAttemptMode mode = session.Journey.CourseComplete
                    ? ChallengeAttemptMode.FreePlay
                    : complete ? ChallengeAttemptMode.Review
                        : checkpoint ? ChallengeAttemptMode.Journey : ChallengeAttemptMode.Review;
                card.Button.onClick.RemoveAllListeners();
                string id = challenge.Id;
                card.Button.onClick.AddListener(() => onSelected?.Invoke(id, mode));
            }

            int completedCount = challenges.Count(challenge => session.Journey.IsChallengeComplete(challenge.Id));
            if (progress != null)
                progress.text = VietText.Fix($"{completedCount}/{challenges.Length} bài hoàn thành");
            for (int index = 0; index < 2; index++)
            {
                bool passed = index < challenges.Length && session.Journey.IsChallengeComplete(challenges[index].Id);
                Color color = passed ? chapterColor : UITheme.Shared.MapLockedBorder;
                transform.Find($"LessonConnector{index + 1}").GetComponent<Image>().color = color;
                transform.Find($"LessonArrow{index + 1}").GetComponent<Image>().color = color;
                bool used = index + 1 < challenges.Length;
                transform.Find($"LessonConnector{index + 1}").gameObject.SetActive(used);
                transform.Find($"LessonArrow{index + 1}").gameObject.SetActive(used);
            }
            if (hint != null)
                hint.text = VietText.Fix(session.Journey.CourseComplete
                    ? "Đã hoàn thành khóa học · Chạm một chặng để chơi lại"
                    : currentOutOfLives
                    ? "Hết lượt thi · Chờ hồi lượt để thi tiếp"
                    : "Hoàn thành từng chặng để mở bài tiếp theo");

            string current = session.Journey.CheckpointChallengeId;
            CurrentChallengeId = !string.IsNullOrEmpty(current) &&
                session.Journey.Catalog.Get(current).Subject == selectedSubject ? current : null;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(CurrentChallengeId != null);
                continueButton.interactable = CurrentChallengeId != null && !currentOutOfLives;
            }

            if (reveal) PlayReveal();
        }

        static void ApplyState(LessonCard card, int index, ChallengeKind kind, bool complete, bool checkpoint,
            bool unlocked, Color chapterColor, bool outOfLives)
        {
            UITheme theme = UITheme.Shared;
            card.Background.color = checkpoint ? theme.TextPrimary
                : complete ? theme.LessonJourney.completedSurface
                : unlocked ? theme.Card : theme.Muted;
            card.Outline.effectColor = checkpoint ? theme.Accent
                : complete ? theme.Success : theme.MapLockedBorder;
            card.StageAccent.color = unlocked ? chapterColor : theme.MapLockedCard;
            card.Title.color = card.Objective.color = theme.Surface;
            card.StateBadge.color = checkpoint ? theme.Accent
                : complete ? theme.Success : theme.MapLockedCard;
            card.StateLabel.text = VietText.Fix(checkpoint ? "TIẾP THEO"
                : complete ? "ĐÃ XONG" : unlocked ? "SẴN SÀNG" : "KHÓA");
            card.StateLabel.color = theme.Surface;
            card.StateBadge.transform.Find("CompletedMark").gameObject.SetActive(complete && !checkpoint);
            card.StateLabel.rectTransform.offsetMin = complete && !checkpoint
                ? new Vector2(16f, 0f) : Vector2.zero;
            card.ActionSurface.color = checkpoint ? theme.Accent
                : complete ? theme.Success : theme.MapLockedCard;
            card.Status.color = theme.Surface;
            card.Status.text = VietText.Fix(checkpoint
                ? outOfLives ? "CHỜ HỒI LƯỢT" : "BẮT ĐẦU  ›"
                : complete ? "ÔN LẠI  ›" : unlocked ? "CHƠI LẠI  ›"
                : kind == ChallengeKind.Final ? "Đạt Bóng đá để mở"
                : index == 1 ? "Hoàn thành HỌC để mở" : "Hoàn thành LUYỆN để mở");
            card.Glow.gameObject.SetActive(checkpoint);
            card.Glow.color = MinigameUiTheme.WithAlpha(theme.Accent, theme.LessonJourney.glowAlpha.x);
            card.StageAccent.transform.Find("Glyph").GetComponent<Image>().color = theme.Surface;
        }

        void PlayReveal()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            // A closed popup can't run coroutines; Open replays the reveal anyway.
            if (!Application.isPlaying || !isActiveAndEnabled)
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
                card.Button.transform.localScale = Vector3.one * UITheme.Shared.LessonJourney.revealScale;
                yield return new WaitForSecondsRealtime(UITheme.Shared.LessonJourney.revealStagger);
                float elapsed = 0f;
                float duration = UITheme.Shared.LessonJourney.revealDuration;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    card.Group.alpha = eased;
                    card.Button.transform.localScale = Vector3.one * Mathf.Lerp(UITheme.Shared.LessonJourney.revealScale, 1f, eased);
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
            UITheme.LessonJourneyStyle style = UITheme.Shared.LessonJourney;
            float pulse = (Mathf.Sin(Time.unscaledTime * style.glowSpeed) + 1f) * .5f;
            lessonCards[currentCardIndex].Glow.color = MinigameUiTheme.WithAlpha(UITheme.Shared.Accent,
                Mathf.Lerp(style.glowAlpha.x, style.glowAlpha.y, pulse));
        }

        void ContinueCheckpoint()
        {
            if (session == null || string.IsNullOrEmpty(CurrentChallengeId)) return;
            string id = CurrentChallengeId;
            onSelected?.Invoke(id, ChallengeAttemptMode.Journey);
        }

        void CacheChildReferences()
        {
            if (heading == null) heading = transform.Find("CourseTitle")?.GetComponent<TMP_Text>();
            if (progress == null) progress = transform.Find("CourseProgress")?.GetComponent<TMP_Text>();
            if (hint == null) hint = transform.Find("JourneyHint")?.GetComponent<TMP_Text>();
            if (continueButton == null)
            {
                continueButton = transform.Find("ContinueCheckpoint")?.GetComponent<Button>();
                if (continueButton != null)
                {
                    continueButton.onClick.RemoveListener(ContinueCheckpoint);
                    continueButton.onClick.AddListener(ContinueCheckpoint);
                }
            }
            if (closeButton == null)
            {
                closeButton = transform.Find("CloseButton")?.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveListener(Close);
                    closeButton.onClick.AddListener(Close);
                }
            }
            if (scrim == null && transform.parent != null)
            {
                scrim = transform.parent.Find("LessonScrim")?.GetComponent<Button>();
                if (scrim != null)
                {
                    scrim.onClick.RemoveListener(Close);
                    scrim.onClick.AddListener(Close);
                }
            }
            if (lessonCards.Count == 0)
            {
                for (int index = 0; index < 3; index++)
                {
                    Transform item = transform.Find($"Lesson{index + 1}");
                    if (item == null) continue;
                    lessonCards.Add(new LessonCard(item));
                }
            }
        }

        sealed class LessonCard
        {
            public readonly Button Button;
            public readonly Image Background;
            public readonly Outline Outline;
            public readonly CanvasGroup Group;
            public readonly Image StageAccent;
            public readonly Image Glow;
            public readonly Image StateBadge;
            public readonly TMP_Text StateLabel;
            public readonly Image ActionSurface;
            public readonly TMP_Text Title;
            public readonly TMP_Text Objective;
            public readonly TMP_Text Status;

            public LessonCard(Transform root)
            {
                Button = root.GetComponent<Button>();
                Background = root.GetComponent<Image>();
                Outline = root.GetComponent<Outline>();
                Group = root.GetComponent<CanvasGroup>();
                StageAccent = root.Find("StageIcon").GetComponent<Image>();
                Glow = root.Find("StageIcon/Glow").GetComponent<Image>();
                StateBadge = root.Find("StateBadge").GetComponent<Image>();
                StateLabel = root.Find("StateBadge/Label").GetComponent<TMP_Text>();
                ActionSurface = root.Find("ActionSurface").GetComponent<Image>();
                Title = root.Find("StageTitle").GetComponent<TMP_Text>();
                Objective = root.Find("Objective").GetComponent<TMP_Text>();
                Status = root.Find("Status").GetComponent<TMP_Text>();
            }
        }
    }
}
