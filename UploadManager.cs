
// ##################################################
//           Upload Manager / 照片上傳管理器
// ##################################################

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityGoogleDrive;
using ZXing;
using ZXing.QrCode;

public class UploadManager : MonoBehaviour
{
    public static UploadManager instance;
    
    [Header("Google Drive Settings")]
    [SerializeField] private string targetFolderId; // 目標資料夾的ID
    

    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }


    // ========== 公開方法 ==========

    /// <summary>上傳圖片並生成 QR code</summary>
    /// <param name="imageBytes">圖片 byte array</param>
    public async Task<Texture2D> UploadPhotoAndGenerateQRCode(byte[] imageBytes) {
        string fileUrl = await UploadPhoto(imageBytes);
        if (fileUrl != null) {
            Texture2D qrCode = GenerateQRCode(fileUrl);
            return qrCode;
        }
        return null;
    }


    // ========== 主方法 ==========

    /// <summary>上傳圖片</summary>
    /// <param name="imageBytes">圖片 byte array</param>
    /// <returns>檔案連結</returns>
    private async Task<string> UploadPhoto(byte[] imageBytes)
    {
        try 
        {
            var file = new UnityGoogleDrive.Data.File
            {
                Name = $"Photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png",
                Content = imageBytes,
                MimeType = "image/png",
                Parents = new List<string> { targetFolderId }
            };

            var uploadedFile = await GoogleDriveFiles.Create(file).Send();

            if (uploadedFile != null)
            {
                Debug.Log($"[INFO] Success to upload image: {uploadedFile.Name}");
                
                // 先設定權限
                var permissionTcs = new TaskCompletionSource<bool>();
                StartCoroutine(SetPermissions(uploadedFile.Id, permissionTcs));
                bool permissionSet = await permissionTcs.Task;
                
                if (permissionSet)
                {
                    // 權限設定成功後獲取分享連結
                    string shareLink = $"https://drive.google.com/file/d/{uploadedFile.Id}/view?usp=sharing";
                    Debug.Log($"[INFO] Share link: {shareLink}");
                    return shareLink;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"上傳失敗: {ex.Message}");
            return null;
        }
    }

    /// <summary>設定權限</summary>
    /// <param name="fileId">檔案 ID</param>
    /// <param name="tcs">TaskCompletionSource<bool></param>
    private IEnumerator SetPermissions(string fileId, TaskCompletionSource<bool> tcs)
    {
        string endpoint = $"https://www.googleapis.com/drive/v3/files/{fileId}/permissions";
        string jsonData = "{\"role\": \"reader\", \"type\": \"anyone\"}";
        
        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + AuthController.AccessToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[INFO] Success to set permissions.");
                tcs.SetResult(true);
            }
            else
            {
                Debug.LogError($"[ERROR] Failed to set permissions: {request.error}");
                tcs.SetResult(false);
            }
        }
    }

    /// <summary>生成 QR code</summary>
    /// <param name="url">圖片連結</param>
    private Texture2D GenerateQRCode(string url)
    {
        try
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = 256,
                    Width = 256
                }
            };
            
            // 創建 texture
            Texture2D qrTexture = new Texture2D(256, 256);
            Color32[] pixels = writer.Write(url);
            qrTexture.SetPixels32(pixels);
            qrTexture.Apply();
            Debug.Log($"[INFO] Success to generate QR code.");
            return qrTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ERROR] Failed to generate QR code: {e.Message}");
            return null;
        }
    }


    // ========== 輔助方法 ==========
    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }


    // ========== 測試用方法 ==========
    private async Task<string> UploadPhotoTest() {
        string testImagePath = Path.Combine(Application.streamingAssetsPath, "TestImages", "test.png");
        byte[] testImageBytes = File.ReadAllBytes(testImagePath);
        string fileUrl = await UploadPhoto(testImageBytes);
        return fileUrl;
    }
}