
// ##################################################
//          System Initializer / 系統初始化器
// ##################################################

using System;
using System.Collections.Generic;
using UnityEngine;

public class SystemInitializer : MonoBehaviour
{
    public static SystemInitializer instance;

    private List<IInitializable> modulesToInitialize = new List<IInitializable>();
   
    // ========== ASU ==========
    void Awake()
    {
        SetInstance();
    }


    // ========== 請在此處添加需要初始化的模組 ==========
    public void InitializeModules(Action callback)
    {
        modulesToInitialize.Add(UIManager.instance);
        modulesToInitialize.Add(SdParameterSettings.instance);
        modulesToInitialize.Add(PhotoManager.instance);

        StartInitializeModules(callback);
    }


    // ========== 主方法 ==========
    private void StartInitializeModules(Action callback)
    {
        int completedCount = 0;
        int totalCount = modulesToInitialize.Count;
        foreach (var module in modulesToInitialize)
        {
            module.Initialize(() => {
                completedCount++;
                if (completedCount == totalCount)
                {
                    Debug.Log("[SYSTEM] All modules initialization completed.");
                    callback?.Invoke();
                }
            });
        }
    }


    // ========== 輔助用方法 ==========
    private void SetInstance()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
}

public interface IInitializable
{
    void Initialize(Action callback);
}