using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.Employees;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using UnityEngine;

namespace ScheduleOne.NPCs.Behaviour;

public class PackagingStationBehaviour : Behaviour
{
	public const float BASE_PACKAGING_TIME = 5f;

	private Coroutine packagingRoutine;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted;

	public PackagingStation Station { get; protected set; }

	public bool PackagingInProgress { get; protected set; }

	protected override void Begin()
	{
		base.Begin();
		StartPackaging();
	}

	protected override void Resume()
	{
		base.Resume();
		StartPackaging();
	}

	protected override void Pause()
	{
		base.Pause();
		if (PackagingInProgress)
		{
			StopPackaging();
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
		if (PackagingInProgress)
		{
			StopPackaging();
		}
		if (InstanceFinder.IsServer && Station != null && Station.NPCUserObject == base.Npc.NetworkObject)
		{
			Station.SetNPCUser(null);
		}
	}

	public override void ActiveMinPass()
	{
		base.ActiveMinPass();
		if (!InstanceFinder.IsServer || PackagingInProgress)
		{
			return;
		}
		if (IsStationReady(Station))
		{
			if (!base.Npc.Movement.IsMoving)
			{
				if (IsAtStation())
				{
					BeginPackaging();
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

	private void StartPackaging()
	{
		if (InstanceFinder.IsServer)
		{
			if (!IsStationReady(Station))
			{
				Console.LogWarning(base.Npc.fullName + " has no station to work with");
				Disable_Networked(null);
			}
			else
			{
				Station.SetNPCUser(base.Npc.NetworkObject);
			}
		}
	}

	public void AssignStation(PackagingStation station)
	{
		Station = station;
	}

	public bool IsAtStation()
	{
		return base.Npc.Movement.IsAsCloseAsPossible(Station.StandPoint.position);
	}

	public void GoToStation()
	{
		base.Npc.Movement.SetDestination(Station.StandPoint.position);
	}

	[ObserversRpc(RunLocally = true)]
	public void BeginPackaging()
	{
		RpcWriter___Observers_BeginPackaging_2166136261();
		RpcLogic___BeginPackaging_2166136261();
	}

	private void StopPackaging()
	{
		if (packagingRoutine != null)
		{
			StopCoroutine(packagingRoutine);
		}
		base.Npc.Avatar.Anim.SetBool("UsePackagingStation", value: false);
		if (InstanceFinder.IsServer && Station != null && Station.NPCUserObject == base.Npc.NetworkObject)
		{
			Station.SetNPCUser(null);
		}
		PackagingInProgress = false;
	}

	public bool IsStationReady(PackagingStation station)
	{
		if (station == null)
		{
			return false;
		}
		if (station.GetState(PackagingStation.EMode.Package) != PackagingStation.EState.CanBegin)
		{
			return false;
		}
		if (((IUsable)station).IsInUse && station.NPCUserObject != base.Npc.NetworkObject)
		{
			return false;
		}
		if (!base.Npc.Movement.CanGetTo(station.StandPoint.position))
		{
			return false;
		}
		return true;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterObserversRpc(15u, RpcReader___Observers_BeginPackaging_2166136261);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EPackagingStationBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Observers_BeginPackaging_2166136261()
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

	public void RpcLogic___BeginPackaging_2166136261()
	{
		if (!PackagingInProgress && !(Station == null))
		{
			PackagingInProgress = true;
			base.Npc.Movement.FaceDirection(Station.StandPoint.forward);
			packagingRoutine = StartCoroutine(Package());
		}
		IEnumerator Package()
		{
			yield return new WaitForEndOfFrame();
			base.Npc.Avatar.Anim.SetBool("UsePackagingStation", value: true);
			float packageTime = 5f / (base.Npc as Packager).PackagingSpeedMultiplier * Station.PackagerEmployeeSpeedMultiplier;
			for (float i = 0f; i < packageTime; i += Time.deltaTime)
			{
				base.Npc.Avatar.LookController.OverrideLookTarget(Station.Container.position, 0);
				yield return new WaitForEndOfFrame();
			}
			base.Npc.Avatar.Anim.SetBool("UsePackagingStation", value: false);
			if (InstanceFinder.IsServer)
			{
				Station.PackSingleInstance();
			}
			Console.Log("Packaging done!");
			PackagingInProgress = false;
			packagingRoutine = null;
		}
	}

	private void RpcReader___Observers_BeginPackaging_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___BeginPackaging_2166136261();
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
