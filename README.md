# UnityURP-MobileScreenSpacePlanarReflection
 A simple and fast ScreenSpacePlanarReflection(SSPR) as a standalone reusable RendererFeature in URP.  
 Can run on PC/console/vulkan android, other platforms not tested but should work if compute shader is supported. 
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
 
On Adreno630 GPU android mobile device(almost all 2018/2019 flagship android mobiles), Toggle SSPR ON/OFF:
 - cost <1ms to update 128 height SSPR RT
 - cost <1ms to update 256 height SSPR RT
 - cost 1~2ms to update 512 height SSPR RT  
 
On Adreno612 GPU android mobile device(Samsung Galaxy A70), Toggle SSPR ON/OFF:
 - cost <1ms to update 128 height SSPR RT
 - cost 1~2ms to update 256 height SSPR RT
 - cost 4~5ms to update 512 height SSPR RT
 
 On Adreno506 GPU android mobile device(Lenovo S5), Toggle SSPR ON/OFF:
 - cost ~1ms to update 128 height SSPR RT
 - cost 3~4ms to update 256 height SSPR RT
 - cost 8~9ms to update 512 height SSPR RT
 
 Where are the important files?
-------------------
 There are only 4 important code files, all inside a folder "Assets \ _MobileSSPR \ ReusableCore".  
https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection/tree/master/Assets/_MobileSSPR/ReusableCore  
 Other files are for demo only, not important.
 
 Can it run on mobile?
-------------------
 Tested on ~10 android devices(all support Vulkan).
 If your android device support Vulkan, result should be correct and rendering should be fast enough. (OpenGLES3.2 is not enough, must support Vulkan!)
   
 *We have received a report that this SSPR is not working on MaliT760 GPU android (Galaxy S6), but we don't have this device to reproduce it
 
 How to try this inside my own URP project?
 -------------------
 - copy "Assets \ _MobileSSPR \ ReusableCore" folder to your project (contains 4 important code files)
 - turn on "Depth Texture" in all your project's URP's setting
 - turn on "Opaque Texture" in all your project's URP's setting
 - Add MobileSSPRRendererFeature(RendererFeature) to your forward renderer asset
   
 - create a new plane game object in your scene (set world space pos y = 0.01)
 - create a material using MobileSSPRExampleShader.shader
 - assign this material to your new plane
   
 - DONE! You should see correct reflection both in scene and game window

 I can see some small flickering in reflection / can't see any reflection
 -------------------
It is not expected! Please report your device name in Issues, thanks!
 
 Notes
 -------------------  
It is not safe to use InterlockedMin() and RenderTexture color format "RInt" on android/iOS compute shader(see -> https://zhuanlan.zhihu.com/p/150890059). 
Instead, we will use RenderTexture color format RFloat / ARGBHalf to produce similar result.
 
 Editor
 -------------------
2020.3.33f1 LTS

Implementation reference
-------------------
- http://remi-genin.fr/blog/screen-space-plane-indexed-reflection-in-ghost-recon-wildlands/
- http://advances.realtimerendering.com/s2017/PixelProjectedReflectionsAC_v_1.92_withNotes.pdf
- https://zhuanlan.zhihu.com/p/150890059
- https://github.com/Steven-Cannavan/URP_ScreenSpacePlanarReflections
- UE4 source - PostProcessPixelProjectedReflectionMobile.usf (UE4 4.26)

Change log
-------------------
- 2020-08-23: add iOS/OSX support (with the help of MusouCrow)
- 2022-05-02: upgrade project to Unity2020.3.33f1, merged a bug fix in MobileSSPRComputeShader.compute
