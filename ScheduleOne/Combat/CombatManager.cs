using System.Collections.Generic;
using EasyButtons;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Combat;

public class CombatManager : NetworkSingleton<CombatManager>
{
	public LayerMask MeleeLayerMask;

	public LayerMask ExplosionLayerMask;

	public LayerMask RangedWeaponLayerMask;

	public Explosion ExplosionPrefab;

	private List<int> explosionIDs = new List<int>();

	private bool NetworkInitialize___EarlyScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted;

	[Button]
	public void CreateTestExplosion()
	{
		Vector3 origin = PlayerSingleton<PlayerCamera>.Instance.transform.position + PlayerSingleton<PlayerCamera>.Instance.transform.forward * 10f;
		if (PlayerSingleton<PlayerCamera>.Instance.LookRaycast(10f, out var hit, ExplosionLayerMask))
		{
			origin = hit.point;
		}
		CreateExplosion(origin, ExplosionData.DefaultSmall);
	}

	public void CreateExplosion(Vector3 origin, ExplosionData data)
	{
		int id = Random.Range(0, int.MaxValue);
		CreateExplosion(origin, data, id);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	private void CreateExplosion(Vector3 origin, ExplosionData data, int id)
	{
		RpcWriter___Server_CreateExplosion_2907189355(origin, data, id);
		RpcLogic___CreateExplosion_2907189355(origin, data, id);
	}

	[ObserversRpc(RunLocally = true)]
	private void Explosion(Vector3 origin, ExplosionData data, int id)
	{
		RpcWriter___Observers_Explosion_2907189355(origin, data, id);
		RpcLogic___Explosion_2907189355(origin, data, id);
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(0u, RpcReader___Server_CreateExplosion_2907189355);
			RegisterObserversRpc(1u, RpcReader___Observers_Explosion_2907189355);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECombat_002ECombatManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_CreateExplosion_2907189355(Vector3 origin, ExplosionData data, int id)
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
			writer.WriteVector3(origin);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECombat_002EExplosionDataFishNet_002ESerializing_002EGenerated(writer, data);
			writer.WriteInt32(id);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___CreateExplosion_2907189355(Vector3 origin, ExplosionData data, int id)
	{
		Explosion(origin, data, id);
	}

	private void RpcReader___Server_CreateExplosion_2907189355(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		Vector3 origin = PooledReader0.ReadVector3();
		ExplosionData data = GeneratedReaders___Internal.Read___ScheduleOne_002ECombat_002EExplosionDataFishNet_002ESerializing_002EGenerateds(PooledReader0);
		int id = PooledReader0.ReadInt32();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___CreateExplosion_2907189355(origin, data, id);
		}
	}

	private void RpcWriter___Observers_Explosion_2907189355(Vector3 origin, ExplosionData data, int id)
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
			writer.WriteVector3(origin);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECombat_002EExplosionDataFishNet_002ESerializing_002EGenerated(writer, data);
			writer.WriteInt32(id);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___Explosion_2907189355(Vector3 origin, ExplosionData data, int id)
	{
		if (!explosionIDs.Contains(id))
		{
			explosionIDs.Add(id);
			Explosion explosion = Object.Instantiate(ExplosionPrefab);
			explosion.Initialize(origin, data);
			Object.Destroy(explosion.gameObject, 3f);
		}
	}

	private void RpcReader___Observers_Explosion_2907189355(PooledReader PooledReader0, Channel channel)
	{
		Vector3 origin = PooledReader0.ReadVector3();
		ExplosionData data = GeneratedReaders___Internal.Read___ScheduleOne_002ECombat_002EExplosionDataFishNet_002ESerializing_002EGenerateds(PooledReader0);
		int id = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___Explosion_2907189355(origin, data, id);
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
