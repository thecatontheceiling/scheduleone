using System.Collections;
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
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Property;
using ScheduleOne.UI.Management;
using UnityEngine;

namespace ScheduleOne.Employees;

public class Packager : Employee, IConfigurable
{
	[Header("References")]
	public Sprite typeIcon;

	[SerializeField]
	protected ConfigurationReplicator configReplicator;

	public PackagingStationBehaviour PackagingBehaviour;

	public BrickPressBehaviour BrickPressBehaviour;

	[Header("UI")]
	public PackagerUIElement WorldspaceUIPrefab;

	public Transform uiPoint;

	[Header("Settings")]
	public int MaxAssignedStations = 3;

	[Header("Proficiency Settings")]
	public float PackagingSpeedMultiplier = 1f;

	[CompilerGenerated]
	[SyncVar]
	public NetworkObject _003CCurrentPlayerConfigurer_003Ek__BackingField;

	public SyncVar<NetworkObject> syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField;

	private bool NetworkInitialize___EarlyScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted;

	public EntityConfiguration Configuration => configuration;

	protected PackagerConfiguration configuration { get; set; }

	public ConfigurationReplicator ConfigReplicator => configReplicator;

	public EConfigurableType ConfigurableType => EConfigurableType.Packager;

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

	public ScheduleOne.Property.Property ParentProperty => base.AssignedProperty;

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

	protected override void AssignProperty(ScheduleOne.Property.Property prop)
	{
		base.AssignProperty(prop);
		prop.AddConfigurable(this);
		configuration = new PackagerConfiguration(configReplicator, this, this);
		CreateWorldspaceUI();
	}

	protected override void Fire()
	{
		if (configuration != null)
		{
			configuration.Destroy();
			DestroyWorldspaceUI();
			if (base.AssignedProperty != null)
			{
				base.AssignedProperty.RemoveConfigurable(this);
			}
		}
		base.Fire();
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
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

	protected override void UpdateBehaviour()
	{
		base.UpdateBehaviour();
		if (PackagingBehaviour.Active)
		{
			MarkIsWorking();
		}
		else if (MoveItemBehaviour.Active)
		{
			MarkIsWorking();
		}
		else if (base.Fired)
		{
			LeavePropertyAndDespawn();
		}
		else
		{
			if (!CanWork())
			{
				return;
			}
			if (configuration.AssignedStationCount + configuration.Routes.Routes.Count == 0)
			{
				SubmitNoWorkReason("I haven't been assigned to any stations or routes.", "You can use your management clipboards to assign stations or routes to me.");
				SetIdle(idle: true);
			}
			else
			{
				if (!InstanceFinder.IsServer)
				{
					return;
				}
				PackagingStation stationToAttend = GetStationToAttend();
				if (stationToAttend != null)
				{
					StartPackaging(stationToAttend);
					return;
				}
				BrickPress brickPress = GetBrickPress();
				if (brickPress != null)
				{
					StartPress(brickPress);
					return;
				}
				PackagingStation stationMoveItems = GetStationMoveItems();
				if (stationMoveItems != null)
				{
					StartMoveItem(stationMoveItems);
					return;
				}
				BrickPress brickPressMoveItems = GetBrickPressMoveItems();
				if (brickPressMoveItems != null)
				{
					StartMoveItem(brickPressMoveItems);
					return;
				}
				ItemInstance item;
				AdvancedTransitRoute transitRouteReady = GetTransitRouteReady(out item);
				if (transitRouteReady != null)
				{
					MoveItemBehaviour.Initialize(transitRouteReady, item, item.Quantity);
					MoveItemBehaviour.Enable_Networked(null);
				}
				else
				{
					SubmitNoWorkReason("There's nothing for me to do right now.", "I need one of my assigned stations to have enough product and packaging to get to work.");
					SetIdle(idle: true);
				}
			}
		}
	}

	private void StartPackaging(PackagingStation station)
	{
		Console.Log("Starting packaging at " + station.gameObject.name);
		PackagingBehaviour.AssignStation(station);
		PackagingBehaviour.Enable_Networked(null);
	}

	private void StartPress(BrickPress press)
	{
		BrickPressBehaviour.AssignStation(press);
		BrickPressBehaviour.Enable_Networked(null);
	}

	private void StartMoveItem(PackagingStation station)
	{
		Console.Log("Starting moving items from " + station.gameObject.name);
		MoveItemBehaviour.Initialize((station.Configuration as PackagingStationConfiguration).DestinationRoute, station.OutputSlot.ItemInstance);
		MoveItemBehaviour.Enable_Networked(null);
	}

	private void StartMoveItem(BrickPress press)
	{
		MoveItemBehaviour.Initialize((press.Configuration as BrickPressConfiguration).DestinationRoute, press.OutputSlot.ItemInstance);
		MoveItemBehaviour.Enable_Networked(null);
	}

	protected PackagingStation GetStationToAttend()
	{
		foreach (PackagingStation assignedStation in configuration.AssignedStations)
		{
			if (PackagingBehaviour.IsStationReady(assignedStation))
			{
				return assignedStation;
			}
		}
		return null;
	}

	protected BrickPress GetBrickPress()
	{
		foreach (BrickPress assignedBrickPress in configuration.AssignedBrickPresses)
		{
			if (BrickPressBehaviour.IsStationReady(assignedBrickPress))
			{
				return assignedBrickPress;
			}
		}
		return null;
	}

	protected PackagingStation GetStationMoveItems()
	{
		foreach (PackagingStation assignedStation in configuration.AssignedStations)
		{
			ItemSlot outputSlot = assignedStation.OutputSlot;
			if (outputSlot.Quantity != 0 && MoveItemBehaviour.IsTransitRouteValid((assignedStation.Configuration as PackagingStationConfiguration).DestinationRoute, outputSlot.ItemInstance.ID))
			{
				return assignedStation;
			}
		}
		return null;
	}

	protected BrickPress GetBrickPressMoveItems()
	{
		foreach (BrickPress assignedBrickPress in configuration.AssignedBrickPresses)
		{
			ItemSlot outputSlot = assignedBrickPress.OutputSlot;
			if (outputSlot.Quantity != 0 && MoveItemBehaviour.IsTransitRouteValid((assignedBrickPress.Configuration as BrickPressConfiguration).DestinationRoute, outputSlot.ItemInstance.ID))
			{
				return assignedBrickPress;
			}
		}
		return null;
	}

	protected AdvancedTransitRoute GetTransitRouteReady(out ItemInstance item)
	{
		item = null;
		foreach (AdvancedTransitRoute route in configuration.Routes.Routes)
		{
			item = route.GetItemReadyToMove();
			if (item != null && movement.CanGetTo(route.Source) && movement.CanGetTo(route.Destination))
			{
				return route;
			}
		}
		return null;
	}

	protected override bool ShouldIdle()
	{
		if (configuration.AssignedStationCount == 0)
		{
			return true;
		}
		return base.ShouldIdle();
	}

	public override BedItem GetBed()
	{
		return configuration.bedItem;
	}

	public WorldspaceUIElement CreateWorldspaceUI()
	{
		if (WorldspaceUI != null)
		{
			Console.LogWarning(base.gameObject.name + " already has a worldspace UI element!");
		}
		ScheduleOne.Property.Property assignedProperty = base.AssignedProperty;
		if (assignedProperty == null)
		{
			Console.LogError(assignedProperty?.ToString() + " is not a child of a property!");
			return null;
		}
		PackagerUIElement component = Object.Instantiate(WorldspaceUIPrefab, assignedProperty.WorldspaceUIContainer).GetComponent<PackagerUIElement>();
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

	public override string GetSaveString()
	{
		return new PackagerData(ID, base.AssignedProperty.PropertyCode, FirstName, LastName, base.IsMale, base.AppearanceIndex, base.transform.position, base.transform.rotation, base.GUID, base.PaidForToday, MoveItemBehaviour.GetSaveData()).GetJson();
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
		if (!NetworkInitialize___EarlyScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField = new SyncVar<NetworkObject>(this, 2u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, CurrentPlayerConfigurer);
			RegisterServerRpc(40u, RpcReader___Server_SetConfigurer_3323014238);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EEmployees_002EPackager);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEmployees_002EPackagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
			syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField.SetRegistered();
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
			SendServerRpc(40u, writer, channel, DataOrderType.Default);
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

	public virtual bool ReadSyncVar___ScheduleOne_002EEmployees_002EPackager(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		if (UInt321 == 2)
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CCurrentPlayerConfigurer_003Ek__BackingField(syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			NetworkObject value = PooledReader0.ReadNetworkObject();
			this.sync___set_value__003CCurrentPlayerConfigurer_003Ek__BackingField(value, Boolean2);
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
