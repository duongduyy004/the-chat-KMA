using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Chess
{
    public sealed class ChessCastView : MonoBehaviour
    {
        [Serializable]
        public struct Pose
        {
            public string name;
            public Sprite sprite;
        }

        [SerializeField] Image student;
        [SerializeField] Image teacher;
        [SerializeField] Pose[] studentPoses;
        [SerializeField] Pose[] teacherPoses;
        [SerializeField] GameObject bubble;
        [SerializeField] TMP_Text bubbleText;
        [SerializeField] float stepSeconds = .3f;
        float bubbleUntil;
        Coroutine teacherSequence;

        public string StudentPose { get; private set; }
        public string TeacherPose { get; private set; }
        public bool TeacherSequenceRunning => teacherSequence != null;
        public string BubbleText => bubble != null && bubble.activeSelf ? bubbleText.text : string.Empty;

        public void Configure(Image studentImage, Image teacherImage, Pose[] studentSet, Pose[] teacherSet,
            GameObject speechBubble, TMP_Text speechText)
        {
            student = studentImage;
            teacher = teacherImage;
            studentPoses = studentSet;
            teacherPoses = teacherSet;
            bubble = speechBubble;
            bubbleText = speechText;
        }

        public void SetStudent(string pose)
        {
            StudentPose = pose;
            student.sprite = Find(studentPoses, pose);
        }

        public void SetTeacher(string pose)
        {
            if (teacherSequence != null) StopCoroutine(teacherSequence);
            teacherSequence = null;
            ApplyTeacher(pose);
        }

        /// Plays the poses one step apart; the last one stays.
        public void PlayTeacher(params string[] poses)
        {
            if (teacherSequence != null) StopCoroutine(teacherSequence);
            teacherSequence = StartCoroutine(Sequence(poses));
        }

        public void Say(string text, float seconds = 2.5f)
        {
            bubble.SetActive(true);
            bubbleText.text = VietText.Fix(text);
            bubbleUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (bubble != null && bubble.activeSelf && Time.unscaledTime > bubbleUntil) bubble.SetActive(false);
        }

        IEnumerator Sequence(string[] poses)
        {
            for (int i = 0; i < poses.Length; i++)
            {
                ApplyTeacher(poses[i]);
                if (i + 1 < poses.Length) yield return new WaitForSeconds(stepSeconds);
            }
            teacherSequence = null;
        }

        void ApplyTeacher(string pose)
        {
            TeacherPose = pose;
            teacher.sprite = Find(teacherPoses, pose);
        }

        static Sprite Find(Pose[] set, string pose) =>
            set.FirstOrDefault(p => p.name == pose).sprite ??
            throw new InvalidOperationException($"[KMA] Pose {pose} is not configured.");
    }
}
