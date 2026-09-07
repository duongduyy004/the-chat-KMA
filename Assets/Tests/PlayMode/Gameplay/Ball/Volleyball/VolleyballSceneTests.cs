using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class VolleyballSceneTests
    {
        [UnityTest]
        public IEnumerator VolleyballScene_HasPlayableControllerAndSinglePhysicsBall()
        {
            yield return SceneManager.LoadSceneAsync("MG_Volleyball", LoadSceneMode.Single);
            yield return null;

            Assert.That(Object.FindObjectsByType<VolleyballController>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<BallRig>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<VolleyballHud>(FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlaceholderMinigameController>(FindObjectsInactive.Include), Is.Null);
            Assert.That(GameObject.Find("VolleyballCourt"), Is.Not.Null);
            Assert.That(GameObject.Find("VolleyballNet"), Is.Not.Null);
        }
    }
}
