
// ##################################################
//              Photo Manager / 照片管理器
// ##################################################

using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class PhotoManager : MonoBehaviour, IInitializable 
{
    public static PhotoManager instance;

    public event Action<bool, WebCamTexture> OnCameraShow;
    public event Action<Texture2D> OnPhotoCaptured;

    private WebCamTexture webCamTexture;
    [SerializeField] private int captureCountdownTime = 5;

    [SerializeField] private Texture2D capturedPhoto; // 拍照暫存

    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }


    // ========== 初始化 ==========
    public void Initialize(Action callback)
    {
        // 檢查是否有可用的攝影機
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("[ERROR] No available camera devices found.");
            callback?.Invoke();
            return;
        }
        else
        {
            Debug.Log("[INFO] Photo Manager: Found available camera devices.");
        }
        webCamTexture = new WebCamTexture();
        callback?.Invoke();
    }


    // ========== 公開方法 ==========
    public IEnumerator OpenCamera()
    {
        webCamTexture.Play();
        while (!webCamTexture.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }
        OnCameraShow?.Invoke(true, webCamTexture);
    }

    public void CloseCamera()
    {
        webCamTexture.Stop();
        OnCameraShow?.Invoke(false, null);
    }

    /// <summary>拍照</summary>
    public void CapturePhoto()
    {
        capturedPhoto = new Texture2D(webCamTexture.width, webCamTexture.height);
        StartCoroutine(CaptureCountdownTimer(() => {
            capturedPhoto.SetPixels(webCamTexture.GetPixels());
            capturedPhoto.Apply();
            CloseCamera();
            OnPhotoCaptured?.Invoke(capturedPhoto);
        }));
    }

    public byte[] GetCapturedPhoto()
    {
        return capturedPhoto.EncodeToPNG();
    }

    /// <summary>保存圖片</summary>
    /// <param name="type">類型</param>
    /// <param name="photo">圖片</param>
    public void SavePhoto(string type, byte[] photo)
    {
        string directoryPath = Path.Combine(Application.dataPath, "Data", DateTime.Now.ToString("yyyyMMdd"), $"{type}Photos");
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        string filePath = Path.Combine(directoryPath, DateTime.Now.ToString("HHmmss") + ".png");
        File.WriteAllBytes(filePath, photo);
    }


    // ========== 輔助用方法 ==========
    /// <summary>拍照倒數計時器</summary>
    /// <param name="callback">計時結束後的回調</param>
    private IEnumerator CaptureCountdownTimer(Action callback)
    {
        for (int i = captureCountdownTime; i >= 0; i--)
        {
            yield return new WaitForSeconds(1);
            if (i == 0) callback?.Invoke();
        }
    }

    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
} 