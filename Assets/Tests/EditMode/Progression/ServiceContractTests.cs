using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ServiceContractTests
    {
        const string MixerPath = "Assets/_Project/Settings/Audio/KMA-AudioMixer.mixer";
        const string SubjectFolder = "Assets/_Project/ScriptableObjects/Subjects";

        readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void AudioManager_ClampsVolumesAndKeepsMusicAndSfxIndependent()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null, $"Missing mixer at {MixerPath}");

            AudioMixerGroup[] authoredGroups = mixer.FindMatchingGroups(string.Empty)
                .Where(group => group.name != "Master")
                .ToArray();
            Assert.That(authoredGroups.Select(group => group.name),
                Is.EquivalentTo(new[] { "Music", "SFX" }));
            Assert.That(authoredGroups, Has.Length.EqualTo(2));
            Assert.That(mixer.GetFloat("MusicVolume", out _), Is.True);
            Assert.That(mixer.GetFloat("SfxVolume", out _), Is.True);

            Type managerType = RequireCoreType("KMA.Gameplay.Core.AudioManager");
            Component manager = AddComponent(managerType, "ServiceContractTests.AudioManager");
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("musicGroup").objectReferenceValue =
                authoredGroups.Single(group => group.name == "Music");
            serializedManager.FindProperty("sfxGroup").objectReferenceValue =
                authoredGroups.Single(group => group.name == "SFX");
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            Invoke(manager, "SetMusicVolume", -2f);
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(0f));
            Assert.That(InvokeStatic<float>(managerType, "LinearToDecibels", -2f),
                Is.EqualTo(-80f).Within(0.001f));

            Invoke(manager, "SetMusicVolume", 2f);
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(1f));
            Assert.That(InvokeStatic<float>(managerType, "LinearToDecibels", 2f),
                Is.EqualTo(0f).Within(0.001f));

            Invoke(manager, "SetMusicVolume", 0.5f);
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(0.5f));
            Assert.That(InvokeStatic<float>(managerType, "LinearToDecibels", 0.5f),
                Is.EqualTo(-6.0206f).Within(0.001f));

            Invoke(manager, "SetSfxVolume", 0.25f);
            Assert.That(GetProperty<float>(manager, "SfxVolume"), Is.EqualTo(0.25f));
            Assert.That(InvokeStatic<float>(managerType, "LinearToDecibels", 0.25f),
                Is.EqualTo(-12.0412f).Within(0.001f));
            Assert.That(GetProperty<float>(manager, "MusicVolume"), Is.EqualTo(0.5f));
        }

        [Test]
        public void AudioManager_MissingDependenciesAndClipFailSafely()
        {
            Type managerType = RequireCoreType("KMA.Gameplay.Core.AudioManager");
            Component manager = AddComponent(managerType, "ServiceContractTests.EmptyAudioManager");

            Assert.That(() => Invoke(manager, "SetMusicVolume", 0.5f), Throws.Nothing);
            Assert.That(() => Invoke(manager, "SetSfxVolume", 0.5f), Throws.Nothing);
            Assert.That(() => Invoke(manager, "PlaySfx", (object)null), Throws.Nothing);
        }

        [Test]
        public void HapticsService_DisabledSettingLeavesAllFeedbackAsSafeNoOps()
        {
            Type serviceType = RequireCoreType("KMA.Gameplay.Core.HapticsService");
            Component service = AddComponent(serviceType, "ServiceContractTests.HapticsService");
            Invoke(service, "ApplySettings", new Settings
            {
                musicVol = 1f,
                sfxVol = 1f,
                vibration = false,
                rhythmOffsetMs = 0f
            });

            PropertyInfo enabledProperty = serviceType.GetProperty("VibrationEnabled");
            Assert.That(enabledProperty, Is.Not.Null);
            Assert.That(enabledProperty.GetValue(service), Is.False);
            Assert.That(() => Invoke(service, "Light"), Throws.Nothing);
            Assert.That(() => Invoke(service, "Medium"), Throws.Nothing);
            Assert.That(() => Invoke(service, "Success"), Throws.Nothing);
            Assert.That(() => Invoke(service, "Fail"), Throws.Nothing);
        }

        [Test]
        public void Pool_ReusesReleasedEntriesWithoutExpandingAfterPrewarm()
        {
            Type poolDefinition = RequireCoreType("KMA.Gameplay.Core.Pool`1");
            Type poolType = poolDefinition.MakeGenericType(typeof(Transform));
            GameObject prefabObject = CreateObject("ServiceContractTests.Prefab");
            GameObject parentObject = CreateObject("ServiceContractTests.Parent");
            object pool = Activator.CreateInstance(poolType, prefabObject.transform, 2, parentObject.transform);

            Assert.That(parentObject.transform.childCount, Is.EqualTo(2));
            Transform first = (Transform)Invoke(pool, "Get");
            Transform second = (Transform)Invoke(pool, "Get");
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(Invoke(pool, "Get"), Is.Null);
            Assert.That(parentObject.transform.childCount, Is.EqualTo(2));

            Invoke(pool, "Release", first);
            Transform reused = (Transform)Invoke(pool, "Get");

            Assert.That(reused, Is.SameAs(first));
            Assert.That(parentObject.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void Pool_UninitializedSerializedGetDoesNotInstantiateUntilExplicitInitialize()
        {
            Type poolDefinition = RequireCoreType("KMA.Gameplay.Core.Pool`1");
            Type poolType = poolDefinition.MakeGenericType(typeof(Transform));
            GameObject prefabObject = CreateObject("ServiceContractTests.SerializedPrefab");
            GameObject parentObject = CreateObject("ServiceContractTests.SerializedParent");
            object pool = Activator.CreateInstance(poolType);
            SetField(pool, "prefab", prefabObject.transform);
            SetField(pool, "prewarmCapacity", 2);
            SetField(pool, "parent", parentObject.transform);

            Assert.That(parentObject.transform.childCount, Is.EqualTo(0));
            Assert.That(Invoke(pool, "Get"), Is.Null);
            Assert.That(parentObject.transform.childCount, Is.EqualTo(0));

            Invoke(pool, "Initialize");
            Assert.That(parentObject.transform.childCount, Is.EqualTo(2));
            Assert.That(Invoke(pool, "Get"), Is.Not.Null);
            Assert.That(parentObject.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void SubjectConfigs_ContainSevenEnumBackedAndThreeComingSoonAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:SubjectConfig", new[] { SubjectFolder });
            UnityEngine.Object[] assets = assetGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .ToArray();

            Assert.That(assets, Has.Length.EqualTo(10));
            Assert.That(assets, Is.All.Not.Null);

            var playableIds = new HashSet<SubjectId>();
            var comingSoonNames = new List<string>();
            foreach (UnityEngine.Object asset in assets)
            {
                var serializedAsset = new SerializedObject(asset);
                SerializedProperty displayName = serializedAsset.FindProperty("displayName");
                SerializedProperty subjectId = serializedAsset.FindProperty("subjectId");
                SerializedProperty unlocked = serializedAsset.FindProperty("unlocked");
                SerializedProperty comingSoon = serializedAsset.FindProperty("comingSoon");
                Assert.That(displayName, Is.Not.Null);
                Assert.That(subjectId, Is.Not.Null);
                Assert.That(unlocked, Is.Not.Null);
                Assert.That(comingSoon, Is.Not.Null);

                if (comingSoon.boolValue)
                {
                    Assert.That(unlocked.boolValue, Is.False);
                    comingSoonNames.Add(displayName.stringValue);
                }
                else
                {
                    Assert.That(unlocked.boolValue, Is.True);
                    playableIds.Add((SubjectId)subjectId.intValue);
                }
            }

            Assert.That(playableIds, Is.EquivalentTo((SubjectId[])Enum.GetValues(typeof(SubjectId))));
            Assert.That(comingSoonNames, Is.EquivalentTo(new[] { "Hít đất", "Nhịp điệu", "Bơi lội" }));

            Type configType = RequireAssemblyCSharpType("KMA.Gameplay.SubjectConfig");
            Assert.That(configType.GetField("sceneName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
        }

        [Test]
        public void AuthoringTypes_ProvideQuoteBandsAndRuntimeRivalProfile()
        {
            Type quoteType = RequireAssemblyCSharpType("KMA.Gameplay.InstructorQuoteSet");
            ScriptableObject quoteSet = ScriptableObject.CreateInstance(quoteType);
            var serializedQuotes = new SerializedObject(quoteSet);
            Assert.That(serializedQuotes.FindProperty("chill").arraySize, Is.GreaterThan(0));
            Assert.That(serializedQuotes.FindProperty("urgent").arraySize, Is.GreaterThan(0));

            Type rivalType = RequireAssemblyCSharpType("KMA.Gameplay.RivalPaceProfileAsset");
            ScriptableObject rivalAsset = ScriptableObject.CreateInstance(rivalType);
            var serializedRival = new SerializedObject(rivalAsset);
            serializedRival.FindProperty("profileName").stringValue = "Test Rival";
            serializedRival.FindProperty("openingSpeed").floatValue = 8.5f;
            serializedRival.FindProperty("sustainedSpeed").floatValue = 7.25f;
            serializedRival.ApplyModifiedPropertiesWithoutUndo();

            object runtime = Invoke(rivalAsset, "ToRuntime");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.GetType().FullName, Is.EqualTo("KMA.Gameplay.RivalPaceProfile"));
            Assert.That(runtime.GetType().GetProperty("Name").GetValue(runtime), Is.EqualTo("Test Rival"));
            Assert.That(runtime.GetType().GetProperty("OpeningSpeed").GetValue(runtime),
                Is.EqualTo(8.5f).Within(0.001f));
            Assert.That(runtime.GetType().GetProperty("SustainedSpeed").GetValue(runtime),
                Is.EqualTo(7.25f).Within(0.001f));

            UnityEngine.Object.DestroyImmediate(quoteSet);
            UnityEngine.Object.DestroyImmediate(rivalAsset);
        }

        GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        Component AddComponent(Type type, string objectName) => CreateObject(objectName).AddComponent(type);

        static Type RequireCoreType(string fullName)
        {
            Type type = typeof(GameManager).Assembly.GetType(fullName);
            Assert.That(type, Is.Not.Null, $"Missing production type {fullName}");
            return type;
        }

        static Type RequireAssemblyCSharpType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, $"Missing production type {fullName}");
            return type;
        }

        static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().FullName}.{methodName}");
            return method.Invoke(target, arguments);
        }

        static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null,
                $"Missing property {target.GetType().FullName}.{propertyName}");
            return (T)property.GetValue(target);
        }

        static T InvokeStatic<T>(Type type, string methodName, params object[] arguments)
        {
            MethodInfo method = type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing static method {type.FullName}.{methodName}");
            return (T)method.Invoke(null, arguments);
        }

        static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}
