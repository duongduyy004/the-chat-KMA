using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class ProjectConfigurator
    {
        const string ProductName = "Thể Chất KMA";
        const string AndroidApplicationId = "com.kma.thechat";
        const int AndroidMinApiLevel = 25;
        const int AndroidTargetApiLevel = 35;
        const int DspBufferSize = 256;

        [MenuItem("KMA/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            ApplyAndroidSettings();
            ApplyOrientation();
            ApplyAudioSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Project settings applied.");
        }

        static void ApplyAndroidSettings()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidApplicationId);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)AndroidMinApiLevel;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)AndroidTargetApiLevel;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
        }

        static void ApplyOrientation()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        static void ApplyAudioSettings()
        {
            var audioManager = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")
                .FirstOrDefault();
            if (audioManager == null)
            {
                throw new InvalidOperationException("Could not load ProjectSettings/AudioManager.asset.");
            }

            var serializedAudioManager = new SerializedObject(audioManager);
            var requestedBufferSize = serializedAudioManager
                .FindProperty("m_RequestedDSPBufferSize");
            if (requestedBufferSize == null)
            {
                throw new InvalidOperationException(
                    "AudioManager no longer exposes m_RequestedDSPBufferSize.");
            }

            requestedBufferSize.intValue = DspBufferSize;
            serializedAudioManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audioManager);

            var runtimeConfiguration = AudioSettings.GetConfiguration();
            runtimeConfiguration.dspBufferSize = DspBufferSize;
            if (!AudioSettings.Reset(runtimeConfiguration))
            {
                throw new InvalidOperationException("Unity rejected the DSP buffer configuration.");
            }
        }
    }
}
