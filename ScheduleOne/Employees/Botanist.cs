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
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.EntityFramework;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Property;
using ScheduleOne.UI.Management;
using UnityEngine;

namespace ScheduleOne.Employees;

public class Botanist : Employee, IConfigurable
{
	public float CRITICAL_WATERING_THRESHOLD = 0.1f;

	public float WATERING_THRESHOLD = 0.3f;

	public float TARGET_WATER_LEVEL_MIN = 0.75f;

	public float TARGET_WATER_LEVEL_MAX = 1f;

	public float SOIL_POUR_TIME = 10f;

	public float WATER_POUR_TIME = 10f;

	public float ADDITIVE_POUR_TIME = 10f;

	public float SEED_SOW_TIME = 15f;

	public float HARVEST_TIME = 15f;

	[Header("References")]
	public Sprite typeIcon;

	[SerializeField]
	protected ConfigurationReplicator configReplicator;

	public PotActionBehaviour PotActionBehaviour;

	public StartDryingRackBehaviour StartDryingRackBehaviour;

	public StopDryingRackBehaviour StopDryingRackBehaviour;

	[Header("UI")]
	public BotanistUIElement WorldspaceUIPrefab;

	public Transform uiPoint;

	[Header("Settings")]
	public int MaxAssignedPots = 8;

	public DialogueContainer NoAssignedStationsDialogue;

	public DialogueContainer UnspecifiedPotsDialogue;

	public DialogueContainer NullDestinationPotsDialogue;

	public DialogueContainer MissingMaterialsDialogue;

	public DialogueContainer NoPotsRequireWorkDialogue;

	[CompilerGenerated]
	[SyncVar]
	public NetworkObject _003CCurrentPlayerConfigurer_003Ek__BackingField;

	public SyncVar<NetworkObject> syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField;

	private bool NetworkInitialize___EarlyScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted;

	public EntityConfiguration Configuration => configuration;

	protected BotanistConfiguration configuration { get; set; }

	public ConfigurationReplicator ConfigReplicator => configReplicator;

	public EConfigurableType ConfigurableType => EConfigurableType.Botanist;

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

	protected override void Start()
	{
		base.Start();
	}

	protected override void UpdateBehaviour()
	{
		base.UpdateBehaviour();
		if (PotActionBehaviour.Active)
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
			if (configuration.AssignedPots.Count + configuration.AssignedRacks.Count == 0)
			{
				SubmitNoWorkReason("I haven't been assigned any pots or drying racks", "You can use your management clipboards to assign pots/drying racks to me.");
				SetIdle(idle: true);
			}
			else
			{
				if (!InstanceFinder.IsServer)
				{
					return;
				}
				Pot potForWatering = GetPotForWatering(CRITICAL_WATERING_THRESHOLD, excludeFullyGrowm: true);
				if (potForWatering != null && NavMeshUtility.GetAccessPoint(potForWatering, this) != null)
				{
					StartAction(potForWatering, PotActionBehaviour.EActionType.Water);
					return;
				}
				Pot potForSoilSour = GetPotForSoilSour();
				if (potForSoilSour != null)
				{
					if (PotActionBehaviour.DoesBotanistHaveMaterialsForTask(this, potForSoilSour, PotActionBehaviour.EActionType.PourSoil))
					{
						StartAction(potForSoilSour, PotActionBehaviour.EActionType.PourSoil);
						return;
					}
					string fix = "Make sure there's soil in my supplies stash.";
					if (configuration.Supplies.SelectedObject == null)
					{
						fix = "Use your management clipboards to assign a supplies stash to me. Then make sure there's soil in it.";
					}
					SubmitNoWorkReason("There are empty pots, but I don't have any soil to pour.", fix);
				}
				foreach (Pot item in GetPotsReadyForSeed())
				{
					if ((bool)NavMeshUtility.GetAccessPoint(item, this))
					{
						if (PotActionBehaviour.DoesBotanistHaveMaterialsForTask(this, item, PotActionBehaviour.EActionType.SowSeed))
						{
							StartAction(item, PotActionBehaviour.EActionType.SowSeed);
							return;
						}
						string fix2 = "Make sure I have the right seeds in my supplies stash.";
						if (configuration.Supplies.SelectedObject == null)
						{
							fix2 = "Use your management clipboards to assign a supplies stash to me, and make sure it contains the right seeds.";
						}
						SubmitNoWorkReason("There is a pot ready for sowing, but I don't have any seeds for it.", fix2, 1);
					}
				}
				int additiveNumber;
				Pot potForAdditives = GetPotForAdditives(out additiveNumber);
				if (potForAdditives != null && PotActionBehaviour.DoesBotanistHaveMaterialsForTask(this, potForAdditives, PotActionBehaviour.EActionType.ApplyAdditive, additiveNumber))
				{
					PotActionBehaviour.AdditiveNumber = additiveNumber;
					StartAction(potForAdditives, PotActionBehaviour.EActionType.ApplyAdditive);
					return;
				}
				foreach (Pot item2 in GetPotsForHarvest())
				{
					if (IsEntityAccessible(item2))
					{
						if (PotActionBehaviour.DoesPotHaveValidDestination(item2))
						{
							StartAction(item2, PotActionBehaviour.EActionType.Harvest);
							return;
						}
						SubmitNoWorkReason("There is a plant ready for harvest, but it has no destination or it's destination is full.", "Use your management clipboard to assign a destination for each of my pots, and make sure the destination isn't full.");
					}
				}
				foreach (DryingRack item3 in GetRacksToStop())
				{
					if (IsEntityAccessible(item3))
					{
						StopDryingRack(item3);
						return;
					}
				}
				foreach (DryingRack item4 in GetRacksToStart())
				{
					if (IsEntityAccessible(item4))
					{
						StartDryingRack(item4);
						return;
					}
				}
				foreach (DryingRack item5 in GetRacksReadyToMove())
				{
					if (IsEntityAccessible(item5))
					{
						MoveItemBehaviour.Initialize((item5.Configuration as DryingRackConfiguration).DestinationRoute, item5.OutputSlot.ItemInstance);
						MoveItemBehaviour.Enable_Networked(null);
						return;
					}
				}
				Pot potForWatering2 = GetPotForWatering(WATERING_THRESHOLD, excludeFullyGrowm: false);
				QualityItemInstance dryable;
				DryingRack destinationRack;
				int moveQuantity;
				if (potForWatering2 != null)
				{
					StartAction(potForWatering2, PotActionBehaviour.EActionType.Water);
				}
				else if (CanMoveDryableToRack(out dryable, out destinationRack, out moveQuantity))
				{
					TransitRoute route = new TransitRoute(configuration.Supplies.SelectedObject as ITransitEntity, destinationRack);
					MoveItemBehaviour.Initialize(route, dryable, moveQuantity);
					MoveItemBehaviour.Enable_Networked(null);
					Console.Log("Moving " + moveQuantity + " " + dryable.ID + " to drying rack");
				}
				else
				{
					SubmitNoWorkReason("There's nothing for me to do right now.", string.Empty);
					SetIdle(idle: true);
				}
			}
		}
	}

	private bool IsEntityAccessible(ITransitEntity entity)
	{
		return NavMeshUtility.GetAccessPoint(entity, this) != null;
	}

	private void StartAction(Pot pot, PotActionBehaviour.EActionType actionType)
	{
		SetIdle(idle: false);
		PotActionBehaviour.Initialize(pot, actionType);
		PotActionBehaviour.Enable_Networked(null);
	}

	private void StartDryingRack(DryingRack rack)
	{
		StartDryingRackBehaviour.AssignRack(rack);
		StartDryingRackBehaviour.Enable_Networked(null);
	}

	private void StopDryingRack(DryingRack rack)
	{
		StopDryingRackBehaviour.AssignRack(rack);
		StopDryingRackBehaviour.Enable_Networked(null);
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

	protected override void AssignProperty(ScheduleOne.Property.Property prop)
	{
		base.AssignProperty(prop);
		prop.AddConfigurable(this);
		configuration = new BotanistConfiguration(configReplicator, this, this);
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

	private bool CanMoveDryableToRack(out QualityItemInstance dryable, out DryingRack destinationRack, out int moveQuantity)
	{
		moveQuantity = 0;
		destinationRack = null;
		dryable = GetDryableInSupplies();
		if (dryable == null)
		{
			return false;
		}
		Console.Log("Found dryable in supplies: " + dryable.ID);
		int rackInputCapacity = 0;
		destinationRack = GetAssignedDryingRackFor(dryable, out rackInputCapacity);
		if (destinationRack == null)
		{
			return false;
		}
		Console.Log("Found rack with capacity: " + rackInputCapacity);
		moveQuantity = Mathf.Min(dryable.Quantity, rackInputCapacity);
		return true;
	}

	public QualityItemInstance GetDryableInSupplies()
	{
		if (configuration.Supplies.SelectedObject == null)
		{
			return null;
		}
		if (!PotActionBehaviour.CanGetToSupplies())
		{
			return null;
		}
		List<ItemSlot> list = new List<ItemSlot>();
		BuildableItem selectedObject = configuration.Supplies.SelectedObject;
		if (selectedObject != null)
		{
			list.AddRange((selectedObject as ITransitEntity).OutputSlots);
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Quantity > 0 && ItemFilter_Dryable.IsItemDryable(list[i].ItemInstance))
			{
				return list[i].ItemInstance as QualityItemInstance;
			}
		}
		return null;
	}

	private DryingRack GetAssignedDryingRackFor(QualityItemInstance dryable, out int rackInputCapacity)
	{
		rackInputCapacity = 0;
		foreach (DryingRack assignedRack in configuration.AssignedRacks)
		{
			if ((assignedRack.Configuration as DryingRackConfiguration).TargetQuality.Value > dryable.Quality)
			{
				int inputCapacityForItem = ((ITransitEntity)assignedRack).GetInputCapacityForItem((ItemInstance)dryable, (NPC)this);
				if (inputCapacityForItem > 0)
				{
					rackInputCapacity = inputCapacityForItem;
					return assignedRack;
				}
			}
		}
		return null;
	}

	public ItemInstance GetItemInSupplies(string id)
	{
		if (configuration.Supplies.SelectedObject == null)
		{
			return null;
		}
		if (!PotActionBehaviour.CanGetToSupplies())
		{
			return null;
		}
		List<ItemSlot> list = new List<ItemSlot>();
		BuildableItem selectedObject = configuration.Supplies.SelectedObject;
		if (selectedObject != null)
		{
			list.AddRange((selectedObject as ITransitEntity).OutputSlots);
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Quantity > 0 && list[i].ItemInstance.ID.ToLower() == id.ToLower())
			{
				return list[i].ItemInstance;
			}
		}
		return null;
	}

	public ItemInstance GetSeedInSupplies()
	{
		if (configuration.Supplies.SelectedObject == null)
		{
			return null;
		}
		if (!PotActionBehaviour.CanGetToSupplies())
		{
			return null;
		}
		List<ItemSlot> list = new List<ItemSlot>();
		BuildableItem selectedObject = configuration.Supplies.SelectedObject;
		if (selectedObject != null)
		{
			list.AddRange((selectedObject as ITransitEntity).OutputSlots);
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Quantity > 0 && list[i].ItemInstance.Definition is SeedDefinition)
			{
				return list[i].ItemInstance;
			}
		}
		return null;
	}

	protected override bool ShouldIdle()
	{
		if (configuration.AssignedStations.SelectedObjects.Count == 0)
		{
			return true;
		}
		return base.ShouldIdle();
	}

	public override BedItem GetBed()
	{
		return configuration.bedItem;
	}

	private bool AreThereUnspecifiedPots()
	{
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if ((configuration.AssignedPots[i].Configuration as PotConfiguration).Seed.SelectedItem == null)
			{
				return true;
			}
		}
		return false;
	}

	private bool AreThereNullDestinationPots()
	{
		foreach (Pot assignedPot in configuration.AssignedPots)
		{
			if (assignedPot.IsReadyForHarvest(out var _) && (assignedPot.Configuration as PotConfiguration).Destination.SelectedObject == null)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsMissingRequiredMaterials()
	{
		Pot potForSoilSour = GetPotForSoilSour();
		if (potForSoilSour != null && !PotActionBehaviour.DoesBotanistHaveMaterialsForTask(this, potForSoilSour, PotActionBehaviour.EActionType.PourSoil))
		{
			return false;
		}
		List<Pot> potsReadyForSeed = GetPotsReadyForSeed();
		for (int i = 0; i < potsReadyForSeed.Count; i++)
		{
			if (PotActionBehaviour.DoesBotanistHaveMaterialsForTask(this, potsReadyForSeed[i], PotActionBehaviour.EActionType.SowSeed))
			{
				return false;
			}
		}
		return false;
	}

	private Pot GetPotForWatering(float threshold, bool excludeFullyGrowm)
	{
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if (PotActionBehaviour.CanPotBeWatered(configuration.AssignedPots[i], threshold) && (!excludeFullyGrowm || configuration.AssignedPots[i].Plant == null || !configuration.AssignedPots[i].Plant.IsFullyGrown) && IsEntityAccessible(configuration.AssignedPots[i]))
			{
				return configuration.AssignedPots[i];
			}
		}
		return null;
	}

	private Pot GetPotForSoilSour()
	{
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if (PotActionBehaviour.CanPotHaveSoilPour(configuration.AssignedPots[i]) && IsEntityAccessible(configuration.AssignedPots[i]))
			{
				return configuration.AssignedPots[i];
			}
		}
		return null;
	}

	private List<Pot> GetPotsReadyForSeed()
	{
		List<Pot> list = new List<Pot>();
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if (PotActionBehaviour.CanPotHaveSeedSown(configuration.AssignedPots[i]))
			{
				list.Add(configuration.AssignedPots[i]);
			}
		}
		return list;
	}

	private T GetAccessableEntity<T>(T entity) where T : ITransitEntity
	{
		if (!(NavMeshUtility.GetAccessPoint(entity, this) != null))
		{
			return default(T);
		}
		return entity;
	}

	private List<T> GetAccessableEntities<T>(List<T> list) where T : ITransitEntity
	{
		return list.Where((T item) => NavMeshUtility.GetAccessPoint(item, this) != null).ToList();
	}

	private List<Pot> FilterPotsForSpecifiedSeed(List<Pot> pots)
	{
		List<Pot> list = new List<Pot>();
		foreach (Pot pot in pots)
		{
			if ((pot.Configuration as PotConfiguration).Seed.SelectedItem != null)
			{
				list.Add(pot);
			}
		}
		return list;
	}

	private Pot GetPotForAdditives(out int additiveNumber)
	{
		additiveNumber = -1;
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if (PotActionBehaviour.CanPotHaveAdditiveApplied(configuration.AssignedPots[i], out additiveNumber) && IsEntityAccessible(configuration.AssignedPots[i]))
			{
				return configuration.AssignedPots[i];
			}
		}
		return null;
	}

	private List<Pot> GetPotsForHarvest()
	{
		List<Pot> list = new List<Pot>();
		for (int i = 0; i < configuration.AssignedPots.Count; i++)
		{
			if (PotActionBehaviour.CanPotBeHarvested(configuration.AssignedPots[i]))
			{
				list.Add(configuration.AssignedPots[i]);
			}
		}
		return list;
	}

	private List<DryingRack> GetRacksToStart()
	{
		List<DryingRack> list = new List<DryingRack>();
		for (int i = 0; i < configuration.AssignedRacks.Count; i++)
		{
			if (StartDryingRackBehaviour.IsRackReady(configuration.AssignedRacks[i]))
			{
				list.Add(configuration.AssignedRacks[i]);
			}
		}
		return list;
	}

	private List<DryingRack> GetRacksToStop()
	{
		List<DryingRack> list = new List<DryingRack>();
		for (int i = 0; i < configuration.AssignedRacks.Count; i++)
		{
			if (StopDryingRackBehaviour.IsRackReady(configuration.AssignedRacks[i]))
			{
				list.Add(configuration.AssignedRacks[i]);
			}
		}
		return list;
	}

	private List<DryingRack> GetRacksReadyToMove()
	{
		List<DryingRack> list = new List<DryingRack>();
		for (int i = 0; i < configuration.AssignedRacks.Count; i++)
		{
			ItemSlot outputSlot = configuration.AssignedRacks[i].OutputSlot;
			if (outputSlot.Quantity != 0 && MoveItemBehaviour.IsTransitRouteValid((configuration.AssignedRacks[i].Configuration as DryingRackConfiguration).DestinationRoute, outputSlot.ItemInstance.ID))
			{
				list.Add(configuration.AssignedRacks[i]);
			}
		}
		return list;
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
		BotanistUIElement component = Object.Instantiate(WorldspaceUIPrefab, assignedProperty.WorldspaceUIContainer).GetComponent<BotanistUIElement>();
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
		return new BotanistData(ID, base.AssignedProperty.PropertyCode, FirstName, LastName, base.IsMale, base.AppearanceIndex, base.transform.position, base.transform.rotation, base.GUID, base.PaidForToday, MoveItemBehaviour.GetSaveData()).GetJson();
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
		if (!NetworkInitialize___EarlyScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			syncVar____003CCurrentPlayerConfigurer_003Ek__BackingField = new SyncVar<NetworkObject>(this, 2u, WritePermission.ServerOnly, ReadPermission.Observers, -1f, Channel.Reliable, CurrentPlayerConfigurer);
			RegisterServerRpc(40u, RpcReader___Server_SetConfigurer_3323014238);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002EEmployees_002EBotanist);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEmployees_002EBotanistAssembly_002DCSharp_002Edll_Excuted = true;
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

	public virtual bool ReadSyncVar___ScheduleOne_002EEmployees_002EBotanist(PooledReader PooledReader0, uint UInt321, bool Boolean2)
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
