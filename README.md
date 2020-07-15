# UnityURP-MobileScreenSpacePlanarReflection
 ScreenSpacePlanarReflection(SSPR) as a reusable RendererFeature in URP.
 Can run within a few ms on most android mobile devices.
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
 Where are the important files?
-------------------
 There are only 3 important code files, all inside a folder "Assets \ _MobileSSPR \ ReusableCore".
 Other files are for demo only, not important.
 
 Can it run on mobile?
-------------------
 Tested on ~10 android devices, should be alright if your android device support OpenGLES3.2 / Vulkan.
 
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

 I can see some strength white/gray areas in reflection
 -------------------
 It is an expected artifact of screen space reflection, currently I am looking for a fast enough mobile solution to solve it.
 - see this for more detail: http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf
 
 Notes
 -------------------
This is just a quick "proof of concept" project to test if screen space reflection & compute shader can run on mobile correctly and fast enough.  
But URP has RendererFeature to allow  render code easily reuse, so you can try this SSPR in your own URP project very quickly(follow the above " How to try this in my own URP project?" section
 
 Editor
 -------------------
2019.4.3f1 LTS

Implementation reference
-------------------
- http://remi-genin.fr/blog/screen-space-plane-indexed-reflection-in-ghost-recon-wildlands/
- http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf

TODO
----------------
- fix reflection result's holes (http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf)
- make a new .hlsl to allow user apply this SSPR's result to their custom shader using 2 lines of code 
 
 
