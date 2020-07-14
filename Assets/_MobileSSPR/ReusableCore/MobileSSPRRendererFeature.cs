using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MobileSSPRRendererFeature : ScriptableRendererFeature
{
    public static MobileSSPRRendererFeature instance; //for debug

    [System.Serializable]
    public class PassSettings
    {
        public float horizontalPlaneHeightWS = 0;

        [Range(32, 1080)]
        public int RT_height = 360;

        public ComputeShader computeShader;
    }
    public PassSettings Settings = new PassSettings();

    public class CustomRenderPass : ScriptableRenderPass
    {

        static readonly int _SSPR_RT_pid = Shader.PropertyToID("_MobileSSPR_RT");
        RenderTargetIdentifier _SSPR_RT_rti = new RenderTargetIdentifier(_SSPR_RT_pid);
        ShaderTagId lightMode_SSPR_sti = new ShaderTagId("MobileSSPR");

        public PassSettings settings;

        public CustomRenderPass(PassSettings settings)
        {
            this.settings = settings;
        }

        int GetRTWidth()
        {
            float aspect = (float)Screen.width / (float)Screen.height;
            return (int)(settings.RT_height * aspect);
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(GetRTWidth(), settings.RT_height);
            rtd.enableRandomWrite = true;
            rtd.sRGB = false; //don't need gamma correction when sampling these RTs
            rtd.colorFormat = RenderTextureFormat.ARGBFloat; //.a needs float(posWS.y)
            
            cmd.GetTemporaryRT(_SSPR_RT_pid, rtd);
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
            int dispatchThreadGroupYCount = Mathf.CeilToInt(settings.RT_height / 32f);
            int dispatchThreadGroupZCount = 1;

            //clear
            cb.SetComputeTextureParam(settings.computeShader, 0, "Result", _SSPR_RT_rti);
            //cb.SetComputeTextureParam(settings.computeShader,0,"_SkyBox", RenderSettings.sky);
            cb.DispatchCompute(settings.computeShader, 0, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            //draw
            cb.SetComputeVectorParam(settings.computeShader, Shader.PropertyToID("_RTSize"), new Vector2(GetRTWidth(), settings.RT_height));
            cb.SetComputeFloatParam(settings.computeShader, Shader.PropertyToID("_HorizontalPlaneHeightWS"), settings.horizontalPlaneHeightWS);
            cb.SetComputeTextureParam(settings.computeShader, 1, "Result", _SSPR_RT_rti);
            cb.SetComputeTextureParam(settings.computeShader, 1, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
            cb.SetComputeTextureParam(settings.computeShader, 1, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));
            cb.DispatchCompute(settings.computeShader, 1, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);
 
            //set global RT
            cb.SetGlobalTexture(_SSPR_RT_pid, _SSPR_RT_rti);

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
            cmd.ReleaseTemporaryRT(_SSPR_RT_pid);
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


