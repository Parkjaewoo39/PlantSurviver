using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Storage;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //싱글톤
    public static GameManager Ins { get; private set; }

    private void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
            //DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 방지 (필요시)
        }
        else if (Ins != this)
        {
            Destroy(gameObject); // 중복 인스턴스 방지
        }
        DataClient.InsFirst(); // DataM 인스턴스 초기화
    }


    public void Save()
    {
        if (PlayfabManager.Ins != null)
        {
            // 플레이어 데이터를 저장
            //PlayfabManager.Ins.Save(); 일괄 저장 이제 필요 없음
            PlayfabManager.Ins.PlayerData.Level++;
            PlayfabManager.Ins.PlayerData.Exp += 10; // 예시로 경험치 100 추가
        }
        else
        {
            Debug.LogWarning("PlayfabManager 또는 playerSaveData가 초기화되지 않았습니다.");
        }
    }


}


