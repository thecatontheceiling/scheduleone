using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.GameTime;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.UI;
using ScheduleOne.Variables;
using UnityEngine;

namespace ScheduleOne.Employees;

public class Employee : NPC
{
	public class NoWorkReason
	{
		public string Reason;

		public string Fix;

		public int Priority;

		public NoWorkReason(string reason, string fix, int priority)
		{
			Reason = reason;
			Fix = fix;
			Priority = priority;
		}
	}

	public bool DEBUG;

	[CompilerGenerated]
	[SyncVar]
	public bool _003CPaidForToday_003Ek__BackingField;

	[SerializeField]
	protected EEmployeeType Type;

	[Header("Payment")]
	public float SigningFee = 500f;

	public float DailyWage = 100f;

	[Header("References")]
	public IdleBehaviour WaitOutside;

	public MoveItemBehaviour MoveItemBehaviour;

	public DialogueContainer BedNotAssignedDialogue;

	public DialogueContainer NotPaidDialogue;

	public DialogueContainer WorkIssueDialogueTemplate;

	public DialogueContainer FireDialogue;

	private List<NoWorkReason> WorkIssues = new List<NoWorkReason>();

	protected bool initialized;

	public SyncVar<bool> syncVar____003CPaidForToday_003Ek__BackingField;

	private bool NetworkInitialize___EarlyScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted;

	public ScheduleOne.Property.Property AssignedProperty { get; protected set; }

	public int EmployeeIndex { get; protected set; }

	public bool PaidForToday
	{
		[CompilerGenerated]
		get
		{
			return SyncAccessor__003CPaidForToday_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			this.sync___set_value__003CPaidForToday_003Ek__BackingField(value, asServer: true);
		}
	}

	public bool Fired { get; private set; }

	public bool IsWaitingOutside => WaitOutside.Active;

	public bool IsMale { get; private set; } = true;

	protected int AppearanceIndex { get; private set; }

	public EEmployeeType EmployeeType => Type;

	public int TimeSinceLastWorked { get; private set; }

	public bool SyncAccessor__003CPaidForToday_003Ek__BackingField
	{
		get
		{
			return PaidForToday;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				PaidForToday = value;
			}
			if (Application.isPlaying)
			{
				syncVar____003CPaidForToday_003Ek__BackingField.SetValue(value, value);
			}
		}
	}

	protected override void Start()
	{
		base.Start();
		DialogueController.DialogueChoice dialogueChoice = new DialogueController.DialogueChoice();
		dialogueChoice.ChoiceText = "I need to trade some items";
		dialogueChoice.Enabled = true;
		dialogueChoice.onChoosen.AddListener(TradeItems);
		dialogueHandler.GetComponent<DialogueController>().AddDialogueChoice(dialogueChoice, 3);
		DialogueController.DialogueChoice dialogueChoice2 = new DialogueController.DialogueChoice();
		dialogueChoice2.ChoiceText = "Why aren't you working?";
		dialogueChoice2.Enabled = true;
		dialogueChoice2.shouldShowCheck = ShouldShowNoWorkDialogue;
		dialogueChoice2.onChoosen.AddListener(OnNotWorkingDialogue);
		dialogueHandler.GetComponent<DialogueController>().AddDialogueChoice(dialogueChoice2);
		DialogueController.DialogueChoice dialogueChoice3 = new DialogueController.DialogueChoice();
		dialogueChoice3.ChoiceText = "Your services are no longer required.";
		dialogueChoice3.Enabled = true;
		dialogueChoice3.shouldShowCheck = ShouldShowFireDialogue;
		dialogueChoice3.Conversation = FireDialogue;
		dialogueHandler.GetComponent<DialogueController>().AddDialogueChoice(dialogueChoice3, -1);
		dialogueHandler.onDialogueChoiceChosen.AddListener(CheckDialogueChoice);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		Health.onDie.AddListener(SendFire);
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (!connection.IsLocalClient)
		{
			Initialize(connection, FirstName, LastName, ID, base.GUID.ToString(), AssignedProperty.PropertyCode, IsMale, AppearanceIndex);
		}
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public virtual void Initialize(NetworkConnection conn, string firstName, string lastName, string id, string guid, string propertyID, bool male, int appearanceIndex)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_Initialize_2260823878(conn, firstName, lastName, id, guid, propertyID, male, appearanceIndex);
			RpcLogic___Initialize_2260823878(conn, firstName, lastName, id, guid, propertyID, male, appearanceIndex);
		}
		else
		{
			RpcWriter___Target_Initialize_2260823878(conn, firstName, lastName, id, guid, propertyID, male, appearanceIndex);
		}
	}

	protected virtual void AssignProperty(ScheduleOne.Property.Property prop)
	{
		AssignedProperty = prop;
		EmployeeIndex = AssignedProperty.RegisterEmployee(this);
		movement.Warp(prop.NPCSpawnPoint.position);
		WaitOutside.IdlePoint = prop.EmployeeIdlePoints[EmployeeIndex];
	}

	protected virtual void InitializeInfo(string firstName, string lastName, string id)
	{
		FirstName = firstName;
		LastName = lastName;
		ID = id;
		NetworkSingleton<EmployeeManager>.Instance.RegisterName(firstName + " " + lastName);
	}

	protected virtual void InitializeAppearance(bool male, int index)
	{
		IsMale = male;
		AppearanceIndex = index;
		EmployeeManager.EmployeeAppearance appearance = NetworkSingleton<EmployeeManager>.Instance.GetAppearance(male, index);
		appearance.Settings.BodyLayerSettings.Clear();
		Avatar.LoadNakedSettings(appearance.Settings, 100);
		MugshotSprite = appearance.Mugshot;
		VoiceOverEmitter.SetDatabase(NetworkSingleton<EmployeeManager>.Instance.GetVoice(male, index));
		int num = (FirstName + LastName).GetHashCode() / 1000;
		VoiceOverEmitter.PitchMultiplier = 0.9f + (float)(num % 10) / 10f * 0.2f;
		NetworkSingleton<EmployeeManager>.Instance.RegisterAppearance(male, index);
		float num2 = (male ? 0.8f : 1.3f);
		float num3 = 0.2f;
		float num4 = (0f - num3) / 2f + Mathf.Clamp01((float)(FirstName.GetHashCode() % 10) / 10f) * num3;
		num2 += num4;
		VoiceOverEmitter.PitchMultiplier = num2;
	}

	protected virtual void CheckDialogueChoice(string choiceLabel)
	{
		if (choiceLabel == "CONFIRM_FIRE")
		{
			SendFire();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendFire()
	{
		RpcWriter___Server_SendFire_2166136261();
	}

	[ObserversRpc]
	private void ReceiveFire()
	{
		RpcWriter___Observers_ReceiveFire_2166136261();
	}

	protected virtual void Fire()
	{
		Console.Log("Firing employee " + FirstName + " " + LastName);
		AssignedProperty.DeregisterEmployee(this);
		Avatar.EmotionManager.AddEmotionOverride("Concerned", "fired");
		SetWaitOutside(wait: false);
		Fired = true;
	}

	protected bool CanWork()
	{
		if (GetBed() != null && SyncAccessor__003CPaidForToday_003Ek__BackingField)
		{
			return !NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.IsEndOfDay;
		}
		return false;
	}

	protected new virtual void OnDestroy()
	{
		if (InstanceFinder.IsServer)
		{
			ScheduleOne.GameTime.TimeManager.onSleepEnd = (Action<int>)Delegate.Remove(ScheduleOne.GameTime.TimeManager.onSleepEnd, new Action<int>(OnSleepEnd));
		}
		if (NetworkSingleton<EmployeeManager>.InstanceExists)
		{
			NetworkSingleton<EmployeeManager>.Instance.AllEmployees.Remove(this);
		}
	}

	protected virtual void UpdateBehaviour()
	{
		if (Fired || (!(behaviour.activeBehaviour == null) && !(behaviour.activeBehaviour == WaitOutside)))
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (GetBed() == null)
		{
			flag = true;
			SubmitNoWorkReason("I haven't been assigned a bed", "You can use your management clipboard to assign me a bed.");
		}
		else if (NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.IsEndOfDay)
		{
			flag = true;
			SubmitNoWorkReason("Sorry boss, my shift ends at 4AM.", string.Empty);
		}
		else if (!SyncAccessor__003CPaidForToday_003Ek__BackingField)
		{
			if (IsPayAvailable())
			{
				flag2 = true;
			}
			else
			{
				flag = true;
				SubmitNoWorkReason("I haven't been paid yet", "You can place cash in my briefcase on my bed.");
			}
		}
		if (flag)
		{
			SetWaitOutside(wait: true);
		}
		else if (InstanceFinder.IsServer && flag2 && IsPayAvailable())
		{
			RemoveDailyWage();
			SetIsPaid();
		}
	}

	protected void MarkIsWorking()
	{
		TimeSinceLastWorked = 0;
	}

	private void SetWaitOutside(bool wait)
	{
		if (wait)
		{
			if (!WaitOutside.Enabled)
			{
				WaitOutside.Enable_Networked(null);
			}
		}
		else if (WaitOutside.Enabled || WaitOutside.Active)
		{
			WaitOutside.Disable_Networked(null);
			WaitOutside.End_Networked(null);
		}
	}

	protected virtual bool ShouldIdle()
	{
		return false;
	}

	protected override bool ShouldNoticeGeneralCrime(Player player)
	{
		return false;
	}

	protected override void MinPass()
	{
		base.MinPass();
		TimeSinceLastWorked++;
		WorkIssues.Clear();
		UpdateBehaviour();
	}

	private void OnSleepEnd(int sleepTime)
	{
		PaidForToday = false;
	}

	public void SetIsPaid()
	{
		PaidForToday = true;
	}

	public override bool ShouldSave()
	{
		return false;
	}

	public override string GetSaveString()
	{
		return new EmployeeData(ID, AssignedProperty.PropertyCode, FirstName, LastName, IsMale, AppearanceIndex, base.transform.position, base.transform.rotation, base.GUID, SyncAccessor__003CPaidForToday_003Ek__BackingField).GetJson();
	}

	public virtual BedItem GetBed()
	{
		Console.LogError("GETBED NOT IMPLEMENTED");
		return null;
	}

	public bool IsPayAvailable()
	{
		BedItem bed = GetBed();
		if (bed == null)
		{
			return false;
		}
		return bed.GetCashSum() >= DailyWage;
	}

	public void RemoveDailyWage()
	{
		Console.Log("Removing daily wage");
		BedItem bed = GetBed();
		if (!(bed == null) && bed.GetCashSum() >= DailyWage)
		{
			bed.RemoveCash(DailyWage);
		}
	}

	public virtual bool GetWorkIssue(out DialogueContainer notWorkingReason)
	{
		if (GetBed() == null)
		{
			notWorkingReason = BedNotAssignedDialogue;
			return true;
		}
		if (!SyncAccessor__003CPaidForToday_003Ek__BackingField)
		{
			notWorkingReason = NotPaidDialogue;
			return true;
		}
		if (TimeSinceLastWorked >= 5 && WorkIssues.Count > 0)
		{
			notWorkingReason = UnityEngine.Object.Instantiate(WorkIssueDialogueTemplate);
			notWorkingReason.GetDialogueNodeByLabel("ENTRY").DialogueText = WorkIssues[0].Reason;
			if (!string.IsNullOrEmpty(WorkIssues[0].Fix))
			{
				notWorkingReason.GetDialogueNodeByLabel("FIX").DialogueText = WorkIssues[0].Fix;
			}
			else
			{
				notWorkingReason.GetDialogueNodeByLabel("ENTRY").choices = new DialogueChoiceData[0];
			}
			return true;
		}
		notWorkingReason = null;
		return false;
	}

	public virtual void SetIdle(bool idle)
	{
		SetWaitOutside(idle);
	}

	protected void LeavePropertyAndDespawn()
	{
		if (!movement.IsMoving && InstanceFinder.IsServer)
		{
			if (movement.IsAsCloseAsPossible(AssignedProperty.NPCSpawnPoint.position, 1f))
			{
				Despawn(base.NetworkObject);
			}
			else
			{
				movement.SetDestination(AssignedProperty.NPCSpawnPoint.position);
			}
		}
	}

	[ObserversRpc(RunLocally = true)]
	public void SubmitNoWorkReason(string reason, string fix, int priority = 0)
	{
		RpcWriter___Observers_SubmitNoWorkReason_15643032(reason, fix, priority);
		RpcLogic___SubmitNoWorkReason_15643032(reason, fix, priority);
	}

	private bool ShouldShowNoWorkDialogue(bool enabled)
	{
		if (Fired)
		{
			return false;
		}
		DialogueContainer notWorkingReason;
		if (WaitOutside.Active)
		{
			return GetWorkIssue(out notWorkingReason);
		}
		return false;
	}

	private void OnNotWorkingDialogue()
	{
		if (GetWorkIssue(out var notWorkingReason))
		{
			dialogueHandler.InitializeDialogue(notWorkingReason);
		}
	}

	private bool ShouldShowFireDialogue(bool enabled)
	{
		if (Fired)
		{
			return false;
		}
		return true;
	}

	private void TradeItems()
	{
		dialogueHandler.SkipNextDialogueBehaviourEnd();
		Singleton<StorageMenu>.Instance.Open(base.Inventory, base.fullName + "'s Inventory", string.Empty);
		Singleton<StorageMenu>.Instance.onClosed.AddListener(TradeItemsDone);
	}

	private void TradeItemsDone()
	{
		Singleton<StorageMenu>.Instance.onClosed.RemoveListener(TradeItemsDone);
		behaviour.GenericDialogueBehaviour.SendDisable();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar____003CPaidForToday_003Ek__BackingField = new SyncVar<bool>(this, 1u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, PaidForToday);
			RegisterObserversRpc(35u, RpcReader___Observers_Initialize_2260823878);
			RegisterTargetRpc(36u, RpcReader___Target_Initialize_2260823878);
			RegisterServerRpc(37u, RpcReader___Server_SendFire_2166136261);
			RegisterObserversRpc(38u, RpcReader___Observers_ReceiveFire_2166136261);
			RegisterObserversRpc(39u, RpcReader___Observers_SubmitNoWorkReason_15643032);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EEmployees_002EEmployee);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEmployees_002EEmployeeAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
			syncVar____003CPaidForToday_003Ek__BackingField.SetRegistered();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Observers_Initialize_2260823878(NetworkConnection conn, string firstName, string lastName, string id, string guid, string propertyID, bool male, int appearanceIndex)
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
			writer.WriteString(firstName);
			writer.WriteString(lastName);
			writer.WriteString(id);
			writer.WriteString(guid);
			writer.WriteString(propertyID);
			writer.WriteBoolean(male);
			writer.WriteInt32(appearanceIndex);
			SendObserversRpc(35u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public virtual void RpcLogic___Initialize_2260823878(NetworkConnection conn, string firstName, string lastName, string id, string guid, string propertyID, bool male, int appearanceIndex)
	{
		if (initialized)
		{
			return;
		}
		NetworkSingleton<EmployeeManager>.Instance.AllEmployees.Add(this);
		initialized = true;
		SetGUID(new Guid(guid));
		InitializeInfo(firstName, lastName, id);
		InitializeAppearance(male, appearanceIndex);
		AssignProperty(Singleton<PropertyManager>.Instance.GetProperty(propertyID));
		movement.Agent.avoidancePriority = 10 + appearanceIndex;
		if (InstanceFinder.IsServer)
		{
			if (!NetworkSingleton<VariableDatabase>.Instance.GetValue<bool>("ClipboardAcquired"))
			{
				NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("ClipboardAcquired", true.ToString());
			}
			ScheduleOne.GameTime.TimeManager.onSleepEnd = (Action<int>)Delegate.Combine(ScheduleOne.GameTime.TimeManager.onSleepEnd, new Action<int>(OnSleepEnd));
		}
	}

	private void RpcReader___Observers_Initialize_2260823878(PooledReader PooledReader0, Channel channel)
	{
		string firstName = PooledReader0.ReadString();
		string lastName = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		string guid = PooledReader0.ReadString();
		string propertyID = PooledReader0.ReadString();
		bool male = PooledReader0.ReadBoolean();
		int appearanceIndex = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___Initialize_2260823878(null, firstName, lastName, id, guid, propertyID, male, appearanceIndex);
		}
	}

	private void RpcWriter___Target_Initialize_2260823878(NetworkConnection conn, string firstName, string lastName, string id, string guid, string propertyID, bool male, int appearanceIndex)
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
			writer.WriteString(firstName);
			writer.WriteString(lastName);
			writer.WriteString(id);
			writer.WriteString(guid);
			writer.WriteString(propertyID);
			writer.WriteBoolean(male);
			writer.WriteInt32(appearanceIndex);
			SendTargetRpc(36u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_Initialize_2260823878(PooledReader PooledReader0, Channel channel)
	{
		string firstName = PooledReader0.ReadString();
		string lastName = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		string guid = PooledReader0.ReadString();
		string propertyID = PooledReader0.ReadString();
		bool male = PooledReader0.ReadBoolean();
		int appearanceIndex = PooledReader0.ReadInt32();
		if (base.IsClientInitialized)
		{
			RpcLogic___Initialize_2260823878(base.LocalConnection, firstName, lastName, id, guid, propertyID, male, appearanceIndex);
		}
	}

	private void RpcWriter___Server_SendFire_2166136261()
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
			SendServerRpc(37u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendFire_2166136261()
	{
		ReceiveFire();
	}

	private void RpcReader___Server_SendFire_2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		if (base.IsServerInitialized)
		{
			RpcLogic___SendFire_2166136261();
		}
	}

	private void RpcWriter___Observers_ReceiveFire_2166136261()
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
			SendObserversRpc(38u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___ReceiveFire_2166136261()
	{
		Fire();
	}

	private void RpcReader___Observers_ReceiveFire_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceiveFire_2166136261();
		}
	}

	private void RpcWriter___Observers_SubmitNoWorkReason_15643032(string reason, string fix, int priority = 0)
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
			writer.WriteString(reason);
			writer.WriteString(fix);
			writer.WriteInt32(priority);
			SendObserversRpc(39u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SubmitNoWorkReason_15643032(string reason, string fix, int priority = 0)
	{
		NoWorkReason noWorkReason = new NoWorkReason(reason, fix, priority);
		for (int i = 0; i < WorkIssues.Count; i++)
		{
			if (WorkIssues[i].Priority < noWorkReason.Priority)
			{
				WorkIssues.Insert(i, noWorkReason);
				return;
			}
		}
		WorkIssues.Add(noWorkReason);
	}

	private void RpcReader___Observers_SubmitNoWorkReason_15643032(PooledReader PooledReader0, Channel channel)
	{
		string reason = PooledReader0.ReadString();
		string fix = PooledReader0.ReadString();
		int priority = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SubmitNoWorkReason_15643032(reason, fix, priority);
		}
	}

	public virtual bool ReadSyncVar___ScheduleOne_002EEmployees_002EEmployee(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		if (UInt321 == 1)
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CPaidForToday_003Ek__BackingField(syncVar____003CPaidForToday_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			bool value = PooledReader0.ReadBoolean();
			this.sync___set_value__003CPaidForToday_003Ek__BackingField(value, Boolean2);
			return true;
		}
		return false;
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
