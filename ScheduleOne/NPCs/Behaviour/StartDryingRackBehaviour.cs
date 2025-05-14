using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using UnityEngine;

namespace ScheduleOne.NPCs.Behaviour;

public class StartDryingRackBehaviour : Behaviour
{
	public const float TIME_PER_ITEM = 1f;

	private Coroutine workRoutine;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted;

	public DryingRack Rack { get; protected set; }

	public bool WorkInProgress { get; protected set; }

	protected override void Begin()
	{
		base.Begin();
		StartWork();
	}

	protected override void Resume()
	{
		base.Resume();
		StartWork();
	}

	protected override void Pause()
	{
		base.Pause();
		if (WorkInProgress)
		{
			StopCauldron();
		}
		if (InstanceFinder.IsServer && Rack != null && Rack.NPCUserObject == base.Npc.NetworkObject)
		{
			Rack.SetNPCUser(null);
		}
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
		if (WorkInProgress)
		{
			StopCauldron();
		}
		if (InstanceFinder.IsServer && Rack != null && Rack.NPCUserObject == base.Npc.NetworkObject)
		{
			Rack.SetNPCUser(null);
		}
	}

	public override void ActiveMinPass()
	{
		base.ActiveMinPass();
		if (!InstanceFinder.IsServer || WorkInProgress)
		{
			return;
		}
		if (IsRackReady(Rack))
		{
			if (!base.Npc.Movement.IsMoving)
			{
				if (IsAtStation())
				{
					BeginAction();
				}
				else
				{
					GoToStation();
				}
			}
		}
		else
		{
			Disable_Networked(null);
		}
	}

	private void StartWork()
	{
		if (InstanceFinder.IsServer)
		{
			if (!IsRackReady(Rack))
			{
				Console.LogWarning(base.Npc.fullName + " has no station to work with");
				Disable_Networked(null);
			}
			else
			{
				Rack.SetNPCUser(base.Npc.NetworkObject);
			}
		}
	}

	public void AssignRack(DryingRack rack)
	{
		if (!(Rack == rack))
		{
			if (Rack != null && Rack.NPCUserObject == base.Npc.NetworkObject)
			{
				Rack.SetNPCUser(null);
			}
			Rack = rack;
		}
	}

	public bool IsAtStation()
	{
		return base.Npc.Movement.IsAsCloseAsPossible(NavMeshUtility.GetAccessPoint(Rack, base.Npc).position);
	}

	public void GoToStation()
	{
		base.Npc.Movement.SetDestination(NavMeshUtility.GetAccessPoint(Rack, base.Npc).position);
	}

	[ObserversRpc(RunLocally = true)]
	public void BeginAction()
	{
		RpcWriter___Observers_BeginAction_2166136261();
		RpcLogic___BeginAction_2166136261();
	}

	private void StopCauldron()
	{
		if (workRoutine != null)
		{
			StopCoroutine(workRoutine);
		}
		WorkInProgress = false;
	}

	public bool IsRackReady(DryingRack rack)
	{
		if (rack == null)
		{
			return false;
		}
		if (((IUsable)rack).IsInUse && (rack.PlayerUserObject != null || rack.NPCUserObject != base.Npc.NetworkObject))
		{
			return false;
		}
		if (rack.InputSlot.Quantity <= 0)
		{
			return false;
		}
		if (rack.GetTotalDryingItems() >= rack.ItemCapacity)
		{
			return false;
		}
		if (!base.Npc.Movement.CanGetTo(rack.transform.position))
		{
			return false;
		}
		return true;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterObserversRpc(15u, RpcReader___Observers_BeginAction_2166136261);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EStartDryingRackBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Observers_BeginAction_2166136261()
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
			SendObserversRpc(15u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___BeginAction_2166136261()
	{
		if (!WorkInProgress && !(Rack == null))
		{
			WorkInProgress = true;
			base.Npc.Movement.FacePoint(Rack.uiPoint.position);
			workRoutine = StartCoroutine(Package());
		}
		IEnumerator Package()
		{
			yield return new WaitForEndOfFrame();
			Rack.InputSlot.ItemInstance.GetCopy(1);
			int itemCount = 0;
			while (Rack != null && Rack.InputSlot.Quantity > itemCount && Rack.GetTotalDryingItems() + itemCount < Rack.ItemCapacity)
			{
				base.Npc.Avatar.Anim.SetTrigger("GrabItem");
				yield return new WaitForSeconds(1f);
				itemCount++;
			}
			if (InstanceFinder.IsServer)
			{
				Rack.StartOperation();
			}
			WorkInProgress = false;
			workRoutine = null;
		}
	}

	private void RpcReader___Observers_BeginAction_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___BeginAction_2166136261();
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
