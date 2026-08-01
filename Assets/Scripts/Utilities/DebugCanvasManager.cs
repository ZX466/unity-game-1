using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DebugCanvasManager : MonoBehaviour
{
    [Header("General References")]
    public Canvas debugWindow;
    public GameObject fpsDisplay;
    public GameObject postProcessingVolumes;

    [System.Serializable]
    public struct UniversalAssetSettings
    {
        public string qualityName;
        public UniversalRenderPipelineAsset qualityRenderPipelineAsset;
        public GameObject qualitySelectedDisplay;
    }

    [Header("LWRP Asset Switch Settings")]
    public UniversalAssetSettings[] lwrpAssetSettings;
    private int currentURPAssetID = 2; // High
    public Text currentURPAssetInfo;

    void Start()
    {
        SwitchLWRPAsset(currentURPAssetID);
        HideUI();
    }

    void HideUI()
    {
        debugWindow.enabled = false;
        fpsDisplay.SetActive(false);
    }

    public void ToggleDebugWindow(bool newState)
    {
        debugWindow.enabled = newState;
    }

    public void ToggleFPSDisplay(bool newState)
    {
        fpsDisplay.SetActive(newState);
    }

    public void TogglePostProcessing(bool newState)
    {
        postProcessingVolumes.SetActive(newState);
    }

    public void SwitchLWRPAsset(int newAssetID)
    {

        GraphicsSettings.renderPipelineAsset = lwrpAssetSettings[newAssetID].qualityRenderPipelineAsset;
        UpdateLWRPAssetUI(newAssetID);

    }

    void UpdateLWRPAssetUI(int newAssetID)
    {

        lwrpAssetSettings[currentURPAssetID].qualitySelectedDisplay.SetActive(false);
        currentURPAssetID = newAssetID;
        lwrpAssetSettings[currentURPAssetID].qualitySelectedDisplay.SetActive(true);


        UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;

        string assetInfoString =
            "HDR:   " + asset.supportsHDR + "\n" +
            "Cast Shadows:  " + asset.supportsMainLightShadows + "\n" +
            "Soft Shadows:  " + asset.supportsSoftShadows;

        currentURPAssetInfo.text = assetInfoString;

    }


    
}
