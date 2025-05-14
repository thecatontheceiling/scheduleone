using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.Variables;
using TMPro;
using UnityEngine;

namespace ScheduleOne.Money;

public class MoneyManager : NetworkSingleton<MoneyManager>, IBaseSaveable, ISaveable
{
	public class FloatContainer
	{
		public float value { get; private set; }

		public void ChangeValue(float value)
		{
			this.value += value;
		}
	}

	public const string MONEY_TEXT_COLOR = "#54E717";

	public const string MONEY_TEXT_COLOR_DARKER = "#46CB4F";

	public const string ONLINE_BALANCE_COLOR = "#4CBFFF";

	public List<Transaction> ledger = new List<Transaction>();

	[SyncVar(WritePermissions = WritePermission.ClientUnsynchronized)]
	public float onlineBalance;

	[SyncVar(WritePermissions = WritePermission.ClientUnsynchronized)]
	public float lifetimeEarnings;

	public AudioSourceController CashSound;

	[Header("Prefabs")]
	[SerializeField]
	protected GameObject moneyChangePrefab;

	[SerializeField]
	protected GameObject cashChangePrefab;

	public Sprite LaunderingNotificationIcon;

	public Action<FloatContainer> onNetworthCalculation;

	private MoneyLoader loader = new MoneyLoader();

	public SyncVar<float> syncVar___onlineBalance;

	public SyncVar<float> syncVar___lifetimeEarnings;

	private bool NetworkInitialize___EarlyScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted;

	public float LifetimeEarnings => SyncAccessor_lifetimeEarnings;

	public float LastCalculatedNetworth { get; protected set; }

	public float cashBalance => cashInstance.Balance;

	protected CashInstance cashInstance => PlayerSingleton<PlayerInventory>.Instance.cashInstance;

	public string SaveFolderName => "Money";

	public string SaveFileName => "Money";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; }

	public float SyncAccessor_onlineBalance
	{
		get
		{
			return onlineBalance;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				onlineBalance = value;
			}
			if (Application.isPlaying)
			{
				syncVar___onlineBalance.SetValue(value, value);
			}
		}
	}

	public float SyncAccessor_lifetimeEarnings
	{
		get
		{
			return lifetimeEarnings;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				lifetimeEarnings = value;
			}
			if (Application.isPlaying)
			{
				syncVar___lifetimeEarnings.SetValue(value, value);
			}
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EMoney_002EMoneyManager_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	protected override void Start()
	{
		base.Start();
		Singleton<LoadManager>.Instance.onLoadComplete.AddListener(Loaded);
		TimeManager timeManager = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		timeManager.onMinutePass = (Action)Delegate.Combine(timeManager.onMinutePass, new Action(MinPass));
		TimeManager timeManager2 = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		timeManager2.onDayPass = (Action)Delegate.Combine(timeManager2.onDayPass, new Action(CheckNetworthAchievements));
		Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(SyncAccessor_onlineBalance);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("LifetimeEarnings", lifetimeEarnings.ToString());
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(SyncAccessor_onlineBalance);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkSingleton<ScheduleOne.GameTime.TimeManager>.InstanceExists)
		{
			TimeManager timeManager = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
			timeManager.onMinutePass = (Action)Delegate.Remove(timeManager.onMinutePass, new Action(MinPass));
			TimeManager timeManager2 = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
			timeManager2.onDayPass = (Action)Delegate.Remove(timeManager2.onDayPass, new Action(CheckNetworthAchievements));
		}
		if (Singleton<LoadManager>.InstanceExists)
		{
			Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(Loaded);
		}
	}

	private void Loaded()
	{
		GetNetWorth();
		Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(SyncAccessor_onlineBalance);
	}

	private void Update()
	{
		HasChanged = true;
	}

	private void MinPass()
	{
		if (NetworkSingleton<VariableDatabase>.InstanceExists)
		{
			NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Online_Balance", onlineBalance.ToString(), network: false);
			if (PlayerSingleton<PlayerInventory>.InstanceExists)
			{
				NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Cash_Balance", cashBalance.ToString(), network: false);
				NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Total_Money", (SyncAccessor_onlineBalance + cashBalance).ToString(), network: false);
			}
		}
	}

	public CashInstance GetCashInstance(float amount)
	{
		CashInstance obj = Registry.GetItem<CashDefinition>("cash").GetDefaultInstance() as CashInstance;
		obj.SetBalance(amount);
		return obj;
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void CreateOnlineTransaction(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		RpcWriter___Server_CreateOnlineTransaction_1419830531(_transaction_Name, _unit_Amount, _quantity, _transaction_Note);
		RpcLogic___CreateOnlineTransaction_1419830531(_transaction_Name, _unit_Amount, _quantity, _transaction_Note);
	}

	[ObserversRpc]
	private void ReceiveOnlineTransaction(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		RpcWriter___Observers_ReceiveOnlineTransaction_1419830531(_transaction_Name, _unit_Amount, _quantity, _transaction_Note);
	}

	protected IEnumerator ShowOnlineBalanceChange(RectTransform changeDisplay)
	{
		TextMeshProUGUI text = changeDisplay.GetComponent<TextMeshProUGUI>();
		float startVert = changeDisplay.anchoredPosition.y;
		float lerpTime = 2.5f;
		float vertOffset = startVert + 60f;
		for (float i = 0f; i < lerpTime; i += Time.unscaledDeltaTime)
		{
			text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Lerp(1f, 0f, i / lerpTime));
			changeDisplay.anchoredPosition = new Vector2(changeDisplay.anchoredPosition.x, Mathf.Lerp(startVert, vertOffset, i / lerpTime));
			yield return new WaitForEndOfFrame();
		}
		UnityEngine.Object.Destroy(changeDisplay.gameObject);
	}

	[ServerRpc(RequireOwnership = false)]
	public void ChangeLifetimeEarnings(float change)
	{
		RpcWriter___Server_ChangeLifetimeEarnings_431000436(change);
	}

	public void ChangeCashBalance(float change, bool visualizeChange = true, bool playCashSound = false)
	{
		float num = Mathf.Clamp(cashInstance.Balance + change, 0f, float.MaxValue) - cashInstance.Balance;
		cashInstance.ChangeBalance(change);
		if (playCashSound && num != 0f)
		{
			Console.Log("Playing cash sound: " + num);
			CashSound.Play();
		}
		if (visualizeChange && num != 0f)
		{
			RectTransform component = UnityEngine.Object.Instantiate(cashChangePrefab, Singleton<HUD>.Instance.cashSlotContainer).GetComponent<RectTransform>();
			component.position = new Vector3(Singleton<HUD>.Instance.cashSlotUI.position.x, component.position.y);
			component.anchoredPosition = new Vector2(component.anchoredPosition.x, 10f);
			TextMeshProUGUI component2 = component.GetComponent<TextMeshProUGUI>();
			if (num > 0f)
			{
				component2.text = "+ " + FormatAmount(num);
				component2.color = new Color32(25, 240, 30, byte.MaxValue);
			}
			else
			{
				component2.text = FormatAmount(num);
				component2.color = new Color32(176, 63, 59, byte.MaxValue);
			}
			Singleton<CoroutineService>.Instance.StartCoroutine(ShowCashChange(component));
		}
	}

	protected IEnumerator ShowCashChange(RectTransform changeDisplay)
	{
		TextMeshProUGUI text = changeDisplay.GetComponent<TextMeshProUGUI>();
		float startVert = changeDisplay.anchoredPosition.y;
		float lerpTime = 2.5f;
		float vertOffset = startVert + 60f;
		for (float i = 0f; i < lerpTime; i += Time.unscaledDeltaTime)
		{
			text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Lerp(1f, 0f, i / lerpTime));
			changeDisplay.anchoredPosition = new Vector2(changeDisplay.anchoredPosition.x, Mathf.Lerp(startVert, vertOffset, i / lerpTime));
			yield return new WaitForEndOfFrame();
		}
		UnityEngine.Object.Destroy(changeDisplay.gameObject);
	}

	public static string FormatAmount(float amount, bool showDecimals = false, bool includeColor = false)
	{
		string text = string.Empty;
		if (includeColor)
		{
			text += "<color=#54E717>";
		}
		if (amount < 0f)
		{
			text = "-";
		}
		text = ((!showDecimals) ? (text + string.Format(new CultureInfo("en-US"), "{0:C0}", Mathf.RoundToInt(Mathf.Abs(amount)))) : (text + string.Format(new CultureInfo("en-US"), "{0:C}", Mathf.Abs(amount))));
		if (includeColor)
		{
			text += "</color>";
		}
		return text;
	}

	public virtual string GetSaveString()
	{
		return new MoneyData(SyncAccessor_onlineBalance, GetNetWorth(), SyncAccessor_lifetimeEarnings, ATM.WeeklyDepositSum).GetJson();
	}

	public void Load(MoneyData data)
	{
		this.sync___set_value_onlineBalance(Mathf.Clamp(data.OnlineBalance, 0f, float.MaxValue), asServer: true);
		this.sync___set_value_lifetimeEarnings(Mathf.Clamp(data.LifetimeEarnings, 0f, float.MaxValue), asServer: true);
		Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(SyncAccessor_onlineBalance);
		ATM.WeeklyDepositSum = data.WeeklyDepositSum;
	}

	public void CheckNetworthAchievements()
	{
		float netWorth = GetNetWorth();
		if (netWorth >= 100000f)
		{
			Singleton<AchievementManager>.Instance.UnlockAchievement(AchievementManager.EAchievement.BUSINESSMAN);
		}
		if (netWorth >= 1000000f)
		{
			Singleton<AchievementManager>.Instance.UnlockAchievement(AchievementManager.EAchievement.BIGWIG);
		}
		if (netWorth >= 10000000f)
		{
			Singleton<AchievementManager>.Instance.UnlockAchievement(AchievementManager.EAchievement.MAGNATE);
		}
	}

	public float GetNetWorth()
	{
		float num = 0f;
		num += SyncAccessor_onlineBalance;
		if (onNetworthCalculation != null)
		{
			FloatContainer floatContainer = new FloatContainer();
			onNetworthCalculation(floatContainer);
			num += floatContainer.value;
		}
		LastCalculatedNetworth = num;
		return num;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar___lifetimeEarnings = new SyncVar<float>(this, 1u, WritePermission.ClientUnsynchronized, ReadPermission.Observers, -1f, Channel.Reliable, lifetimeEarnings);
			syncVar___onlineBalance = new SyncVar<float>(this, 0u, WritePermission.ClientUnsynchronized, ReadPermission.Observers, -1f, Channel.Reliable, onlineBalance);
			RegisterServerRpc(0u, RpcReader___Server_CreateOnlineTransaction_1419830531);
			RegisterObserversRpc(1u, RpcReader___Observers_ReceiveOnlineTransaction_1419830531);
			RegisterServerRpc(2u, RpcReader___Server_ChangeLifetimeEarnings_431000436);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EMoney_002EMoneyManager);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EMoney_002EMoneyManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
			syncVar___lifetimeEarnings.SetRegistered();
			syncVar___onlineBalance.SetRegistered();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_CreateOnlineTransaction_1419830531(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(_transaction_Name);
			writer.WriteSingle(_unit_Amount);
			writer.WriteSingle(_quantity);
			writer.WriteString(_transaction_Note);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___CreateOnlineTransaction_1419830531(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		ReceiveOnlineTransaction(_transaction_Name, _unit_Amount, _quantity, _transaction_Note);
	}

	private void RpcReader___Server_CreateOnlineTransaction_1419830531(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string transaction_Name = PooledReader0.ReadString();
		float unit_Amount = PooledReader0.ReadSingle();
		float quantity = PooledReader0.ReadSingle();
		string transaction_Note = PooledReader0.ReadString();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___CreateOnlineTransaction_1419830531(transaction_Name, unit_Amount, quantity, transaction_Note);
		}
	}

	private void RpcWriter___Observers_ReceiveOnlineTransaction_1419830531(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(_transaction_Name);
			writer.WriteSingle(_unit_Amount);
			writer.WriteSingle(_quantity);
			writer.WriteString(_transaction_Note);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___ReceiveOnlineTransaction_1419830531(string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
	{
		Transaction transaction = new Transaction(_transaction_Name, _unit_Amount, _quantity, _transaction_Note);
		ledger.Add(transaction);
		this.sync___set_value_onlineBalance(SyncAccessor_onlineBalance + transaction.total_Amount, asServer: true);
		Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(SyncAccessor_onlineBalance);
		Singleton<HUD>.Instance.OnlineBalanceDisplay.Show();
		RectTransform component = UnityEngine.Object.Instantiate(moneyChangePrefab, Singleton<HUD>.Instance.cashSlotContainer).GetComponent<RectTransform>();
		component.position = new Vector3(Singleton<HUD>.Instance.onlineBalanceSlotUI.position.x, component.position.y);
		component.anchoredPosition = new Vector2(component.anchoredPosition.x, 10f);
		TextMeshProUGUI component2 = component.GetComponent<TextMeshProUGUI>();
		if (transaction.total_Amount > 0f)
		{
			component2.text = "+ " + FormatAmount(transaction.total_Amount);
			component2.color = new Color32(25, 190, 240, byte.MaxValue);
		}
		else
		{
			component2.text = FormatAmount(transaction.total_Amount);
			component2.color = new Color32(176, 63, 59, byte.MaxValue);
		}
		Singleton<CoroutineService>.Instance.StartCoroutine(ShowOnlineBalanceChange(component));
		HasChanged = true;
	}

	private void RpcReader___Observers_ReceiveOnlineTransaction_1419830531(PooledReader PooledReader0, Channel channel)
	{
		string transaction_Name = PooledReader0.ReadString();
		float unit_Amount = PooledReader0.ReadSingle();
		float quantity = PooledReader0.ReadSingle();
		string transaction_Note = PooledReader0.ReadString();
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceiveOnlineTransaction_1419830531(transaction_Name, unit_Amount, quantity, transaction_Note);
		}
	}

	private void RpcWriter___Server_ChangeLifetimeEarnings_431000436(float change)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteSingle(change);
			SendServerRpc(2u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___ChangeLifetimeEarnings_431000436(float change)
	{
		this.sync___set_value_lifetimeEarnings(Mathf.Clamp(SyncAccessor_lifetimeEarnings + change, 0f, float.MaxValue), asServer: true);
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("LifetimeEarnings", lifetimeEarnings.ToString());
	}

	private void RpcReader___Server_ChangeLifetimeEarnings_431000436(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		float change = PooledReader0.ReadSingle();
		if (base.IsServerInitialized)
		{
			RpcLogic___ChangeLifetimeEarnings_431000436(change);
		}
	}

	public virtual bool ReadSyncVar___ScheduleOne_002EMoney_002EMoneyManager(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		switch (UInt321)
		{
		case 1u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value_lifetimeEarnings(syncVar___lifetimeEarnings.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			float value2 = PooledReader0.ReadSingle();
			this.sync___set_value_lifetimeEarnings(value2, Boolean2);
			return true;
		}
		case 0u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value_onlineBalance(syncVar___onlineBalance.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			float value = PooledReader0.ReadSingle();
			this.sync___set_value_onlineBalance(value, Boolean2);
			return true;
		}
		default:
			return false;
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EMoney_002EMoneyManager_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		InitializeSaveable();
	}
}
