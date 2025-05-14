using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.Property;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Delivery;

public class DeliveryManager : NetworkSingleton<DeliveryManager>, IBaseSaveable, ISaveable
{
	public List<DeliveryInstance> Deliveries = new List<DeliveryInstance>();

	public UnityEvent<DeliveryInstance> onDeliveryCreated;

	public UnityEvent<DeliveryInstance> onDeliveryCompleted;

	private DeliveriesLoader loader = new DeliveriesLoader();

	private List<string> writtenVehicles = new List<string>();

	private Dictionary<DeliveryInstance, int> minsSinceVehicleEmpty = new Dictionary<DeliveryInstance, int>();

	private bool NetworkInitialize___EarlyScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted;

	public string SaveFolderName => "Deliveries";

	public string SaveFileName => "Deliveries";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; }

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EDelivery_002EDeliveryManager_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Start()
	{
		base.Start();
		TimeManager timeManager = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		timeManager.onMinutePass = (Action)Delegate.Combine(timeManager.onMinutePass, new Action(OnMinPass));
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		foreach (DeliveryInstance delivery in Deliveries)
		{
			SendDelivery(delivery);
		}
	}

	private void OnMinPass()
	{
		if (Singleton<LoadManager>.Instance.IsLoading)
		{
			return;
		}
		DeliveryInstance[] array = Deliveries.ToArray();
		foreach (DeliveryInstance deliveryInstance in array)
		{
			deliveryInstance.OnMinPass();
			if (!InstanceFinder.IsServer)
			{
				continue;
			}
			if (deliveryInstance.TimeUntilArrival == 0 && deliveryInstance.Status != EDeliveryStatus.Arrived)
			{
				if (IsLoadingBayFree(deliveryInstance.Destination, deliveryInstance.LoadingDockIndex))
				{
					deliveryInstance.AddItemsToDeliveryVehicle();
					SetDeliveryState(deliveryInstance.DeliveryID, EDeliveryStatus.Arrived);
				}
				else if (deliveryInstance.Status != EDeliveryStatus.Waiting)
				{
					SetDeliveryState(deliveryInstance.DeliveryID, EDeliveryStatus.Waiting);
				}
			}
			if (deliveryInstance.Status != EDeliveryStatus.Arrived)
			{
				continue;
			}
			if (!minsSinceVehicleEmpty.ContainsKey(deliveryInstance))
			{
				minsSinceVehicleEmpty.Add(deliveryInstance, 0);
			}
			if (deliveryInstance.ActiveVehicle.Vehicle.Storage.ItemCount == 0 && deliveryInstance.ActiveVehicle.Vehicle.Storage.CurrentAccessor == null)
			{
				minsSinceVehicleEmpty[deliveryInstance]++;
				if (minsSinceVehicleEmpty[deliveryInstance] >= 3)
				{
					SetDeliveryState(deliveryInstance.DeliveryID, EDeliveryStatus.Completed);
				}
			}
			else
			{
				minsSinceVehicleEmpty[deliveryInstance] = 0;
			}
		}
	}

	public bool IsLoadingBayFree(ScheduleOne.Property.Property destination, int loadingDockIndex)
	{
		return !destination.LoadingDocks[loadingDockIndex].IsInUse;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendDelivery(DeliveryInstance delivery)
	{
		RpcWriter___Server_SendDelivery_2813439055(delivery);
	}

	[ObserversRpc]
	[TargetRpc]
	private void ReceiveDelivery(NetworkConnection conn, DeliveryInstance delivery)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_ReceiveDelivery_2795369214(conn, delivery);
		}
		else
		{
			RpcWriter___Target_ReceiveDelivery_2795369214(conn, delivery);
		}
	}

	[ObserversRpc(RunLocally = true)]
	private void SetDeliveryState(string deliveryID, EDeliveryStatus status)
	{
		RpcWriter___Observers_SetDeliveryState_316609003(deliveryID, status);
		RpcLogic___SetDeliveryState_316609003(deliveryID, status);
	}

	private DeliveryInstance GetDelivery(string deliveryID)
	{
		return Deliveries.FirstOrDefault((DeliveryInstance d) => d.DeliveryID == deliveryID);
	}

	public DeliveryInstance GetDelivery(ScheduleOne.Property.Property destination)
	{
		return Deliveries.FirstOrDefault((DeliveryInstance d) => d.DestinationCode == destination.PropertyCode);
	}

	public DeliveryInstance GetActiveShopDelivery(DeliveryShop shop)
	{
		return Deliveries.FirstOrDefault((DeliveryInstance d) => d.StoreName == shop.MatchingShopInterfaceName);
	}

	public ShopInterface GetShopInterface(string shopName)
	{
		return ShopInterface.AllShops.Find((ShopInterface x) => x.ShopName == shopName);
	}

	public virtual string GetSaveString()
	{
		List<VehicleData> list = new List<VehicleData>();
		foreach (DeliveryInstance delivery in Deliveries)
		{
			if (!(delivery.ActiveVehicle == null))
			{
				list.Add(delivery.ActiveVehicle.Vehicle.GetVehicleData());
			}
		}
		return new DeliveriesData(Deliveries.ToArray(), list.ToArray()).GetJson();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(0u, RpcReader___Server_SendDelivery_2813439055);
			RegisterObserversRpc(1u, RpcReader___Observers_ReceiveDelivery_2795369214);
			RegisterTargetRpc(2u, RpcReader___Target_ReceiveDelivery_2795369214);
			RegisterObserversRpc(3u, RpcReader___Observers_SetDeliveryState_316609003);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EDelivery_002EDeliveryManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendDelivery_2813439055(DeliveryInstance delivery)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerated(writer, delivery);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendDelivery_2813439055(DeliveryInstance delivery)
	{
		ReceiveDelivery(null, delivery);
	}

	private void RpcReader___Server_SendDelivery_2813439055(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		DeliveryInstance delivery = GeneratedReaders___Internal.Read___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized)
		{
			RpcLogic___SendDelivery_2813439055(delivery);
		}
	}

	private void RpcWriter___Observers_ReceiveDelivery_2795369214(NetworkConnection conn, DeliveryInstance delivery)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerated(writer, delivery);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___ReceiveDelivery_2795369214(NetworkConnection conn, DeliveryInstance delivery)
	{
		if (GetDelivery(delivery.DeliveryID) == null)
		{
			Deliveries.Add(delivery);
			delivery.SetStatus(delivery.Status);
			if (onDeliveryCreated != null)
			{
				onDeliveryCreated.Invoke(delivery);
			}
			HasChanged = true;
		}
	}

	private void RpcReader___Observers_ReceiveDelivery_2795369214(PooledReader PooledReader0, Channel channel)
	{
		DeliveryInstance delivery = GeneratedReaders___Internal.Read___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceiveDelivery_2795369214(null, delivery);
		}
	}

	private void RpcWriter___Target_ReceiveDelivery_2795369214(NetworkConnection conn, DeliveryInstance delivery)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerated(writer, delivery);
			SendTargetRpc(2u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_ReceiveDelivery_2795369214(PooledReader PooledReader0, Channel channel)
	{
		DeliveryInstance delivery = GeneratedReaders___Internal.Read___ScheduleOne_002EDelivery_002EDeliveryInstanceFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceiveDelivery_2795369214(base.LocalConnection, delivery);
		}
	}

	private void RpcWriter___Observers_SetDeliveryState_316609003(string deliveryID, EDeliveryStatus status)
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
			writer.WriteString(deliveryID);
			GeneratedWriters___Internal.Write___ScheduleOne_002EDelivery_002EEDeliveryStatusFishNet_002ESerializing_002EGenerated(writer, status);
			SendObserversRpc(3u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetDeliveryState_316609003(string deliveryID, EDeliveryStatus status)
	{
		DeliveryInstance delivery = GetDelivery(deliveryID);
		delivery?.SetStatus(status);
		if (status == EDeliveryStatus.Completed)
		{
			if (onDeliveryCompleted != null)
			{
				onDeliveryCompleted.Invoke(delivery);
			}
			Deliveries.Remove(delivery);
		}
		HasChanged = true;
	}

	private void RpcReader___Observers_SetDeliveryState_316609003(PooledReader PooledReader0, Channel channel)
	{
		string deliveryID = PooledReader0.ReadString();
		EDeliveryStatus status = GeneratedReaders___Internal.Read___ScheduleOne_002EDelivery_002EEDeliveryStatusFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetDeliveryState_316609003(deliveryID, status);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EDelivery_002EDeliveryManager_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		InitializeSaveable();
	}
}
