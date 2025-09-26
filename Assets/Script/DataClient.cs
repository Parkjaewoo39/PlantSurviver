using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "DataM", menuName = "Scriptable Objects/DataM")]
public class DataClient : ScriptableObject
{

    //클라이언트에서만 관리하는 데이터를 이곳에서


    #region// Singleton by Addressables
    private static DataClient instance;
    public static DataClient Ins
    {
        get
        {
            if (instance == null)
            {
                Addressables.LoadAssetAsync<DataClient>("DataM").Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        instance = handle.Result;
                    }
                    else
                    {
                        Debug.LogError("Failed to load DataM instance from Addressables.");
                    }
                };
            }
            return instance;
        }
    }
    public static void InsFirst()
    {
        if (instance == null)
        {
            Addressables.LoadAssetAsync<DataClient>("DataM").Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    instance = handle.Result;
                }
                else
                {
                    Debug.LogError("Failed to load DataM instance from Addressables.");
                }
            };
        }
    }
    #endregion



    //데이터 관리 ex)  DataM.Ins.testData1[1].name     DataM.Ins.testData1[1].name
    public List<testData> testData0;
    public List<testData> testData1;
    public List<testData2> testData3;
}

[System.Serializable]
public class testData
{
    public ObscuredString name;
    public ObscuredFloat atk;
    public ObscuredFloat hp;
    public ObscuredInt levelToExp;
}

[System.Serializable]
public class testData2
{
    public ObscuredString name;
    public ObscuredFloat atk;
    public ObscuredFloat hp;
    public ObscuredInt levelToExp;
}

