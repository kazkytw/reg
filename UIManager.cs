
// ##################################################
//              UI Manager / 介面管理器
// ##################################################

using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;


public class UIManager : MonoBehaviour, IInitializable
{
    public static UIManager instance;

    [SerializeField] private GameObject currentPanel;
    [SerializeField] private float fadeTime = 1f;

    // --- Loading ---
    [SerializeField] private GameObject panel_loading;

    // --- Start Menu ---
    [SerializeField] private GameObject panel_startMenu;
    [SerializeField] private Button btn_start;

    // --- Options ---
    [SerializeField] private GameObject panel_options;
    [SerializeField] private Button btn_male;
    [SerializeField] private Button btn_female;
    [SerializeField] private Button btn_teenager;
    [SerializeField] private Button btn_adult;
    [SerializeField] private Button btn_elderly;

    // --- Take Photo ---
    [SerializeField] private GameObject panel_takePhoto;
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private Button btn_shot;

    // --- Photo Confirm ---
    [SerializeField] private GameObject panel_photoConfirm;
    [SerializeField] private RawImage shotPhoto;
    [SerializeField] private Button btn_confirm;
    [SerializeField] private Button btn_retry;

    // --- Result ---
    [SerializeField] private GameObject panel_result;
    [SerializeField] private Button btn_finish;
    [SerializeField] private RawImage resultPhotoT2I;
    [SerializeField] private RawImage resultPhotoI2I;
    [SerializeField] private RawImage qrCodeDisplay;


    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }


    // ========== 初始化 ==========
    public void Initialize(Action callback)
    {
        currentPanel = panel_loading;
        DOTween.Init();  // 初始化 DOTween

        BtnAddEventListeners();
        OnlyShowRequiredUI();
        AddEventListeners();

        Debug.Log("[INFO] UI Manager: UI Initialized.");
        callback?.Invoke();
    }

    private void OnlyShowRequiredUI() {
        // --- Btn ---
        ShowAllOptionButton(false);

        // --- Img ---
        ShowImg("Shot", false, null);
        ShowImg("Result_T2I", false, null);
        ShowImg("Result_I2I", false, null);
        ShowImg("QRCode", false, null);

        // --- Panel ---
        ShowPanel(panel_loading, true);
        ShowPanel(panel_startMenu, false);
        ShowPanel(panel_options, false);
        ShowPanel(panel_takePhoto, false);
        ShowPanel(panel_photoConfirm, false);
        ShowPanel(panel_result, false);
    }


    // ========== 按鈕動作與流程切換 ==========
    private void BtnAddEventListeners()
    {
        // --- Start Menu ---
        btn_start.onClick.AddListener(() => 
        {
            SetSystemState(SystemManager.SystemState.Options);
            ChangeOptions("gender");
        });

        // --- Options ---
            // --- Gender ---
            btn_male.onClick.AddListener(() => 
            {
                SdParameterSettings.instance.SetGender("male");
                ChangeOptions("age");
            });
            btn_female.onClick.AddListener(() => 
            {
                SdParameterSettings.instance.SetGender("female");
                ChangeOptions("age");
            });

            // --- Age ---
            btn_teenager.onClick.AddListener(() => 
            {
                SdParameterSettings.instance.SetAge("teen");
                SetSystemState(SystemManager.SystemState.TakePhoto);
            });
            btn_adult.onClick.AddListener(() => 
            {
                SdParameterSettings.instance.SetAge("adult");
                SetSystemState(SystemManager.SystemState.TakePhoto);
            });
            btn_elderly.onClick.AddListener(() => 
            {
                SdParameterSettings.instance.SetAge("elderly");
                SetSystemState(SystemManager.SystemState.TakePhoto);
            });

        // --- Take Photo ---
        btn_shot.onClick.AddListener(() => {
            PhotoManager.instance.CapturePhoto();
            EnableButton(btn_shot, false);
        });

        // --- Photo Confirm ---
        btn_confirm.onClick.AddListener(() => SetSystemState(SystemManager.SystemState.SDGenerating));
        btn_retry.onClick.AddListener(() => SetSystemState(SystemManager.SystemState.TakePhoto));

        // --- Result ---
        btn_finish.onClick.AddListener(() => {
            SetSystemState(SystemManager.SystemState.StartMenu);
            Debug.Log("[INFO] Interaction Finished.");
        });
    }


    // ========== 主方法 ==========

    private void OnStartMenu()
    {
        SwitchPanelWithFade(panel_startMenu);
        BtnScaleFadeIn(btn_start);
    }

    private void OnTest(byte[] resultPhotoT2I, byte[] resultPhotoI2I, Texture2D qrcode)
    {
        SwitchPanelWithFade(panel_result);
        Texture2D resultPhotoT2D = ChangeToTexture2D(resultPhotoT2I);
        Texture2D resultPhotoI2D = ChangeToTexture2D(resultPhotoI2I);
        ShowImg("Result_T2I", true, resultPhotoT2D);
        ShowImg("Result_I2I", true, resultPhotoI2D);
        ShowImg("QRCode", true, qrcode);
    }   

    private void OnInteractionStart()
    {
    }

    private void OnOptions()
    {
        SwitchPanelWithFade(panel_options);
        BtnScaleFadeIn(btn_male);
        BtnScaleFadeIn(btn_female);
    }

    private void OnTakePhoto()
    {
        SwitchPanelWithFade(panel_takePhoto);
        EnableButton(btn_shot, true);
        BtnScaleFadeIn(btn_shot);
    }

    private void OnPhotoConfirm(Texture2D shotPhoto)
    {
        SwitchPanelWithFade(panel_photoConfirm);
        ShowImg("Shot", true, shotPhoto);
        BtnScaleFadeIn(btn_confirm);
        BtnScaleFadeIn(btn_retry);
    }

    private void OnSDGenerating()
    {
        SwitchPanelWithFade(panel_loading);
    }

    private void OnResult(byte[] resultPhoto, Texture2D qrcode)
    {
        SwitchPanelWithFade(panel_result);
        Texture2D resultPhotoT2D = ChangeToTexture2D(resultPhoto);
        ShowImg("Result_I2I", true, resultPhotoT2D);
        ShowImg("QRCode", true, qrcode);
        BtnScaleFadeIn(btn_finish);
    }


    // ========== 顯示型輔助方法 ==========

    /// <summary>直接顯示 / 隱藏面板</summary>
    /// <param name="panel">指定之面板</param>
    /// <param name="isShow">是否顯示</param>
    private void ShowPanel(GameObject panel, bool isShow) {
        panel.gameObject.SetActive(isShow);
    }

    /// <summary>顯示 / 隱藏按鈕</summary>
    /// <param name="button">指定之按鈕</param>
    /// <param name="isShow">是否顯示</param>
    private void ShowButton(Button button, bool isShow)
    {
        button.gameObject.SetActive(isShow);
    }

    /// <summary>顯示 / 隱藏所有選項按鈕</summary>
    /// <param name="isShow">是否顯示</param>
    private void ShowAllOptionButton(bool isShow)
    {
        ShowButton(btn_male, isShow);
        ShowButton(btn_female, isShow);

        ShowButton(btn_teenager, isShow);
        ShowButton(btn_adult, isShow);
        ShowButton(btn_elderly, isShow);
    }

    /// <summary>顯示 / 隱藏相機預覽</summary>
    /// <param name="isShow">是否顯示</param>
    /// <param name="texture">相機預覽</param>
    private void ShowCameraPreview(bool isShow, WebCamTexture texture)
    {
        cameraPreview.gameObject.SetActive(isShow);
        if (isShow) cameraPreview.texture = texture;
    }

    /// <summary>顯示 / 隱藏生成結果</summary>
    /// <param name="type">結果類型</param>
    /// <param name="isShow">是否顯示</param>
    /// <param name="texture">結果圖片 (Texture2D)</param>
    private void ShowImg(string type, bool isShow, Texture2D texture)
    {
        if (type == "Shot") {
            shotPhoto.gameObject.SetActive(isShow);
            if (isShow) shotPhoto.texture = texture;
        }
        else if (type == "Result_T2I") {
            resultPhotoT2I.gameObject.SetActive(isShow);
            if (isShow) resultPhotoT2I.texture = texture;
        }
        else if (type == "Result_I2I") {
            resultPhotoI2I.gameObject.SetActive(isShow);
            if (isShow) resultPhotoI2I.texture = texture;
        }
        else if (type == "QRCode") {
            qrCodeDisplay.gameObject.SetActive(isShow);
            if (isShow) qrCodeDisplay.texture = texture;
        }
    }

    /// <summary>淡入 / 淡出切換面板</summary>
    /// <param name="targetPanel">欲切換之面板</param>
    private void SwitchPanelWithFade(GameObject targetPanel)
    {
        targetPanel.gameObject.SetActive(true);
        targetPanel.GetComponent<CanvasGroup>().alpha = 0;
        targetPanel.GetComponent<CanvasGroup>().DOFade(1, fadeTime);
        if (currentPanel != null) {
            currentPanel.GetComponent<CanvasGroup>().DOFade(0, fadeTime).OnComplete(() => {
                currentPanel.gameObject.SetActive(false);
                currentPanel = targetPanel;
            });
        }
        else currentPanel = targetPanel;
    }

    /// <summary>按鈕淡入 (超過後彈回)</summary>
    /// <param name="button">指定之按鈕</param>
    private void BtnScaleFadeIn(Button button)
    {
        button.transform.DOScale(0f, 0f);
        button.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }
    

    // ========== 功能型輔助方法 ==========
    private void SetSystemState(SystemManager.SystemState state, object arg = null)
    {
        SystemManager.instance.SetState(state, arg);
    }

    /// <summary>啟用/禁用按鈕</summary>
    /// <param name="button">指定之按鈕</param>
    /// <param name="isEnable">是否啟用</param>
    private void EnableButton(Button button, bool isEnable)
    {
        button.interactable = isEnable;
    }

    /// <summary>切換選項</summary>
    /// <param name="type">選項類型</param>
    private void ChangeOptions(string type)
    {
        ShowAllOptionButton(false);
        if (type == "gender") {
            ShowButton(btn_male, true);
            ShowButton(btn_female, true);
        }
        else if (type == "age") {
            ShowButton(btn_teenager, true);
            ShowButton(btn_adult, true);
            ShowButton(btn_elderly, true);
        }
        //else if (type == "profession") {
        //}
    }

    private Texture2D ChangeToTexture2D(byte[] imageBytes)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);
        return texture;
    }

    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }


    // ========== 事件接收 ==========
    private void AddEventListeners()
    {
        SystemManager.instance.OnStartMenu += OnStartMenu;
        SystemManager.instance.OnTest += OnTest;
        SystemManager.instance.OnInteractionStart += OnInteractionStart;
        SystemManager.instance.OnOptions += OnOptions;
        SystemManager.instance.OnTakePhoto += OnTakePhoto;
        SystemManager.instance.OnPhotoConfirm += OnPhotoConfirm;
        SystemManager.instance.OnSDGenerating += OnSDGenerating;
        SystemManager.instance.OnResult += OnResult;

        PhotoManager.instance.OnCameraShow += ShowCameraPreview;
    }
}