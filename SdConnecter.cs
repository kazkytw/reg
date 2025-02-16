
// ##################################################
//              Sd Connecter / SD 介接器
// ##################################################

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

public class SdConnecter : MonoBehaviour
{
    public static SdConnecter instance;

    private string sdApiUrl = "http://127.0.0.1:7860";
    private readonly string txt2imgEndpoint = "/sdapi/v1/txt2img";
    private readonly string img2imgEndpoint = "/sdapi/v1/img2img";


    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }


    // ========== 公開方法 ==========

    /// <summary>文生圖</summary>
    /// <param name="importImage">輸入圖片</param>
    /// <returns>生成圖片</returns>
    public async Task<byte[]> TextToImg(byte[] importImage)
    {
        Debug.Log($"[SYSTEM] Generating Image... (First Stage: Text to Image with Roop)");
        Dictionary<string, object> payload = SdParameterSettings.instance.GetFinalPayload("t2i", importImage);
        byte[] resultImage = await SendRequest(txt2imgEndpoint, payload);

        return resultImage;
    }

    /// <summary>圖生圖</summary>
    /// <param name="importImage">輸入圖片</param>
    /// <returns>生成圖片</returns>
    public async Task<byte[]> ImgToImg(byte[] importImage)
    {
        Debug.Log($"[SYSTEM] Generating Image... (Second Stage: Image to Image)");
        Dictionary<string, object> payload = SdParameterSettings.instance.GetFinalPayload("i2i", importImage);
        byte[] resultImage = await SendRequest(img2imgEndpoint, payload);

        return resultImage;
    }


    // ========== 主方法 ==========

    /// <summary>向 SD API 發送請求</summary>
    private async Task<byte[]> SendRequest(string endpoint, Dictionary<string, object> payload)
    {
        var jsonString = JsonConvert.SerializeObject(payload);
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);

        string fullUrl = sdApiUrl + endpoint;

        using (var www = new UnityWebRequest(fullUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SdResponse>(www.downloadHandler.text);
                Debug.Log($"[INFO] Success to generate image.");

                // 將base64圖片轉換為Texture2D
                byte[] imageBytes = Convert.FromBase64String(response.images[0]);
                return imageBytes;
            }
            else
            {
                Debug.LogError($"[ERROR] Failed to generate image: {www.error}");
                return null;
            }
        }
    }


    // ========== 輔助方法 ==========
    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
}


// ========== SD 回應 ==========
[System.Serializable]
public class SdResponse
{
    public string[] images;
    public Dictionary<string, object> parameters;
    public string info;
}
