using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MobileSSPRRendererFeature : ScriptableRendererFeature
{
    public static MobileSSPRRendererFeature instance; //for example scene to call, user should only add 1 MobileSSPRRendererFeature(not > 1)

    [System.Serializable]
    public class PassSettings
    {
        public float horizontalPlaneHeightWS = 0;

        [Range(32, 1080)]
        public int RT_height = 512;

        public ComputeShader computeShader;
    }
    public PassSettings Settings = new PassSettings();

    public class CustomRenderPass : ScriptableRenderPass
    {

        static readonly int _SSPR_ColorRT_pid = Shader.PropertyToID("_MobileSSPR_ColorRT");
        static readonly int _SSPR_PosWSyRT_pid = Shader.PropertyToID("_MobileSSPR_PosWSyRTRT");
        RenderTargetIdentifier _SSPR_ColorRT_rti = new RenderTargetIdentifier(_SSPR_ColorRT_pid);
        RenderTargetIdentifier _SSPR_PosWSyRT_rti = new RenderTargetIdentifier(_SSPR_PosWSyRT_pid);
        ShaderTagId lightMode_SSPR_sti = new ShaderTagId("MobileSSPR");

        public PassSettings settings;

        public CustomRenderPass(PassSettings settings)
        {
            this.settings = settings;
        }

        //RT must be multiply of 32x32 in order to maximize compute shader performance in SM5
        int GetRTWidth()
        {
            return (int)Mathf.Ceil((float)settings.RT_height / 32f) * 32;
        }
        int GetRTHeight()
        {
            float aspect = (float)Screen.width / (float)Screen.height;
            return (int)Mathf.Ceil((float)GetRTWidth() * aspect / 32f) * 32;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(GetRTWidth(), GetRTHeight());
            rtd.enableRandomWrite = true;
            rtd.sRGB = false; //don't need gamma correction when sampling these RTs
            rtd.colorFormat = RenderTextureFormat.ARGB32;
            
            cmd.GetTemporaryRT(_SSPR_ColorRT_pid, rtd);

            rtd.colorFormat = RenderTextureFormat.RFloat;
            cmd.GetTemporaryRT(_SSPR_PosWSyRT_pid, rtd);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //render RT using compute shader
            CommandBuffer cb = CommandBufferPool.Get("SSPR");

            int dispatchThreadGroupXCount = Mathf.CeilToInt(GetRTWidth() / 32f);
            int dispatchThreadGroupYCount = Mathf.CeilToInt(GetRTHeight() / 32f);
            int dispatchThreadGroupZCount = 1;

            //clear colorRT
            cb.SetComputeTextureParam(settings.computeShader, 0, "ColorRT", _SSPR_ColorRT_rti);
            cb.SetComputeTextureParam(settings.computeShader, 0, "PosWSyRT", _SSPR_PosWSyRT_rti);
            cb.DispatchCompute(settings.computeShader, 0, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            //draw RT
            cb.SetComputeVectorParam(settings.computeShader, Shader.PropertyToID("_RTSize"), new Vector2(GetRTWidth(), GetRTHeight()));
            cb.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_HorizontalPlaneHeightWS"), settings.horizontalPlaneHeightWS);
            cb.SetComputeTextureParam(settings.computeShader, 1, "ColorRT", _SSPR_ColorRT_rti);
            cb.SetComputeTextureParam(settings.computeShader, 1, "PosWSyRT", _SSPR_PosWSyRT_rti);
            cb.SetComputeTextureParam(settings.computeShader, 1, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
            cb.SetComputeTextureParam(settings.computeShader, 1, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));
            cb.DispatchCompute(settings.computeShader, 1, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);
 
            //set global RT
            cb.SetGlobalTexture(_SSPR_ColorRT_pid, _SSPR_ColorRT_rti);

            context.ExecuteCommandBuffer(cb);
            CommandBufferPool.Release(cb);

            //======================================================================
            //draw objects with lightmode "SSPR", which use that RT
            DrawingSettings drawingSettings = CreateDrawingSettings(lightMode_SSPR_sti, ref renderingData, SortingCriteria.CommonTransparent);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_SSPR_ColorRT_pid);
        }
    }

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (!Settings.computeShader) return;

        instance = this;

        m_ScriptablePass = new CustomRenderPass(Settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


