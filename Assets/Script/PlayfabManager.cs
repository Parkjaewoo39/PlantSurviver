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
    #region ###������ �� ���̺� �ǻ�� ����###

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

    #region ### ���� �� ������Ƽ ###
    // --- Public Properties --- //
    /// <summary>
    /// �÷��̾��� Ŀ���� ������. (����, ����ġ, ���� ��)
    /// �������� �ε�Ǹ�, ObscuredString�� ����Ͽ� JSON ���ڿ� ���·� ���� ����, �޸� ������ �����մϴ�.
    /// </summary>
    public Dictionary<string, ObscuredString> UserProperties { get; private set; } = new Dictionary<string, ObscuredString>();

    /// <summary>
    /// �÷��̾ ������ ���� ��ȭ ��� (��: "GD": 100).
    /// ObscuredInt�� ����Ͽ� �޸� ������ �����մϴ�.
    /// </summary>
    public Dictionary<string, ObscuredLong> VirtualCurrencies { get; private set; } = new Dictionary<string, ObscuredLong>();

    /// <summary>
    /// �÷��̾��� �κ��丮 ������ ����Ʈ. GetUserInventory()�� ���� �������� �ε�˴ϴ�.
    /// </summary>
    public List<ItemInstance> Inventory { get; private set; } = new List<ItemInstance>();

    /// <summary>
    /// ���� ��ü�� ����Ǵ� ���� ������ (�̺�Ʈ ���� ��). GetTitleData()�� ���� �������� �ε�˴ϴ�.
    /// </summary>
    public Dictionary<string, string> TitleData { get; private set; } = new Dictionary<string, string>();

    /// <summary>
    /// PlayFab�� ��ϵ� ������ īŻ�α�. GetCatalogItems()�� ���� �������� �ε�˴ϴ�.
    /// </summary>
    public List<CatalogItem> Catalog { get; private set; } = new List<CatalogItem>();

    /// <summary>
    /// ���� PlayFab �α��� ���¸� ��Ÿ���ϴ�.
    /// </summary>
    public bool IsLoggedIn { get; private set; }

    // --- Singleton Instance --- //
    /// <summary>
    /// PlayfabManager�� �̱��� �ν��Ͻ��Դϴ�.
    /// </summary>
    public static PlayfabManager Ins { get; private set; }

    // --- Private Variables --- //
    private float lastServerSaveTime = -100f; // ���������� ������ ������ �ð� (���� ���� ������)
    private const float serverSaveInterval = 60f; // ���� ���� �ּ� ����(��) // �� ���� �������� �ް� ���� ���

    // --- Serialized Fields for UI --- //
    [Header("PlayFab �⺻ ����")]
    [Tooltip("PlayFab ��ú��忡�� �߱޹��� Ÿ��Ʋ ID�� �Է��ϼ���.")]
    [SerializeField] private string titleId = "YOUR_TITLE_ID_HERE";

    [Header("UI ���� (Login Scene)")]
    [Tooltip("���� ID�� �Է¹޴� InputField")]
    public TMP_InputField idInputField;
    [Tooltip("�α��� ���� ��ư")]
    public Button loginButton;
    [Tooltip("�α��� ���¸� ǥ���ϴ� �ؽ�Ʈ")]
    public Text statusText;

    // --- Debug --- //
    private List<string> debugLogMessages = new List<string>();
    private Vector2 logScrollPos = Vector2.zero;
    #endregion

    #region ### MonoBehaviour �����ֱ� ###
    void Awake()
    {
        // �̱��� ���� ����
        if (Ins == null)
        {
            Ins = this;
            DontDestroyOnLoad(gameObject); // ���� ��ȯ�Ǿ �ı����� �ʵ��� ����
        }
        else
        {
            Destroy(gameObject); // �̹� �ν��Ͻ��� ������ �ߺ� ���� ����
        }
    }

    void Start()
    {
        // �α��� ��ư�� Login() �޼��� ����
        if (loginButton != null)
            loginButton.onClick.AddListener(Login);

        // ������ ����� ID�� ������ �ڵ����� �Է� �ʵ忡 ä���ֱ� (�ڵ� �α��� X)
        string lastCustomId = ObscuredPrefs.GetString("LastCustomId", "");
        if (!string.IsNullOrEmpty(lastCustomId) && idInputField != null)
        {
            idInputField.text = lastCustomId;
        }
    }

    /// <summary>
    /// ���� �ٽ� ��Ŀ���� ����� �� (��: �ٸ� �� ��� �� ����) ȣ��˴ϴ�.
    /// ������ ���� ������(Ÿ��Ʋ, īŻ�α�)�� ����Ǿ��� �� �����Ƿ�, �ٽ� �ε��Ͽ� ����ȭ�մϴ�.
    /// �̴� ��� ȿ������ ������ ���� ����Դϴ�.
    /// </summary>
    private void OnApplicationFocus(bool focus)
    {
        if (focus && IsLoggedIn)
        {
            AddLog("���ø����̼� ��Ŀ�� ����. ������ ���� �����͸� �����մϴ�.");
            LoadNonSaveData();
        }
    }
    #endregion

    #region ### �α��� �� ���� ���� ###
    /// <summary>
    /// �� ��ȯ �� �ı��� UI ������Ʈ�� �ٽ� �����մϴ�.
    /// </summary>
    public void ReconnectUI(TMP_InputField input, Button button, Text status)
    {
        idInputField = input;
        loginButton = button;
        statusText = status;
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners(); // ���� ������ ����
            loginButton.onClick.AddListener(Login);   // �� ������ �߰�
        }
    }

    /// <summary>
    /// �Է� �ʵ��� Custom ID�� PlayFab�� �α����մϴ�. ID�� ��������� ��� ���� ID�� �͸� �α����մϴ�.
    /// </summary>
    public void Login()
    {
        string customId = idInputField != null ? idInputField.text.Trim() : "";

        // �Էµ� ID�� ������ �ش� ID�� �α��� �õ�
        if (!string.IsNullOrEmpty(customId))
        {
            ObscuredPrefs.SetString("LastCustomId", customId); // ���� ������ ���� ID ����
            ObscuredPrefs.Save();
            LoginWithCustomId(customId);
        }
        // �Էµ� ID�� ������ ��� ���� ID�� �͸�(�Խ�Ʈ) �α���
        else
        {
            // SystemInfo.deviceUniqueIdentifier�� ��⸶�� ������ ���� �����մϴ�.
            LoginWithCustomId(SystemInfo.deviceUniqueIdentifier);
            SetStatus("�Խ�Ʈ �α��� ��...");
            AddLog("�Խ�Ʈ �α����� �õ��մϴ�...");
        }
    }

    /// <summary>
    /// ������ Custom ID�� PlayFab �α����� ��û�մϴ�.
    /// </summary>
    /// <param name="customId">�α��ο� ����� Custom ID</param>
    public void LoginWithCustomId(string customId)
    {
        if (string.IsNullOrEmpty(customId))
        {
            AddLog("�α��� ����: Custom ID�� ����ֽ��ϴ�.");
            return;
        }

        SetStatus($"�α��� ��: {customId}");
        AddLog($"�α��� �õ�: {customId}");

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true // �ش� ID�� ������ ������ �ڵ����� ����
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    // �α��� ���� �� ȣ��Ǵ� �ݹ� �Լ�
    private void OnLoginSuccess(LoginResult result)
    {
        IsLoggedIn = true;
        SetStatus("�α��� ����!");
        AddLog($"�α��� ����! PlayFabID: {result.PlayFabId}");

        // �α��� �� ���ӿ� �ʿ��� ��� �����͸� �������� ���������� �ε��մϴ�.
        LoadAllData();

        // ������ �ε��� ���۵� ��, ���� ������ ��ȯ�մϴ�.
        // ������ �ε� �Ϸ�� LoadAllData ������ �ݹ鿡�� ó���˴ϴ�.
        ChangeScene(1); // ��: ���� �� �ε��� 1
    }

    // �α��� ���� �� ȣ��Ǵ� �ݹ� �Լ�
    private void OnLoginFailure(PlayFabError error)
    {
        IsLoggedIn = false;
        SetStatus($"�α��� ����: {error.GenerateErrorReport()}");
        AddLog($"�α��� ����: {error.GenerateErrorReport()}");
    }
    #endregion

    #region ### �� ���� ###
    /// <summary>
    /// ������ �ε����� ������ ��ȯ�մϴ�.
    /// </summary>
    public void ChangeScene(int sceneIndex)
    {
        AddLog($"�� ��ȯ ��û: �ε��� {sceneIndex}");
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// ������ �̸��� ������ ��ȯ�մϴ�.
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        AddLog($"�� ��ȯ ��û: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    #endregion

    #region ### ������ �ε� �÷ο� ###
    /// <summary>
    /// �α��� �� �ʿ��� ��� ���� �����͸� ���������� �ҷ��ɴϴ�. (Ÿ��Ʋ ������ -> īŻ�α� -> �κ��丮 -> �÷��̾� ������)
    /// </summary>
    public void LoadAllData()
    {
        if (!IsLoggedIn) return;

        AddLog("��� ���� ������ �ε带 �����մϴ�...");

        // 1. Ÿ��Ʋ ������ & īŻ�α� �ε� (������� �ʴ� ���� ������)
        LoadNonSaveData(() =>
        {
            // 2. �÷��̾� �κ��丮/��ȭ �ε�
            GetUserInventory(() =>
            {
                // 3. �÷��̾� Ŀ���� ������ �ε� (����, ����ġ ��)
                LoadAllUserProperties(() =>
                {
                    // 4. ��� ������ �ε� �Ϸ�
                    AddLog("��� ���� ������ �ε尡 ���������� �Ϸ�Ǿ����ϴ�.");
                    // TODO: ������ �ε� �Ϸ� �� UI ����, ���� ���� ���� ��
                    // ��: UIManager.Instance.UpdateAllUI();
                });
            });
        });
    }

    /// <summary>
    /// ������ ���� ������(Ÿ��Ʋ ������, īŻ�α�)�� �ε��մϴ�.
    /// �� �����ʹ� �÷��̾�� ������� ������, ���� ��ü�� ����˴ϴ�.
    /// </summary>
    public void LoadNonSaveData(Action onComplete = null)
    {
        if (!IsLoggedIn) return;

        GetTitleData(() =>
        {
            GetCatalogItems(() =>
            {
                AddLog("���� ������(Ÿ��Ʋ, īŻ�α�) �ε尡 �Ϸ�Ǿ����ϴ�.");
                onComplete?.Invoke();
            });
        });
    }
    #endregion

    #region ### �÷��̾� ������ (UserProperties) ###

    // JsonUtility�� ���׸� Ÿ���� ���� ó������ ���ϹǷ�, ���� Ŭ������ ����մϴ�.
    [Serializable] private class JsonWrapper<T> { public T value; }

    /// <summary>
    /// ������ Key�� �ش��ϴ� �÷��̾� �����͸� �پ��� Ÿ������ �����ɴϴ�.
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (UserProperties.TryGetValue(key, out ObscuredString value))
        {
            try
            {
                // �⺻ Ÿ��(int, float, string, bool)�� ���۸� ���� ó��
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return JsonUtility.FromJson<JsonWrapper<T>>(value).value;
                }
                // �� �� ���� Ÿ��(Ŭ����, ����Ʈ ��)�� ���� ó��
                return JsonUtility.FromJson<T>(value);
            }
            catch (Exception e)
            {
                AddLog($"'{key}' ������ ��ȯ ����: {e.Message}. �⺻���� ��ȯ�մϴ�.");
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// ������ Ű�� �ش��ϴ� �÷��̾� �����͸� �����մϴ�.
    /// UserProperties�� ���� �����ϰ�, �������� ��� �ݿ��մϴ�.
    /// </summary>
    /// <typeparam name="T">������ �������� Ÿ��</typeparam>
    /// <param name="key">������ Ű</param>
    /// <param name="value">������ ��</param>
    public void SetProperty<T>(string key, T value)
    {
        // UserProperties�� �� ����
        string jsonValue;
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            // �⺻ Ÿ��(int, float, string ��)�� ���� Ŭ������ ���� ����ȭ
            var wrapper = new JsonWrapper<T> { value = value };
            jsonValue = JsonUtility.ToJson(wrapper);
        }
        else
        {
            // ���� Ÿ��(Ŭ����, ����Ʈ ��)�� ���� ����ȭ
            jsonValue = JsonUtility.ToJson(value);
        }
        UserProperties[key] = jsonValue;

        // ������ ��� ���� �õ� (�ش� Ű�� ���� ����)
        var dataPayload = new Dictionary<string, object> { { key, value } };
        if (SaveUserProperties(dataPayload))
        {
            ForceDataUpdate();// �������� TitleData ���� ���� �� �ݹ� ��� ��� ȣ�� �̰� ȣ�� �Ǹ� ������� �ʴ� ������ �� �ٽ� �ε��� (Ÿ��Ʋ������,īŻ�α�)
        }

    }

    /// <summary>
    /// ���� �÷��̾� �����͸� �� ���� ������ �����մϴ�. (���� ȣ�� ���� ��� ����)
    /// </summary>
    /// <param name="propertiesToSave">������ ������ Dictionary. ���� �ڵ����� JSON ����ȭ�˴ϴ�.</param>
    public bool SaveUserProperties(Dictionary<string, object> propertiesToSave)
    {
        if (!IsLoggedIn)
        {
            AddLog("�α��� ���°� �ƴϹǷ� ������ �� �����ϴ�.");
            return false;//����
        }

        if (Time.time - lastServerSaveTime < serverSaveInterval)
        {
            return false;//����
        }
        lastServerSaveTime = Time.time;

        var dataPayload = new Dictionary<string, string>();
        foreach (var prop in propertiesToSave)
        {
            string jsonValue;
            // �⺻ Ÿ���� ���۸� ���� ����ȭ
            if (prop.Value.GetType().IsPrimitive || prop.Value is string)
            {
                var wrapper = new JsonWrapper<object> { value = prop.Value };
                jsonValue = JsonUtility.ToJson(wrapper);
            }
            else // ���� Ÿ���� ���� ����ȭ
            {
                jsonValue = JsonUtility.ToJson(prop.Value);
            }

            // ���� ĳ�ÿ��� ��� �ݿ�
            UserProperties[prop.Key] = jsonValue;
            dataPayload[prop.Key] = jsonValue;
        }

        var request = new UpdateUserDataRequest { Data = dataPayload };
        PlayFabClientAPI.UpdateUserData(request,
            (result) => AddLog($"[UserProperties] ������ ���� ����: Keys = {string.Join(", ", dataPayload.Keys)}"),
            (error) => AddLog($"[UserProperties] ������ ���� ����: {error.GenerateErrorReport()}"));

        return true;//����
    }


    /// <summary>
    /// �������� ��� �÷��̾� �����͸� �ҷ��ɴϴ�.
    /// </summary>
    private void LoadAllUserProperties(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }

        AddLog("[UserProperties] ��� �÷��̾� ������ �ε带 �����մϴ�.");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            (result) =>
            {
                AddLog("[UserProperties] ������ �ε� ����!");
                if (result.Data != null && result.Data.Count > 0)
                {
                    UserProperties.Clear();
                    foreach (var item in result.Data)
                    {
                        UserProperties[item.Key] = item.Value.Value;
                    }
                    AddLog($"[UserProperties] �� {UserProperties.Count}���� �����͸� �����߽��ϴ�.");
                }
                else
                {
                    AddLog("[UserProperties] ������ ����� �����Ͱ� �����ϴ�. �� �����ͷ� �����մϴ�.");
                    // �� ������ ���, �⺻�� ���� �� ����
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
                AddLog($"[UserProperties] ������ �ε� ����: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }
    #endregion

    #region ### ���� (V1) - ��ȭ �� �κ��丮 ###
    /// <summary>
    /// �������� �÷��̾��� �κ��丮(��ȭ ����) ������ �����ɴϴ�.
    /// </summary>
    private void GetUserInventory(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[Economy] �κ��丮 �� ��ȭ ���� �ε带 �����մϴ�.");
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            (result) =>
            {
                AddLog("[Economy] �κ��丮/��ȭ ���� �ε� ����!");
                Inventory = result.Inventory;

                // ��ȭ ������ ObscuredInt Ÿ������ ��ȯ�Ͽ� ����
                VirtualCurrencies.Clear();
                foreach (var currency in result.VirtualCurrency)
                {
                    VirtualCurrencies[currency.Key] = currency.Value;
                }

                AddLog($"[Economy] ���� ��ȭ: {string.Join(", ", VirtualCurrencies)}");
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[Economy] �κ��丮/��ȭ ���� �ε� ����: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }

    /// <summary>
    /// �÷��̾��� ���� ��ȭ�� �߰��ϰų� �����մϴ�.
    /// </summary>
    /// <param name="currencyCode">��ȭ �ڵ� (��: "GD" for Gold). PlayFab ��ú��忡�� ������ �ڵ�� ��ġ�ؾ� �մϴ�.</param>
    /// <param name="amount">������ �� (����� �߰�, ������ ����)</param>
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
                AddLog($"[Economy] ��ȭ ���� ����! {result.VirtualCurrency}: {result.Balance}");
                // ���� �����͵� ��� �����Ͽ� UI � �ٷ� �ݿ��� �� �ֵ��� ��
                VirtualCurrencies[result.VirtualCurrency] = result.Balance;
                // TODO: ��ȭ UI ������Ʈ ȣ��
            },
            (error) => AddLog($"[Economy] ��ȭ ���� ����: {error.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// īŻ�α� �������� ��ȭ�� ����� �����մϴ�.
    /// </summary>
    /// <param name="itemId">������ �������� ID</param>
    /// <param name="currencyCode">����� ��ȭ�� �ڵ�</param>
    /// <param name="price">������ ����</param>
    public void PurchaseItem(string itemId, string currencyCode, int price)
    {
        if (!IsLoggedIn) return;
        AddLog($"[Economy] ������ ���� �õ�: {itemId}");
        var request = new PurchaseItemRequest
        {
            ItemId = itemId,
            VirtualCurrency = currencyCode,
            Price = price
        };
        PlayFabClientAPI.PurchaseItem(request,
            (result) =>
            {
                AddLog($"[Economy] ������ ���� ����: {result.Items[0].DisplayName}");
                // ���� �� �ֽ� �κ��丮 ������ �ٽ� �ҷ���
                GetUserInventory();
            },
            (error) => AddLog($"[Economy] ������ ���� ����: {error.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// �Ҹ� �������� ����մϴ�.
    /// </summary>
    /// <param name="itemInstanceId">����� �������� ���� �ν��Ͻ� ID (ItemInstance.ItemInstanceId)</param>
    /// <param name="count">�Ҹ��� ����</param>
    public void ConsumeItem(string itemInstanceId, int count = 1)
    {
        if (!IsLoggedIn) return;
        AddLog($"[Economy] ������ ��� �õ�: {itemInstanceId}");
        var request = new ConsumeItemRequest
        {
            ItemInstanceId = itemInstanceId,
            ConsumeCount = count
        };
        PlayFabClientAPI.ConsumeItem(request,
            (result) =>
            {
                // ConsumeItemResult���� ItemId�� �����Ƿ�, ItemInstanceId�� ���� ���θ� �˸��ϴ�.
                AddLog($"[Economy] ������ ��� ����: {result.ItemInstanceId}, ���� Ƚ��: {result.RemainingUses}");
                // ������ ��� �� �ֽ� �κ��丮 ������ �ٽ� �ҷ���
                GetUserInventory();
            },
            (error) => AddLog($"[Economy] ������ ��� ����: {error.GenerateErrorReport()}")
        );
    }
    #endregion

    #region ### Ÿ��Ʋ ������ (���� ����) ###
    /// <summary>
    /// ���� ��ü ���� ������(TitleData)�� �������� �����ɴϴ�.
    /// </summary>
    private void GetTitleData(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[TitleData] Ÿ��Ʋ ������ �ε带 �����մϴ�.");
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            (result) =>
            {
                AddLog("[TitleData] Ÿ��Ʋ ������ �ε� ����!");
                TitleData = result.Data;
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[TitleData] Ÿ��Ʋ ������ �ε� ����: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }

    /// <summary>
    /// �������� TitleData�� Ư�� Ű�� �ش��ϴ� ���� �����ɴϴ�.
    /// </summary>
    /// <param name="key">������ �������� Ű</param>
    /// <param name="onComplete">���� �� ���� ���޹��� �ݹ�</param>
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
                    AddLog($"[TitleData] '{key}' �� �������� �������� ����: {value}");
                    onComplete?.Invoke(value);
                }
                else
                {
                    AddLog($"[TitleData] �������� '{key}' Ű�� ã�� �� �����ϴ�.");
                    onComplete?.Invoke(null);
                }
            },
            (error) =>
            {
                AddLog($"[TitleData] �������� '{key}' �� �������� ����: {error.GenerateErrorReport()}");
                onComplete?.Invoke(null);
            }
        );
    }

    #endregion

    #region ### īŻ�α� (������ ����) ###
    /// <summary>
    /// ���� ������ īŻ�α׸� �������� �����ɴϴ�.
    /// </summary>
    private void GetCatalogItems(Action onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke();
            return;
        }
        AddLog("[Catalog] ������ īŻ�α� �ε带 �����մϴ�.");
        var request = new GetCatalogItemsRequest { CatalogVersion = "Main" };
        PlayFabClientAPI.GetCatalogItems(request,
            (result) =>
            {
                AddLog("[Catalog] īŻ�α� �ε� ����!");
                Catalog = result.Catalog;
                onComplete?.Invoke();
            },
            (error) =>
            {
                AddLog($"[Catalog] īŻ�α� �ε� ����: {error.GenerateErrorReport()}");
                onComplete?.Invoke();
            }
        );
    }
    #endregion

    #region ### ���� �� ����� �α� ###
    /// <summary>
    /// ���� �޽����� UI �ؽ�Ʈ�� ǥ���մϴ�.
    /// </summary>
    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    /// <summary>
    /// ����� �α� ����Ʈ�� �޽����� �߰��ϰ� �ֿܼ��� ����մϴ�.
    /// </summary>
    private void AddLog(string message)
    {
        if (!enableDebugLogs) return;
        string log = $"[{DateTime.Now:HH:mm:ss}] {message}";
        debugLogMessages.Add(log);
        Debug.Log(log); // Unity �ֿܼ��� ���
        if (debugLogMessages.Count > 50) // �α״� �ֱ� 50���� ����
            debugLogMessages.RemoveAt(0);
    }

    [Header("����� ����")]
    [SerializeField] public bool enableDebugLogs = true; // ����� �α� UI ��� ����

    // IMGUI�� ����Ͽ� ȭ�鿡 ����� �α׸� ����մϴ�.
    void OnGUI()
    {
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, Screen.width * 0.5f, Screen.height * 0.5f), "PlayFabManager Debug Log", GUI.skin.window);
        enableDebugLogs = GUILayout.Toggle(enableDebugLogs, "����� �α� ���");
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

    #region ### ���� �ڵ� (���� ��� �ó�����) ###

    #endregion

    //���������� Ȯ�� �ȵ� ������ �ȵ� üũ��� (���� ������ �÷��̾� �����͵��� ���� �������� �ϴ±������ ������ �� ��)
    #region ### ���� ���� ��� �߰� ###
    /// <summary>
    /// ���� �÷��̾� �����͸� ������ ��� ���� �����մϴ�. (���� ���� ����)
    /// </summary>
    public void ForceSaveUserProperties(Dictionary<string, object> propertiesToSave)
    {
        if (!IsLoggedIn)
        {
            AddLog("�α��� ���°� �ƴϹǷ� ������ �� �����ϴ�.");
            return;
        }

        // ���� ���� ���� �ٷ� ����
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
            (result) => AddLog($"[UserProperties] ���� ���� ����: Keys = {string.Join(", ", dataPayload.Keys)}"),
            (error) => AddLog($"[UserProperties] ���� ���� ����: {error.GenerateErrorReport()}"));
    }
    #endregion
    #region ### �������� ������Ʈ

    /// <summary>
    /// TitleData�� Ư�� Ű �� ������ �����ϰ�, ���� �� ��ü �����͸� �ٽ� �ε��մϴ�.
    /// ��: �̺�Ʈ ���� ���� �� ��� ����. �ֱ������� ȣ���Ͽ� ���� ��������� Ȯ���� �� �ֽ��ϴ�.
    /// </summary>
    private void ForceDataUpdate()
    {
        string key = "ForceUpdate"; // ������ TitleData Ű

        // 1. ���� ���ÿ� ĳ�õ� ���� �����ɴϴ�.
        TitleData.TryGetValue(key, out string oldValue);

        // 2. �������� �ֽ� ���� �񵿱������� �����ɴϴ�.
        GetTitleDataValueFromServer(key, (newValue) =>
        {
            // 3. �������� ���� ���������� �����԰�, ���� ���� �ٸ� ���
            if (newValue != null && oldValue != newValue)
            {
                AddLog($"[TitleData] '{key}' �� ���� ����: '{oldValue ?? "����"}' -> '{newValue}'. ��� �����͸� �ٽ� �ε��մϴ�.");

                // 4. ��� �����͸� �ٽ� �ε��Ͽ� ��������� ���ӿ� �����մϴ�.
                // LoadAllData�� ���������� TitleData�� �����ϹǷ�, ���� TitleData[key] = newValue; �ڵ�� ���ʿ��մϴ�.
                //LoadAllData();
                LoadNonSaveData();//Ŭ���̾�Ʈ���� �������� �ʴ� ����
            }
            else
            {
                AddLog($"[TitleData] '{key}' ���� ������� �ʾҽ��ϴ�. (���� ��: {oldValue ?? "����"})");
            }
        });
    }


    #region ### PlayFab �߰� ��� ���̵�� ###

    //���� �� �¸�
    //����
    //�̺�Ʈ �Ⱓ ����
    //������ (�̺�Ʈ ���� ��ȭ ���淮 ���� �ɷ�ġ ��Ÿ ���)
    //���� ������ ��� ����
    //������ īŻ�α� ������Ʈ(���� ���� ���� ����)

    //���� �������� Ư�� ���̺긦(Ŭ�󿡼� �����ϴ� ������) ���� �����Ѵٸ�? - �������̺굥���� ����
    // ������ �޽��� ���
    // ���� ���� ��ȭ �Ǵ� ������ ���� ���� ���� ���� ��
    //���Ǻ� ��� ����(Ư�� ������ ����)
    //ȯ�� ���� ��ȭ ���̳ʽ�
    //

    //��Ƽ ����߰�
    //ü�� ��Ƽ�÷��� ��

    // --- �⺻ ��� Ȯ�� ---
    // 1. Ŭ���� ��ũ��Ʈ(Cloud Script): ���� �� ������ �����Ͽ� ���� ��ȭ �� ������ ���� ó��.
    //    - ��: ������ ��ȭ, �̱� �� Ȯ�� ��� �ý����� ����� �������� �����Ͽ� Ŭ���̾�Ʈ ���� ����.
    //    - ��: �÷��̾� ������ ���� (������ ����, ��ȭ �Ҹ� ��)�� �������� ����.
    //    - ��: ����� �۾��� ���� ���� ������ ���� ����Ʈ �ʱ�ȭ �Ǵ� �ְ� ��ŷ ���� ����.

    // 2. ����ǥ (Leaderboards / Player Statistics): �÷��̾� �� ���� ��� ����.
    //    - ��: �������� Ŭ���� �ð�, ���� ���� ������ ����ǥ ����.
    //    - ��: �ְ�/���� ����ǥ�� ��ϰ� ���� �������� ���� ����.

    // 3. �÷��̾� ���׸�Ʈ (Player Segments): ���� �׷캰 ���� �.
    //    - ��: '����� ����', '�ű� ����', '�޸� ����' ������ �׷��� ������ ���� �ٸ� Ǫ�� �˸�, �ΰ��� �޽���, Ư�� ���� ������ ����.

    // --- ���� ���� �� �Ҽ� ---
    // 4. ���� ���� �� ����: �Խ�Ʈ ������ �Ҽ� ����(Google, Facebook ��)�̳� �̸���/��й�ȣ�� �����Ͽ� ������ ��ȣ.
    //    - PlayFabClientAPI.LinkCustomIDAsync, LinkGoogleAccountAsync �� ���.
    //    - ��й�ȣ �缳�� ��� ����.

    // 5. ģ�� �ý���: ģ�� ���, ģ�� ��û/����/���� ���.
    //    - PlayFabClientAPI.GetFriendsListAsync, AddFriendAsync �� ���.
    //    - ģ������ ���� ������, ģ���� ���� ���� ��Ȳ ���� �� �Ҽ� ��� ����.

    // 6. ������/�޽��� �ý���: ��� ���� �Ǵ� Ư�� �������� ������, ��ȭ, �޽��� �߼�.
    //    - �ַ� ���� ����(Game Manager)�� Ŭ���� ��ũ��Ʈ�� ���� �������� �ο��ϰ�, Ŭ���̾�Ʈ�� �ش� ������ Ȯ���Ͽ� UI�� ǥ��.
    //    - ���� ����, �̺�Ʈ ����, ��� ���� ���� �����ϴ� �뵵�� Ȱ��.

    // --- BM �� ������ ---
    // 7. �ǽð� ����(IAP) ����: ���� �÷��� �����, ���� �۽���� ���� �������� PlayFab �������� �����Ͽ� ��ŷ�� ���� ����.
    //    - PlayFabClientAPI.ValidateGooglePlayPurchaseAsync, ValidateIOSReceiptAsync �� ���.

    // 8. A/B �׽�Ʈ: ������ ���� �׷��� ������� ���� ������ ���� ������ �����̳� ���� �뷱���� �׽�Ʈ�Ͽ� ������ ���� ����.
    //    - ��: A�׷쿡�� 1000��¥�� ��Ű��, B�׷쿡�� 1500��¥�� ��Ű���� �����Ͽ� ���� ��ȯ�� ��.

    // 9. �÷��̾� �� ������ �ŷ�: ������ Ȯ���� �ŷ� �ý��� ����.
    //    - PlayFabClientAPI.OpenTradeAsync, AcceptTradeAsync �� ���.

    // --- �ǽð� ��Ƽ�÷��� ---
    // 10. ��ġ����ŷ: ���ǿ� �´� �ٸ� �÷��̾ ã�� �ǽð� ��Ƽ�÷��� ���� ����.
    //     - PlayFabMultiplayerAPI.CreateMatchmakingTicketAsync �� ���.

    // 11. �κ� �� �ǽð� ������ ��� (Lobby & PubSub): ��ġ�� �÷��̾� ���� �غ� ����, ĳ���� ���� �� �����͸� �ǽð����� ����ȭ.
    //     - ��Ƽ�÷��� ������ ���� ��� ����.

    #endregion
    #endregion


}
