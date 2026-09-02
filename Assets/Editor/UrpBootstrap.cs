using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KMA.EditorTools
{
    public static class UrpBootstrap
    {
        const string UrpFolder = "Assets/_Project/Settings/URP";
        const string PipelineAssetPath = UrpFolder + "/URP-2D.asset";
        const string RendererAssetPath = UrpFolder + "/URP-2D_Renderer2D.asset";
        const string VolumeProfilePath = UrpFolder + "/DefaultVolumeProfile.asset";

        [MenuItem("KMA/Create or Repair URP 2D")]
        public static void CreateOrRepair()
        {
            EnsureFolder(UrpFolder);

            var renderer = LoadOrCreateRenderer();
            DisablePostProcessing(renderer);

            var pipeline = LoadOrCreatePipeline(renderer);
            AssignRenderer(pipeline, renderer);
            pipeline.supportsHDR = false;

            var volumeProfile = LoadOrCreateVolumeProfile();
            pipeline.volumeProfile = volumeProfile;

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssignAllQualityLevels(pipeline);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[KMA] URP 2D created or repaired at {PipelineAssetPath}.");
        }

        static Renderer2DData LoadOrCreateRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererAssetPath);
            if (renderer != null)
            {
                return renderer;
            }

            DeleteUnexpectedAsset(RendererAssetPath);
            renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            renderer.name = "URP-2D_Renderer2D";
            AssetDatabase.CreateAsset(renderer, RendererAssetPath);
            return renderer;
        }

        static UniversalRenderPipelineAsset LoadOrCreatePipeline(Renderer2DData renderer)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline != null)
            {
                return pipeline;
            }

            DeleteUnexpectedAsset(PipelineAssetPath);
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = "URP-2D";
            AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            return pipeline;
        }

        static VolumeProfile LoadOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                return profile;
            }

            DeleteUnexpectedAsset(VolumeProfilePath);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "DefaultVolumeProfile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            return profile;
        }

        static void AssignRenderer(UniversalRenderPipelineAsset pipeline, Renderer2DData renderer)
        {
            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            var defaultRendererIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");
            if (rendererList == null || defaultRendererIndex == null)
            {
                throw new InvalidOperationException("The URP renderer assignment API has changed.");
            }

            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            defaultRendererIndex.intValue = 0;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
        }

        static void DisablePostProcessing(Renderer2DData renderer)
        {
            var serializedRenderer = new SerializedObject(renderer);
            var postProcessData = serializedRenderer.FindProperty("m_PostProcessData");
            if (postProcessData == null)
            {
                throw new InvalidOperationException("The Renderer2D post-process API has changed.");
            }

            postProcessData.objectReferenceValue = null;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignAllQualityLevels(RenderPipelineAsset pipeline)
        {
            var originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (var index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        static void DeleteUnexpectedAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null && !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException($"Could not replace unexpected asset at {path}.");
            }
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
