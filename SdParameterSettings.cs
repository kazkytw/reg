
// ##################################################
//         Sd Parameter Settings / SD 參數設定
// ##################################################

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class SdParameterSettings : MonoBehaviour, IInitializable
{
    public static SdParameterSettings instance;

    [Header("選項 / Options")]
    [SerializeField] private string currentGender;
    [SerializeField] private string currentAge;
    [SerializeField] private string currentProfession;

    private SdParameters baseSettings_t2i;
    private SdParameters baseSettings_i2i;
    private CategoryOptions presets;

    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }

    public void Initialize(Action callback)
    {
        LoadPresets();
        callback?.Invoke();
    }

    // ========== 公開方法 ==========

    public string[] GetAvailableGenders() => presets.gender.Keys.ToArray();
    public string[] GetAvailableAges() => presets.age.Keys.ToArray();
    public string[] GetAvailableProfessions() => presets.profession.Keys.ToArray();

    public void SetGender(string gender)
    {
        if (presets.gender.ContainsKey(gender))
        {
            currentGender = gender;
            Debug.Log($"[INFO] Set Gender: {gender}");
        }
    }

    public void SetAge(string age)
    {
        if (presets.age.ContainsKey(age))
        {
            currentAge = age;
            Debug.Log($"[INFO] Set Age: {age}");
        }
    }

    public void SetProfession(string profession)
    {
        if (presets.profession.ContainsKey(profession))
        {
            currentProfession = profession;
            Debug.Log($"[INFO] Set Profession: {profession}");
        }
    }

    /// <summary>獲取最終資料包</summary>
    /// <param name="mode">模式</param>
    /// <param name="importImage">輸入圖片</param>
    /// <returns>最終資料包</returns>
    public Dictionary<string, object> GetFinalPayload(string mode, byte[] importImage)
    {
        SdParameters parameters = AddOptionsParameters(mode);
        parameters = AddImage(mode, parameters, importImage);
        Dictionary<string, object> payload = ParametersChangeIntoPayload(mode, parameters);

        return payload;
    }


    // ========== 主方法 ==========

    /// <summary>加載預設參數集</summary>
    private void LoadPresets()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("sd_presets");
        if (jsonFile != null)
        {
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonFile.text);

            var baseSettings_t2i_Json = JsonConvert.SerializeObject(jsonData["baseSettings_t2i"]);
            var baseSettings_i2i_Json = JsonConvert.SerializeObject(jsonData["baseSettings_i2i"]);
            var presetsJson = JsonConvert.SerializeObject(jsonData);

            baseSettings_t2i = JsonConvert.DeserializeObject<SdParameters>(baseSettings_t2i_Json);
            baseSettings_i2i = JsonConvert.DeserializeObject<SdParameters>(baseSettings_i2i_Json);
            presets = JsonConvert.DeserializeObject<CategoryOptions>(presetsJson);

            Debug.Log("[INFO] SD Parameter Settings: Load Presets Success.");
        }
        else
        {
            Debug.LogError("[ERROR] SD Parameter Settings: Failed to load preset parameters file!");
        }
    }

    /// <summary>添加選項參數</summary>
    /// <returns>添加選項後的參數</returns>
    private SdParameters AddOptionsParameters(string mode)
    {
        // 匯入基礎參數
        SdParameters parameters = mode == "t2i" ? baseSettings_t2i : baseSettings_i2i;

        // 添加互動選項參數
        parameters.prompt += ", " + string.Join(", ", new List<string> 
        {
            presets.gender[currentGender],
            presets.age[currentAge], 
            presets.profession[currentProfession]
        });

        Debug.Log($"[INFO] Parameters imported.");
        
        return parameters;
    }

    /// <summary>添加圖片</summary>
    /// <param name="mode">模式</param>
    /// <param name="parameters">參數</param>
    /// <param name="image">圖片</param>
    /// <returns>添加圖片後的參數</returns>
    private SdParameters AddImage(string mode, SdParameters parameters, byte[] image)
    {
        if (mode == "t2i") parameters.imageForT2IRoop = Convert.ToBase64String(image);
        else parameters.imageForI2I = Convert.ToBase64String(image);

        Debug.Log($"[INFO] Image imported.");
        
        return parameters;
    }

    /// <summary>將參數轉換為資料包</summary>
    /// <param name="mode">模式</param>
    /// <param name="importParameters">輸入參數</param>
    /// <returns>資料包</returns>
    private Dictionary<string, object> ParametersChangeIntoPayload(string mode, SdParameters importParameters)
    {
        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { "sd_model_name", importParameters.sd_model_name },
            { "prompt", importParameters.prompt },
            { "negative_prompt", importParameters.negative_prompt },
            { "steps", importParameters.steps },
            { "width", importParameters.width },
            { "height", importParameters.height },
            { "cfgScale", importParameters.cfgScale },
        };
        
        // 一階 roop 綁臉參數
        if (mode == "t2i")
        {
            payload.Add("alwayson_scripts", new Dictionary<string, object>
            {
                {
                    "roop", new Dictionary<string, object>
                    {
                        { "args", new object[]
                            {
                                importParameters.imageForT2IRoop,       // Import Image, 要綁臉的圖片  
                                importParameters.enable,                // Enable
                                importParameters.faces_index,           // Faces Index
                                importParameters.roop_model,            // Model
                                importParameters.restore_face,          // Restore Face
                                importParameters.restore_visibility,    // Restore Visibility
                                importParameters.upscaler,              // Upscaler
                                importParameters.upscaler_scale,        // Upscaler Scale
                                importParameters.upscaler_visibility,   // Upscaler Visibility
                                false,                                  // Swap in source image
                                true                                    // Swap in generated image
                            }
                        }
                    }
                }
            });
        }
        else // 二階圖生圖參數
        {
            payload.Add("denoising_strength", importParameters.denoising_strength);
            payload.Add("init_images", new string[] { importParameters.imageForI2I });
        }

        LogParameters(mode, importParameters);
        return payload;
    }


    // ========== 輔助方法 ==========

    /// <summary>印出參數</summary>
    private void LogParameters(string mode, SdParameters parameters)
    {
        if (mode == "t2i")
        {
            Debug.Log($"[INFO] MODE: {mode} \n" +
                      $"===SD PARAMETERS===\n" +
                      $"Model name: {parameters.sd_model_name}\n" +
                      $"Prompt: {parameters.prompt}\n" + 
                      $"Negative prompt: {parameters.negative_prompt}\n" +
                      $"CFG scale: {parameters.cfgScale}\n" +
                      $"===ROOP PARAMETERS===\n" +
                      $"Roop enable: {parameters.enable}\n" +
                      $"Lock face image: {(parameters.imageForT2IRoop != null ? "[Captured image]" : "N/A")}\n" +
                      $"Restore visibility: {parameters.restore_visibility}\n");
        }
        else
        {
            Debug.Log($"[INFO] MODE: {mode} \n" +
                      $"===SD PARAMETERS===\n" +
                      $"Model name: {parameters.sd_model_name}\n" +
                      $"Prompt: {parameters.prompt}\n" + 
                      $"Negative prompt: {parameters.negative_prompt}\n" +
                      $"CFG scale: {parameters.cfgScale}\n" +
                      $"Denoising strength: {parameters.denoising_strength}\n" +
                      $"Import image: {(parameters.imageForI2I != null ? "[Image from t2i]" : "N/A")}\n");
        }
    }

    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
} 

// ========== SD 參數 ==========
[System.Serializable]
public class SdParameters
{
    public string sd_model_name;
    public string prompt;
    public string negative_prompt;
    public int steps;
    public int width;
    public int height;
    public float cfgScale;
    public float denoising_strength;

    public string imageForI2I; // 一階生的圖, 進二階圖生圖

    // Roop 參數
    public string imageForT2IRoop; // 一階要綁臉的圖
    public bool enable;
    public int faces_index;
    public string roop_model;
    public string restore_face;
    public float restore_visibility;
    public string upscaler;
    public float upscaler_scale;
    public float upscaler_visibility;
}

// ========== 選項 ==========
[System.Serializable]
public class CategoryOptions
{
    public Dictionary<string, string> gender;
    public Dictionary<string, string> age;
    public Dictionary<string, string> profession;
}