using System;
using System.Collections;
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.NPCs.Behaviour;

public class MoveItemBehaviour : Behaviour
{
	public enum EState
	{
		Idle = 0,
		WalkingToSource = 1,
		Grabbing = 2,
		WalkingToDestination = 3,
		Placing = 4
	}

	private TransitRoute assignedRoute;

	private ItemInstance itemToRetrieveTemplate;

	private int grabbedAmount;

	private int maxMoveAmount = -1;

	private EState currentState;

	private Coroutine walkToSourceRoutine;

	private Coroutine grabRoutine;

	private Coroutine walkToDestinationRoutine;

	private Coroutine placingRoutine;

	private bool skipPickup;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted;

	public bool Initialized { get; protected set; }

	public void Initialize(TransitRoute route, ItemInstance _itemToRetrieveTemplate, int _maxMoveAmount = -1, bool _skipPickup = false)
	{
		if (!IsTransitRouteValid(route, _itemToRetrieveTemplate, out var invalidReason))
		{
			Console.LogError("Invalid transit route for move item behaviour! Reason: " + invalidReason);
			return;
		}
		assignedRoute = route;
		itemToRetrieveTemplate = _itemToRetrieveTemplate;
		maxMoveAmount = _maxMoveAmount;
		if (base.Npc.behaviour.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour initialized with route: " + route.Source.Name + " -> " + route.Destination.Name + " for item: " + _itemToRetrieveTemplate.ID);
		}
		skipPickup = _skipPickup;
	}

	public void Resume(TransitRoute route, ItemInstance _itemToRetrieveTemplate, int _maxMoveAmount = -1)
	{
		assignedRoute = route;
		itemToRetrieveTemplate = _itemToRetrieveTemplate;
		maxMoveAmount = _maxMoveAmount;
	}

	protected override void Begin()
	{
		base.Begin();
		StartTransit();
	}

	protected override void Pause()
	{
		base.Pause();
		StopCurrentActivity();
	}

	protected override void Resume()
	{
		base.Resume();
		StartTransit();
	}

	protected override void End()
	{
		base.End();
		skipPickup = false;
		EndTransit();
	}

	public override void Disable()
	{
		base.Disable();
		if (base.Active)
		{
			End();
		}
	}

	private void StartTransit()
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (base.Npc.Inventory.GetIdenticalItemAmount(itemToRetrieveTemplate) == 0)
		{
			if (!IsTransitRouteValid(assignedRoute, itemToRetrieveTemplate, out var _))
			{
				Console.LogWarning("Invalid transit route for move item behaviour!");
				Disable_Networked(null);
				return;
			}
		}
		else
		{
			ItemInstance firstIdenticalItem = base.Npc.Inventory.GetFirstIdenticalItem(itemToRetrieveTemplate, IsNpcInventoryItemValid);
			if (base.Npc.behaviour.DEBUG_MODE)
			{
				Console.Log("Moving item: " + firstIdenticalItem);
			}
			if (!IsDestinationValid(assignedRoute, firstIdenticalItem))
			{
				Console.LogWarning("Invalid transit route for move item behaviour!");
				Disable_Networked(null);
				return;
			}
		}
		currentState = EState.Idle;
	}

	private bool IsNpcInventoryItemValid(ItemInstance item)
	{
		if (assignedRoute.Destination.GetInputCapacityForItem(item, base.Npc) == 0)
		{
			return false;
		}
		return true;
	}

	private void EndTransit()
	{
		StopCurrentActivity();
		if (assignedRoute != null && base.Npc != null && assignedRoute.Destination != null)
		{
			assignedRoute.Destination.RemoveSlotLocks(base.Npc.NetworkObject);
		}
		Initialized = false;
		assignedRoute = null;
		itemToRetrieveTemplate = null;
		grabbedAmount = 0;
	}

	public override void ActiveMinPass()
	{
		base.ActiveMinPass();
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (!assignedRoute.AreEntitiesNonNull())
		{
			Console.LogWarning("Transit route entities are null!");
			Disable_Networked(null);
			return;
		}
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("State: " + currentState);
			Console.Log("Moving: " + base.Npc.Movement.IsMoving);
		}
		if (currentState != EState.Idle)
		{
			return;
		}
		if (base.Npc.Inventory.GetIdenticalItemAmount(itemToRetrieveTemplate) > 0 && grabbedAmount > 0)
		{
			if (IsAtDestination())
			{
				PlaceItem();
			}
			else
			{
				WalkToDestination();
			}
		}
		else if (skipPickup)
		{
			TakeItem();
			skipPickup = false;
		}
		else if (IsAtSource())
		{
			GrabItem();
		}
		else
		{
			WalkToSource();
		}
	}

	public void WalkToSource()
	{
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour.WalkToSource");
		}
		if (!base.Npc.Movement.CanGetTo(GetSourceAccessPoint(assignedRoute).position))
		{
			Console.LogWarning("MoveItemBehaviour.WalkToSource: Can't get to source");
			Disable_Networked(null);
		}
		else
		{
			currentState = EState.WalkingToSource;
			walkToSourceRoutine = StartCoroutine(Routine());
		}
		IEnumerator Routine()
		{
			SetDestination(GetSourceAccessPoint(assignedRoute).position);
			yield return new WaitForSeconds(0.5f);
			yield return new WaitUntil(() => !base.Npc.Movement.IsMoving);
			currentState = EState.Idle;
			walkToSourceRoutine = null;
		}
	}

	public void GrabItem()
	{
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour.GrabItem");
		}
		currentState = EState.Grabbing;
		grabRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			Transform sourceAccessPoint = GetSourceAccessPoint(assignedRoute);
			if (sourceAccessPoint == null)
			{
				Console.LogWarning("Could not find source access point!");
				grabRoutine = null;
				Disable_Networked(null);
			}
			else
			{
				base.Npc.Movement.FaceDirection(sourceAccessPoint.forward);
				base.Npc.SetAnimationTrigger_Networked(null, "GrabItem");
				float seconds = 0.5f;
				yield return new WaitForSeconds(seconds);
				if (!IsTransitRouteValid(assignedRoute, itemToRetrieveTemplate, out var invalidReason))
				{
					Console.LogWarning("Transit route no longer valid! Reason: " + invalidReason);
					grabRoutine = null;
					Disable_Networked(null);
				}
				else
				{
					TakeItem();
					yield return new WaitForSeconds(0.5f);
					grabRoutine = null;
					currentState = EState.Idle;
				}
			}
		}
	}

	private void TakeItem()
	{
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour.TakeItem");
		}
		int amountToGrab = GetAmountToGrab();
		if (amountToGrab == 0)
		{
			Console.LogWarning("Amount to grab is 0!");
			return;
		}
		ItemSlot firstSlotContainingTemplateItem = assignedRoute.Source.GetFirstSlotContainingTemplateItem(itemToRetrieveTemplate, ITransitEntity.ESlotType.Output);
		ItemInstance copy = (firstSlotContainingTemplateItem?.ItemInstance).GetCopy(amountToGrab);
		grabbedAmount = amountToGrab;
		firstSlotContainingTemplateItem.ChangeQuantity(-amountToGrab);
		base.Npc.Inventory.InsertItem(copy);
		assignedRoute.Destination.ReserveInputSlotsForItem(copy, base.Npc.NetworkObject);
	}

	public void WalkToDestination()
	{
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour.WalkToDestination");
		}
		if (!base.Npc.Movement.CanGetTo(GetDestinationAccessPoint(assignedRoute).position))
		{
			Console.LogWarning("MoveItemBehaviour.WalkToDestination: Can't get to destination");
			Disable_Networked(null);
		}
		else
		{
			currentState = EState.WalkingToDestination;
			walkToDestinationRoutine = StartCoroutine(Routine());
		}
		IEnumerator Routine()
		{
			SetDestination(GetDestinationAccessPoint(assignedRoute).position);
			yield return new WaitForSeconds(0.5f);
			yield return new WaitUntil(() => !base.Npc.Movement.IsMoving);
			currentState = EState.Idle;
			walkToDestinationRoutine = null;
		}
	}

	public void PlaceItem()
	{
		if (base.beh.DEBUG_MODE)
		{
			Console.Log("MoveItemBehaviour.PlaceItem");
		}
		currentState = EState.Placing;
		placingRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			if (GetDestinationAccessPoint(assignedRoute) != null)
			{
				base.Npc.Movement.FaceDirection(GetDestinationAccessPoint(assignedRoute).forward);
			}
			base.Npc.SetAnimationTrigger_Networked(null, "GrabItem");
			float seconds = 0.5f;
			yield return new WaitForSeconds(seconds);
			assignedRoute.Destination.RemoveSlotLocks(base.Npc.NetworkObject);
			ItemInstance firstIdenticalItem = base.Npc.Inventory.GetFirstIdenticalItem(itemToRetrieveTemplate);
			if (firstIdenticalItem != null && grabbedAmount > 0)
			{
				ItemInstance copy = firstIdenticalItem.GetCopy(grabbedAmount);
				if (assignedRoute.Destination.GetInputCapacityForItem(copy, base.Npc) >= grabbedAmount)
				{
					assignedRoute.Destination.InsertItemIntoInput(copy, base.Npc);
				}
				else
				{
					Console.LogWarning("Destination does not have enough capacity for item! Attempting to return item to source.");
					if (assignedRoute.Source.GetOutputCapacityForItem(copy, base.Npc) >= grabbedAmount)
					{
						assignedRoute.Source.InsertItemIntoOutput(copy, base.Npc);
					}
					else
					{
						Console.LogWarning("Source does not have enough capacity for item! Item will be lost.");
					}
				}
				firstIdenticalItem.ChangeQuantity(-grabbedAmount);
			}
			else
			{
				Console.LogWarning("Could not find carried item to place!");
			}
			yield return new WaitForSeconds(0.5f);
			placingRoutine = null;
			currentState = EState.Idle;
			Disable_Networked(null);
		}
	}

	private int GetAmountToGrab()
	{
		ItemInstance itemInstance = assignedRoute.Source.GetFirstSlotContainingTemplateItem(itemToRetrieveTemplate, ITransitEntity.ESlotType.Output)?.ItemInstance;
		if (itemInstance == null)
		{
			return 0;
		}
		int num = itemInstance.Quantity;
		if (maxMoveAmount > 0)
		{
			num = Mathf.Min(maxMoveAmount, num);
		}
		int inputCapacityForItem = assignedRoute.Destination.GetInputCapacityForItem(itemInstance, base.Npc);
		return Mathf.Min(num, inputCapacityForItem);
	}

	private void StopCurrentActivity()
	{
		switch (currentState)
		{
		case EState.WalkingToSource:
			if (walkToSourceRoutine != null)
			{
				StopCoroutine(walkToSourceRoutine);
			}
			break;
		case EState.Grabbing:
			if (grabRoutine != null)
			{
				StopCoroutine(grabRoutine);
			}
			break;
		case EState.WalkingToDestination:
			if (walkToDestinationRoutine != null)
			{
				StopCoroutine(walkToDestinationRoutine);
			}
			break;
		case EState.Placing:
			if (placingRoutine != null)
			{
				StopCoroutine(placingRoutine);
			}
			break;
		}
		currentState = EState.Idle;
	}

	public bool IsTransitRouteValid(TransitRoute route, string itemID, out string invalidReason)
	{
		invalidReason = string.Empty;
		if (route == null)
		{
			invalidReason = "Route is null!";
			return false;
		}
		if (!route.AreEntitiesNonNull())
		{
			invalidReason = "Entities are null!";
			return false;
		}
		ItemInstance itemInstance = route.Source.GetFirstSlotContainingItem(itemID, ITransitEntity.ESlotType.Output)?.ItemInstance;
		if (itemInstance == null || itemInstance.Quantity <= 0)
		{
			invalidReason = "Item is null or quantity is 0!";
			return false;
		}
		if (!IsDestinationValid(route, itemInstance))
		{
			invalidReason = "Can't access source, destination or destination is full!";
			return false;
		}
		return true;
	}

	public bool IsTransitRouteValid(TransitRoute route, ItemInstance templateItem, out string invalidReason)
	{
		invalidReason = string.Empty;
		if (route == null)
		{
			invalidReason = "Route is null!";
			return false;
		}
		if (!route.AreEntitiesNonNull())
		{
			invalidReason = "Entities are null!";
			return false;
		}
		ItemInstance itemInstance = route.Source.GetFirstSlotContainingTemplateItem(templateItem, ITransitEntity.ESlotType.Output)?.ItemInstance;
		if (itemInstance == null || itemInstance.Quantity <= 0)
		{
			invalidReason = "Item is null or quantity is 0!";
			return false;
		}
		if (!IsDestinationValid(route, itemInstance))
		{
			invalidReason = "Can't access source, destination or destination is full!";
			return false;
		}
		return true;
	}

	public bool IsTransitRouteValid(TransitRoute route, string itemID)
	{
		string invalidReason;
		return IsTransitRouteValid(route, itemID, out invalidReason);
	}

	public bool IsDestinationValid(TransitRoute route, ItemInstance item)
	{
		if (route.Destination.GetInputCapacityForItem(item, base.Npc) == 0)
		{
			Console.LogWarning("Destination has no capacity for item!");
			return false;
		}
		if (!CanGetToDestination(route))
		{
			Console.LogWarning("Cannot get to destination!");
			return false;
		}
		if (!CanGetToSource(route))
		{
			Console.LogWarning("Cannot get to source!");
			return false;
		}
		return true;
	}

	public bool CanGetToSource(TransitRoute route)
	{
		return GetSourceAccessPoint(route) != null;
	}

	private Transform GetSourceAccessPoint(TransitRoute route)
	{
		return NavMeshUtility.GetAccessPoint(route.Source, base.Npc);
	}

	private bool IsAtSource()
	{
		return NavMeshUtility.IsAtTransitEntity(assignedRoute.Source, base.Npc);
	}

	public bool CanGetToDestination(TransitRoute route)
	{
		return GetDestinationAccessPoint(route) != null;
	}

	private Transform GetDestinationAccessPoint(TransitRoute route)
	{
		if (route.Destination == null)
		{
			Console.LogWarning("Destination is null!");
			return null;
		}
		return NavMeshUtility.GetAccessPoint(route.Destination, base.Npc);
	}

	private bool IsAtDestination()
	{
		if (base.beh.DEBUG_MODE)
		{
			ITransitEntity destination = assignedRoute.Destination;
			Console.Log("Destination: " + destination.Name);
			Transform[] accessPoints = destination.AccessPoints;
			foreach (Transform transform in accessPoints)
			{
				Debug.DrawLine(base.Npc.transform.position, transform.position, Color.red, 0.1f);
			}
		}
		return NavMeshUtility.IsAtTransitEntity(assignedRoute.Destination, base.Npc);
	}

	public MoveItemData GetSaveData()
	{
		if (!base.Active || grabbedAmount == 0)
		{
			return null;
		}
		string templateItemJson = string.Empty;
		if (itemToRetrieveTemplate != null)
		{
			templateItemJson = itemToRetrieveTemplate.GetItemData().GetJson(prettyPrint: false);
		}
		return new MoveItemData(templateItemJson, grabbedAmount, (assignedRoute.Source as IGUIDRegisterable).GUID, (assignedRoute.Destination as IGUIDRegisterable).GUID);
	}

	public void Load(MoveItemData moveItemData)
	{
		if (moveItemData == null || moveItemData.GrabbedItemQuantity == 0 || string.IsNullOrEmpty(moveItemData.TemplateItemJSON))
		{
			return;
		}
		ITransitEntity transitEntity = GUIDManager.GetObject<ITransitEntity>(new Guid(moveItemData.SourceGUID));
		ITransitEntity transitEntity2 = GUIDManager.GetObject<ITransitEntity>(new Guid(moveItemData.DestinationGUID));
		if (transitEntity == null)
		{
			Console.LogWarning("Failed to load source transit entity");
			return;
		}
		if (transitEntity2 == null)
		{
			Console.LogWarning("Failed to load destination transit entity");
			return;
		}
		TransitRoute route = new TransitRoute(transitEntity, transitEntity2);
		grabbedAmount = moveItemData.GrabbedItemQuantity;
		Debug.Log("Resuming move item behaviour");
		ItemInstance itemInstance = ItemDeserializer.LoadItem(moveItemData.TemplateItemJSON);
		if (itemInstance != null)
		{
			Resume(route, itemInstance);
			Enable_Networked(null);
		}
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EMoveItemBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
