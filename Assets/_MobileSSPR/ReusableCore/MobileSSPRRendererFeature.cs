//see README here: https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MobileSSPRRendererFeature : ScriptableRendererFeature
{
    public static MobileSSPRRendererFeature instance; //for example scene to call, user should add 1 and not more than 1 MobileSSPRRendererFeature anyway so it is safe to use static ref

    [System.Serializable]
    public class PassSettings
    {
        [Header("Settings")]
        public bool ShouldRenderSSPR = true;
        public float HorizontalReflectionPlaneHeightWS = 0.01f; //default higher than ground a bit, to avoid ZFighting if user placed a ground plane at y=0
        [Range(0.01f, 1f)]
        public float FadeOutScreenBorderWidthVerticle = 0.25f;
        [Range(0.01f, 1f)]
        public float FadeOutScreenBorderWidthHorizontal = 0.35f;
        [Range(0,8f)]
        public float ScreenLRStretchIntensity = 4;
        [Range(-1f,1f)]
        public float ScreenLRStretchThreshold = 0.7f;
        [ColorUsage(true,true)]
        public Color TintColor = Color.white;

        //////////////////////////////////////////////////////////////////////////////////
        [Header("Performance Settings")]
        [Range(128, 1024)]
        [Tooltip("set to 512 or below for better performance, if visual quality lost is acceptable")]
        public int RT_height = 512;
        [Tooltip("can set to false for better performance, if visual quality lost is acceptable")]
        public bool UseHDR = true;
        [Tooltip("can set to false for better performance, if visual quality lost is acceptable")]
        public bool ApplyFillHoleFix = true;
        [Tooltip("can set to false for better performance, if flickering is acceptable")]
        public bool ShouldRemoveFlickerFinalControl = true;

        //////////////////////////////////////////////////////////////////////////////////
        [Header("Danger Zone")]
        [Tooltip("You should always turn this on, unless you want to debug")]
        public bool EnablePerPlatformAutoSafeGuard = true;
    }
    public PassSettings Settings = new PassSettings();

    public class CustomRenderPass : ScriptableRenderPass
    {
        static readonly int _SSPR_ColorRT_pid = Shader.PropertyToID("_MobileSSPR_ColorRT");
        static readonly int _SSPR_PackedDataRT_pid = Shader.PropertyToID("_MobileSSPR_PackedDataRT");
        static readonly int _SSPR_PosWSyRT_pid = Shader.PropertyToID("_MobileSSPR_PosWSyRT");
        RenderTargetIdentifier _SSPR_ColorRT_rti = new RenderTargetIdentifier(_SSPR_ColorRT_pid);
        RenderTargetIdentifier _SSPR_PackedDataRT_rti = new RenderTargetIdentifier(_SSPR_PackedDataRT_pid);
        RenderTargetIdentifier _SSPR_PosWSyRT_rti = new RenderTargetIdentifier(_SSPR_PosWSyRT_pid);

        ShaderTagId lightMode_SSPR_sti = new ShaderTagId("MobileSSPR");//reflection plane renderer's material's shader must use this LightMode

        const int SHADER_NUMTHREAD_X = 8; //must match compute shader's [numthread(x)]
        const int SHADER_NUMTHREAD_Y = 8; //must match compute shader's [numthread(y)]

        PassSettings settings;
        ComputeShader cs;
        public CustomRenderPass(PassSettings settings)
        {
            this.settings = settings;

            cs = (ComputeShader)Resources.Load("MobileSSPRComputeShader");
        }

        int GetRTHeight()
        {
            return Mathf.CeilToInt(settings.RT_height / (float)SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
        }
        int GetRTWidth()
        {
            float aspect = (float)Screen.width / Screen.height;
            return Mathf.CeilToInt(GetRTHeight() * aspect / (float)SHADER_NUMTHREAD_X) * SHADER_NUMTHREAD_X;
        }

        bool ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve()
        {
            if (settings.EnablePerPlatformAutoSafeGuard)
            {
#if UNITY_EDITOR
                return false; //PC / Mac must support the Non-Mobile path
#endif
                //force use MobilePathSinglePassColorRTDirectResolve, if RInt RT is not supported
                if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RInt))
                    return true;
#if UNITY_ANDROID
                //- samsung galaxy A70(Adreno612) will fail if use RenderTextureFormat.RInt + InterlockedMin() in compute shader
                //- but Lenovo S5(Adreno506) is correct, WTF???
                //because behavior is different across android devices, we assume all android are not safe to use RenderTextureFormat.RInt + InterlockedMin() in compute shader
                return true;
#endif

#if UNITY_IOS
                //we don't know the answer now, need to build test
#endif
            }

            //let user decide if we still don't know the correct answer
            return !settings.ShouldRemoveFlickerFinalControl;
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {    
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(GetRTWidth(), GetRTHeight(),RenderTextureFormat.Default, 0, 0);

            rtd.sRGB = false; //don't need gamma correction when sampling these RTs, it is linear data already because it will be filled by screen's linear data
            rtd.enableRandomWrite = true; //using RWTexture2D in compute shader need to turn on this

            //color RT
            bool shouldUseHDRColorRT = settings.UseHDR;
            if (cameraTextureDescriptor.colorFormat == RenderTextureFormat.ARGB32)
                shouldUseHDRColorRT = false;// if there are no HDR info to reflect anyway, no need a HDR colorRT
            rtd.colorFormat = shouldUseHDRColorRT ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32; //we need alpha! (usually LDR is enough, ignore HDR is acceptable for reflection)
            cmd.GetTemporaryRT(_SSPR_ColorRT_pid, rtd);

            //PackedData RT
            if (ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
            {
                //use unsafe method if mobile
                //posWSy RT (will use this RT for posWSy compare test, just like the concept of regular depth buffer)
                rtd.colorFormat = RenderTextureFormat.RFloat;
                cmd.GetTemporaryRT(_SSPR_PosWSyRT_pid, rtd);
            }
            else
            {
                //use 100% correct method if console/PC
                rtd.colorFormat = RenderTextureFormat.RInt;
                cmd.GetTemporaryRT(_SSPR_PackedDataRT_pid, rtd);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cb = CommandBufferPool.Get("SSPR");

            int dispatchThreadGroupXCount = GetRTWidth() / SHADER_NUMTHREAD_X; //divide by shader's numthreads.x
            int dispatchThreadGroupYCount = GetRTHeight() / SHADER_NUMTHREAD_Y; //divide by shader's numthreads.y
            int dispatchThreadGroupZCount = 1; //divide by shader's numthreads.z

            if (settings.ShouldRenderSSPR)
            {
                cb.SetComputeVectorParam(cs, Shader.PropertyToID("_RTSize"), new Vector2(GetRTWidth(), GetRTHeight()));
                cb.SetComputeFloatParam(cs, Shader.PropertyToID("_HorizontalPlaneHeightWS"), settings.HorizontalReflectionPlaneHeightWS);

                cb.SetComputeFloatParam(cs, Shader.PropertyToID("_FadeOutScreenBorderWidthVerticle"), settings.FadeOutScreenBorderWidthVerticle);
                cb.SetComputeFloatParam(cs, Shader.PropertyToID("_FadeOutScreenBorderWidthHorizontal"), settings.FadeOutScreenBorderWidthHorizontal);
                cb.SetComputeVectorParam(cs, Shader.PropertyToID("_CameraDirection"), renderingData.cameraData.camera.transform.forward);
                cb.SetComputeFloatParam(cs, Shader.PropertyToID("_ScreenLRStretchIntensity"), settings.ScreenLRStretchIntensity);
                cb.SetComputeFloatParam(cs, Shader.PropertyToID("_ScreenLRStretchThreshold"), settings.ScreenLRStretchThreshold);
                cb.SetComputeVectorParam(cs, Shader.PropertyToID("_FinalTintColor"), settings.TintColor);


                if (ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
                {
                    ////////////////////////////////////////////////
                    //Android Path
                    ////////////////////////////////////////////////

                    //kernel MobilePathsinglePassColorRTDirectResolve
                    int kernel_MobilePathSinglePassColorRTDirectResolve = cs.FindKernel("MobilePathSinglePassColorRTDirectResolve");
                    cb.SetComputeTextureParam(cs, kernel_MobilePathSinglePassColorRTDirectResolve, "ColorRT", _SSPR_ColorRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_MobilePathSinglePassColorRTDirectResolve, "PosWSyRT", _SSPR_PosWSyRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_MobilePathSinglePassColorRTDirectResolve, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
                    cb.SetComputeTextureParam(cs, kernel_MobilePathSinglePassColorRTDirectResolve, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));
                    cb.DispatchCompute(cs, kernel_MobilePathSinglePassColorRTDirectResolve, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                }
                else
                {
                    ////////////////////////////////////////////////
                    //Non-Android Path (PC/console..)
                    ////////////////////////////////////////////////

                    //kernel NonMobilePathClear
                    int kernel_NonMobilePathClear = cs.FindKernel("NonMobilePathClear");
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathClear, "HashRT", _SSPR_PackedDataRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathClear, "ColorRT", _SSPR_ColorRT_rti);
                    cb.DispatchCompute(cs, kernel_NonMobilePathClear, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                    //kernel NonMobilePathRenderHashRT
                    int kernel_NonMobilePathRenderHashRT = cs.FindKernel("NonMobilePathRenderHashRT");
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathRenderHashRT, "HashRT", _SSPR_PackedDataRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathRenderHashRT, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));

                    cb.DispatchCompute(cs, kernel_NonMobilePathRenderHashRT, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

                    //resolve to ColorRT
                    int kernel_NonMobilePathResolveColorRT = cs.FindKernel("NonMobilePathResolveColorRT");
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathResolveColorRT, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathResolveColorRT, "ColorRT", _SSPR_ColorRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_NonMobilePathResolveColorRT, "HashRT", _SSPR_PackedDataRT_rti);
                    cb.DispatchCompute(cs, kernel_NonMobilePathResolveColorRT, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);
                }

                //optional shared pass to improve result only: fill RT hole
                if(settings.ApplyFillHoleFix)
                {
                    int kernel_FillHoles = cs.FindKernel("FillHoles");
                    cb.SetComputeTextureParam(cs, kernel_FillHoles, "ColorRT", _SSPR_ColorRT_rti);
                    cb.SetComputeTextureParam(cs, kernel_FillHoles, "PackedDataRT", _SSPR_PackedDataRT_rti);
                    cb.DispatchCompute(cs, kernel_FillHoles, Mathf.CeilToInt(dispatchThreadGroupXCount / 2f), Mathf.CeilToInt(dispatchThreadGroupYCount / 2f), dispatchThreadGroupZCount);
                }

                //send out to global, for user's shader to sample reflection result RT (_MobileSSPR_ColorRT)
                //where _MobileSSPR_ColorRT's rgb is reflection color, a is reflection usage 0~1 for user's shader to lerp with fallback reflection probe's rgb
                cb.SetGlobalTexture(_SSPR_ColorRT_pid, _SSPR_ColorRT_rti);
                cb.EnableShaderKeyword("_MobileSSPR");
            }
            else
            {
                //allow user to skip SSPR related code if disabled
                cb.DisableShaderKeyword("_MobileSSPR");
            }

            context.ExecuteCommandBuffer(cb);
            CommandBufferPool.Release(cb);

            //======================================================================
            //draw objects(e.g. reflective wet ground plane) with lightmode "MobileSSPR", which will sample _MobileSSPR_ColorRT
            DrawingSettings drawingSettings = CreateDrawingSettings(lightMode_SSPR_sti, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_SSPR_ColorRT_pid);

            if(ShouldUseSinglePassUnsafeAllowFlickeringDirectResolve())
                cmd.ReleaseTemporaryRT(_SSPR_PosWSyRT_pid);
            else
                cmd.ReleaseTemporaryRT(_SSPR_PackedDataRT_pid);
        }
    }

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        instance = this;

        m_ScriptablePass = new CustomRenderPass(Settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;//we must wait _CameraOpaqueTexture & _CameraDepthTexture is usable
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


