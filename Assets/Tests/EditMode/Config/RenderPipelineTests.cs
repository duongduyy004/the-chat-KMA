using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KMA.Tests.Config
{
    public sealed class RenderPipelineTests
    {
        const string PipelinePath = "Assets/_Project/Settings/URP/URP-2D.asset";
        const string RendererPath = "Assets/_Project/Settings/URP/URP-2D_Renderer2D.asset";
        const string VolumeProfilePath = "Assets/_Project/Settings/URP/DefaultVolumeProfile.asset";

        [Test]
        public void DefaultPipelineResolvesToAuthoredUrpAsset()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null, "The default render pipeline is unassigned.");
            Assert.That(AssetDatabase.GetAssetPath(pipeline), Is.EqualTo(PipelinePath));
            Assert.That(pipeline.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"));
        }

        [Test]
        public void UniversalPipelineUsesRenderer2D()
        {
            var renderer = GetDefaultRendererData();
            Assert.That(renderer, Is.Not.Null, "The URP asset has no default renderer data.");
            Assert.That(AssetDatabase.GetAssetPath(renderer), Is.EqualTo(RendererPath));
            Assert.That(renderer.GetType().Name, Is.EqualTo("Renderer2DData"));
        }

        [Test]
        public void HdrAndPostProcessingAreDisabled()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null);

            var supportsHdr = pipeline.GetType().GetProperty("supportsHDR");
            Assert.That(supportsHdr, Is.Not.Null, "URP supportsHDR API was not found.");
            Assert.That((bool)supportsHdr.GetValue(pipeline), Is.False);

            var renderer = GetDefaultRendererData();
            Assert.That(renderer, Is.Not.Null);
            var postProcessData = new SerializedObject(renderer).FindProperty("m_PostProcessData");
            Assert.That(postProcessData, Is.Not.Null, "Renderer2D post-process setting was not found.");
            Assert.That(postProcessData.objectReferenceValue, Is.Null);
        }

        [Test]
        public void DefaultVolumeProfileExists()
        {
            var profile = AssetDatabase.LoadMainAssetAtPath(VolumeProfilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.VolumeProfile"));
        }

        [Test]
        public void EveryQualityLevelUsesAuthoredPipeline()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null);

            var originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (var index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipeline),
                        $"Quality level {QualitySettings.names[index]} does not use the authored URP asset.");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        static Object GetDefaultRendererData()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null)
            {
                return null;
            }

            var rendererList = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            if (rendererList == null || rendererList.arraySize == 0)
            {
                return null;
            }

            return rendererList.GetArrayElementAtIndex(0).objectReferenceValue;
        }
    }
}
