using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class VolleyballCampaignTests
    {
        [UnityTest]
        public IEnumerator MapToVolleyballRoute_LoadsTheProductionScene()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Volleyball), Is.True);
            while (SceneManager.GetActiveScene().name != "MG_Volleyball") yield return null;
            Assert.That(Object.FindFirstObjectByType<VolleyballController>(), Is.Not.Null);
        }
    }
}
