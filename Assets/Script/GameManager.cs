using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Storage;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //�̱���
    public static GameManager Ins { get; private set; }

    private void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
            //DontDestroyOnLoad(gameObject); // �� ��ȯ �� �ı� ���� (�ʿ��)
        }
        else if (Ins != this)
        {
            Destroy(gameObject); // �ߺ� �ν��Ͻ� ����
        }
        DataClient.InsFirst(); // DataM �ν��Ͻ� �ʱ�ȭ
    }


    public void Save()
    {
        if (PlayfabManager.Ins != null)
        {
            // �÷��̾� �����͸� ����
            //PlayfabManager.Ins.Save(); �ϰ� ���� ���� �ʿ� ����
            PlayfabManager.Ins.PlayerData.Level++;
            PlayfabManager.Ins.PlayerData.Exp += 10; // ���÷� ����ġ 100 �߰�
        }
        else
        {
            Debug.LogWarning("PlayfabManager �Ǵ� playerSaveData�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
        }
    }


}


