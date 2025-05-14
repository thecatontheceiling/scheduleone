using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Levelling;
using ScheduleOne.Messaging;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Relation;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Quests;
using ScheduleOne.Storage;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Phone.Messages;
using ScheduleOne.UI.Shop;
using ScheduleOne.Variables;
using ScheduleOne.VoiceOver;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Economy;

public class Supplier : NPC
{
	public enum ESupplierStatus
	{
		Idle = 0,
		PreppingDeadDrop = 1,
		Meeting = 2
	}

	public const float MEETUP_RELATIONSHIP_REQUIREMENT = 4f;

	public const int MEETUP_DURATION_MINS = 360;

	public const int MEETING_COOLDOWN_MINS = 720;

	public const int DEADDROP_WAIT_PER_ITEM = 30;

	public const int DEADDROP_MAX_WAIT = 360;

	public const int DEADDROP_ITEM_LIMIT = 10;

	public const float DELIVERY_RELATIONSHIP_REQUIREMENT = 5f;

	public static Color32 SupplierLabelColor = new Color32(byte.MaxValue, 150, 145, byte.MaxValue);

	[Header("Supplier Settings")]
	public float MinOrderLimit = 100f;

	public float MaxOrderLimit = 500f;

	public PhoneShopInterface.Listing[] OnlineShopItems;

	[TextArea(3, 10)]
	public string SupplierRecommendMessage = "My friend <NAME> can hook you up with <PRODUCT>. I've passed your number on to them.";

	[TextArea(3, 10)]
	public string SupplierUnlockHint = "You can now order <PRODUCT> from <NAME>. <PRODUCT> can be used to <PURPOSE>.";

	[Header("References")]
	public ShopInterface Shop;

	public SupplierStash Stash;

	public UnityEvent onDeaddropReady;

	private int minsSinceMeetingStart = -1;

	private int minsSinceLastMeetingEnd = 720;

	private SupplierLocation currentLocation;

	private DialogueController dialogueController;

	private DialogueController.GreetingOverride meetingGreeting;

	private DialogueController.DialogueChoice meetingChoice;

	[SyncVar]
	public float debt;

	[SyncVar]
	public bool deadDropPreparing;

	private StringIntPair[] deaddropItems;

	private int minsSinceDeaddropOrder;

	private bool repaymentReminderSent;

	public SyncVar<float> syncVar___debt;

	public SyncVar<bool> syncVar___deadDropPreparing;

	private bool NetworkInitialize___EarlyScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted;

	public ESupplierStatus Status { get; private set; }

	public bool DeliveriesEnabled { get; private set; }

	public float Debt => SyncAccessor_debt;

	public int minsUntilDeaddropReady { get; private set; } = -1;

	public float SyncAccessor_debt
	{
		get
		{
			return debt;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				debt = value;
			}
			if (Application.isPlaying)
			{
				syncVar___debt.SetValue(value, value);
			}
		}
	}

	public bool SyncAccessor_deadDropPreparing
	{
		get
		{
			return deadDropPreparing;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				deadDropPreparing = value;
			}
			if (Application.isPlaying)
			{
				syncVar___deadDropPreparing.SetValue(value, value);
			}
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EEconomy_002ESupplier_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Start()
	{
		base.Start();
		NPCRelationData relationData = RelationData;
		relationData.onUnlocked = (Action<NPCRelationData.EUnlockType, bool>)Delegate.Combine(relationData.onUnlocked, new Action<NPCRelationData.EUnlockType, bool>(SupplierUnlocked));
		NPCRelationData relationData2 = RelationData;
		relationData2.onRelationshipChange = (Action<float>)Delegate.Combine(relationData2.onRelationshipChange, new Action<float>(RelationshipChange));
		string orderCompleteDialogue = dialogueHandler.Database.GetLine(EDialogueModule.Generic, "meeting_order_complete");
		Shop.onOrderCompleted.AddListener(delegate
		{
			dialogueHandler.ShowWorldspaceDialogue(orderCompleteDialogue, 3f);
		});
		dialogueController = dialogueHandler.GetComponent<DialogueController>();
		meetingGreeting = new DialogueController.GreetingOverride();
		meetingGreeting.Greeting = dialogueHandler.Database.GetLine(EDialogueModule.Generic, "supplier_meeting_greeting");
		meetingGreeting.PlayVO = true;
		meetingGreeting.VOType = EVOLineType.Question;
		dialogueController.AddGreetingOverride(meetingGreeting);
		meetingChoice = new DialogueController.DialogueChoice();
		meetingChoice.ChoiceText = "Yes";
		meetingChoice.onChoosen.AddListener(delegate
		{
			Shop.SetIsOpen(isOpen: true);
		});
		meetingChoice.Enabled = false;
		dialogueController.AddDialogueChoice(meetingChoice);
		TimeManager instance = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		instance.onTimeSkip = (Action<int>)Delegate.Combine(instance.onTimeSkip, new Action<int>(OnTimeSkip));
		PhoneShopInterface.Listing[] onlineShopItems = OnlineShopItems;
		foreach (PhoneShopInterface.Listing listing in onlineShopItems)
		{
			if ((listing.Item as StorableItemDefinition).RequiresLevelToPurchase)
			{
				NetworkSingleton<LevelManager>.Instance.AddUnlockable(new Unlockable((listing.Item as StorableItemDefinition).RequiredRank, listing.Item.Name, listing.Item.Icon));
			}
		}
		TimeManager instance2 = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		instance2.onHourPass = (Action)Delegate.Remove(instance2.onHourPass, new Action(HourPass));
		TimeManager instance3 = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		instance3.onHourPass = (Action)Delegate.Combine(instance3.onHourPass, new Action(HourPass));
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (!connection.IsLocalClient)
		{
			if (Status == ESupplierStatus.Meeting)
			{
				MeetAtLocation(connection, SupplierLocation.AllLocations.IndexOf(currentLocation), 360);
			}
			if (DeliveriesEnabled)
			{
				EnableDeliveries(connection);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendUnlocked()
	{
		RpcWriter___Server_SendUnlocked_2166136261();
	}

	[ObserversRpc]
	private void SetUnlocked()
	{
		RpcWriter___Observers_SetUnlocked_2166136261();
	}

	protected override void MinPass()
	{
		base.MinPass();
		minsSinceDeaddropOrder++;
		if (Status == ESupplierStatus.Meeting)
		{
			minsSinceMeetingStart++;
			minsSinceLastMeetingEnd = 0;
			if (minsSinceMeetingStart > 360)
			{
				EndMeeting();
			}
		}
		else
		{
			minsSinceLastMeetingEnd++;
		}
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (SyncAccessor_deadDropPreparing)
		{
			minsUntilDeaddropReady--;
			if (minsUntilDeaddropReady <= 0)
			{
				CompleteDeaddrop();
			}
		}
		if (SyncAccessor_debt > 0f && !Stash.Storage.IsOpened && Stash.CashAmount > 1f && minsSinceDeaddropOrder > 3)
		{
			TryRecoverDebt();
		}
	}

	protected void HourPass()
	{
		if (InstanceFinder.IsServer && !repaymentReminderSent && SyncAccessor_debt > GetDeadDropLimit() * 0.5f && !SyncAccessor_deadDropPreparing)
		{
			float num = 1f / 48f;
			if (UnityEngine.Random.Range(0f, 1f) < num)
			{
				SendDebtReminder();
			}
		}
	}

	private void OnTimeSkip(int minsSlept)
	{
		if (Status == ESupplierStatus.Meeting)
		{
			minsSinceMeetingStart += minsSlept;
		}
		if (SyncAccessor_deadDropPreparing)
		{
			minsUntilDeaddropReady -= minsSlept;
		}
	}

	[ObserversRpc(RunLocally = true)]
	public void MeetAtLocation(NetworkConnection conn, int locationIndex, int expireIn)
	{
		RpcWriter___Observers_MeetAtLocation_3470796954(conn, locationIndex, expireIn);
		RpcLogic___MeetAtLocation_3470796954(conn, locationIndex, expireIn);
	}

	public void EndMeeting()
	{
		Console.Log("Meeting ended");
		Status = ESupplierStatus.Idle;
		minsSinceMeetingStart = -1;
		meetingGreeting.ShouldShow = false;
		meetingChoice.Enabled = false;
		currentLocation.SetActiveSupplier(null);
		SetVisible(visible: false);
	}

	protected virtual void SupplierUnlocked(NPCRelationData.EUnlockType type, bool notify)
	{
		if (notify)
		{
			SetUnlockMessage();
		}
	}

	protected virtual void RelationshipChange(float change)
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (Singleton<LoadManager>.Instance.IsLoading)
		{
			if (RelationData.RelationDelta >= 5f && !DeliveriesEnabled)
			{
				EnableDeliveries(null);
			}
			return;
		}
		float num = RelationData.RelationDelta - change;
		float relationDelta = RelationData.RelationDelta;
		if (num < 4f && relationDelta >= 4f)
		{
			Console.Log("Supplier relationship high enough for meetings");
			DialogueChain chain = dialogueHandler.Database.GetChain(EDialogueModule.Generic, "supplier_meetings_unlocked");
			if (chain == null)
			{
				return;
			}
			base.MSGConversation.SendMessageChain(chain.GetMessageChain(), 3f);
		}
		if (relationDelta >= 5f && !DeliveriesEnabled)
		{
			Console.Log("Supplier relationship high enough for deliveries");
			EnableDeliveries(null);
			DialogueChain chain2 = dialogueHandler.Database.GetChain(EDialogueModule.Generic, "supplier_deliveries_unlocked");
			if (chain2 != null)
			{
				base.MSGConversation.SendMessageChain(chain2.GetMessageChain(), 3f);
			}
		}
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	private void EnableDeliveries(NetworkConnection conn)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_EnableDeliveries_328543758(conn);
			RpcLogic___EnableDeliveries_328543758(conn);
		}
		else
		{
			RpcWriter___Target_EnableDeliveries_328543758(conn);
		}
	}

	public void SetUnlockMessage()
	{
		if (InstanceFinder.IsServer)
		{
			DialogueChain chain = dialogueHandler.Database.GetChain(EDialogueModule.Generic, "supplier_unlocked");
			if (chain != null)
			{
				base.MSGConversation.SendMessageChain(chain.GetMessageChain());
			}
		}
	}

	protected override void CreateMessageConversation()
	{
		base.CreateMessageConversation();
		SendableMessage sendableMessage = base.MSGConversation.CreateSendableMessage("I need to order a dead drop");
		sendableMessage.IsValidCheck = IsDeadDropValid;
		sendableMessage.disableDefaultSendBehaviour = true;
		sendableMessage.onSelected = (Action)Delegate.Combine(sendableMessage.onSelected, new Action(DeaddropRequested));
		SendableMessage sendableMessage2 = base.MSGConversation.CreateSendableMessage("We need to meet up");
		sendableMessage2.IsValidCheck = IsMeetupValid;
		sendableMessage2.onSent = (Action)Delegate.Combine(sendableMessage2.onSent, new Action(MeetupRequested));
		SendableMessage sendableMessage3 = base.MSGConversation.CreateSendableMessage("I want to pay off my debt");
		sendableMessage3.onSent = (Action)Delegate.Combine(sendableMessage3.onSent, new Action(PayDebtRequested));
	}

	protected virtual void DeaddropRequested()
	{
		float orderLimit = Mathf.Max(GetDeadDropLimit() - SyncAccessor_debt, 0f);
		PlayerSingleton<MessagesApp>.Instance.PhoneShopInterface.Open("Request Dead Drop", "Select items to order from " + FirstName, base.MSGConversation, OnlineShopItems.ToList(), orderLimit, SyncAccessor_debt, DeaddropConfirmed);
	}

	protected virtual void DeaddropConfirmed(List<PhoneShopInterface.CartEntry> cart, float totalPrice)
	{
		if (SyncAccessor_deadDropPreparing)
		{
			Console.LogWarning("Already preparing a dead drop");
			return;
		}
		int num = cart.Sum((PhoneShopInterface.CartEntry x) => x.Quantity);
		StringIntPair[] array = new StringIntPair[cart.Count];
		for (int num2 = 0; num2 < cart.Count; num2++)
		{
			array[num2] = new StringIntPair(cart[num2].Listing.Item.ID, cart[num2].Quantity);
		}
		string text = "I need a dead drop:\n";
		for (int num3 = 0; num3 < cart.Count; num3++)
		{
			if (cart[num3].Quantity > 0)
			{
				text = text + cart[num3].Quantity + "x " + cart[num3].Listing.Item.Name;
				if (num3 < cart.Count - 1)
				{
					text += "\n";
				}
			}
		}
		base.MSGConversation.SendMessage(new Message(text, Message.ESenderType.Player));
		int num4 = Mathf.Clamp(num * 30, 30, 360);
		string line = dialogueHandler.Database.GetLine(EDialogueModule.Supplier, "deaddrop_requested");
		if (num4 < 60)
		{
			line = line.Replace("<TIME>", num4 + ((num4 == 1) ? " min" : " mins"));
		}
		else
		{
			float num5 = Mathf.FloorToInt((float)num4 / 60f);
			float num6 = (float)num4 - num5 * 60f;
			string text2 = num5 + ((num5 == 1f) ? " hour" : " hours");
			if (num6 > 0f)
			{
				text2 = text2 + " " + num6 + " min";
			}
			line = line.Replace("<TIME>", text2);
		}
		base.MSGConversation.SendMessageChain(new MessageChain
		{
			Messages = new List<string> { line },
			id = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
		}, 0.5f, notify: false);
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Deaddrops_Ordered", (NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("Deaddrops_Ordered") + 1f).ToString());
		SetDeaddrop(array, num4);
		minsSinceDeaddropOrder = 0;
		ChangeDebt(totalPrice);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetDeaddrop(StringIntPair[] items, int minsUntilReady)
	{
		RpcWriter___Server_SetDeaddrop_3971994486(items, minsUntilReady);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	private void ChangeDebt(float amount)
	{
		RpcWriter___Server_ChangeDebt_431000436(amount);
		RpcLogic___ChangeDebt_431000436(amount);
	}

	private void TryRecoverDebt()
	{
		float num = Mathf.Min(SyncAccessor_debt, Stash.CashAmount);
		if (num > 0f)
		{
			Debug.Log("Recovering debt: " + num);
			float num2 = SyncAccessor_debt;
			Stash.RemoveCash(num);
			ChangeDebt(0f - num);
			RelationData.ChangeRelationship(num / MaxOrderLimit * 0.5f);
			float num3 = num2 - num;
			string text = "I've received " + MoneyManager.FormatAmount(num) + " cash from you.";
			text = ((!(num3 <= 0f)) ? (text + " Your debt is now " + MoneyManager.FormatAmount(num3)) : (text + " Your debt is now paid off."));
			repaymentReminderSent = false;
			base.MSGConversation.SendMessageChain(new MessageChain
			{
				Messages = new List<string> { text },
				id = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
			});
		}
	}

	private void CompleteDeaddrop()
	{
		Console.Log("Dead drop ready");
		DeadDrop randomEmptyDrop = DeadDrop.GetRandomEmptyDrop(Player.Local.transform.position);
		if (randomEmptyDrop == null)
		{
			Console.LogError("No empty dead drop locations");
			return;
		}
		StringIntPair[] array = deaddropItems;
		foreach (StringIntPair stringIntPair in array)
		{
			ItemDefinition item = Registry.GetItem(stringIntPair.String);
			if (item == null)
			{
				Console.LogError("Item not found: " + stringIntPair.String);
				continue;
			}
			int num = stringIntPair.Int;
			while (num > 0)
			{
				int num2 = Mathf.Min(num, item.StackLimit);
				ItemInstance defaultInstance = item.GetDefaultInstance(num2);
				randomEmptyDrop.Storage.InsertItem(defaultInstance);
				num -= num2;
			}
		}
		string line = dialogueHandler.Database.GetLine(EDialogueModule.Supplier, "deaddrop_ready");
		line = line.Replace("<LOCATION>", randomEmptyDrop.DeadDropDescription);
		base.MSGConversation.SendMessageChain(new MessageChain
		{
			Messages = new List<string> { line },
			id = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
		});
		this.sync___set_value_deadDropPreparing(value: false, asServer: true);
		minsUntilDeaddropReady = -1;
		deaddropItems = null;
		if (onDeaddropReady != null)
		{
			onDeaddropReady.Invoke();
		}
		string guidString = GUIDManager.GenerateUniqueGUID().ToString();
		NetworkSingleton<QuestManager>.Instance.CreateDeaddropCollectionQuest(null, randomEmptyDrop.GUID.ToString(), guidString);
		SetDeaddrop(null, -1);
	}

	private void SendDebtReminder()
	{
		repaymentReminderSent = true;
		DialogueChain chain = dialogueHandler.Database.GetChain(EDialogueModule.Supplier, "supplier_request_repayment");
		chain.Lines[0] = chain.Lines[0].Replace("<DEBT>", "<color=#46CB4F>" + MoneyManager.FormatAmount(SyncAccessor_debt) + "</color>");
		base.MSGConversation.SendMessageChain(chain.GetMessageChain());
	}

	protected virtual void MeetupRequested()
	{
		if (InstanceFinder.IsServer)
		{
			int locationIndex;
			SupplierLocation appropriateLocation = GetAppropriateLocation(out locationIndex);
			string line = dialogueHandler.Database.GetLine(EDialogueModule.Generic, "supplier_meet_confirm");
			line = line.Replace("<LOCATION>", appropriateLocation.LocationDescription);
			MessageChain messageChain = new MessageChain();
			messageChain.Messages.Add(line);
			messageChain.id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			base.MSGConversation.SendMessageChain(messageChain, 0.5f);
			MeetAtLocation(null, locationIndex, 360);
		}
	}

	protected virtual void PayDebtRequested()
	{
		if (InstanceFinder.IsServer)
		{
			MessageChain messageChain = new MessageChain();
			messageChain.Messages.Add("You can pay off your debt by placing cash in my stash. It's " + Stash.locationDescription + ".");
			messageChain.id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			base.MSGConversation.SendMessageChain(messageChain, 0.5f);
		}
	}

	protected SupplierLocation GetAppropriateLocation(out int locationIndex)
	{
		locationIndex = -1;
		List<SupplierLocation> list = new List<SupplierLocation>();
		list.AddRange(SupplierLocation.AllLocations);
		foreach (SupplierLocation allLocation in SupplierLocation.AllLocations)
		{
			if (allLocation.IsOccupied)
			{
				list.Remove(allLocation);
			}
		}
		foreach (SupplierLocation allLocation2 in SupplierLocation.AllLocations)
		{
			foreach (Player player in Player.PlayerList)
			{
				if (Vector3.Distance(allLocation2.transform.position, player.Avatar.CenterPoint) < 30f)
				{
					list.Remove(allLocation2);
				}
			}
		}
		if (list.Count == 0)
		{
			Console.LogError("No available locations for supplier");
			return null;
		}
		SupplierLocation supplierLocation = list[UnityEngine.Random.Range(0, list.Count)];
		locationIndex = SupplierLocation.AllLocations.IndexOf(supplierLocation);
		return supplierLocation;
	}

	private bool IsDeadDropValid(SendableMessage message, out string invalidReason)
	{
		invalidReason = string.Empty;
		if (SyncAccessor_deadDropPreparing)
		{
			invalidReason = "Already waiting for a dead drop";
			return false;
		}
		return true;
	}

	private bool IsMeetupValid(SendableMessage message, out string invalidReason)
	{
		if (RelationData.RelationDelta < 4f)
		{
			invalidReason = "Insufficient trust";
			return false;
		}
		if (Status != ESupplierStatus.Idle)
		{
			invalidReason = "Busy";
			return false;
		}
		invalidReason = "";
		return true;
	}

	public virtual float GetDeadDropLimit()
	{
		return Mathf.Lerp(MinOrderLimit, MaxOrderLimit, RelationData.RelationDelta / 5f);
	}

	public override string GetSaveString()
	{
		return new SupplierData(ID, minsSinceMeetingStart, minsSinceLastMeetingEnd, SyncAccessor_debt, minsUntilDeaddropReady, deaddropItems, repaymentReminderSent).GetJson();
	}

	public override void Load(NPCData data, string containerPath)
	{
		base.Load(data, containerPath);
		if (((ISaveable)this).TryLoadFile(containerPath, "NPC", out string contents))
		{
			SupplierData supplierData = null;
			try
			{
				supplierData = JsonUtility.FromJson<SupplierData>(contents);
			}
			catch (Exception ex)
			{
				Console.LogWarning("Failed to deserialize character data: " + ex.Message);
				return;
			}
			minsSinceMeetingStart = supplierData.timeSinceMeetingStart;
			minsSinceLastMeetingEnd = supplierData.timeSinceLastMeetingEnd;
			this.sync___set_value_debt(supplierData.debt, asServer: true);
			minsUntilDeaddropReady = supplierData.minsUntilDeadDropReady;
			if (minsUntilDeaddropReady > 0)
			{
				this.sync___set_value_deadDropPreparing(value: true, asServer: true);
			}
			if (supplierData.deaddropItems != null)
			{
				deaddropItems = supplierData.deaddropItems.ToArray();
			}
			repaymentReminderSent = supplierData.debtReminderSent;
		}
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar___deadDropPreparing = new SyncVar<bool>(this, 2u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, deadDropPreparing);
			syncVar___debt = new SyncVar<float>(this, 1u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, debt);
			RegisterServerRpc(35u, RpcReader___Server_SendUnlocked_2166136261);
			RegisterObserversRpc(36u, RpcReader___Observers_SetUnlocked_2166136261);
			RegisterObserversRpc(37u, RpcReader___Observers_MeetAtLocation_3470796954);
			RegisterObserversRpc(38u, RpcReader___Observers_EnableDeliveries_328543758);
			RegisterTargetRpc(39u, RpcReader___Target_EnableDeliveries_328543758);
			RegisterServerRpc(40u, RpcReader___Server_SetDeaddrop_3971994486);
			RegisterServerRpc(41u, RpcReader___Server_ChangeDebt_431000436);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EEconomy_002ESupplier);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEconomy_002ESupplierAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
			syncVar___deadDropPreparing.SetRegistered();
			syncVar___debt.SetRegistered();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendUnlocked_2166136261()
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
			SendServerRpc(35u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendUnlocked_2166136261()
	{
		SetUnlocked();
	}

	private void RpcReader___Server_SendUnlocked_2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		if (base.IsServerInitialized)
		{
			RpcLogic___SendUnlocked_2166136261();
		}
	}

	private void RpcWriter___Observers_SetUnlocked_2166136261()
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
			SendObserversRpc(36u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetUnlocked_2166136261()
	{
		RelationData.Unlock(NPCRelationData.EUnlockType.Recommendation);
	}

	private void RpcReader___Observers_SetUnlocked_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized)
		{
			RpcLogic___SetUnlocked_2166136261();
		}
	}

	private void RpcWriter___Observers_MeetAtLocation_3470796954(NetworkConnection conn, int locationIndex, int expireIn)
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
			writer.WriteNetworkConnection(conn);
			writer.WriteInt32(locationIndex);
			writer.WriteInt32(expireIn);
			SendObserversRpc(37u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___MeetAtLocation_3470796954(NetworkConnection conn, int locationIndex, int expireIn)
	{
		SupplierLocation supplierLocation = SupplierLocation.AllLocations[locationIndex];
		if (supplierLocation == null)
		{
			Console.LogError("Location not found: " + locationIndex);
			return;
		}
		if (supplierLocation.SupplierStandPoint == null)
		{
			Console.LogError("Supplier stand point not set up for location: " + supplierLocation.name);
			return;
		}
		if (meetingGreeting == null || meetingChoice == null)
		{
			Console.LogError("Meeting greeting or choice not set up");
			return;
		}
		Console.Log(base.fullName + " meeting at " + supplierLocation.name + " for " + expireIn + " minutes");
		Status = ESupplierStatus.Meeting;
		currentLocation = supplierLocation;
		minsSinceMeetingStart = 0;
		supplierLocation.SetActiveSupplier(this);
		ShopInterface shop = Shop;
		StorageEntity[] deliveryBays = supplierLocation.DeliveryBays;
		shop.DeliveryBays = deliveryBays;
		meetingGreeting.ShouldShow = true;
		meetingChoice.Enabled = true;
		movement.Warp(supplierLocation.SupplierStandPoint.position);
		movement.FaceDirection(supplierLocation.SupplierStandPoint.forward);
		SetVisible(visible: true);
	}

	private void RpcReader___Observers_MeetAtLocation_3470796954(PooledReader PooledReader0, Channel channel)
	{
		NetworkConnection conn = PooledReader0.ReadNetworkConnection();
		int locationIndex = PooledReader0.ReadInt32();
		int expireIn = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___MeetAtLocation_3470796954(conn, locationIndex, expireIn);
		}
	}

	private void RpcWriter___Observers_EnableDeliveries_328543758(NetworkConnection conn)
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

	private void RpcLogic___EnableDeliveries_328543758(NetworkConnection conn)
	{
		DeliveriesEnabled = true;
		Singleton<CoroutineService>.Instance.StartCoroutine(Wait());
		IEnumerator Wait()
		{
			yield return new WaitUntil(() => PlayerSingleton<DeliveryApp>.InstanceExists);
			PlayerSingleton<DeliveryApp>.Instance.GetShop(Shop).SetIsAvailable();
		}
	}

	private void RpcReader___Observers_EnableDeliveries_328543758(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___EnableDeliveries_328543758(null);
		}
	}

	private void RpcWriter___Target_EnableDeliveries_328543758(NetworkConnection conn)
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
			SendTargetRpc(39u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_EnableDeliveries_328543758(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized)
		{
			RpcLogic___EnableDeliveries_328543758(base.LocalConnection);
		}
	}

	private void RpcWriter___Server_SetDeaddrop_3971994486(StringIntPair[] items, int minsUntilReady)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EDevUtilities_002EStringIntPair_005B_005DFishNet_002ESerializing_002EGenerated(writer, items);
			writer.WriteInt32(minsUntilReady);
			SendServerRpc(40u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___SetDeaddrop_3971994486(StringIntPair[] items, int minsUntilReady)
	{
		if (items != null)
		{
			minsSinceDeaddropOrder = 0;
			this.sync___set_value_deadDropPreparing(value: true, asServer: true);
		}
		else
		{
			this.sync___set_value_deadDropPreparing(value: false, asServer: true);
		}
		minsUntilDeaddropReady = minsUntilReady;
		deaddropItems = items;
	}

	private void RpcReader___Server_SetDeaddrop_3971994486(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		StringIntPair[] items = GeneratedReaders___Internal.Read___ScheduleOne_002EDevUtilities_002EStringIntPair_005B_005DFishNet_002ESerializing_002EGenerateds(PooledReader0);
		int minsUntilReady = PooledReader0.ReadInt32();
		if (base.IsServerInitialized)
		{
			RpcLogic___SetDeaddrop_3971994486(items, minsUntilReady);
		}
	}

	private void RpcWriter___Server_ChangeDebt_431000436(float amount)
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
			writer.WriteSingle(amount);
			SendServerRpc(41u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___ChangeDebt_431000436(float amount)
	{
		this.sync___set_value_debt(Mathf.Clamp(SyncAccessor_debt + amount, 0f, GetDeadDropLimit()), asServer: true);
	}

	private void RpcReader___Server_ChangeDebt_431000436(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		float amount = PooledReader0.ReadSingle();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___ChangeDebt_431000436(amount);
		}
	}

	public virtual bool ReadSyncVar___ScheduleOne_002EEconomy_002ESupplier(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		switch (UInt321)
		{
		case 2u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value_deadDropPreparing(syncVar___deadDropPreparing.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			bool value2 = PooledReader0.ReadBoolean();
			this.sync___set_value_deadDropPreparing(value2, Boolean2);
			return true;
		}
		case 1u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value_debt(syncVar___debt.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			float value = PooledReader0.ReadSingle();
			this.sync___set_value_debt(value, Boolean2);
			return true;
		}
		default:
			return false;
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EEconomy_002ESupplier_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
	}
}
