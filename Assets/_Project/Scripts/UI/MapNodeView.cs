using KMA.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class MapNodeView : MonoBehaviour
    {
        [SerializeField] SubjectId subjectId;
        [SerializeField] string displayName;
        [SerializeField] bool comingSoon;
        [SerializeField] ScriptableObject subjectConfigAsset;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text detailLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] Button button;
        [SerializeField] Image cardImage;
        [SerializeField] Image iconPlate;
        [SerializeField] Outline cardOutline;
        GameObject detailVisibilityRoot;

        Color subjectColor = Color.white;

        static Color ReadyBorder => UITheme.Shared.Accent;
        static Color CompleteBorder => UITheme.Shared.MapCompleteBorder;
        static Color LockedBorder => UITheme.Shared.MapLockedBorder;
        static Color LockedCard => UITheme.Shared.MapLockedCard;
        static Color LockedIcon => UITheme.Shared.MapLockedIcon;

        public SubjectId SubjectId => subjectId;
        public string DisplayName => displayName;
        public bool IsComingSoon => comingSoon;
        public bool HasSubjectConfigAsset => subjectConfigAsset != null;
        public ScriptableObject SubjectConfigAsset => subjectConfigAsset;
        public string DetailText => detailLabel == null ? string.Empty : detailLabel.text;
        public bool IsInteractable => button != null && button.interactable;
        public int Stars { get; private set; }
        public Rank BestRank { get; private set; }
        public int Lives { get; private set; }

        bool completed;

        public void Configure(SubjectId id, string name, bool isComingSoon, SubjectRecord record, int lives)
        {
            ConfigureState(id, name, isComingSoon, record, lives, !isComingSoon);
        }

        void ConfigureState(SubjectId id, string name, bool isComingSoon, SubjectRecord record,
            int lives, bool unlocked)
        {
            subjectId = id;
            displayName = name;
            comingSoon = isComingSoon;
            BestRank = record == null ? Rank.F : record.BestRank;
            completed = record != null && record.Passed;
            Stars = completed ? ScoreUtil.ToStars(BestRank) : 0;
            Lives = lives;
            if (titleLabel != null)
                titleLabel.text = VietText.Fix(displayName);
            RenderAvailability(unlocked && !isComingSoon,
                isComingSoon ? "ĐANG PHÁT TRIỂN" : "CHƯA MỞ KHÓA");
        }

        public void Configure(ScriptableObject configAsset, SubjectRecord record, int lives)
        {
            if (configAsset == null)
                throw new System.ArgumentNullException(nameof(configAsset));
            subjectConfigAsset = configAsset;
            var type = configAsset.GetType();
            subjectId = (SubjectId)type.GetField("subjectId").GetValue(configAsset);
            displayName = (string)type.GetField("displayName").GetValue(configAsset);
            comingSoon = (bool)type.GetField("comingSoon").GetValue(configAsset);
            var unlocked = (bool)type.GetField("unlocked").GetValue(configAsset);
            ConfigureState(subjectId, displayName, comingSoon, record, lives, unlocked);
        }

        public void Bind(Button target, TMP_Text title, TMP_Text detail, GameObject detailRoot = null)
        {
            button = target;
            titleLabel = title;
            detailLabel = detail;
            detailVisibilityRoot = detailRoot != null ? detailRoot : detail == null ? null : detail.gameObject;
        }

        public void BindStatusLabel(TMP_Text status) => statusLabel = status;

        public void BindPresentation(TMP_Text status, TMP_Text action, Image background, Image sportIcon,
            Outline outline, Color accent)
        {
            statusLabel = status;
            actionLabel = action;
            cardImage = background;
            iconPlate = sportIcon;
            cardOutline = outline;
            subjectColor = accent;
        }

        public void SetAvailability(bool selectable, string unavailableLabel)
        {
            RenderAvailability(selectable && !comingSoon, unavailableLabel);
        }

        void RenderAvailability(bool selectable, string unavailableLabel)
        {
            if (button != null)
                button.interactable = selectable;
            BrutalButton feedback = GetComponent<BrutalButton>();
            if (feedback != null)
                feedback.enabled = selectable;
            if (detailLabel != null)
            {
                detailLabel.text = VietText.Fix(selectable
                    ? completed ? $"HẠNG {BestRank}  {Stars} SAO" : "SẴN SÀNG"
                    : unavailableLabel);
                if (detailVisibilityRoot != null)
                    detailVisibilityRoot.SetActive(selectable && completed);
            }
            if (statusLabel != null)
                statusLabel.text = VietText.Fix(selectable
                    ? completed ? "HOÀN THÀNH" : "SẴN SÀNG"
                    : unavailableLabel);
            if (actionLabel != null)
                actionLabel.text = VietText.Fix(selectable ? "THI" : string.Empty);
            ApplyVisualState(completed, !selectable);
        }

        void ApplyVisualState(bool completed, bool locked)
        {
            if (cardImage != null)
                cardImage.color = locked ? LockedCard : UITheme.Shared.Card;
            if (iconPlate != null)
                iconPlate.color = locked ? LockedIcon : subjectColor;
            if (cardOutline != null)
                cardOutline.effectColor = locked ? LockedBorder : completed ? CompleteBorder : ReadyBorder;
            if (statusLabel != null)
                statusLabel.color = locked
                    ? UITheme.Shared.MapLockedText
                    : completed ? CompleteBorder : UITheme.Shared.MapReadyText;
        }
    }
}
