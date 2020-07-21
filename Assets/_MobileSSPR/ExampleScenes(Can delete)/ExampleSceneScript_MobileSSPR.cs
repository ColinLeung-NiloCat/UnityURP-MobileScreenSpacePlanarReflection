//see README here: https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection

using System.Collections.Generic;
using UnityEngine;

public class ExampleSceneScript_MobileSSPR : MonoBehaviour
{
    public float rotationSpeed = 22.5f;
    public List<Material> skyboxs = new List<Material>();

    private void LateUpdate()
    {
        //rotate camera around scene
        transform.RotateAround(Vector3.zero, Vector3.up, rotationSpeed * Time.deltaTime);
    }
    int skyBoxIndex = 0;
    private void OnGUI()
    {
        GUI.contentColor = Color.black;
        //show an On/OFF toggle, to check rendering SSPR_RT alone's net ms difference
        MobileSSPRRendererFeature.instance.Settings.ShouldRenderSSPR = (GUI.Toggle(new Rect(200, 25, 100, 100), MobileSSPRRendererFeature.instance.Settings.ShouldRenderSSPR, "SSPR on"));

        //show slider to control SSPR performance settings
        GUI.Label(new Rect(350, 25, 200, 25), $"ColorRT Height = {MobileSSPRRendererFeature.instance.Settings.RT_height}");
        MobileSSPRRendererFeature.instance.Settings.RT_height = (int)(GUI.HorizontalSlider(new Rect(550, 25, 200, 25), MobileSSPRRendererFeature.instance.Settings.RT_height/128, 1,8))*128;

        MobileSSPRRendererFeature.instance.Settings.ApplyFillHoleFix = (GUI.Toggle(new Rect(550, 225, 200, 25), MobileSSPRRendererFeature.instance.Settings.ApplyFillHoleFix,"Apply Fill Hole Fix"));

        //view SSPR's result using different skyboxs
        if (GUI.Button(new Rect(200, 200, 100, 100), "SwitchSkyBox"))
        {
            RenderSettings.skybox = skyboxs[(skyBoxIndex++)%skyboxs.Count];
        }

        GUI.Label(new Rect(200, 150, 100, 100), $"{(int)(Time.smoothDeltaTime * 1000)} ms ({ Mathf.CeilToInt(1f/Time.smoothDeltaTime)}fps)", new GUIStyle() { fontSize = 30 } );

        GUI.Label(new Rect(850, 25, 200, 25), $"Rotate speed = {(int)rotationSpeed}");
        rotationSpeed = (int)(GUI.HorizontalSlider(new Rect(1000, 25, 200, 25), rotationSpeed, 0, 45));
    }
}
