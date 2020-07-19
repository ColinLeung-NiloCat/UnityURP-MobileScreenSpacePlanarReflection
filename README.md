# UnityURP-MobileScreenSpacePlanarReflection
 ScreenSpacePlanarReflection(SSPR) as a reusable RendererFeature in URP.  
 - See runtime video here: https://youtu.be/Cy46A8EyX4Q
 - download pre-built .apk here: https://drive.google.com/file/d/14Z_Gjb1ADz8RhcBgAFpa96dm-oQuOyQx/view?usp=sharing  
 
 SSPR ON
 ![screenshot](https://i.imgur.com/cNaVHLK.png)
 SSPR OFF
 ![screenshot](https://i.imgur.com/0WCIcTM.png)
 SSPR ON
 ![screenshot](https://i.imgur.com/XvudHkR.png)
 SSPR OFF
 ![screenshot](https://i.imgur.com/AZ08hZ8.png)
 
On Adreno612 GPU android mobile device(Samsung Galaxy A70), Toggle SSPR ON/OFF:
 - cost <1ms to update 128 height SSPR RT (ForceFastestSinglePassColorResolve on)
 - cost 1~2ms to update 256 height SSPR RT (ForceFastestSinglePassColorResolve on)
 - cost 4~5ms to update 512 height SSPR RT (ForceFastestSinglePassColorResolve on)  
 
 On Adreno506 GPU android mobile device(Lenovo S5), Toggle SSPR ON/OFF:
 - cost ~1ms to update 128 height SSPR RT (ForceFastestSinglePassColorResolve on)
 - cost 3~4ms to update 256 height SSPR RT (ForceFastestSinglePassColorResolve on)
 - cost 7~8ms to update 512 height SSPR RT (ForceFastestSinglePassColorResolve on)
 
 Where are the important files?
-------------------
 There are only 3 important code files, all inside a folder "Assets \ _MobileSSPR \ ReusableCore".
 Other files are for demo only, not important.
 
 Can it run on mobile?
-------------------
 Tested on ~10 android devices, should be alright if your android device support Vulkan.
 
 How to try this in my own URP project?
 -------------------
 - copy "Assets \ _MobileSSPR \ ReusableCore" folder to your project (contains 3 important code files)
 - turn on "Depth Texture" in your project's URP's setting
 - turn on "Opaque Texture" in your project's URP's setting
 - Add MobileSSPRRendererFeature(RendererFeature) to your forward renderer asset
 - assign MobileSSPRComputeShader to this new RendererFeature's "compute shader" slot
 - set horizontalReflectionPlaneHeightWS to 0.01 in this new RendererFeature
 - create a new plane game object in your scene (set world space pos y = 0.01)
 - create a material using MobileSSPRExampleShader
 - assign this material to your new plane
 - DONE! You should see correct reflection both in scene and game window

 I can see some small flickering in reflection even camera and scene is not moving
 -------------------
 It is an expected artifact of this mobile screen space planar reflection implementation(because we can't use InterlockedMin and uint RenderTexture on mobile)
see these for more detail: 
 - http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf
 - https://zhuanlan.zhihu.com/p/150890059
 
 Notes
 -------------------
This is a test project to see if screen space reflection & compute shader can run on mobile correctly and fast enough.   
We need to avoid InterlockedMin and RenderTexture color format "uint" to support mobile (see -> https://zhuanlan.zhihu.com/p/150890059). 
RenderTexture color format ARGBHalf can be used on mobile devices, we use this instead of uint RT.
 
 Editor
 -------------------
2019.4.3f1 LTS

Implementation reference
-------------------
- http://remi-genin.fr/blog/screen-space-plane-indexed-reflection-in-ghost-recon-wildlands/
- http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf
- https://zhuanlan.zhihu.com/p/150890059
- https://github.com/Steven-Cannavan/URP_ScreenSpacePlanarReflections

TODO
----------------
- make a new .hlsl to allow user apply this SSPR's result to their custom shader using 2 lines of code 
 
 
