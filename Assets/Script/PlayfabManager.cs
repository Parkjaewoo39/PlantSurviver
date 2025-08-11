using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.Storage;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;



public class PlayfabManager : MonoBehaviour
{
    #region ###데이터 및 세이브 실사용 도구###

    public class playerData
    {
        public int Level
        {
            get => PlayfabManager.Ins.GetProperty<int>("Level", 1);
            set => PlayfabManager.Ins.SetProperty("Level", value);
        }
        public int Exp
        {
            get => PlayfabManager.Ins.GetProperty<int>("Exp", 0);
            set => PlayfabManager.Ins.SetProperty("Exp", value);
        }
        public string ClassName
        {
            get => PlayfabManager.Ins.GetProperty<string>("ClassName", "");
            set => PlayfabManager.Ins.SetProperty("ClassName", value);
        }
        public List<int> UnlockedSkills
        {
            get => PlayfabManager.Ins.GetProperty<List<int>>("UnlockedSkills", new List<int>());
            set => PlayfabManager.Ins.SetProperty("UnlockedSkills", value);
        }
        public List<ItemInstance> Inventory
        {
            get => PlayfabManager.Ins.GetProperty<List<ItemInstance>>("Inventory", new List<ItemInstance>());
            set => PlayfabManager.Ins.SetProperty("Inventory", value);
        }
    }
    public playerData PlayerData { get; private set; } = new playerData();
    #endregion

    #region ### 변수 및 프로퍼티 ###
    // --- Public Properties --- //
    /// <summary>
    /// 플레이어의 커스텀 데이터. (레벨, 경험치, 스탯 등)
    /// 서버에서 로드되며, ObscuredString을 사용하여 JSON 문자열 형태로 값을 저장, 메모리 변조를 방지합니다.
    /// </summary>
    public Dictionary<string, ObscuredString> UserProperties { get; private set; } = new Dictionary<string, ObscuredString>();

    /// <summary>
    /// 플레이어가 보유한 가상 재화 목록 (예: "GD": 100).
    /// ObscuredInt를 사용하여 메모리 변조를 방지합니다.
    /// </summary>
    public Dictionary<string, ObscuredLong> VirtualCurrencies { get; private set; } = new Dictionary<string, ObscuredLong>();

    /// <summary>
    /// 플레이어의 인벤토리 아이템 리스트. GetUserInventory()를 통해 서버에서 로드됩니다.
    /// </summary>
    public List<ItemInstance> Inventory { get; private set; } = new List<ItemInstance>();

    /// <summary>
    /// 게임 전체에 적용되는 설정 데이터 (이벤트 배율 등). GetTitleData()를 통해 서버에서 로드됩니다.
    /// </summary>
    public Dictionary<string, string> TitleData { get; private set; } = new Dictionary<string, string>();

    /// <summary>
    /// PlayFab에 등록된 아이템 카탈로그. GetCatalogItems()를 통해 서버에서 로드됩니다.
    /// </summary>
    public List<CatalogItem> Catalog { get; private set; } = new List<CatalogItem>();

    /// <summary>
    /// 현재 PlayFab 로그인 상태를 나타냅니다.
    /// </summary>
    public bool IsLoggedIn { get; private set; }

    // --- Singleton Instance --- //
    /// <summary>
    /// PlayfabManager의 싱글톤 인스턴스입니다.
    /// </summary>
    public static PlayfabManager Ins { get; private set; }

    // --- Private Variables --- //
    private float lastServerSaveTime = -100f; // 마지막으로 서버에 저장한 시각 (잦은 저장 방지용)
    private const float serverSaveInterval = 60f; // 서버 저장 최소 간격(초) // 이 값도 서버에서 받게 수정 요망

    // --- Serialized Fields for UI --- //
    [Header("PlayFab 기본 설정")]
    [Tooltip("PlayFab 대시보드에서 발급받은 타이틀 ID를 입력하세요.")]
    [SerializeField] private string titleId = "YOUR_TITLE_ID_HERE";

    [Header("UI 연동 (Login Scene)")]
    [Tooltip("계정 ID를 입력받는 InputField")]
    public TMP_InputField idInputField;
    [Tooltip("로그인 실행 버튼")]
    public Button loginButton;
    [Tooltip("로그인 상태를 표시하는 텍스트")]
    public Text statusText;

    // --- Debug --- //
    private List<string> debugLogMessages = new List<string>();
    private Vector2 logScrollPos = Vector2.zero;
    #endregion

    #region ### MonoBehaviour 생명주기 ###
    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Ins == null)
        {
            Ins = this;
            DontDestroyOnLoad(gameObject); // 씬이 전환되어도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있으면 중복 생성 방지
        }
    }

    void Start()
    {
        // 로그인 버튼에 Login() 메서드 연결
        if (loginButton != null)
            loginButton.onClick.AddListener(Login);

        // 이전에 사용한 ID가 있으면 자동으로 입력 필드에 채워넣기 (자동 로그인 X)
        string lastCustomId = ObscuredPrefs.GetString("LastCustomId", "");
        if (!string.IsNullOrEmpty(lastCustomId) && idInputField != null)
        {
            idInputField.text = lastCustomId;
        }
    }

    /// <summary>
    /// 앱이 다시 포커스를 얻었을 때 (예: 다른 앱 사용 후 복귀) 호출됩니다.
    /// 서버의 고정 데이터(타이틀, 카탈로그)가 변경되었을 수 있으므로, 다시 로드하여 동기화합니다.
    /// 이는 비용 효율적인 데이터 갱신 방법입니다.
    /// </summary>
    private void OnApplicationFocus(bool focus)
    {
        if (focus && IsLoggedIn)
        {
            AddLog("애플리케이션 포커스 감지. 서버의 고정 데이터를 갱신합니다.");
            LoadNonSaveData();
        }
    }
    #endregion

    #region ### 로그인 및 계정 관리 ###
    /// <summary>
    /// 씬 전환 후 파괴된 UI 컴포넌트를 다시 연결합니다.
    /// </summary>
    public void ReconnectUI(TMP_InputField input, Button button, Text status)
    {
        idInputField = input;
        loginButton = button;
        statusText = status;
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            loginButton.onClick.AddListener(Login);   // 새 리스너 추가
        }
    }

    /// <summary>
    /// 입력 필드의 Custom ID로 PlayFab에 로그인합니다. ID가 비어있으면 기기 고유 ID로 익명 로그인합니다.
    /// </summary>
    public void Login()
    {
        string customId = idInputField != null ? idInputField.text.Trim() : "";

        // 입력된 ID가 있으면 해당 ID로 로그인 시도
        if (!string.IsNullOrEmpty(customId))
        {
            ObscuredPrefs.SetString("LastCustomId", customId); // 다음 접속을 위해 ID 저장
            ObscuredPrefs.Save();
            LoginWithCustomId(customId);
        }
        // 입력된 ID가 없으면 기기 고유 ID로 익명(게스트) 로그인
        else
        {
            // SystemInfo.deviceUniqueIdentifier는 기기마다 고유한 값을 제공합니다.
            LoginWithCustomId(SystemInfo.deviceUniqueIdentifier);
            SetStatus("게스트 로그인 중...");
            AddLog("게스트 로그인을 시도합니다...");
        }
    }

    /// <summary>
    /// 제공된 Custom ID로 PlayFab 로그인을 요청합니다.
    /// </summary>
    /// <param name="customId">로그인에 사용할 Custom ID</param>
    public void LoginWithCustomId(string customId)
    {
        if (string.IsNullOrEmpty(customId))
        {
            AddLog("로그인 실패: Custom ID가 비어있습니다.");
            return;
        }

        SetStatus($"로그인 중: {customId}");
        AddLog($"로그인 시도: {customId}");

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true // 해당 ID의 계정이 없으면 자동으로 생성
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    // 로그인 성공 시 호출되는 콜백 함수
    private void OnLoginSuccess(LoginResult result)
    {
        IsLoggedIn = true;
        SetStatus("로그인 성공!");
        AddLog($"로그인 성공! PlayFabID: {result.PlayFabId}");

        // 로그인 후 게임에 필요한 모든 데이터를 서버에서 순차적으로 로드합니다.
        LoadAllData();

        // 데이터 로딩이 시작된 후, 메인 씬으로 전환합니다.
        // 데이터 로딩 완료는 LoadAllData 내부의 콜백에서 처리됩니다.
        ChangeScene(1); // 예: 메인 씬 인덱스 1
    }

    // 로그인 실패 시 호출되는 콜백 함수
    private void OnLoginFailure(PlayFabError error)
    {
        IsLoggedIn = false;
        SetStatus($"로그인 실패: {error.GenerateErrorReport()}");
        AddLog($"로그인 실패: {error.GenerateErrorReport()}");
    }
    #endregion

    #region ### 씬 관리 ###
    /// <summary>
    /// 지정된 인덱스의 씬으로 전환합니다.
    /// </summary>
    public void ChangeScene(int sceneIndex)
    {
        AddLog($"씬 전환 요청: 인덱스 {sceneIndex}");
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// 지정된 이름의 씬으로 전환합니다.
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        AddLog($"씬 전환 요청: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    #endregion

    #region ### 데이터 로드 플로우 ###
    /// <summary>
    /// 로그인 후 필요한 모든 서버 데이터를 순차적으로 불러옵니다. (타이틀 데이터 -> 카탈로그 -> 인벤토리 -> 플레이어 데이터)
    /// </summary>
    public void LoadAllData()
    {
        if (!IsLoggedIn) return;

        AddLog("모든 서버 데이터 로드를 시작합니다...");

        // 1. 타이틀 데이터 & 카탈로그 로드 (저장되지 않는 고정 데이터)
        LoadNonSaveData(() =>
        {
            // 2. 플레이어 인벤토리/재화 로드
            GetUserInventory(() =>
            {
                // 3. 플레이어 커스텀 데이터 로드 (레벨, 경험치 등)
                LoadAllUserProperties(() =>
                {
                    // 4. 모든 데이터 로드 완료
                    AddLog("모든 서버 데이터 로드가 성공적으로 완료되었습니다.");
                    // TODO: 데이터 로드 완료 후 UI 갱신, 게임 로직 시작 등
                    // 예: UIManager.Instance.UpdateAllUI();
                });
            });
        });
    }

    /// <summary>
    /// 게임의 고정 데이터(타이틀 데이터, 카탈로그)를 로드합니다.
    /// 이 데이터는 플레이어별로 저장되지 않으며, 게임 전체에 적용됩니다.
    /// </summary>
    public void LoadNonSaveData(Action onComplete = null)
    {
        if (!IsLoggedIn) return;

        GetTitleData(() =>
        {
            GetCatalogItems(() =>
            {
                AddLog("고정 데이터(타이틀, 카탈로그) 로드가 완료되었습니다.");
                onComplete?.Invoke();
            });
        });
    }
    #endregion

    #region ### 플레이어 데이터 (UserProperties) ###

    // JsonUtility가 제네릭 타입을 직접 처리하지 못하므로, 래퍼 클래스를 사용합니다.
    [Serializable] private class JsonWrapper<T> { public T value; }

    /// <summary>
    /// 지정된 Key에 해당하는 플레이어 데이터를 다양한 타입으로 가져옵니다.
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (UserProperties.TryGetValue(key, out ObscuredString value))
        {
            try
            {
                // 기본 타입(int, float, string, bool)은 래퍼를 통해 처리
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return JsonUtility.FromJson<JsonWrapper<T>>(value).value;
                }
                // 그 외 복합 타입(클래스, 리스트 등)은 직접 처리
                return JsonUtility.FromJson<T>(value);
            }
            catch (Exception e)
            {
                AddLog($"'{key}' 데이터 변환 실패: {e.Message}. 기본값을 반환합니다.");
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 지정된 키에 해당하는 플레이어 데이터를 저장합니다.
    /// UserProperties에 값을 저장하고, 서버에도 즉시 반영합니다.
    /// </summary>
    /// <typeparam name="T">저장할 데이터의 타입</typeparam>
    /// <param name="key">데이터 키</param>
    /// <param name="value">저장할 값</param>
    public void SetProperty<T>(string key, T value)
    {
        // UserProperties에 값 저장
        string jsonValue;
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            // 기본 타입(int, float, string 등)은 래퍼 클래스를 통해 직렬화
            var wrapper = new JsonWrapper<T> { value = value };
            jsonValue = JsonUtility.ToJson(wrapper);
        }
        else
        {
            // 복합 타입(클래스, 리스트 등)은 직접 직렬화
            jsonValue = JsonUtility.ToJson(value);
        }
        UserProperties[key] = jsonValue;

        // 서버에 즉시 저장 시도 (해당 키와 값만 저장)
        var dataPayload = new Dictionary<string, object> { { key, value } };
        if (SaveUserProperties(dataPayload))
        {
            ForceDataUpdate();// 서버에서 TitleData 변경 감지 및 콜백 등록 기능 호출 이게 호출 되면 변경되지 않는 데이터 를 다시 로드함 (타이틀데이터,카탈로그)
        }

    }

    /// <summary>
    /// 여러 플레이어 데이터를 한 번에 서버에 저장합니다. (잦은 호출 방지 기능 포함)
    /// </summary>
    /// <param name="propertiesToSave">저장할 데이터 Dictionary. 값은 자동으로 JSON 직렬화됩니다.</param>
    public bool SaveUserProperties(Dictionary<string, object> propertiesToSave)
    {
        if (!IsLoggedIn)
        {
            AddLog("로그인 상태가 아니므로 저장할 수 없습니다.");
            return false;//실패
        }

        if (Time.time - lastServerSaveTime < serverSaveInterval)
        {
            return false;//실패
        }
        lastServerSaveTime = Time.time;

        var dataPayload = new Dictionary<string, string>();
        foreach (var prop in propertiesToSave)
        {
            string jsonValue;
            // 기본 타입은 래퍼를 통해 직렬화
            if (prop.Value.GetType().IsPrimitive || prop.Value is string)
            {
                var wrapper = new JsonWrapper<object> { value = prop.Value };
                jsonValue = JsonUtility.ToJson(wrapper);
            }
            else // 복합 타입은 직접 직렬화
            {
                jsonValue = JsonUtility.ToJson(prop.Value);
            }

            // 로컬 캐시에도 즉시 반영
            UserProperties[prop.Key] = jsonValue;
            dataPayload[prop.Key] = jsonValue;
        }

        var request = new UpdateUserDataRequest { Data = dataPayload };
        PlayFabClientAPI.UpdateUserData(request,
            (result) => AddLog($"[UserProperties] 데이터 저장 성공: Keys = {string.Join(", ", dataPayload.Keys)}"),
            (error) => AddLog($"[UserProperties] 데이터 저장 실패: {error.GenerateErrorReport()}"));

        return true;//성공
    }


    /// <summary>
    /// 서버에서 모든 플레이어 데이터를 불러옵니다.
    /// </summary>
    private void LoadAllUserProperties(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }

        AddLog("[UserProperties] 모든 플레이어 데이터 로드를 시작합니다.");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            (result) =>
            {
                AddLog("[UserProperties] 데이터 로드 성공!");
                if (result.Data != null && result.Data.Count > 0)
                {
                    UserProperties.Clear();
                    foreach (var item in result.Data)
                    {
                        UserProperties[item.Key] = item.Value.Value;
                    }
                    AddLog($"[UserProperties] 총 {UserProperties.Count}개의 데이터를 적용했습니다.");
                }
                else
                {
                    AddLog("[UserProperties] 서버에 저장된 데이터가 없습니다. 새 데이터로 시작합니다.");
                    // 새 계정일 경우, 기본값 설정 및 저장
                    var initialProps = new Dictionary<string, object>
                    {
                        { "Level", 1 },
                        { "Exp", 0 }
                    };
                    SaveUserProperties(initialProps);
                }
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[UserProperties] 데이터 로드 실패: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }
    #endregion

    #region ### 경제 (V1) - 재화 및 인벤토리 ###
    /// <summary>
    /// 서버에서 플레이어의 인벤토리(재화 포함) 정보를 가져옵니다.
    /// </summary>
    private void GetUserInventory(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[Economy] 인벤토리 및 재화 정보 로드를 시작합니다.");
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            (result) =>
            {
                AddLog("[Economy] 인벤토리/재화 정보 로드 성공!");
                Inventory = result.Inventory;

                // 재화 정보를 ObscuredInt 타입으로 변환하여 저장
                VirtualCurrencies.Clear();
                foreach (var currency in result.VirtualCurrency)
                {
                    VirtualCurrencies[currency.Key] = currency.Value;
                }

                AddLog($"[Economy] 보유 재화: {string.Join(", ", VirtualCurrencies)}");
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[Economy] 인벤토리/재화 정보 로드 실패: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }

    /// <summary>
    /// 플레이어의 가상 재화를 추가하거나 차감합니다.
    /// </summary>
    /// <param name="currencyCode">재화 코드 (예: "GD" for Gold). PlayFab 대시보드에서 설정한 코드와 일치해야 합니다.</param>
    /// <param name="amount">변경할 양 (양수는 추가, 음수는 차감)</param>
    public void AddUserVirtualCurrency(string currencyCode, int amount)
    {
        if (!IsLoggedIn) return;
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = currencyCode,
            Amount = amount
        };
        PlayFabClientAPI.AddUserVirtualCurrency(request,
            (result) =>
            {
                AddLog($"[Economy] 재화 변경 성공! {result.VirtualCurrency}: {result.Balance}");
                // 로컬 데이터도 즉시 갱신하여 UI 등에 바로 반영할 수 있도록 함
                VirtualCurrencies[result.VirtualCurrency] = result.Balance;
                // TODO: 재화 UI 업데이트 호출
            },
            (error) => AddLog($"[Economy] 재화 변경 실패: {error.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// 카탈로그 아이템을 재화를 사용해 구매합니다.
    /// </summary>
    /// <param name="itemId">구매할 아이템의 ID</param>
    /// <param name="currencyCode">사용할 재화의 코드</param>
    /// <param name="price">아이템 가격</param>
    public void PurchaseItem(string itemId, string currencyCode, int price)
    {
        if (!IsLoggedIn) return;
        AddLog($"[Economy] 아이템 구매 시도: {itemId}");
        var request = new PurchaseItemRequest
        {
            ItemId = itemId,
            VirtualCurrency = currencyCode,
            Price = price
        };
        PlayFabClientAPI.PurchaseItem(request,
            (result) =>
            {
                AddLog($"[Economy] 아이템 구매 성공: {result.Items[0].DisplayName}");
                // 구매 후 최신 인벤토리 정보를 다시 불러옴
                GetUserInventory();
            },
            (error) => AddLog($"[Economy] 아이템 구매 실패: {error.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// 소모성 아이템을 사용합니다.
    /// </summary>
    /// <param name="itemInstanceId">사용할 아이템의 고유 인스턴스 ID (ItemInstance.ItemInstanceId)</param>
    /// <param name="count">소모할 개수</param>
    public void ConsumeItem(string itemInstanceId, int count = 1)
    {
        if (!IsLoggedIn) return;
        AddLog($"[Economy] 아이템 사용 시도: {itemInstanceId}");
        var request = new ConsumeItemRequest
        {
            ItemInstanceId = itemInstanceId,
            ConsumeCount = count
        };
        PlayFabClientAPI.ConsumeItem(request,
            (result) =>
            {
                // ConsumeItemResult에는 ItemId가 없으므로, ItemInstanceId로 성공 여부를 알립니다.
                AddLog($"[Economy] 아이템 사용 성공: {result.ItemInstanceId}, 남은 횟수: {result.RemainingUses}");
                // 아이템 사용 후 최신 인벤토리 정보를 다시 불러옴
                GetUserInventory();
            },
            (error) => AddLog($"[Economy] 아이템 사용 실패: {error.GenerateErrorReport()}")
        );
    }
    #endregion

    #region ### 타이틀 데이터 (원격 설정) ###
    /// <summary>
    /// 게임 전체 설정 데이터(TitleData)를 서버에서 가져옵니다.
    /// </summary>
    private void GetTitleData(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[TitleData] 타이틀 데이터 로드를 시작합니다.");
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            (result) =>
            {
                AddLog("[TitleData] 타이틀 데이터 로드 성공!");
                TitleData = result.Data;
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[TitleData] 타이틀 데이터 로드 실패: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }

    /// <summary>
    /// 서버에서 TitleData의 특정 키에 해당하는 값만 가져옵니다.
    /// </summary>
    /// <param name="key">가져올 데이터의 키</param>
    /// <param name="onComplete">성공 시 값을 전달받을 콜백</param>
    public void GetTitleDataValueFromServer(string key, Action<string> onComplete)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(null);
            return;
        }

        var request = new GetTitleDataRequest
        {
            Keys = new List<string> { key }
        };

        PlayFabClientAPI.GetTitleData(request,
            (result) =>
            {
                if (result.Data != null && result.Data.TryGetValue(key, out var value))
                {
                    AddLog($"[TitleData] '{key}' 값 서버에서 가져오기 성공: {value}");
                    onComplete?.Invoke(value);
                }
                else
                {
                    AddLog($"[TitleData] 서버에서 '{key}' 키를 찾을 수 없습니다.");
                    onComplete?.Invoke(null);
                }
            },
            (error) =>
            {
                AddLog($"[TitleData] 서버에서 '{key}' 값 가져오기 실패: {error.GenerateErrorReport()}");
                onComplete?.Invoke(null);
            }
        );
    }

    #endregion

    #region ### 카탈로그 (아이템 정보) ###
    /// <summary>
    /// 게임 아이템 카탈로그를 서버에서 가져옵니다.
    /// </summary>
    private void GetCatalogItems(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[Catalog] 아이템 카탈로그 로드를 시작합니다.");
        var request = new GetCatalogItemsRequest { CatalogVersion = "Main" };
        PlayFabClientAPI.GetCatalogItems(request,
            (result) =>
            {
                AddLog("[Catalog] 카탈로그 로드 성공!");
                Catalog = result.Catalog;
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[Catalog] 카탈로그 로드 실패: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }
    #endregion

    #region ### 상태 및 디버그 로그 ###
    /// <summary>
    /// 상태 메시지를 UI 텍스트에 표시합니다.
    /// </summary>
    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    /// <summary>
    /// 디버그 로그 리스트에 메시지를 추가하고 콘솔에도 출력합니다.
    /// </summary>
    private void AddLog(string message)
    {
        if (!enableDebugLogs) return;
        string log = $"[{DateTime.Now:HH:mm:ss}] {message}";
        debugLogMessages.Add(log);
        Debug.Log(log); // Unity 콘솔에도 출력
        if (debugLogMessages.Count > 50) // 로그는 최근 50개만 유지
            debugLogMessages.RemoveAt(0);
    }

    [Header("디버그 설정")]
    [SerializeField] public bool enableDebugLogs = true; // 디버그 로그 UI 출력 여부

    // IMGUI를 사용하여 화면에 디버그 로그를 출력합니다.
    void OnGUI()
    {
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, Screen.width * 0.5f, Screen.height * 0.5f), "PlayFabManager Debug Log", GUI.skin.window);
        enableDebugLogs = GUILayout.Toggle(enableDebugLogs, "디버그 로그 출력");
        logScrollPos = GUILayout.BeginScrollView(logScrollPos);
        GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        foreach (var msg in debugLogMessages)
        {
            GUILayout.Label(msg, logStyle);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    #endregion

    #region ### 예시 코드 (실제 사용 시나리오) ###

    #endregion

    //강제저장기능 확인 안됨 실적용 안됨 체크요망 (변경 가능한 플레이어 데이터들을 전부 강제저장 하는기능으로 만들어야 할 듯)
    #region ### 강제 저장 기능 추가 ###
    /// <summary>
    /// 여러 플레이어 데이터를 서버에 즉시 강제 저장합니다. (간격 제한 무시)
    /// </summary>
    public void ForceSaveUserProperties(Dictionary<string, object> propertiesToSave)
    {
        if (!IsLoggedIn)
        {
            AddLog("로그인 상태가 아니므로 저장할 수 없습니다.");
            return;
        }

        // 간격 제한 없이 바로 저장
        lastServerSaveTime = Time.time;

        var dataPayload = new Dictionary<string, string>();
        foreach (var prop in propertiesToSave)
        {
            string jsonValue;
            if (prop.Value.GetType().IsPrimitive || prop.Value is string)
            {
                var wrapper = new JsonWrapper<object> { value = prop.Value };
                jsonValue = JsonUtility.ToJson(wrapper);
            }
            else
            {
                jsonValue = JsonUtility.ToJson(prop.Value);
            }
            UserProperties[prop.Key] = jsonValue;
            dataPayload[prop.Key] = jsonValue;
        }

        var request = new UpdateUserDataRequest { Data = dataPayload };
        PlayFabClientAPI.UpdateUserData(request,
            (result) => AddLog($"[UserProperties] 강제 저장 성공: Keys = {string.Join(", ", dataPayload.Keys)}"),
            (error) => AddLog($"[UserProperties] 강제 저장 실패: {error.GenerateErrorReport()}"));
    }
    #endregion
    #region ### 서버에서 업데이트

    /// <summary>
    /// TitleData의 특정 키 값 변경을 감지하고, 변경 시 전체 데이터를 다시 로드합니다.
    /// 예: 이벤트 배율 변경 시 즉시 적용. 주기적으로 호출하여 서버 변경사항을 확인할 수 있습니다.
    /// </summary>
    private void ForceDataUpdate()
    {
        string key = "ForceUpdate"; // 감지할 TitleData 키

        // 1. 현재 로컬에 캐시된 값을 가져옵니다.
        TitleData.TryGetValue(key, out string oldValue);

        // 2. 서버에서 최신 값을 비동기적으로 가져옵니다.
        GetTitleDataValueFromServer(key, (newValue) =>
        {
            // 3. 서버에서 값을 성공적으로 가져왔고, 이전 값과 다를 경우
            if (newValue != null && oldValue != newValue)
            {
                AddLog($"[TitleData] '{key}' 값 변경 감지: '{oldValue ?? "없음"}' -> '{newValue}'. 모든 데이터를 다시 로드합니다.");

                // 4. 모든 데이터를 다시 로드하여 변경사항을 게임에 적용합니다.
                // LoadAllData가 내부적으로 TitleData를 갱신하므로, 로컬 TitleData[key] = newValue; 코드는 불필요합니다.
                //LoadAllData();
                LoadNonSaveData();//클라이언트에서 수정하지 않는 정보
            }
            else
            {
                AddLog($"[TitleData] '{key}' 값은 변경되지 않았습니다. (현재 값: {oldValue ?? "없음"})");
            }
        });
    }


    #region ### PlayFab 추가 기능 아이디어 ###

    //서버 값 온리
    //공지
    //이벤트 기간 설정
    //벨런스 (이벤트 배율 재화 습득량 몬스터 능력치 기타 등등)
    //버그 대응용 기능 정지
    //아이템 카탈로그 업데이트(가격 종류 성능 변경)

    //만약 서버에서 특정 세이브를(클라에서 변경하는 데이터) 강제 수정한다면? - 유저세이브데이터 기준
    // 우편함 메시지 기능
    // 버그 대응 제화 또는 아이템 강제 변경 계정 정지 등
    //조건부 기능 정지(특정 유저만 정지)
    //환불 대응 재화 마이너스
    //

    //멀티 기능추가
    //체팅 멀티플레이 등

    // --- 기본 기능 확장 ---
    // 1. 클라우드 스크립트(Cloud Script): 서버 측 로직을 실행하여 보안 강화 및 복잡한 로직 처리.
    //    - 예: 아이템 강화, 뽑기 등 확률 기반 시스템의 결과를 서버에서 결정하여 클라이언트 조작 방지.
    //    - 예: 플레이어 데이터 검증 (레벨업 조건, 재화 소모량 등)을 서버에서 수행.
    //    - 예: 예약된 작업을 통해 매일 자정에 일일 퀘스트 초기화 또는 주간 랭킹 보상 지급.

    // 2. 순위표 (Leaderboards / Player Statistics): 플레이어 간 경쟁 요소 도입.
    //    - 예: 스테이지 클리어 시간, 누적 점수 등으로 순위표 생성.
    //    - 예: 주간/시즌별 순위표를 운영하고 상위 유저에게 보상 지급.

    // 3. 플레이어 세그먼트 (Player Segments): 유저 그룹별 맞춤 운영.
    //    - 예: '고과금 유저', '신규 유저', '휴면 유저' 등으로 그룹을 나누어 각기 다른 푸시 알림, 인게임 메시지, 특별 상점 아이템 제공.

    // --- 유저 관리 및 소셜 ---
    // 4. 계정 연동 및 관리: 게스트 계정을 소셜 계정(Google, Facebook 등)이나 이메일/비밀번호에 연결하여 데이터 보호.
    //    - PlayFabClientAPI.LinkCustomIDAsync, LinkGoogleAccountAsync 등 사용.
    //    - 비밀번호 재설정 기능 제공.

    // 5. 친구 시스템: 친구 목록, 친구 요청/수락/삭제 기능.
    //    - PlayFabClientAPI.GetFriendsListAsync, AddFriendAsync 등 사용.
    //    - 친구에게 선물 보내기, 친구의 게임 진행 상황 보기 등 소셜 기능 구현.

    // 6. 우편함/메시지 시스템: 모든 유저 또는 특정 유저에게 아이템, 재화, 메시지 발송.
    //    - 주로 서버 도구(Game Manager)나 클라우드 스크립트를 통해 아이템을 부여하고, 클라이언트는 해당 내역을 확인하여 UI로 표시.
    //    - 점검 보상, 이벤트 보상, 운영자 선물 등을 지급하는 용도로 활용.

    // --- BM 및 콘텐츠 ---
    // 7. 실시간 결제(IAP) 검증: 구글 플레이 스토어, 애플 앱스토어 결제 영수증을 PlayFab 서버에서 검증하여 해킹된 결제 방지.
    //    - PlayFabClientAPI.ValidateGooglePlayPurchaseAsync, ValidateIOSReceiptAsync 등 사용.

    // 8. A/B 테스트: 동일한 유저 그룹을 대상으로 여러 버전의 상점 아이템 가격이나 게임 밸런스를 테스트하여 최적의 값을 도출.
    //    - 예: A그룹에는 1000원짜리 패키지, B그룹에는 1500원짜리 패키지를 노출하여 구매 전환율 비교.

    // 9. 플레이어 간 아이템 거래: 보안이 확보된 거래 시스템 구축.
    //    - PlayFabClientAPI.OpenTradeAsync, AcceptTradeAsync 등 사용.

    // --- 실시간 멀티플레이 ---
    // 10. 매치메이킹: 조건에 맞는 다른 플레이어를 찾아 실시간 멀티플레이 세션 구성.
    //     - PlayFabMultiplayerAPI.CreateMatchmakingTicketAsync 등 사용.

    // 11. 로비 및 실시간 데이터 통신 (Lobby & PubSub): 매치된 플레이어 간에 준비 상태, 캐릭터 선택 등 데이터를 실시간으로 동기화.
    //     - 멀티플레이 게임의 대기방 기능 구현.

    #endregion
    #endregion


}
