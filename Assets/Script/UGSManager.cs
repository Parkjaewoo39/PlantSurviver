using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;
//using Unity.Services.Authentication;
//using Unity.Services.Core;
//using Unity.Services.RemoteConfig;
//using Unity.Services.CloudSave; // Cloud Save 기능을 위한 네임스페이스 추가
using UnityEngine;
//using TMPro;
//using CodeStage.AntiCheat.Storage;
//using UnityEngine.SceneManagement;

public class UGSManager : MonoBehaviour
{
//    // 싱글톤 인스턴스
//    public static UGSManager Ins { get; private set; }

//    private void Awake()
//    {
//        if (Ins == null)
//        {
//            Ins = this;
//            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 방지
//        }
//        else if (Ins != this)
//        {
//            Destroy(gameObject); // 중복 인스턴스 방지
//        }
//    }

//    #region//플레이어 데이터
//    public PlayerSaveData playerSaveData = new PlayerSaveData();
//    public void InitSaveData(PlayerSaveData data)// 이게 최초에 초기화가 되어야 함
//    {
//        playerSaveData = data;
//    }
//    #endregion

//    //시작화면+ 계정관련 UI 및 관리 인스펙터
//    public TMP_InputField idInputField; // 계정 입력 필드
//    public const string lastIDSaveKey = "LastAccountKey"; // 마지막 계정 저장 키
//    public string nowID;

//    // Remote Config에서 사용자 속성 전달용 구조체 (필요시 확장 가능)
//    public struct userAttributes { }
//    // Remote Config에서 앱 속성 전달용 구조체 (필요시 확장 가능)
//    public struct appAttributes { }

//    // 게임 시작 시 마지막 계정 정보를 입력 필드에 표시
//    private void Start()
//    {
//        if (idInputField != null && ObscuredPrefs.HasKey(lastIDSaveKey))
//        {
//            idInputField.text = ObscuredPrefs.GetString(lastIDSaveKey);
//        }
//    }

//    // 버튼을 누르면 시작 (로그인 및 초기화)
//    public void OnStartButtonClicked()
//    {
//        // 인터넷 연결 체크 후, 없으면 경고 로그 출력
//        if (Utilities.CheckForInternetConnection())
//        {
//            _ = StartConfig();
//        }
//        else
//        {
//            Debug.LogWarning("인터넷 연결 없음: Remote Config 초기화 생략됨");
//        }

//        //메인 씬 이동
//        SceneManager.LoadScene(1);

//    }

//    // 로그인 처리: idInputField 값이 있으면 커스텀 로그인, 없으면 게스트
//    private async Task LoginOrGuestAsync()
//    {
//        string accountId = idInputField != null ? idInputField.text : string.Empty;
//        await UnityServices.InitializeAsync();

//        if (string.IsNullOrWhiteSpace(accountId))
//        {
//            await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            ObscuredPrefs.SetString(lastIDSaveKey, AuthenticationService.Instance.PlayerId);
//        }
//        else
//        {
//            // 커스텀 로그인 (SignInWithCustomIDAsync가 없으면 게스트로 대체)
//            try
//            {
//                // 최신 Unity Authentication SDK에서 지원하는 경우만 사용
//                // await AuthenticationService.Instance.SignInWithCustomIDAsync(accountId);
//                // PlayerPrefs.SetString(lastIDSaveKey, accountId);
//                // 만약 지원하지 않으면 익명 로그인만 사용
//                await AuthenticationService.Instance.SignInAnonymouslyAsync();
//                ObscuredPrefs.SetString(lastIDSaveKey, AuthenticationService.Instance.PlayerId);
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"로그인 실패: {e.Message}");
//            }
//        }
//        ObscuredPrefs.Save();
//    }

//    // UGS 및 Remote Config 초기화, 인증, 이벤트 등록 등 핵심 로직
//    async Task StartConfig()
//    {
//        try
//        {
//            await LoginOrGuestAsync();
//            Debug.Log($"현재 로그인 계정: {AuthenticationService.Instance.PlayerId}");

//            // [3] Remote Config Fetch 완료 이벤트 등록
//            RemoteConfigService.Instance.FetchCompleted += ApplyRemoteSettings;

//            // [4] Remote Config 서버에서 설정값 불러오기
//            RemoteConfigService.Instance.FetchConfigs(new userAttributes(), new appAttributes());

//            // [5] Cloud Save 초기화
//            InitSaveData(await SaveManager.LoadAsync());//로드
//            Save();//로드 후 최초세이브

//            //리모트 컨피그 설정값 오버라이드 세이브 관련 변경사항 적용 예시

//            // ------- 이후 확장 예정 기능 (지금은 주석 처리) -------
//            /*
//            // Economy 초기화 예시
//            if (EconomyService.Instance.Configuration == null)
//            {
//                await EconomyService.Instance.Configuration.GetCurrenciesAsync();
//            }

//            // 로그인 완료 UI 표시 예시
//            UIManager.Instance.ShowLoginStatus(AuthenticationService.Instance.PlayerId);
//            */
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"❌ Remote Config 초기화 실패: {e.Message}");
//        }
//    }

//    // Remote Config에서 설정값을 받아왔을 때 호출되는 콜백 함수
//    void ApplyRemoteSettings(ConfigResponse configResponse)
//    {
//        // 서버에서 설정값을 정상적으로 받아온 후 호출됨
//        float multiplier = RemoteConfigService.Instance.appConfig.GetFloat("eventMultiplier", 1.0f);
//        Debug.Log($"📦 적용된 이벤트 배율: {multiplier}");

//        // TODO: 여기에 게임 설정 적용
//        // GameManager.Instance.SetExpMultiplier(multiplier);
//    }

//    //// TestKey 라는 리모트 컨피그 키값으로 부터 값을 받아 로그 출력 자료형은 float 참고용
//    //Debug.Log("TestKey: " + RemoteConfigService.Instance.appConfig.GetFloat("TestKey", 0.0f));

//    //save 호출
//    public void Save()
//    {
//        if (UGSManager.Ins == null || UGSManager.Ins.playerSaveData == null)
//        {
//            Debug.LogError("UGSSetup or playerSaveData is not initialized.");
//            return;
//        }
//        // 현재 플레이어 데이터를 저장
//        _ = SaveManager.SaveAsync(UGSManager.Ins.playerSaveData);
//    }
//}
//public static class SaveManager
//{
//    static readonly string LocalPath =
//        Path.Combine(Application.persistentDataPath, "save.json");

//    /* ---------- 저장 ---------- */
//    public static async Task SaveAsync(PlayerSaveData data)
//    {
//        data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

//        /* 1) 로컬에 즉시 저장 */
//        File.WriteAllText(LocalPath, JsonUtility.ToJson(data));

//        /* 2) 가능한 경우 클라우드에 업로드 */
//        if (Utilities.CheckForInternetConnection() &&
//            AuthenticationService.Instance.IsSignedIn)
//        {
//            var dict = new Dictionary<string, object>
//            {
//                { "playerData", JsonUtility.ToJson(data) }
//            };

//            try { await CloudSaveService.Instance.Data.Player.SaveAsync(dict); }
//            catch { Debug.LogWarning("⬆️ Cloud Save 실패 – 다음 기회에 재시도"); }
//        }
//    }

//    /* ---------- 로드 ---------- */
//    public static async Task<PlayerSaveData> LoadAsync()
//    {
//        PlayerSaveData cloud = null;
//        PlayerSaveData localFile = null;

//        // 1) 로컬 파일 우선
//        if (File.Exists(LocalPath))
//        {
//            localFile = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(LocalPath));
//        }

//        // 2) 네트워크 가능하면 클라우드 병합
//        if (Utilities.CheckForInternetConnection() && AuthenticationService.Instance.IsSignedIn)
//        {
//            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "playerData" });
//            if (result.TryGetValue("playerData", out var item) && item != null)
//            {
//                var json = item.Value.GetAsString();
//                if (!string.IsNullOrEmpty(json))
//                    cloud = JsonUtility.FromJson<PlayerSaveData>(json);
//            }
//        }

//        // 3) 충돌 해결 – timestamp 최신 값 선택
//        PlayerSaveData finalData = ResolveConflict(localFile, cloud);

//        // 4) 로컬·클라우드 동기화 보장
//        if (finalData != null)
//        {
//            File.WriteAllText(LocalPath, JsonUtility.ToJson(finalData));
//            if (cloud == null || finalData.timestamp > (cloud?.timestamp ?? 0))
//                _ = SaveAsync(finalData); // 최신 데이터 동기화
//        }

//        return finalData ?? new PlayerSaveData();
//    }

//    //충돌 해결 로직
//    static PlayerSaveData ResolveConflict(PlayerSaveData a, PlayerSaveData b)
//    {
//        if (a == null) return b;
//        else if (b == null) return a;
//        else return a.timestamp >= b.timestamp ? a : b;
//    }
}