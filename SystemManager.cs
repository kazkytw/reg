// ##################################################
//  TODO: UI
// ##################################################


// ##################################################
//              System Manager / 系統管理器
// ##################################################

using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager instance;

    // ========== 變數 ==========
    public enum SystemState
    {
        Initialize,         // 系統初始化
        Test,               // 測試模式
        StartMenu,          // 開始選單
        InteractionStart,   // 互動開始
        Options,            // 各選項選擇
        TakePhoto,          // 拍照
        PhotoConfirm,       // 確認拍照結果
        SDGenerating,       // SD 生成圖片
        Result              // 顯示生成結果
    }
    [SerializeField] private SystemState currentState = SystemState.Initialize;


    // ========== 系統狀態事件 ==========
    #region System State Event
    public event Action OnInitialize;
    public event Action OnStartMenu;
    public event Action<byte[], byte[], Texture2D> OnTest;
    public event Action OnInteractionStart;
    public event Action OnOptions;
    public event Action OnTakePhoto;
    public event Action<Texture2D> OnPhotoConfirm;
    public event Action OnSDGenerating;
    public event Action<byte[], Texture2D> OnResult;
    #endregion


    // ========== 變數 ==========
    [SerializeField] private bool isTesting = true;
    [SerializeField] private string testImageIndex = "m1";
    private byte[] testImage_byte; // 測試圖片
    [SerializeField] private bool isUploading = true;
    

    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }

    void Start()
    {   
        SetState(SystemState.Initialize);
    }


    // ========== 公開方法 ==========

    /// <summary>設定系統狀態</summary>
    /// <param name="state">系統狀態</param>
    /// <param name="arg1">參數1</param>
    /// <param name="arg2">參數2</param>
    public void SetState(SystemState state, object arg1 = null, object arg2 = null)
    {
        currentState = state;

        switch (currentState)
        {
            case SystemState.Initialize:
                Initialize();
                break;
            case SystemState.StartMenu:
                StartMenu();
                break;
            case SystemState.Test:
                Test();
                break;
            case SystemState.InteractionStart:
                InteractionStart();
                break;
            case SystemState.Options:
                Options();
                break;
            case SystemState.TakePhoto:
                TakePhoto();
                break;
            case SystemState.PhotoConfirm:
                PhotoConfirm(arg1 as Texture2D);
                break;
            case SystemState.SDGenerating:
                SDGenerating();
                break;
            case SystemState.Result:
                Result(arg1 as byte[]);
                break;
        }
    }


    // ========== 流程主方法 ==========
    private void Initialize()
    {
        Debug.Log($"[SYSTEM] System Initializing...");
        if (isTesting) LoadTestImage();

        SystemInitializer.instance.InitializeModules(() => {
            if (isTesting) SetState(SystemState.Test);
            else SetState(SystemState.StartMenu);
        });

        PhotoManager.instance.OnPhotoCaptured += PhotoConfirm;
    }

    /// <summary>測試 SD 生成圖片</summary>
    private async void Test()
    {
        Debug.Log("[SYSTEM] In test mode.");
        byte[] resultImage_t2i = await SdConnecter.instance.TextToImg(testImage_byte);  // 一次文生圖
        byte[] resultImage_i2i = await SdConnecter.instance.ImgToImg(resultImage_t2i);   // 二次圖生圖
        Texture2D qrcode = null;
        if (isUploading) {
            qrcode = await UploadManager.instance.UploadPhotoAndGenerateQRCode(resultImage_i2i);
        }
        OnTest?.Invoke(resultImage_t2i, resultImage_i2i, qrcode);
    }

    private void StartMenu()
    {
        OnStartMenu?.Invoke();
    }

    private void InteractionStart()
    {
        Debug.Log($"[SYSTEM] Interaction Start.");
        SetState(SystemState.Options);
    }

    private void Options()
    {   
        OnOptions?.Invoke();
    }

    private void TakePhoto()
    {
        OnTakePhoto?.Invoke();
        StartCoroutine(PhotoManager.instance.OpenCamera());
    }

    private void PhotoConfirm(Texture2D shotPhoto)
    {
        OnPhotoConfirm?.Invoke(shotPhoto);
    }

    private async void SDGenerating()
    {
        OnSDGenerating?.Invoke();
        byte[] capturedPhoto = PhotoManager.instance.GetCapturedPhoto();
        byte[] resultImage_t2i = await SdConnecter.instance.TextToImg(capturedPhoto);  // 一次文生圖
        byte[] resultImage_i2i = await SdConnecter.instance.ImgToImg(resultImage_t2i);   // 二次圖生圖
        
        PhotoManager.instance.SavePhoto("Captured", capturedPhoto);
        PhotoManager.instance.SavePhoto("SDGenerated", resultImage_i2i);
        SetState(SystemState.Result, resultImage_i2i);
    }

    private async void Result(byte[] resultImage)
    {
        Texture2D qrcode = null;
        if (isUploading) {
            qrcode = await UploadManager.instance.UploadPhotoAndGenerateQRCode(resultImage);
        }
        OnResult?.Invoke(resultImage, qrcode);
    }


    // ========== 輔助方法 ==========
    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void LoadTestImage()
    {
        string testImagePath_png = System.IO.Path.Combine(Application.streamingAssetsPath, "TestImages", $"test_face_{testImageIndex}.png");
        string testImagePath_jpg = System.IO.Path.Combine(Application.streamingAssetsPath, "TestImages", $"test_face_{testImageIndex}.jpg");

        string imagePath;
        if (System.IO.File.Exists(testImagePath_png)) imagePath = testImagePath_png;
        else if (System.IO.File.Exists(testImagePath_jpg)) imagePath = testImagePath_jpg;
        else
        {
            Debug.LogError($"[ERROR] No test image found in StreamingAssets/TestImages folder");
            return;
        }

        testImage_byte = System.IO.File.ReadAllBytes(imagePath);
    }
}
