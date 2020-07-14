using System.Collections.Generic;
using UnityEngine;

public class ExampleSceneScript_MobileSSPR : MonoBehaviour
{
    public List<Material> skyboxs = new List<Material>();
    private void LateUpdate()
    {
        //rotate camera around scene
        transform.RotateAround(Vector3.zero, Vector3.up, 22.5f * Time.deltaTime);
    }
    int skyBoxIndex = 0;
    private void OnGUI()
    {
        //show an On/OFF toggle, to check rendering ms difference
        MobileSSPRRendererFeature.instance.SetActive(GUI.Toggle(new Rect(400, 25, 400, 400), MobileSSPRRendererFeature.instance.isActive, "SSPR on"));
        //show slider to control SSPR RT size
        MobileSSPRRendererFeature.instance.Settings.RT_height = (int)(GUI.HorizontalSlider(new Rect(800, 25, 200, 200), MobileSSPRRendererFeature.instance.Settings.RT_height, 32,1080));
        //view SSPR in different envi
        if (GUI.Button(new Rect(25, 200, 100, 100), "SwitchSkyBox"))
        {
            RenderSettings.skybox = skyboxs[(++skyBoxIndex)%skyboxs.Count];
        }

        GUI.Label(new Rect(25, 150, 100, 100), (int)(Time.smoothDeltaTime * 1000) + "ms", new GUIStyle() { fontSize = 20 } );
    }
}
