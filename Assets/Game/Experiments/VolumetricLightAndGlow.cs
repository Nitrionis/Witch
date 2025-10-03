using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VolumetricLightAndGlow : ScriptableRendererFeature
{
	private CustomRenderPass scriptablePass;

	public Material depthBlitMaterial;
	public Material upscaleBlurMaterial;
	public Material finalMaterial;

	/// <inheritdoc/>
	public override void Create()
	{
		scriptablePass = new CustomRenderPass();

		// Configures where the render pass should be injected.
		scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
	}

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (!TrySetShadersAndMaterials()) {
			Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
			return;
		}

		// Important to halt rendering here if camera is different, otherwise render textures will detect descriptor changes
		if (
			renderingData.cameraData.isPreviewCamera ||
			renderingData.cameraData.isSceneViewCamera
		) {
			return;
		}

		var featureConfig = GetBlurConfig(renderingData);
		scriptablePass.Setup(featureConfig);
		renderer.EnqueuePass(scriptablePass);
	}

	private FeatureConfig GetBlurConfig(in RenderingData renderingData)
	{
		var descriptor = renderingData.cameraData.cameraTargetDescriptor;
		return new FeatureConfig {
			MainCameraResolution = new(descriptor.width, descriptor.height),
			DepthBlitMaterial = depthBlitMaterial,
			UpscaleBlurMaterial = upscaleBlurMaterial,
			FinalMaterial = finalMaterial,
		};
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		scriptablePass?.Dispose();
	}

	private bool TrySetShadersAndMaterials()
	{
		return
			depthBlitMaterial != null &&
			upscaleBlurMaterial != null &&
			finalMaterial != null;
	}

	private class CustomRenderPass : ScriptableRenderPass, IDisposable
	{
		private const string PassName = nameof(VolumetricLightAndGlow);

		private static int Id_CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
		private static int Id_VolumetricLightAndGlowTexture = Shader.PropertyToID("_VolumetricLightAndGlowTexture");

		// This class stores the data needed by the RenderGraph pass.
		// It is passed as a parameter to the delegate function that executes the RenderGraph pass.
		private class PassData
		{
			public FeatureConfig FeatureConfig;
			public MaterialPropertyBlock MaterialPropertyBlock;
			public TextureHandle CameraColorRT;
			public TextureHandle CameraDepthRT;
			public TextureHandle DepthLowResTempRT;
			public TextureHandle LowResTempRT;
			public TextureHandle QuarterResTempRT;
			public TextureHandle HalfResTempRT;
			public RendererListHandle ObjectsToDraw;
		}

		private FeatureConfig featureConfig;
		private readonly MaterialPropertyBlock propertyBlock;

		public void Setup(FeatureConfig featureConfig)
		{
			this.featureConfig = featureConfig;
		}

		public void Dispose()
		{
			// Nothing to dispose
		}

		// This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
		// It is used to execute draw commands.
		private static void ExecutePass(PassData data, UnsafeGraphContext context)
		{
			context.cmd.SetRenderTarget(
				color: data.LowResTempRT,
				colorLoadAction: RenderBufferLoadAction.DontCare,
				colorStoreAction: RenderBufferStoreAction.Store,
				depth: data.DepthLowResTempRT,
				depthLoadAction: RenderBufferLoadAction.DontCare,
				depthStoreAction: RenderBufferStoreAction.DontCare
			);
			var depthBlitMaterial = data.FeatureConfig.DepthBlitMaterial;
			BlitTexture(data.CameraDepthRT, depthBlitMaterial);

			context.cmd.ClearRenderTarget(
				clearDepth: false, clearColor: true, backgroundColor: new UnityEngine.Color(0, 0, 0)
			);
			context.cmd.DrawRendererList(data.ObjectsToDraw);

			var blurUpscaleMaterial = data.FeatureConfig.UpscaleBlurMaterial;
			context.cmd.SetRenderTarget(
				rt: data.QuarterResTempRT,
				loadAction: RenderBufferLoadAction.DontCare,
				storeAction: RenderBufferStoreAction.Store
			);
			BlitTexture(data.LowResTempRT, blurUpscaleMaterial);

			//context.cmd.SetRenderTarget(
			//	rt: data.HalfResTempRT,
			//	loadAction: RenderBufferLoadAction.DontCare,
			//	storeAction: RenderBufferStoreAction.Store
			//);
			//Blitter.BlitTexture(context.cmd, data.QuarterResTempRT, new Vector4(1, 1, 0, 0), blurUpscaleMaterial, 0);

			context.cmd.SetRenderTarget(
				rt: data.CameraColorRT,
				loadAction: RenderBufferLoadAction.Load,
				storeAction: RenderBufferStoreAction.Store
			);
			var finalMaterial = data.FeatureConfig.FinalMaterial;
			finalMaterial.SetTexture(Id_CameraDepthTexture, data.CameraDepthRT);
			finalMaterial.SetTexture(Id_VolumetricLightAndGlowTexture, data.QuarterResTempRT);
			BlitTexture(data.QuarterResTempRT, finalMaterial);
			//context.cmd.DrawProcedural(Matrix4x4.identity, finalMaterial, 0, MeshTopology.Quads, 4, 1);

			//context.cmd.SetRenderTarget(
			//	rt: data.CameraColorRT,
			//	loadAction: RenderBufferLoadAction.Load,
			//	storeAction: RenderBufferStoreAction.Store
			//);
			//Blitter.BlitTexture(context.cmd, data.HalfResTempRT, new Vector4(1, 1, 0, 0), blurUpscaleMaterial, 0);

			//var colorBlitMaterial = Blitter.GetBlitMaterial(TextureDimension.Tex2D);
			//context.cmd.SetRenderTarget(data.CameraColorRT);
			//Blitter.BlitTexture(context.cmd, data.HalfResTempRT, new Vector4(1, 1, 0, 0), colorBlitMaterial, 0);

			void BlitTexture(RTHandle source, Material material)
			{
				Blitter.BlitTexture(context.cmd, source, new Vector4(1, 1, 0, 0), material, 0);
			}

			void DrawFullScreen(RTHandle source, Material material, MaterialPropertyBlock mpb)
			{
				context.cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Quads, 4, 1, mpb);
			}
		}

		// RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
		// FrameData is a context container through which URP resources can be accessed and managed.
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			const string passName = "Render Custom Pass";

			// AddRasterRenderPass

			var resourceData = frameData.Get<UniversalResourceData>();

			if (resourceData.isActiveTargetBackBuffer) {
				Debug.LogError(
					$"Skipping render pass. UniversalBlurPass requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
				return;
			}

			var cameraColorSource = resourceData.activeColorTexture;
			var cameraDepthSource = resourceData.activeDepthTexture;

			var descriptor = new TextureDesc(GetDescriptor());

			var tempDepthDescriptor = descriptor;
			tempDepthDescriptor.width /= 4;
			tempDepthDescriptor.height /= 4;
			const string DepthTempRTName = PassName + ": 1/8_temp_depth_rt";
			tempDepthDescriptor.name = DepthTempRTName;
			tempDepthDescriptor.format = GraphicsFormat.D16_UNorm;
			TextureHandle depthTempRT = renderGraph.CreateTexture(tempDepthDescriptor);

			var tempLowResDescriptor = descriptor;
			tempLowResDescriptor.width /= 4;
			tempLowResDescriptor.height /= 4;
			const string LowResTempRTName = PassName + ": 1/8_temp_color_rt";
			tempLowResDescriptor.name = LowResTempRTName;
			TextureHandle lowResTempRT = renderGraph.CreateTexture(tempLowResDescriptor);

			var tempQuarterResDescriptor = descriptor;
			tempQuarterResDescriptor.width /= 2;
			tempQuarterResDescriptor.height /= 2;
			const string QuarterResTempRTName = PassName + ": 1/4_temp_color_rt";
			tempQuarterResDescriptor.name = QuarterResTempRTName;
			TextureHandle quarterResTempRT = renderGraph.CreateTexture(tempQuarterResDescriptor);

			var tempHalfResDescriptor = descriptor;
			tempHalfResDescriptor.width /= 2;
			tempHalfResDescriptor.height /= 2;
			const string HalfResTempRTRTName = PassName + ": 1/2_temp_color_rt";
			tempHalfResDescriptor.name = HalfResTempRTRTName;
			TextureHandle halfResTempRT = renderGraph.CreateTexture(tempHalfResDescriptor);


			// This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
			using (var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData)) {
				
				passData.CameraColorRT = cameraColorSource;
				passData.CameraDepthRT = cameraDepthSource;
				passData.LowResTempRT = lowResTempRT;
				passData.QuarterResTempRT = quarterResTempRT;
				passData.HalfResTempRT = halfResTempRT;
				passData.DepthLowResTempRT = depthTempRT;

				builder.UseTexture(lowResTempRT, AccessFlags.ReadWrite);
				builder.UseTexture(quarterResTempRT, AccessFlags.ReadWrite);
				builder.UseTexture(halfResTempRT, AccessFlags.ReadWrite);
				builder.UseTexture(depthTempRT, AccessFlags.ReadWrite);

				passData.MaterialPropertyBlock = propertyBlock;

				passData.FeatureConfig = featureConfig;

				// Get the data needed to create the list of objects to draw
				UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				UniversalLightData lightData = frameData.Get<UniversalLightData>();
				SortingCriteria sortFlags = SortingCriteria.CommonTransparent;
				RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
				FilteringSettings filterSettings = new(renderQueueRange, layerMask: ~0);

				// Redraw only objects that have their LightMode tag set to UniversalForward 
				ShaderTagId shadersTags = new ShaderTagId("Custom_Volumetric_Light");

				// Create drawing settings
				DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
					shadersTags, renderingData, cameraData, lightData, sortFlags
				);
				// Create the list of objects to draw
				RendererListParams rendererListParameters = new(
					renderingData.cullResults, drawSettings, filterSettings
				);
				var rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
				passData.ObjectsToDraw = rendererListHandle;
				builder.UseRendererList(passData.ObjectsToDraw);

				builder.AllowPassCulling(false);

				

				// builder.SetGlobalTextureAfterPass(cameraColorSource, Constants.GlobalFullScreenBlurTextureId);

				builder.SetRenderFunc<PassData>(ExecutePass);
			}
		}

		// NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
		// This method is called before executing the render pass.
		// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
		// When empty this render pass will render to the active camera render target.
		// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
		// The render pipeline will ensure target setup and clearing happens in a performant manner.
		[System.Obsolete]
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
		}

		// NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
		// Here you can implement the rendering logic.
		// Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
		// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
		// You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
		[System.Obsolete]
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
		}

		// NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
		// Cleanup any allocated resources that were created during the execution of this render pass.
		public override void OnCameraCleanup(CommandBuffer cmd)
		{
		}

		private RenderTextureDescriptor GetDescriptor() =>
			new(
				featureConfig.MainCameraResolution.Width,
				featureConfig.MainCameraResolution.Height,
				GraphicsFormat.R8G8B8A8_UNorm,
				depthBufferBits: 0
			);
	}

	private struct FeatureConfig
	{
		internal Material DepthBlitMaterial;
		internal Material UpscaleBlurMaterial;
		internal Material FinalMaterial;

		public Size MainCameraResolution;
	}
}
