using System.Collections;
using FishNet;
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.DevUtilities;
using ScheduleOne.Employees;
using ScheduleOne.Equipping;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using ScheduleOne.ObjectScripts.Soil;
using ScheduleOne.Trash;
using UnityEngine;

namespace ScheduleOne.NPCs.Behaviour;

public class PotActionBehaviour : Behaviour
{
	public enum EActionType
	{
		None = 0,
		PourSoil = 1,
		SowSeed = 2,
		Water = 3,
		ApplyAdditive = 4,
		Harvest = 5
	}

	public enum EState
	{
		Idle = 0,
		WalkingToSupplies = 1,
		GrabbingSupplies = 2,
		WalkingToPot = 3,
		PerformingAction = 4,
		WalkingToDestination = 5
	}

	[HideInInspector]
	public int AdditiveNumber = -1;

	[Header("Equippables")]
	public AvatarEquippable WateringCanEquippable;

	public AvatarEquippable TrimmersEquippable;

	private Botanist botanist;

	private Coroutine walkToSuppliesRoutine;

	private Coroutine grabRoutine;

	private Coroutine walkToPotRoutine;

	private Coroutine performActionRoutine;

	private string currentActionAnimation = string.Empty;

	private AvatarEquippable currentActionEquippable;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted;

	public bool Initialized { get; protected set; }

	public Pot AssignedPot { get; protected set; }

	public EActionType CurrentActionType { get; protected set; }

	public EState CurrentState { get; protected set; }

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviour_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public virtual void Initialize(Pot pot, EActionType actionType)
	{
		if (botanist.DEBUG)
		{
			Debug.Log("PotActionBehaviour.Initialize: " + pot?.ToString() + " - " + actionType);
		}
		AssignedPot = pot;
		CurrentActionType = actionType;
		Initialized = true;
		CurrentState = EState.Idle;
	}

	protected override void Begin()
	{
		base.Begin();
		StartAction();
	}

	protected override void Resume()
	{
		base.Resume();
		StartAction();
	}

	protected override void Pause()
	{
		base.Pause();
		StopAllActions();
	}

	public override void Disable()
	{
		base.Disable();
		if (base.Active)
		{
			End();
		}
	}

	protected override void End()
	{
		base.End();
		StopAllActions();
	}

	public override void ActiveMinPass()
	{
		base.ActiveMinPass();
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (base.Npc.behaviour.DEBUG_MODE)
		{
			Console.Log("Current state: " + CurrentState);
			Console.Log("Is walking: " + base.Npc.Movement.IsMoving);
		}
		if (CurrentState != EState.Idle)
		{
			return;
		}
		if (!DoesTaskTypeRequireSupplies(CurrentActionType) || base.Npc.Inventory.GetMaxItemCount(GetRequiredItemIDs()) > 0)
		{
			if (IsAtPot())
			{
				PerformAction();
			}
			else
			{
				WalkToPot();
			}
		}
		else if (AssignedPot == null)
		{
			Console.LogWarning("PotActionBehaviour.ActiveMinPass: No pot assigned for botanist " + botanist);
			Disable_Networked(null);
		}
		else if (IsAtSupplies())
		{
			if (DoesBotanistHaveMaterialsForTask(botanist, AssignedPot, CurrentActionType, AdditiveNumber))
			{
				GrabItem();
				return;
			}
			StopPerformAction();
			Disable_Networked(null);
		}
		else
		{
			WalkToSupplies();
		}
	}

	private void StartAction()
	{
		if (!AreActionConditionsMet())
		{
			Console.LogWarning("PotActionBehaviour.StartAction: Conditions not met for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
			Disable_Networked(null);
			return;
		}
		if (!DoesBotanistHaveMaterialsForTask(base.Npc as Botanist, AssignedPot, CurrentActionType, AdditiveNumber))
		{
			Console.LogWarning("PotActionBehaviour.StartAction: Botanist does not have materials for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
			Disable_Networked(null);
			return;
		}
		if (botanist.DEBUG)
		{
			Console.Log("PotActionBehaviour.StartAction: Starting action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
		}
		CurrentState = EState.Idle;
	}

	private void StopAllActions()
	{
		if (walkToSuppliesRoutine != null)
		{
			StopCoroutine(walkToSuppliesRoutine);
			walkToSuppliesRoutine = null;
		}
		if (grabRoutine != null)
		{
			StopCoroutine(grabRoutine);
			grabRoutine = null;
		}
		if (walkToPotRoutine != null)
		{
			StopCoroutine(walkToPotRoutine);
			walkToPotRoutine = null;
		}
		if (performActionRoutine != null)
		{
			StopPerformAction();
		}
	}

	public void WalkToSupplies()
	{
		if (!base.Npc.Movement.CanGetTo(AssignedPot))
		{
			Console.LogWarning("PotActionBehaviour.WalkToPot: Can't get to pot " + AssignedPot);
			Disable_Networked(null);
			return;
		}
		if (botanist.DEBUG)
		{
			Debug.Log("PotActionBehaviour.WalkToSupplies: Walking to supplies for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
		}
		CurrentState = EState.WalkingToSupplies;
		walkToSuppliesRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			SetDestination(GetSuppliesAsTransitEntity());
			yield return new WaitForEndOfFrame();
			yield return new WaitUntil(() => !base.Npc.Movement.IsMoving);
			CurrentState = EState.Idle;
			walkToSuppliesRoutine = null;
		}
	}

	public void GrabItem()
	{
		if (botanist.DEBUG)
		{
			Debug.Log("PotActionBehaviour.GrabItem: Grabbing item for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
		}
		CurrentState = EState.GrabbingSupplies;
		grabRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			base.Npc.Movement.FacePoint((botanist.Configuration as BotanistConfiguration).Supplies.SelectedObject.transform.position);
			base.Npc.Avatar.Anim.ResetTrigger("GrabItem");
			base.Npc.Avatar.Anim.SetTrigger("GrabItem");
			float seconds = 0.5f;
			yield return new WaitForSeconds(seconds);
			if (!DoesBotanistHaveMaterialsForTask(botanist, AssignedPot, CurrentActionType, AdditiveNumber))
			{
				Console.LogWarning("Botanist does not have materials for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
				grabRoutine = null;
				CurrentState = EState.Idle;
			}
			else if (!AreActionConditionsMet())
			{
				Console.LogWarning("Conditions not met for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
				grabRoutine = null;
				CurrentState = EState.Idle;
			}
			else
			{
				ItemSlot itemSlot = null;
				string[] requiredItemIDs = GetRequiredItemIDs();
				for (int i = 0; i < requiredItemIDs.Length; i++)
				{
					itemSlot = ((botanist.Configuration as BotanistConfiguration).Supplies.SelectedObject as ITransitEntity).GetFirstSlotContainingItem(requiredItemIDs[i], ITransitEntity.ESlotType.Both);
					if (itemSlot != null)
					{
						break;
					}
				}
				ItemInstance itemInstance = itemSlot?.ItemInstance;
				if (itemInstance == null)
				{
					Console.LogWarning("PotActionBehaviour.GrabItem: No item found for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
					grabRoutine = null;
					CurrentState = EState.Idle;
				}
				else
				{
					base.Npc.Inventory.InsertItem(itemInstance.GetCopy(1));
					itemSlot.ChangeQuantity(-1);
					yield return new WaitForSeconds(0.5f);
					grabRoutine = null;
					CurrentState = EState.Idle;
				}
			}
		}
	}

	public void WalkToPot()
	{
		if (!base.Npc.Movement.CanGetTo(GetPotAccessPoint().position))
		{
			Console.LogWarning("PotActionBehaviour.WalkToPot: Can't get to pot " + AssignedPot);
			Disable_Networked(null);
			return;
		}
		if (botanist.DEBUG)
		{
			Debug.Log("PotActionBehaviour.WalkToPot: Walking to pot " + AssignedPot);
		}
		CurrentState = EState.WalkingToPot;
		walkToPotRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			SetDestination(GetPotAccessPoint().position);
			yield return new WaitForEndOfFrame();
			yield return new WaitUntil(() => !base.Npc.Movement.IsMoving);
			CurrentState = EState.Idle;
			walkToPotRoutine = null;
		}
	}

	public void PerformAction()
	{
		if (botanist.DEBUG)
		{
			Debug.Log("PotActionBehaviour.PerformAction: Performing action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
		}
		CurrentState = EState.PerformingAction;
		performActionRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			AssignedPot.SetNPCUser(botanist.NetworkObject);
			base.Npc.Movement.FacePoint(AssignedPot.transform.position);
			string actionAnimation = GetActionAnimation(CurrentActionType);
			if (actionAnimation != string.Empty)
			{
				currentActionAnimation = actionAnimation;
				base.Npc.SetAnimationBool_Networked(null, actionAnimation, value: true);
			}
			if (CurrentActionType == EActionType.SowSeed && !base.Npc.Avatar.Anim.IsCrouched)
			{
				base.Npc.SetCrouched_Networked(crouched: true);
			}
			AvatarEquippable actionEquippable = GetActionEquippable(CurrentActionType);
			if (actionEquippable != null)
			{
				currentActionEquippable = base.Npc.SetEquippable_Networked_Return(null, actionEquippable.AssetPath);
			}
			float waitTime = GetWaitTime(CurrentActionType);
			for (float i = 0f; i < waitTime; i += Time.deltaTime)
			{
				base.Npc.Avatar.LookController.OverrideLookTarget(AssignedPot.transform.position, 0);
				yield return new WaitForEndOfFrame();
			}
			StopPerformAction();
			CompleteAction();
		}
	}

	private void CompleteAction()
	{
		if (AssignedPot == null)
		{
			Console.LogWarning("PotActionBehaviour.CompleteAction: No pot assigned for botanist " + botanist);
			return;
		}
		ItemInstance itemInstance = null;
		string[] requiredItemIDs = GetRequiredItemIDs();
		for (int i = 0; i < requiredItemIDs.Length; i++)
		{
			itemInstance = base.Npc.Inventory.GetFirstItem(requiredItemIDs[i]);
			if (itemInstance != null)
			{
				break;
			}
		}
		if (DoesTaskTypeRequireSupplies(CurrentActionType) && itemInstance == null)
		{
			Console.LogWarning("PotActionBehaviour.CompleteAction: No item held for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
			return;
		}
		ItemInstance itemInstance2 = null;
		switch (CurrentActionType)
		{
		case EActionType.PourSoil:
		{
			SoilDefinition soilDefinition = itemInstance.Definition as SoilDefinition;
			if (soilDefinition == null)
			{
				Console.LogWarning("PotActionBehaviour.CompleteAction: Required item is not soil for action " + CurrentActionType.ToString() + " on pot " + AssignedPot);
				return;
			}
			AssignedPot.AddSoil(AssignedPot.SoilCapacity);
			AssignedPot.SetSoilID(soilDefinition.ID);
			AssignedPot.SetSoilUses(soilDefinition.Uses);
			NetworkSingleton<TrashManager>.Instance.CreateTrashItem((soilDefinition.Equippable as Equippable_Soil).PourablePrefab.TrashItem.ID, base.transform.position + Vector3.up * 0.5f, Random.rotation);
			break;
		}
		case EActionType.SowSeed:
			AssignedPot.PlantSeed(null, itemInstance.ID, 0f, -1f, -1f);
			NetworkSingleton<TrashManager>.Instance.CreateTrashItem((itemInstance.Definition as SeedDefinition).FunctionSeedPrefab.TrashPrefab.ID, base.transform.position + Vector3.up * 0.5f, Random.rotation);
			break;
		case EActionType.Water:
		{
			float num = Random.Range(botanist.TARGET_WATER_LEVEL_MIN, botanist.TARGET_WATER_LEVEL_MAX);
			AssignedPot.ChangeWaterAmount(num * AssignedPot.WaterCapacity - AssignedPot.WaterLevel);
			break;
		}
		case EActionType.ApplyAdditive:
			AssignedPot.ApplyAdditive(null, (itemInstance.Definition as AdditiveDefinition).AdditivePrefab.AssetPath, initial: true);
			NetworkSingleton<TrashManager>.Instance.CreateTrashItem(((itemInstance.Definition as AdditiveDefinition).Equippable as Equippable_Additive).PourablePrefab.TrashItem.ID, base.transform.position + Vector3.up * 0.5f, Random.rotation);
			break;
		case EActionType.Harvest:
			if (!DoesPotHaveValidDestination(AssignedPot))
			{
				Console.LogWarning("PotActionBehaviour.CompleteAction: Pot " + AssignedPot?.ToString() + " does not have a valid destination");
				return;
			}
			itemInstance2 = AssignedPot.Plant.GetHarvestedProduct(AssignedPot.Plant.ActiveHarvestables.Count);
			AssignedPot.ResetPot();
			break;
		}
		itemInstance?.ChangeQuantity(-1);
		if (CurrentActionType == EActionType.Harvest)
		{
			((ITransitEntity)AssignedPot).InsertItemIntoOutput(itemInstance2, (NPC)null);
			TransitRoute route = new TransitRoute(AssignedPot, (AssignedPot.Configuration as PotConfiguration).Destination.SelectedObject as ITransitEntity);
			botanist.MoveItemBehaviour.Initialize(route, itemInstance2, -1, _skipPickup: true);
			botanist.MoveItemBehaviour.Enable_Networked(null);
		}
		Disable_Networked(null);
	}

	private void StopPerformAction()
	{
		if (CurrentActionType == EActionType.SowSeed && base.Npc.Avatar.Anim.IsCrouched)
		{
			base.Npc.SetCrouched_Networked(crouched: false);
		}
		CurrentState = EState.Idle;
		if (performActionRoutine != null)
		{
			StopCoroutine(performActionRoutine);
			performActionRoutine = null;
		}
		if (currentActionEquippable != null)
		{
			base.Npc.SetEquippable_Networked(null, string.Empty);
			currentActionEquippable = null;
		}
		if (currentActionAnimation != string.Empty)
		{
			base.Npc.SetAnimationBool_Networked(null, currentActionAnimation, value: false);
			currentActionAnimation = string.Empty;
		}
		if (AssignedPot != null && AssignedPot.NPCUserObject == botanist.NetworkObject)
		{
			AssignedPot.SetNPCUser(null);
		}
	}

	private string GetActionAnimation(EActionType actionType)
	{
		return actionType switch
		{
			EActionType.PourSoil => "PourItem", 
			EActionType.SowSeed => "PatSoil", 
			EActionType.Water => "PourItem", 
			EActionType.ApplyAdditive => "PourItem", 
			EActionType.Harvest => "Snipping", 
			_ => string.Empty, 
		};
	}

	private AvatarEquippable GetActionEquippable(EActionType actionType)
	{
		switch (actionType)
		{
		case EActionType.SowSeed:
			return null;
		case EActionType.Water:
			return WateringCanEquippable;
		case EActionType.Harvest:
			return TrimmersEquippable;
		default:
		{
			ItemInstance itemInstance = null;
			string[] requiredItemIDs = GetRequiredItemIDs();
			for (int i = 0; i < requiredItemIDs.Length; i++)
			{
				itemInstance = base.Npc.Inventory.GetFirstItem(requiredItemIDs[i]);
				if (itemInstance != null)
				{
					break;
				}
			}
			if (itemInstance != null)
			{
				return (itemInstance.Equippable as Equippable_Viewmodel)?.AvatarEquippable;
			}
			return null;
		}
		}
	}

	public float GetWaitTime(EActionType actionType)
	{
		switch (actionType)
		{
		case EActionType.PourSoil:
			return botanist.SOIL_POUR_TIME;
		case EActionType.SowSeed:
			return botanist.SEED_SOW_TIME;
		case EActionType.Water:
			return botanist.WATER_POUR_TIME;
		case EActionType.ApplyAdditive:
			return botanist.ADDITIVE_POUR_TIME;
		case EActionType.Harvest:
			return botanist.HARVEST_TIME;
		default:
			Console.LogWarning("Can't find wait time for " + actionType);
			return 10f;
		}
	}

	public bool CanGetToSupplies()
	{
		return base.Npc.Movement.CanGetTo((botanist.Configuration as BotanistConfiguration).Supplies.SelectedObject as ITransitEntity);
	}

	private bool IsAtSupplies()
	{
		return NavMeshUtility.IsAtTransitEntity(GetSuppliesAsTransitEntity(), base.Npc);
	}

	private ITransitEntity GetSuppliesAsTransitEntity()
	{
		if ((botanist.Configuration as BotanistConfiguration).Supplies.SelectedObject == null)
		{
			Console.LogWarning("PotActionBehaviour.GetSuppliesAsTransitEntity: No supplies selected for botanist " + botanist);
			return null;
		}
		return (botanist.Configuration as BotanistConfiguration).Supplies.SelectedObject as ITransitEntity;
	}

	public bool CanGetToPot()
	{
		return GetPotAccessPoint() != null;
	}

	private Transform GetPotAccessPoint()
	{
		if (AssignedPot == null)
		{
			Console.LogWarning("PotActionBehaviour.GetpotAccessPoint: No pot selected for botanist " + botanist);
			return null;
		}
		Transform accessPoint = NavMeshUtility.GetAccessPoint(AssignedPot, base.Npc);
		if (accessPoint == null)
		{
			Console.LogWarning("PotActionBehaviour.GetpotAccessPoint: No access point found for pot " + AssignedPot);
			return AssignedPot.transform;
		}
		return accessPoint;
	}

	private bool IsAtPot()
	{
		if (AssignedPot == null)
		{
			Console.LogWarning("PotActionBehaviour.IsAtpot: No pot selected for botanist " + botanist);
			return false;
		}
		return NavMeshUtility.IsAtTransitEntity(AssignedPot, base.Npc);
	}

	private string[] GetRequiredItemIDs(EActionType actionType, Pot pot)
	{
		PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
		switch (actionType)
		{
		case EActionType.PourSoil:
			return new string[3] { "soil", "longlifesoil", "extralonglifesoil" };
		case EActionType.SowSeed:
			if (potConfiguration.Seed.SelectedItem == null)
			{
				return Singleton<Registry>.Instance.Seeds.ConvertAll((SeedDefinition x) => x.ID).ToArray();
			}
			return new string[1] { potConfiguration.Seed.SelectedItem.ID };
		case EActionType.ApplyAdditive:
			if (AdditiveNumber == 1)
			{
				return new string[1] { potConfiguration.Additive1.SelectedItem.ID };
			}
			if (AdditiveNumber == 2)
			{
				return new string[1] { potConfiguration.Additive2.SelectedItem.ID };
			}
			if (AdditiveNumber == 3)
			{
				return new string[1] { potConfiguration.Additive3.SelectedItem.ID };
			}
			break;
		}
		return new string[0];
	}

	private string[] GetRequiredItemIDs()
	{
		return GetRequiredItemIDs(CurrentActionType, AssignedPot);
	}

	private bool AreActionConditionsMet()
	{
		int additiveNumber;
		return CurrentActionType switch
		{
			EActionType.PourSoil => CanPotHaveSoilPour(AssignedPot), 
			EActionType.SowSeed => CanPotHaveSeedSown(AssignedPot), 
			EActionType.Water => CanPotBeWatered(AssignedPot, 1f), 
			EActionType.ApplyAdditive => CanPotHaveAdditiveApplied(AssignedPot, out additiveNumber), 
			EActionType.Harvest => CanPotBeHarvested(AssignedPot), 
			_ => false, 
		};
	}

	public bool DoesTaskTypeRequireSupplies(EActionType actionType)
	{
		if ((uint)(actionType - 1) <= 1u || actionType == EActionType.ApplyAdditive)
		{
			return true;
		}
		return false;
	}

	public bool DoesBotanistHaveMaterialsForTask(Botanist botanist, Pot pot, EActionType actionType, int additiveNumber = -1)
	{
		switch (actionType)
		{
		case EActionType.PourSoil:
			if (GetSoilInSupplies() == null)
			{
				return base.Npc.Inventory.GetMaxItemCount(GetRequiredItemIDs(actionType, pot)) > 0;
			}
			return true;
		case EActionType.SowSeed:
			if (GetSeedInSupplies(pot) == null)
			{
				return base.Npc.Inventory.GetMaxItemCount(GetRequiredItemIDs(actionType, pot)) > 0;
			}
			return true;
		case EActionType.ApplyAdditive:
			if (GetAdditiveInSupplies(pot, additiveNumber) == null)
			{
				return base.Npc.Inventory.GetMaxItemCount(GetRequiredItemIDs(actionType, pot)) > 0;
			}
			return true;
		default:
			return true;
		}
	}

	private ItemInstance GetSoilInSupplies()
	{
		ItemInstance itemInSupplies = botanist.GetItemInSupplies("soil");
		if (itemInSupplies != null)
		{
			return itemInSupplies;
		}
		ItemInstance itemInSupplies2 = botanist.GetItemInSupplies("longlifesoil");
		if (itemInSupplies2 != null)
		{
			return itemInSupplies2;
		}
		ItemInstance itemInSupplies3 = botanist.GetItemInSupplies("extralonglifesoil");
		if (itemInSupplies3 != null)
		{
			return itemInSupplies3;
		}
		return null;
	}

	private ItemInstance GetSeedInSupplies(Pot pot)
	{
		PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
		if (potConfiguration.Seed.SelectedItem == null)
		{
			return botanist.GetSeedInSupplies();
		}
		return botanist.GetItemInSupplies(potConfiguration.Seed.SelectedItem.ID);
	}

	private ItemInstance GetAdditiveInSupplies(Pot pot, int additiveNumber)
	{
		PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
		ItemDefinition itemDefinition = null;
		switch (additiveNumber)
		{
		case 1:
			itemDefinition = potConfiguration.Additive1.SelectedItem;
			break;
		case 2:
			itemDefinition = potConfiguration.Additive2.SelectedItem;
			break;
		case 3:
			itemDefinition = potConfiguration.Additive3.SelectedItem;
			break;
		default:
			Console.LogWarning("PotActionBehaviour.DoesBotanistHaveMaterialsForTask: Invalid additive number " + additiveNumber);
			return null;
		}
		if (itemDefinition == null)
		{
			return null;
		}
		return botanist.GetItemInSupplies(itemDefinition.ID);
	}

	public bool CanPotBeWatered(Pot pot, float threshold)
	{
		if (((IUsable)pot).IsInUse)
		{
			return false;
		}
		if (!pot.IsFilledWithSoil)
		{
			return false;
		}
		if (pot.Plant == null)
		{
			return false;
		}
		if (pot.WaterLevel > threshold)
		{
			return false;
		}
		return true;
	}

	public bool CanPotHaveSoilPour(Pot pot)
	{
		if (((IUsable)pot).IsInUse)
		{
			return false;
		}
		if (pot.IsFilledWithSoil)
		{
			return false;
		}
		return true;
	}

	public bool CanPotHaveSeedSown(Pot pot)
	{
		if (((IUsable)pot).IsInUse)
		{
			return false;
		}
		if (!pot.IsFilledWithSoil)
		{
			return false;
		}
		if (pot.Plant != null)
		{
			return false;
		}
		return true;
	}

	public bool CanPotHaveAdditiveApplied(Pot pot, out int additiveNumber)
	{
		additiveNumber = -1;
		if (((IUsable)pot).IsInUse)
		{
			return false;
		}
		if (!pot.IsFilledWithSoil)
		{
			return false;
		}
		if (pot.Plant == null)
		{
			return false;
		}
		PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
		if (potConfiguration.Additive1.SelectedItem != null && pot.GetAdditive((potConfiguration.Additive1.SelectedItem as AdditiveDefinition).AdditivePrefab.AdditiveName) == null)
		{
			additiveNumber = 1;
			return true;
		}
		if (potConfiguration.Additive2.SelectedItem != null && pot.GetAdditive((potConfiguration.Additive2.SelectedItem as AdditiveDefinition).AdditivePrefab.AdditiveName) == null)
		{
			additiveNumber = 2;
			return true;
		}
		if (potConfiguration.Additive3.SelectedItem != null && pot.GetAdditive((potConfiguration.Additive3.SelectedItem as AdditiveDefinition).AdditivePrefab.AdditiveName) == null)
		{
			additiveNumber = 3;
			return true;
		}
		return false;
	}

	public bool CanPotBeHarvested(Pot pot)
	{
		if (((IUsable)pot).IsInUse)
		{
			return false;
		}
		if (pot.Plant == null)
		{
			return false;
		}
		_ = pot.Configuration;
		if (pot.Plant.IsFullyGrown)
		{
			return true;
		}
		return false;
	}

	public bool DoesPotHaveValidDestination(Pot pot)
	{
		PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
		if (potConfiguration.Destination.SelectedObject == null)
		{
			return false;
		}
		if ((potConfiguration.Destination.SelectedObject as ITransitEntity).GetInputCapacityForItem(pot.Plant.GetHarvestedProduct(), botanist) >= pot.Plant.ActiveHarvestables.Count)
		{
			return true;
		}
		return false;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002EBehaviour_002EPotActionBehaviour_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		botanist = base.Npc as Botanist;
	}
}
