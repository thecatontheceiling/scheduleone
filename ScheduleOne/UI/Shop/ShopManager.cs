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

namespace ScheduleOne.UI.Shop;

public class ShopManager : NetworkSingleton<ShopManager>, IBaseSaveable, ISaveable
{
	private ShopManagerLoader loader = new ShopManagerLoader();

	private bool NetworkInitialize___EarlyScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted;

	public string SaveFolderName => "Shops";

	public string SaveFileName => "Shops";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; } = true;

	protected override void Start()
	{
		base.Start();
		InitializeSaveable();
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	public virtual string GetSaveString()
	{
		List<ShopData> list = new List<ShopData>();
		for (int i = 0; i < ShopInterface.AllShops.Count; i++)
		{
			if (!(ShopInterface.AllShops[i] == null) && ShopInterface.AllShops[i].ShouldSave())
			{
				list.Add(ShopInterface.AllShops[i].GetSaveData());
			}
		}
		return new ShopManagerData(list.ToArray()).GetJson();
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendStock(string shopCode, string itemID, int stock)
	{
		RpcWriter___Server_SendStock_15643032(shopCode, itemID, stock);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetStock(NetworkConnection conn, string shopCode, string itemID, int stock)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetStock_3509965635(conn, shopCode, itemID, stock);
			RpcLogic___SetStock_3509965635(conn, shopCode, itemID, stock);
		}
		else
		{
			RpcWriter___Target_SetStock_3509965635(conn, shopCode, itemID, stock);
		}
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(0u, RpcReader___Server_SendStock_15643032);
			RegisterObserversRpc(1u, RpcReader___Observers_SetStock_3509965635);
			RegisterTargetRpc(2u, RpcReader___Target_SetStock_3509965635);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EUI_002EShop_002EShopManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendStock_15643032(string shopCode, string itemID, int stock)
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
			writer.WriteString(shopCode);
			writer.WriteString(itemID);
			writer.WriteInt32(stock);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendStock_15643032(string shopCode, string itemID, int stock)
	{
		SetStock(null, shopCode, itemID, stock);
	}

	private void RpcReader___Server_SendStock_15643032(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string shopCode = PooledReader0.ReadString();
		string itemID = PooledReader0.ReadString();
		int stock = PooledReader0.ReadInt32();
		if (base.IsServerInitialized)
		{
			RpcLogic___SendStock_15643032(shopCode, itemID, stock);
		}
	}

	private void RpcWriter___Observers_SetStock_3509965635(NetworkConnection conn, string shopCode, string itemID, int stock)
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
			writer.WriteString(shopCode);
			writer.WriteString(itemID);
			writer.WriteInt32(stock);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetStock_3509965635(NetworkConnection conn, string shopCode, string itemID, int stock)
	{
		ShopInterface shopInterface = ShopInterface.AllShops.Find((ShopInterface x) => x.ShopCode == shopCode);
		if (shopInterface == null)
		{
			Debug.LogError("Failed to set stock: Shop not found: " + shopCode);
			return;
		}
		ShopListing listing = shopInterface.GetListing(itemID);
		if (listing == null)
		{
			Debug.LogError("Failed to set stock: Listing not found: " + itemID);
		}
		else
		{
			listing.SetStock(stock, network: false);
		}
	}

	private void RpcReader___Observers_SetStock_3509965635(PooledReader PooledReader0, Channel channel)
	{
		string shopCode = PooledReader0.ReadString();
		string itemID = PooledReader0.ReadString();
		int stock = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetStock_3509965635(null, shopCode, itemID, stock);
		}
	}

	private void RpcWriter___Target_SetStock_3509965635(NetworkConnection conn, string shopCode, string itemID, int stock)
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
			writer.WriteString(shopCode);
			writer.WriteString(itemID);
			writer.WriteInt32(stock);
			SendTargetRpc(2u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetStock_3509965635(PooledReader PooledReader0, Channel channel)
	{
		string shopCode = PooledReader0.ReadString();
		string itemID = PooledReader0.ReadString();
		int stock = PooledReader0.ReadInt32();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetStock_3509965635(base.LocalConnection, shopCode, itemID, stock);
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
