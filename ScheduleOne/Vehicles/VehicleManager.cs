using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using UnityEngine;

namespace ScheduleOne.Vehicles;

public class VehicleManager : NetworkSingleton<VehicleManager>, IBaseSaveable, ISaveable
{
	public List<LandVehicle> AllVehicles = new List<LandVehicle>();

	[Header("Vehicles")]
	public List<LandVehicle> VehiclePrefabs = new List<LandVehicle>();

	public List<LandVehicle> PlayerOwnedVehicles = new List<LandVehicle>();

	private VehiclesLoader loader = new VehiclesLoader();

	private bool NetworkInitialize___EarlyScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted;

	public string SaveFolderName => "OwnedVehicles";

	public string SaveFileName => "OwnedVehicles";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; } = true;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EVehicles_002EVehicleManager_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SpawnVehicle(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
	{
		RpcWriter___Server_SpawnVehicle_3323115898(vehicleCode, position, rotation, playerOwned);
	}

	public LandVehicle SpawnAndReturnVehicle(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
	{
		LandVehicle vehiclePrefab = GetVehiclePrefab(vehicleCode);
		if (vehiclePrefab == null)
		{
			Console.LogError("SpawnVehicle: '" + vehicleCode + "' is not a valid vehicle code!");
			return null;
		}
		LandVehicle component = UnityEngine.Object.Instantiate(vehiclePrefab.gameObject).GetComponent<LandVehicle>();
		component.transform.position = position;
		component.transform.rotation = rotation;
		base.NetworkObject.Spawn(component.gameObject);
		component.SetIsPlayerOwned(null, playerOwned);
		if (playerOwned)
		{
			PlayerOwnedVehicles.Add(component);
		}
		return component;
	}

	public LandVehicle GetVehiclePrefab(string vehicleCode)
	{
		return VehiclePrefabs.Find((LandVehicle x) => x.VehicleCode.ToLower() == vehicleCode.ToLower());
	}

	public LandVehicle SpawnAndLoadVehicle(VehicleData data, string path, bool playerOwned)
	{
		LandVehicle landVehicle = SpawnAndReturnVehicle(data.VehicleCode, data.Position, data.Rotation, playerOwned);
		landVehicle.Load(data, path);
		return landVehicle;
	}

	public void LoadVehicle(VehicleData data, string path)
	{
		LandVehicle landVehicle = GUIDManager.GetObject<LandVehicle>(new Guid(data.GUID));
		if (landVehicle == null)
		{
			Console.LogError("LoadVehicle: Vehicle not found with GUID " + data.GUID);
		}
		else
		{
			landVehicle.Load(data, path);
		}
	}

	public virtual string GetSaveString()
	{
		List<VehicleData> list = new List<VehicleData>();
		for (int i = 0; i < PlayerOwnedVehicles.Count; i++)
		{
			list.Add(PlayerOwnedVehicles[i].GetVehicleData());
		}
		return new VehicleCollectionData(list.ToArray()).GetJson();
	}

	public void SpawnLoanSharkVehicle(Vector3 position, Quaternion rot)
	{
		LandVehicle landVehicle = NetworkSingleton<VehicleManager>.Instance.SpawnAndReturnVehicle("shitbox", position, rot, playerOwned: true);
		EnableLoanSharkVisuals(landVehicle.NetworkObject);
	}

	[ObserversRpc(RunLocally = true)]
	private void EnableLoanSharkVisuals(NetworkObject veh)
	{
		RpcWriter___Observers_EnableLoanSharkVisuals_3323014238(veh);
		RpcLogic___EnableLoanSharkVisuals_3323014238(veh);
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(0u, RpcReader___Server_SpawnVehicle_3323115898);
			RegisterObserversRpc(1u, RpcReader___Observers_EnableLoanSharkVisuals_3323014238);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EVehicles_002EVehicleManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SpawnVehicle_3323115898(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
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
			writer.WriteString(vehicleCode);
			writer.WriteVector3(position);
			writer.WriteQuaternion(rotation);
			writer.WriteBoolean(playerOwned);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SpawnVehicle_3323115898(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
	{
		SpawnAndReturnVehicle(vehicleCode, position, rotation, playerOwned);
	}

	private void RpcReader___Server_SpawnVehicle_3323115898(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string vehicleCode = PooledReader0.ReadString();
		Vector3 position = PooledReader0.ReadVector3();
		Quaternion rotation = PooledReader0.ReadQuaternion();
		bool playerOwned = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized)
		{
			RpcLogic___SpawnVehicle_3323115898(vehicleCode, position, rotation, playerOwned);
		}
	}

	private void RpcWriter___Observers_EnableLoanSharkVisuals_3323014238(NetworkObject veh)
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
			writer.WriteNetworkObject(veh);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___EnableLoanSharkVisuals_3323014238(NetworkObject veh)
	{
		if (veh == null)
		{
			Console.LogWarning("Vehicle not found");
		}
		else
		{
			veh.GetComponent<LoanSharkCarVisuals>().Configure(enabled: true, noteVisible: true);
		}
	}

	private void RpcReader___Observers_EnableLoanSharkVisuals_3323014238(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject veh = PooledReader0.ReadNetworkObject();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___EnableLoanSharkVisuals_3323014238(veh);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EVehicles_002EVehicleManager_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		InitializeSaveable();
	}
}
