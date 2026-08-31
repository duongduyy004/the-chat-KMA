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
        [SerializeField] Button button;

        public SubjectId SubjectId => subjectId;
        public string DisplayName => displayName;
        public bool IsComingSoon => comingSoon;
        public bool HasSubjectConfigAsset => subjectConfigAsset != null;
        public ScriptableObject SubjectConfigAsset => subjectConfigAsset;
        public int Stars { get; private set; }
        public Rank BestRank { get; private set; }
        public int Lives { get; private set; }

        public void Configure(SubjectId id, string name, bool isComingSoon, SubjectRecord record, int lives)
        {
            subjectId = id;
            displayName = name;
            comingSoon = isComingSoon;
            BestRank = record == null ? Rank.F : record.BestRank;
            Stars = record == null || !record.Passed ? 0 : ScoreUtil.ToStars(BestRank);
            Lives = lives;
            if (titleLabel != null)
                titleLabel.text = displayName;
            if (detailLabel != null)
                detailLabel.text = IsComingSoon ? "COMING SOON" :
                    record != null && record.Passed ? $"RANK {BestRank}  STARS {Stars}" : "LOCKED";
            if (button != null)
                button.interactable = !comingSoon;
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
            Configure(subjectId, displayName, comingSoon, record, lives);
            if (button != null)
                button.interactable = unlocked && !comingSoon;
        }

        public void Bind(Button target, Text title, Text detail)
        {
            button = target;
            titleLabel = title;
            detailLabel = detail;
        }
    }
}
