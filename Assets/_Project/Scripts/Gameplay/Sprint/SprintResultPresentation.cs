using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class SprintResultPresentation : MonoBehaviour
    {
        const string SuccessTitle = "HOÀN THÀNH!";
        const string FailureTitle = "THẤT BẠI";
        static readonly Color32 FailColor = new Color32(255, 89, 94, 255);
        static readonly Color32 SuccessColor = new Color32(94, 222, 140, 255);

        const float ScrimDuration = .12f;
        const float ModalDuration = .18f;
        const float TitleDuration = .14f;
        const float ScoreDuration = .35f;
        const float RankDuration = .16f;

        KMA.Gameplay.UI.ResultPanel panel;
        SprintController controller;
        CanvasGroup scrimGroup;
        CanvasGroup modalGroup;
        RectTransform modalRect;
        TMP_Text titleLabel;
        TMP_Text scoreLabel;
        RectTransform rankRect;
        TMP_Text rankLabel;
        MinigameResult lastSeenResult;
        string finalScoreText = string.Empty;

        public void Bind(KMA.Gameplay.UI.ResultPanel resultPanel, SprintController sprintController)
        {
            panel = resultPanel;
            controller = sprintController;
            lastSeenResult = null;
            CacheVisuals();
        }

        void Update()
        {
            if (panel == null)
                return;

            if (panel.gameObject.activeInHierarchy && panel.CurrentResult != null &&
                !ReferenceEquals(panel.CurrentResult, lastSeenResult))
                ShowForTest(panel.CurrentResult);
        }

        public void ShowForTest(MinigameResult result)
        {
            if (result == null)
                return;

            lastSeenResult = result;
            CacheVisuals();

            if (titleLabel != null)
            {
                titleLabel.text = result.Pass ? SuccessTitle : FailureTitle;
                titleLabel.color = result.Pass ? (Color)SuccessColor : (Color)FailColor;
            }
            if (rankLabel != null)
                rankLabel.text = result.Rank.ToString();
            finalScoreText = scoreLabel == null ? string.Empty : scoreLabel.text;

            StopAllCoroutines();
            StartCoroutine(AnimateReveal(result.Score));
        }

        void CacheVisuals()
        {
            if (panel == null)
                return;

            Transform root = panel.transform;
            Transform backdrop = root.Find("Backdrop");
            Transform content = root.Find("Content");
            if (backdrop != null)
                scrimGroup = backdrop.GetComponent<CanvasGroup>() ?? backdrop.gameObject.AddComponent<CanvasGroup>();
            if (content == null)
                return;

            modalRect = content.GetComponent<RectTransform>();
            modalGroup = content.GetComponent<CanvasGroup>() ?? content.gameObject.AddComponent<CanvasGroup>();
            titleLabel = content.Find("StatusLabel")?.GetComponent<TMP_Text>();
            scoreLabel = content.Find("ScoreLabel")?.GetComponent<TMP_Text>();
            Transform rank = content.Find("RankLabel");
            rankLabel = rank?.GetComponent<TMP_Text>();
            rankRect = rank?.GetComponent<RectTransform>();
        }

        IEnumerator AnimateReveal(float finalScore)
        {
            yield return FadeGroup(scrimGroup, ScrimDuration);
            yield return ScaleAndFadeModal();
            yield return FadeText(titleLabel, TitleDuration);
            yield return CountUpScore(finalScore);
            yield return PopRank();
        }

        IEnumerator FadeGroup(CanvasGroup group, float duration)
        {
            if (group == null)
                yield break;

            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            group.alpha = 1f;
        }

        IEnumerator ScaleAndFadeModal()
        {
            if (modalGroup == null || modalRect == null)
                yield break;

            float elapsed = 0f;
            modalGroup.alpha = 0f;
            while (elapsed < ModalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ModalDuration);
                modalGroup.alpha = t;
                modalRect.localScale = Vector3.one * Mathf.Lerp(.9f, 1f, t);
                yield return null;
            }
            modalGroup.alpha = 1f;
            modalRect.localScale = Vector3.one;
        }

        IEnumerator FadeText(TMP_Text label, float duration)
        {
            if (label == null)
                yield break;

            Color target = label.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                label.color = new Color(target.r, target.g, target.b, alpha);
                yield return null;
            }
            label.color = target;
        }

        IEnumerator CountUpScore(float finalScore)
        {
            if (scoreLabel == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < ScoreDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ScoreDuration);
                scoreLabel.text = Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, t)).ToString();
                yield return null;
            }
            scoreLabel.text = finalScoreText;
        }

        IEnumerator PopRank()
        {
            if (rankRect == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < RankDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / RankDuration);
                float scale = t < .5f ? Mathf.Lerp(.6f, 1.2f, t / .5f) : Mathf.Lerp(1.2f, 1f, (t - .5f) / .5f);
                rankRect.localScale = Vector3.one * scale;
                yield return null;
            }
            rankRect.localScale = Vector3.one;
        }
    }
}
