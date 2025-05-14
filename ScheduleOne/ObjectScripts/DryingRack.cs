using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.GameTime;
using ScheduleOne.Interaction;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Storage;
using ScheduleOne.Tiles;
using ScheduleOne.Tools;
using ScheduleOne.UI.Compass;
using ScheduleOne.UI.Management;
using ScheduleOne.UI.Stations;
using UnityEngine;

namespace ScheduleOne.ObjectScripts;

public class DryingRack : GridItem, IUsable, IItemSlotOwner, ITransitEntity, IConfigurable
{
	public const int DRY_MINS_PER_TIER = 720;

	[Header("Settings")]
	public int ItemCapacity = 20;

	[Header("References")]
	public Transform[] CameraPositions;

	public InteractableObject IntObj;

	public Transform uiPoint;

	public Transform[] accessPoints;

	public StorageVisualizer InputVisuals;

	public StorageVisualizer OutputVisuals;

	public StorageVisualizer HangingVisuals;

	public Transform[] HangAlignments;

	public ConfigurationReplicator configReplicator;

	[Header("UI")]
	public DryingRackUIElement WorldspaceUIPrefab;

	public Sprite typeIcon;

	[CompilerGenerated]
	[HideInInspector]
	[SyncVar(WritePermissions = WritePermission.ClientUnsynchronized)]
	public NetworkObject _003CNPCUserObject_003Ek__BackingField;

	[CompilerGenerated]
	[HideInInspector]
	[SyncVar(WritePermissions = WritePermission.ClientUnsynchronized)]
	public NetworkObject _003CPlayerUserObject_003Ek__BackingField;

	[CompilerGenerated]
	[SyncVar]
	public NetworkObject _003CCurrentPlayerConfigurer_003Ek__BackingField;

	public Action<DryingOperation> onOperationStart;

	public Action<DryingOperation> onOperationComplete;

	public Action onOperationsChanged;

	private ItemSlot[] hangSlots;

	private List<int> requestIDs = new List<int>();

	public SyncVar<NetworkObject> syncVar____003CNPCUserObject_003Ek__BackingField;

	public SyncVar<NetworkObject> syncVar____003CPlayerUserObject_003Ek__BackingField;

	public SyncVar<NetworkObject> syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField;

	private bool NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted;

	public NetworkObject NPCUserObject
	{
		[CompilerGenerated]
		get
		{
			return SyncAccessor__003CNPCUserObject_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			this.sync___set_value__003CNPCUserObject_003Ek__BackingField(value, asServer: true);
		}
	}

	public NetworkObject PlayerUserObject
	{
		[CompilerGenerated]
		get
		{
			return SyncAccessor__003CPlayerUserObject_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			this.sync___set_value__003CPlayerUserObject_003Ek__BackingField(value, asServer: true);
		}
	}

	public List<ItemSlot> ItemSlots { get; set; } = new List<ItemSlot>();

	public string Name => base.ItemInstance.Name;

	public List<ItemSlot> InputSlots { get; set; } = new List<ItemSlot>();

	public List<ItemSlot> OutputSlots { get; set; } = new List<ItemSlot>();

	public Transform LinkOrigin => uiPoint;

	public Transform[] AccessPoints => accessPoints;

	public bool Selectable { get; } = true;

	public bool IsAcceptingItems { get; set; } = true;

	public EntityConfiguration Configuration => stationConfiguration;

	protected DryingRackConfiguration stationConfiguration { get; set; }

	public ConfigurationReplicator ConfigReplicator => configReplicator;

	public EConfigurableType ConfigurableType => EConfigurableType.DryingRack;

	public WorldspaceUIElement WorldspaceUI { get; set; }

	public NetworkObject CurrentPlayerConfigurer
	{
		[CompilerGenerated]
		get
		{
			return SyncAccessor__003CCurrentPlayerConfigurer_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			this.sync___set_value__003CCurrentPlayerConfigurer_003Ek__BackingField(value, asServer: true);
		}
	}

	public Sprite TypeIcon => typeIcon;

	public Transform Transform => base.transform;

	public Transform UIPoint => uiPoint;

	public bool CanBeSelected => true;

	public ItemSlot InputSlot { get; private set; }

	public ItemSlot OutputSlot { get; private set; }

	public bool IsOpen { get; private set; }

	public List<DryingOperation> DryingOperations { get; set; } = new List<DryingOperation>();

	public NetworkObject SyncAccessor__003CNPCUserObject_003Ek__BackingField
	{
		get
		{
			return NPCUserObject;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				NPCUserObject = value;
			}
			if (Application.isPlaying)
			{
				syncVar____003CNPCUserObject_003Ek__BackingField.SetValue(value, value);
			}
		}
	}

	public NetworkObject SyncAccessor__003CPlayerUserObject_003Ek__BackingField
	{
		get
		{
			return PlayerUserObject;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				PlayerUserObject = value;
			}
			if (Application.isPlaying)
			{
				syncVar____003CPlayerUserObject_003Ek__BackingField.SetValue(value, value);
			}
		}
	}

	public NetworkObject SyncAccessor__003CCurrentPlayerConfigurer_003Ek__BackingField
	{
		get
		{
			return CurrentPlayerConfigurer;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				CurrentPlayerConfigurer = value;
			}
			if (Application.isPlaying)
			{
				syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField.SetValue(value, value);
			}
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SetConfigurer(NetworkObject player)
	{
		RpcWriter___Server_SetConfigurer_3323014238(player);
		RpcLogic___SetConfigurer_3323014238(player);
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EObjectScripts_002EDryingRack_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public override void InitializeGridItem(ItemInstance instance, Grid grid, Vector2 originCoordinate, int rotation, string GUID)
	{
		bool initialized = base.Initialized;
		base.InitializeGridItem(instance, grid, originCoordinate, rotation, GUID);
		if (!initialized && !isGhost)
		{
			base.ParentProperty.AddConfigurable(this);
			stationConfiguration = new DryingRackConfiguration(configReplicator, this, this);
			CreateWorldspaceUI();
			GameInput.RegisterExitListener(Exit, 4);
			TimeManager instance2 = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
			instance2.onMinutePass = (Action)Delegate.Combine(instance2.onMinutePass, new Action(MinPass));
		}
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		((IItemSlotOwner)this).SendItemsToClient(connection);
		foreach (DryingOperation dryingOperation in DryingOperations)
		{
			PleaseReceiveOp(connection, dryingOperation);
		}
		SendConfigurationToClient(connection);
	}

	public void SendConfigurationToClient(NetworkConnection conn)
	{
		if (!conn.IsHost)
		{
			Singleton<CoroutineService>.Instance.StartCoroutine(WaitForConfig());
		}
		IEnumerator WaitForConfig()
		{
			yield return new WaitUntil(() => Configuration != null);
			Configuration.ReplicateAllFields(conn);
		}
	}

	private void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen && action.exitType == ExitType.Escape)
		{
			action.Used = true;
			Close();
		}
	}

	public override bool CanBeDestroyed(out string reason)
	{
		if (((IUsable)this).IsInUse)
		{
			reason = "In use";
			return false;
		}
		if (((IItemSlotOwner)this).GetTotalItemCount() > 0 || DryingOperations.Count > 0)
		{
			reason = "Contains items";
			return false;
		}
		return base.CanBeDestroyed(out reason);
	}

	public override void DestroyItem(bool callOnServer = true)
	{
		GameInput.DeregisterExitListener(Exit);
		TimeManager instance = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		instance.onMinutePass = (Action)Delegate.Remove(instance.onMinutePass, new Action(MinPass));
		if (Configuration != null)
		{
			Configuration.Destroy();
			DestroyWorldspaceUI();
			base.ParentProperty.RemoveConfigurable(this);
		}
		base.DestroyItem(callOnServer);
	}

	private void MinPass()
	{
		DryingOperation[] array = DryingOperations.ToArray();
		foreach (DryingOperation dryingOperation in array)
		{
			dryingOperation.Time++;
			if (dryingOperation.Time < 720)
			{
				continue;
			}
			if (dryingOperation.StartQuality >= EQuality.Premium)
			{
				if (InstanceFinder.IsServer && GetOutputCapacityForOperation(dryingOperation, EQuality.Heavenly) >= dryingOperation.Quantity)
				{
					TryEndOperation(DryingOperations.IndexOf(dryingOperation), allowSplitting: false, EQuality.Heavenly, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
				}
			}
			else
			{
				dryingOperation.IncreaseQuality();
			}
		}
	}

	public bool CanStartOperation()
	{
		if (GetTotalDryingItems() >= ItemCapacity)
		{
			return false;
		}
		if (InputSlot.Quantity == 0)
		{
			return false;
		}
		if (InputSlot.IsLocked || InputSlot.IsRemovalLocked)
		{
			return false;
		}
		return true;
	}

	public void StartOperation()
	{
		int num = Mathf.Min(InputSlot.Quantity, ItemCapacity - GetTotalDryingItems());
		EQuality quality = (InputSlot.ItemInstance as QualityItemInstance).Quality;
		DryingOperation op = new DryingOperation(InputSlot.ItemInstance.ID, num, quality, 0);
		SendOperation(op);
		InputSlot.ChangeQuantity(-num);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void TryEndOperation(int operationIndex, bool allowSplitting, EQuality quality, int requestID)
	{
		RpcWriter___Server_TryEndOperation_4146970406(operationIndex, allowSplitting, quality, requestID);
		RpcLogic___TryEndOperation_4146970406(operationIndex, allowSplitting, quality, requestID);
	}

	public List<DryingOperation> GetOperationsAtTargetQuality()
	{
		EQuality targetQuality = (Configuration as DryingRackConfiguration).TargetQuality.Value;
		return DryingOperations.Where((DryingOperation x) => x.StartQuality >= targetQuality).ToList();
	}

	public int GetOutputCapacityForOperation(DryingOperation operation, EQuality quality)
	{
		QualityItemInstance qualityItemInstance = Registry.GetItem(operation.ItemID).GetDefaultInstance() as QualityItemInstance;
		qualityItemInstance.SetQuality(quality);
		return OutputSlot.GetCapacityForItem(qualityItemInstance);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SendOperation(DryingOperation op)
	{
		RpcWriter___Server_SendOperation_1307702229(op);
	}

	[TargetRpc]
	[ObserversRpc]
	private void PleaseReceiveOp(NetworkConnection conn, DryingOperation op)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_PleaseReceiveOp_1575047616(conn, op);
		}
		else
		{
			RpcWriter___Target_PleaseReceiveOp_1575047616(conn, op);
		}
	}

	[ObserversRpc(RunLocally = true, ExcludeServer = true)]
	private void RemoveOperation(int opIndex)
	{
		RpcWriter___Observers_RemoveOperation_3316948804(opIndex);
		RpcLogic___RemoveOperation_3316948804(opIndex);
	}

	[ObserversRpc]
	private void SetOperationQuantity(int opIndex, int quantity)
	{
		RpcWriter___Observers_SetOperationQuantity_1692629761(opIndex, quantity);
	}

	public int GetTotalDryingItems()
	{
		return DryingOperations.Sum((DryingOperation x) => x.Quantity);
	}

	public void RefreshHangingVisuals()
	{
		for (int i = 0; i < hangSlots.Length; i++)
		{
			if (DryingOperations.Count > i)
			{
				QualityItemInstance qualityItemInstance = DryingOperations[i].GetQualityItemInstance();
				hangSlots[i].SetStoredItem(qualityItemInstance);
			}
			else
			{
				hangSlots[i].ClearStoredInstance();
			}
		}
		HangingVisuals.RefreshVisuals();
		StoredItem[] array = (from x in HangingVisuals.ItemContainer.GetComponentsInChildren<StoredItem>()
			where !x.Destroyed
			select x).ToArray();
		for (int num = 0; num < array.Length && num < HangAlignments.Length; num++)
		{
			Transform transform = array[num].GetComponentsInChildren<Transform>().FirstOrDefault((Transform x) => x.name == "HangingAlignment");
			if (transform == null)
			{
				Console.LogError("Missing alignment transform on stored item: " + array[num].name);
				continue;
			}
			Transform obj = HangAlignments[num];
			Quaternion quaternion = obj.rotation * Quaternion.Inverse(transform.rotation);
			array[num].transform.rotation = quaternion * array[num].transform.rotation;
			Vector3 vector = obj.position - transform.position;
			array[num].transform.position += vector;
		}
	}

	public WorldspaceUIElement CreateWorldspaceUI()
	{
		if (WorldspaceUI != null)
		{
			Console.LogWarning(base.gameObject.name + " already has a worldspace UI element!");
		}
		if (base.ParentProperty == null)
		{
			Console.LogError(base.ParentProperty?.ToString() + " is not a child of a property!");
			return null;
		}
		DryingRackUIElement component = UnityEngine.Object.Instantiate(WorldspaceUIPrefab, base.ParentProperty.WorldspaceUIContainer).GetComponent<DryingRackUIElement>();
		component.Initialize(this);
		WorldspaceUI = component;
		return component;
	}

	public void DestroyWorldspaceUI()
	{
		if (WorldspaceUI != null)
		{
			WorldspaceUI.Destroy();
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SetPlayerUser(NetworkObject playerObject)
	{
		RpcWriter___Server_SetPlayerUser_3323014238(playerObject);
		RpcLogic___SetPlayerUser_3323014238(playerObject);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SetNPCUser(NetworkObject npcObject)
	{
		RpcWriter___Server_SetNPCUser_3323014238(npcObject);
		RpcLogic___SetNPCUser_3323014238(npcObject);
	}

	public void Hovered()
	{
		if (((IUsable)this).IsInUse || Singleton<ManagementClipboard>.Instance.IsEquipped)
		{
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Disabled);
			return;
		}
		IntObj.SetMessage("Use " + base.ItemInstance.Name);
		IntObj.SetInteractableState(InteractableObject.EInteractableState.Default);
	}

	public void Interacted()
	{
		if (!((IUsable)this).IsInUse && !Singleton<ManagementClipboard>.Instance.IsEquipped)
		{
			Open();
		}
	}

	public void Open()
	{
		IsOpen = true;
		SetPlayerUser(Player.Local.NetworkObject);
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		Transform transform = CameraPositions[0];
		if (Vector3.Distance(PlayerSingleton<PlayerCamera>.Instance.transform.position, CameraPositions[1].position) < Vector3.Distance(PlayerSingleton<PlayerCamera>.Instance.transform.position, CameraPositions[0].position))
		{
			transform = CameraPositions[1];
		}
		PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(transform.position, transform.rotation, 0.2f);
		PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(70f, 0.2f);
		PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
		PlayerSingleton<PlayerMovement>.Instance.canMove = false;
		Singleton<CompassManager>.Instance.SetVisible(visible: false);
		Singleton<DryingRackCanvas>.Instance.SetIsOpen(this, open: true);
	}

	public void Close()
	{
		IsOpen = false;
		Singleton<DryingRackCanvas>.Instance.SetIsOpen(null, open: false);
		SetPlayerUser(null);
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.StopTransformOverride(0.2f);
		PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(0.2f);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		Singleton<CompassManager>.Instance.SetVisible(visible: true);
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: true);
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
	}

	[ServerRpc(RunLocally = true, RequireOwnership = false)]
	public void SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
	{
		RpcWriter___Server_SetStoredInstance_2652194801(conn, itemSlotIndex, instance);
		RpcLogic___SetStoredInstance_2652194801(conn, itemSlotIndex, instance);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc(RunLocally = true)]
	private void SetStoredInstance_Internal(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetStoredInstance_Internal_2652194801(conn, itemSlotIndex, instance);
			RpcLogic___SetStoredInstance_Internal_2652194801(conn, itemSlotIndex, instance);
		}
		else
		{
			RpcWriter___Target_SetStoredInstance_Internal_2652194801(conn, itemSlotIndex, instance);
			RpcLogic___SetStoredInstance_Internal_2652194801(conn, itemSlotIndex, instance);
		}
	}

	[ServerRpc(RunLocally = true, RequireOwnership = false)]
	public void SetItemSlotQuantity(int itemSlotIndex, int quantity)
	{
		RpcWriter___Server_SetItemSlotQuantity_1692629761(itemSlotIndex, quantity);
		RpcLogic___SetItemSlotQuantity_1692629761(itemSlotIndex, quantity);
	}

	[ObserversRpc(RunLocally = true)]
	private void SetItemSlotQuantity_Internal(int itemSlotIndex, int quantity)
	{
		RpcWriter___Observers_SetItemSlotQuantity_Internal_1692629761(itemSlotIndex, quantity);
		RpcLogic___SetItemSlotQuantity_Internal_1692629761(itemSlotIndex, quantity);
	}

	[ServerRpc(RunLocally = true, RequireOwnership = false)]
	public void SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
	{
		RpcWriter___Server_SetSlotLocked_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
		RpcLogic___SetSlotLocked_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
	}

	[TargetRpc(RunLocally = true)]
	[ObserversRpc(RunLocally = true)]
	private void SetSlotLocked_Internal(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetSlotLocked_Internal_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
			RpcLogic___SetSlotLocked_Internal_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
		}
		else
		{
			RpcWriter___Target_SetSlotLocked_Internal_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
			RpcLogic___SetSlotLocked_Internal_3170825843(conn, itemSlotIndex, locked, lockOwner, lockReason);
		}
	}

	public override string GetSaveString()
	{
		return new DryingRackData(base.GUID, base.ItemInstance, 0, base.OwnerGrid, OriginCoordinate, Rotation, new ItemSet(new List<ItemSlot> { InputSlot }), new ItemSet(new List<ItemSlot> { OutputSlot }), DryingOperations.ToArray()).GetJson();
	}

	public override List<string> WriteData(string parentFolderPath)
	{
		List<string> list = new List<string>();
		if (Configuration.ShouldSave())
		{
			list.Add("Configuration.json");
			((ISaveable)this).WriteSubfile(parentFolderPath, "Configuration", Configuration.GetSaveString());
		}
		list.AddRange(base.WriteData(parentFolderPath));
		return list;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField = new SyncVar<NetworkObject>(this, 2u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, CurrentPlayerConfigurer);
			syncVar____003CPlayerUserObject_003Ek__BackingField = new SyncVar<NetworkObject>(this, 1u, WritePermission.ClientUnsynchronized, ReadPermission.Observers, -1f, Channel.Reliable, PlayerUserObject);
			syncVar____003CNPCUserObject_003Ek__BackingField = new SyncVar<NetworkObject>(this, 0u, WritePermission.ClientUnsynchronized, ReadPermission.Observers, -1f, Channel.Reliable, NPCUserObject);
			RegisterServerRpc(8u, RpcReader___Server_SetConfigurer_3323014238);
			RegisterServerRpc(9u, RpcReader___Server_TryEndOperation_4146970406);
			RegisterServerRpc(10u, RpcReader___Server_SendOperation_1307702229);
			RegisterTargetRpc(11u, RpcReader___Target_PleaseReceiveOp_1575047616);
			RegisterObserversRpc(12u, RpcReader___Observers_PleaseReceiveOp_1575047616);
			RegisterObserversRpc(13u, RpcReader___Observers_RemoveOperation_3316948804);
			RegisterObserversRpc(14u, RpcReader___Observers_SetOperationQuantity_1692629761);
			RegisterServerRpc(15u, RpcReader___Server_SetPlayerUser_3323014238);
			RegisterServerRpc(16u, RpcReader___Server_SetNPCUser_3323014238);
			RegisterServerRpc(17u, RpcReader___Server_SetStoredInstance_2652194801);
			RegisterObserversRpc(18u, RpcReader___Observers_SetStoredInstance_Internal_2652194801);
			RegisterTargetRpc(19u, RpcReader___Target_SetStoredInstance_Internal_2652194801);
			RegisterServerRpc(20u, RpcReader___Server_SetItemSlotQuantity_1692629761);
			RegisterObserversRpc(21u, RpcReader___Observers_SetItemSlotQuantity_Internal_1692629761);
			RegisterServerRpc(22u, RpcReader___Server_SetSlotLocked_3170825843);
			RegisterTargetRpc(23u, RpcReader___Target_SetSlotLocked_Internal_3170825843);
			RegisterObserversRpc(24u, RpcReader___Observers_SetSlotLocked_Internal_3170825843);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EObjectScripts_002EDryingRack);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EObjectScripts_002EDryingRackAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
			syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField.SetRegistered();
			syncVar____003CPlayerUserObject_003Ek__BackingField.SetRegistered();
			syncVar____003CNPCUserObject_003Ek__BackingField.SetRegistered();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SetConfigurer_3323014238(NetworkObject player)
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
			writer.WriteNetworkObject(player);
			SendServerRpc(8u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetConfigurer_3323014238(NetworkObject player)
	{
		CurrentPlayerConfigurer = player;
	}

	private void RpcReader___Server_SetConfigurer_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject player = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetConfigurer_3323014238(player);
		}
	}

	private void RpcWriter___Server_TryEndOperation_4146970406(int operationIndex, bool allowSplitting, EQuality quality, int requestID)
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
			writer.WriteInt32(operationIndex);
			writer.WriteBoolean(allowSplitting);
			GeneratedWriters___Internal.Write___ScheduleOne_002EItemFramework_002EEQualityFishNet_002ESerializing_002EGenerated(writer, quality);
			writer.WriteInt32(requestID);
			SendServerRpc(9u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___TryEndOperation_4146970406(int operationIndex, bool allowSplitting, EQuality quality, int requestID)
	{
		if (requestIDs.Contains(requestID))
		{
			return;
		}
		requestIDs.Add(requestID);
		if (operationIndex >= DryingOperations.Count)
		{
			Console.LogError("Invalid operation index: " + operationIndex);
			return;
		}
		DryingOperation dryingOperation = DryingOperations[operationIndex];
		int outputCapacityForOperation = GetOutputCapacityForOperation(dryingOperation, quality);
		int num = Mathf.Min(dryingOperation.Quantity, outputCapacityForOperation);
		if (num == 0)
		{
			Console.LogWarning("No space in output slot for operation: " + operationIndex);
			return;
		}
		if (!allowSplitting && num < dryingOperation.Quantity)
		{
			Console.LogWarning("Operation would be split, but splitting is not allowed");
			return;
		}
		QualityItemInstance qualityItemInstance = Registry.GetItem(dryingOperation.ItemID).GetDefaultInstance(num) as QualityItemInstance;
		qualityItemInstance.SetQuality(quality);
		OutputSlot.InsertItem(qualityItemInstance);
		if (num == dryingOperation.Quantity)
		{
			RemoveOperation(DryingOperations.IndexOf(dryingOperation));
		}
		else
		{
			SetOperationQuantity(DryingOperations.IndexOf(dryingOperation), dryingOperation.Quantity - num);
		}
	}

	private void RpcReader___Server_TryEndOperation_4146970406(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		int operationIndex = PooledReader0.ReadInt32();
		bool allowSplitting = PooledReader0.ReadBoolean();
		EQuality quality = GeneratedReaders___Internal.Read___ScheduleOne_002EItemFramework_002EEQualityFishNet_002ESerializing_002EGenerateds(PooledReader0);
		int requestID = PooledReader0.ReadInt32();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___TryEndOperation_4146970406(operationIndex, allowSplitting, quality, requestID);
		}
	}

	private void RpcWriter___Server_SendOperation_1307702229(DryingOperation op)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerated(writer, op);
			SendServerRpc(10u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___SendOperation_1307702229(DryingOperation op)
	{
		PleaseReceiveOp(null, op);
	}

	private void RpcReader___Server_SendOperation_1307702229(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		DryingOperation op = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized)
		{
			RpcLogic___SendOperation_1307702229(op);
		}
	}

	private void RpcWriter___Target_PleaseReceiveOp_1575047616(NetworkConnection conn, DryingOperation op)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerated(writer, op);
			SendTargetRpc(11u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___PleaseReceiveOp_1575047616(NetworkConnection conn, DryingOperation op)
	{
		if (op.Quantity == 0)
		{
			Console.LogWarning("Operation quantity is 0. Ignoring");
			return;
		}
		DryingOperations.Add(op);
		if (onOperationStart != null)
		{
			onOperationStart(op);
		}
		if (onOperationsChanged != null)
		{
			onOperationsChanged();
		}
		RefreshHangingVisuals();
	}

	private void RpcReader___Target_PleaseReceiveOp_1575047616(PooledReader PooledReader0, Channel channel)
	{
		DryingOperation op = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___PleaseReceiveOp_1575047616(base.LocalConnection, op);
		}
	}

	private void RpcWriter___Observers_PleaseReceiveOp_1575047616(NetworkConnection conn, DryingOperation op)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerated(writer, op);
			SendObserversRpc(12u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_PleaseReceiveOp_1575047616(PooledReader PooledReader0, Channel channel)
	{
		DryingOperation op = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EDryingOperationFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___PleaseReceiveOp_1575047616(null, op);
		}
	}

	private void RpcWriter___Observers_RemoveOperation_3316948804(int opIndex)
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
			writer.WriteInt32(opIndex);
			SendObserversRpc(13u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: true, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___RemoveOperation_3316948804(int opIndex)
	{
		if (opIndex < DryingOperations.Count)
		{
			DryingOperation dryingOperation = DryingOperations[opIndex];
			DryingOperations.Remove(dryingOperation);
			if (onOperationComplete != null)
			{
				onOperationComplete(dryingOperation);
			}
			if (onOperationsChanged != null)
			{
				onOperationsChanged();
			}
			RefreshHangingVisuals();
		}
		else
		{
			Console.LogError("Invalid operation index: " + opIndex);
		}
	}

	private void RpcReader___Observers_RemoveOperation_3316948804(PooledReader PooledReader0, Channel channel)
	{
		int opIndex = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___RemoveOperation_3316948804(opIndex);
		}
	}

	private void RpcWriter___Observers_SetOperationQuantity_1692629761(int opIndex, int quantity)
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
			writer.WriteInt32(opIndex);
			writer.WriteInt32(quantity);
			SendObserversRpc(14u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetOperationQuantity_1692629761(int opIndex, int quantity)
	{
		if (opIndex < DryingOperations.Count)
		{
			DryingOperations[opIndex].Quantity = quantity;
			if (onOperationsChanged != null)
			{
				onOperationsChanged();
			}
			RefreshHangingVisuals();
		}
		else
		{
			Console.LogError("Invalid operation index: " + opIndex);
		}
	}

	private void RpcReader___Observers_SetOperationQuantity_1692629761(PooledReader PooledReader0, Channel channel)
	{
		int opIndex = PooledReader0.ReadInt32();
		int quantity = PooledReader0.ReadInt32();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetOperationQuantity_1692629761(opIndex, quantity);
		}
	}

	private void RpcWriter___Server_SetPlayerUser_3323014238(NetworkObject playerObject)
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
			writer.WriteNetworkObject(playerObject);
			SendServerRpc(15u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetPlayerUser_3323014238(NetworkObject playerObject)
	{
		PlayerUserObject = playerObject;
	}

	private void RpcReader___Server_SetPlayerUser_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetPlayerUser_3323014238(playerObject);
		}
	}

	private void RpcWriter___Server_SetNPCUser_3323014238(NetworkObject npcObject)
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
			writer.WriteNetworkObject(npcObject);
			SendServerRpc(16u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetNPCUser_3323014238(NetworkObject npcObject)
	{
		NPCUserObject = npcObject;
	}

	private void RpcReader___Server_SetNPCUser_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject npcObject = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetNPCUser_3323014238(npcObject);
		}
	}

	private void RpcWriter___Server_SetStoredInstance_2652194801(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
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
			writer.WriteNetworkConnection(conn);
			writer.WriteInt32(itemSlotIndex);
			writer.WriteItemInstance(instance);
			SendServerRpc(17u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetStoredInstance_2652194801(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
	{
		if (conn == null || conn.ClientId == -1)
		{
			SetStoredInstance_Internal(null, itemSlotIndex, instance);
		}
		else
		{
			SetStoredInstance_Internal(conn, itemSlotIndex, instance);
		}
	}

	private void RpcReader___Server_SetStoredInstance_2652194801(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkConnection conn2 = PooledReader0.ReadNetworkConnection();
		int itemSlotIndex = PooledReader0.ReadInt32();
		ItemInstance instance = PooledReader0.ReadItemInstance();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetStoredInstance_2652194801(conn2, itemSlotIndex, instance);
		}
	}

	private void RpcWriter___Observers_SetStoredInstance_Internal_2652194801(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteItemInstance(instance);
			SendObserversRpc(18u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetStoredInstance_Internal_2652194801(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
	{
		if (instance != null)
		{
			ItemSlots[itemSlotIndex].SetStoredItem(instance, _internal: true);
		}
		else
		{
			ItemSlots[itemSlotIndex].ClearStoredInstance(_internal: true);
		}
	}

	private void RpcReader___Observers_SetStoredInstance_Internal_2652194801(PooledReader PooledReader0, Channel channel)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		ItemInstance instance = PooledReader0.ReadItemInstance();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetStoredInstance_Internal_2652194801(null, itemSlotIndex, instance);
		}
	}

	private void RpcWriter___Target_SetStoredInstance_Internal_2652194801(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteItemInstance(instance);
			SendTargetRpc(19u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetStoredInstance_Internal_2652194801(PooledReader PooledReader0, Channel channel)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		ItemInstance instance = PooledReader0.ReadItemInstance();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetStoredInstance_Internal_2652194801(base.LocalConnection, itemSlotIndex, instance);
		}
	}

	private void RpcWriter___Server_SetItemSlotQuantity_1692629761(int itemSlotIndex, int quantity)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteInt32(quantity);
			SendServerRpc(20u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetItemSlotQuantity_1692629761(int itemSlotIndex, int quantity)
	{
		SetItemSlotQuantity_Internal(itemSlotIndex, quantity);
	}

	private void RpcReader___Server_SetItemSlotQuantity_1692629761(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		int quantity = PooledReader0.ReadInt32();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetItemSlotQuantity_1692629761(itemSlotIndex, quantity);
		}
	}

	private void RpcWriter___Observers_SetItemSlotQuantity_Internal_1692629761(int itemSlotIndex, int quantity)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteInt32(quantity);
			SendObserversRpc(21u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetItemSlotQuantity_Internal_1692629761(int itemSlotIndex, int quantity)
	{
		ItemSlots[itemSlotIndex].SetQuantity(quantity, _internal: true);
	}

	private void RpcReader___Observers_SetItemSlotQuantity_Internal_1692629761(PooledReader PooledReader0, Channel channel)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		int quantity = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetItemSlotQuantity_Internal_1692629761(itemSlotIndex, quantity);
		}
	}

	private void RpcWriter___Server_SetSlotLocked_3170825843(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
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
			writer.WriteNetworkConnection(conn);
			writer.WriteInt32(itemSlotIndex);
			writer.WriteBoolean(locked);
			writer.WriteNetworkObject(lockOwner);
			writer.WriteString(lockReason);
			SendServerRpc(22u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetSlotLocked_3170825843(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
	{
		if (conn == null || conn.ClientId == -1)
		{
			SetSlotLocked_Internal(null, itemSlotIndex, locked, lockOwner, lockReason);
		}
		else
		{
			SetSlotLocked_Internal(conn, itemSlotIndex, locked, lockOwner, lockReason);
		}
	}

	private void RpcReader___Server_SetSlotLocked_3170825843(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkConnection conn2 = PooledReader0.ReadNetworkConnection();
		int itemSlotIndex = PooledReader0.ReadInt32();
		bool locked = PooledReader0.ReadBoolean();
		NetworkObject lockOwner = PooledReader0.ReadNetworkObject();
		string lockReason = PooledReader0.ReadString();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetSlotLocked_3170825843(conn2, itemSlotIndex, locked, lockOwner, lockReason);
		}
	}

	private void RpcWriter___Target_SetSlotLocked_Internal_3170825843(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteBoolean(locked);
			writer.WriteNetworkObject(lockOwner);
			writer.WriteString(lockReason);
			SendTargetRpc(23u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetSlotLocked_Internal_3170825843(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
	{
		if (locked)
		{
			ItemSlots[itemSlotIndex].ApplyLock(lockOwner, lockReason, _internal: true);
		}
		else
		{
			ItemSlots[itemSlotIndex].RemoveLock(_internal: true);
		}
	}

	private void RpcReader___Target_SetSlotLocked_Internal_3170825843(PooledReader PooledReader0, Channel channel)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		bool locked = PooledReader0.ReadBoolean();
		NetworkObject lockOwner = PooledReader0.ReadNetworkObject();
		string lockReason = PooledReader0.ReadString();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetSlotLocked_Internal_3170825843(base.LocalConnection, itemSlotIndex, locked, lockOwner, lockReason);
		}
	}

	private void RpcWriter___Observers_SetSlotLocked_Internal_3170825843(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
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
			writer.WriteInt32(itemSlotIndex);
			writer.WriteBoolean(locked);
			writer.WriteNetworkObject(lockOwner);
			writer.WriteString(lockReason);
			SendObserversRpc(24u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_SetSlotLocked_Internal_3170825843(PooledReader PooledReader0, Channel channel)
	{
		int itemSlotIndex = PooledReader0.ReadInt32();
		bool locked = PooledReader0.ReadBoolean();
		NetworkObject lockOwner = PooledReader0.ReadNetworkObject();
		string lockReason = PooledReader0.ReadString();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetSlotLocked_Internal_3170825843(null, itemSlotIndex, locked, lockOwner, lockReason);
		}
	}

	public virtual bool ReadSyncVar___ScheduleOne_002EObjectScripts_002EDryingRack(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		switch (UInt321)
		{
		case 2u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CCurrentPlayerConfigurer_003Ek__BackingField(syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			NetworkObject value3 = PooledReader0.ReadNetworkObject();
			this.sync___set_value__003CCurrentPlayerConfigurer_003Ek__BackingField(value3, Boolean2);
			return true;
		}
		case 1u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CPlayerUserObject_003Ek__BackingField(syncVar____003CPlayerUserObject_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			NetworkObject value2 = PooledReader0.ReadNetworkObject();
			this.sync___set_value__003CPlayerUserObject_003Ek__BackingField(value2, Boolean2);
			return true;
		}
		case 0u:
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CNPCUserObject_003Ek__BackingField(syncVar____003CNPCUserObject_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			NetworkObject value = PooledReader0.ReadNetworkObject();
			this.sync___set_value__003CNPCUserObject_003Ek__BackingField(value, Boolean2);
			return true;
		}
		default:
			return false;
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EObjectScripts_002EDryingRack_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		if (!isGhost)
		{
			InputSlot = new ItemSlot();
			InputSlot.SetSlotOwner(this);
			InputSlot.AddFilter(new ItemFilter_Dryable());
			InputVisuals.AddSlot(InputSlot);
			OutputSlot = new ItemSlot();
			OutputSlot.SetSlotOwner(this);
			OutputSlot.SetIsAddLocked(locked: true);
			OutputVisuals.AddSlot(OutputSlot);
			InputSlots.Add(InputSlot);
			OutputSlots.Add(OutputSlot);
			HangingVisuals.BlockRefreshes = true;
			hangSlots = new ItemSlot[HangAlignments.Length];
			for (int i = 0; i < HangAlignments.Length; i++)
			{
				hangSlots[i] = new ItemSlot();
				HangingVisuals.AddSlot(hangSlots[i]);
			}
		}
	}
}
