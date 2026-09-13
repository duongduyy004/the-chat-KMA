using KMA.Gameplay;
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
        [SerializeField] Text titleLabel;
        [SerializeField] Text detailLabel;
        [SerializeField] Text statusLabel;
        [SerializeField] Text actionLabel;
        [SerializeField] Button button;
        [SerializeField] Image cardImage;
        [SerializeField] Image iconPlate;
        [SerializeField] Outline cardOutline;
        GameObject detailVisibilityRoot;

        Color subjectColor = Color.white;

        static readonly Color ReadyBorder = new Color32(255, 202, 58, 255);
        static readonly Color CompleteBorder = new Color32(65, 170, 104, 255);
        static readonly Color LockedBorder = new Color32(117, 138, 156, 255);
        static readonly Color LockedCard = new Color32(184, 199, 211, 255);
        static readonly Color LockedIcon = new Color32(124, 144, 160, 255);

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
                titleLabel.text = displayName;
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

        public void Bind(Button target, Text title, Text detail, GameObject detailRoot = null)
        {
            button = target;
            titleLabel = title;
            detailLabel = detail;
            detailVisibilityRoot = detailRoot != null ? detailRoot : detail == null ? null : detail.gameObject;
        }

        public void BindStatusLabel(Text status) => statusLabel = status;

        public void BindPresentation(Text status, Text action, Image background, Image sportIcon,
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
                detailLabel.text = selectable
                    ? completed ? $"HẠNG {BestRank}  ★ {Stars}" : "SẴN SÀNG"
                    : unavailableLabel;
                if (detailVisibilityRoot != null)
                    detailVisibilityRoot.SetActive(selectable && completed);
            }
            if (statusLabel != null)
                statusLabel.text = selectable
                    ? completed ? "✓  HOÀN THÀNH" : "SẴN SÀNG"
                    : $"🔒  {unavailableLabel}";
            if (actionLabel != null)
                actionLabel.text = selectable ? "THI →" : string.Empty;
            ApplyVisualState(completed, !selectable);
        }

        void ApplyVisualState(bool completed, bool locked)
        {
            if (cardImage != null)
                cardImage.color = locked ? LockedCard : Color.white;
            if (iconPlate != null)
                iconPlate.color = locked ? LockedIcon : subjectColor;
            if (cardOutline != null)
                cardOutline.effectColor = locked ? LockedBorder : completed ? CompleteBorder : ReadyBorder;
            if (statusLabel != null)
                statusLabel.color = locked
                    ? new Color32(56, 75, 90, 255)
                    : completed ? CompleteBorder : new Color32(12, 105, 94, 255);
        }
    }
}
