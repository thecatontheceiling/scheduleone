using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.GameTime;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Product;
using ScheduleOne.Storage;
using ScheduleOne.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Property;

public class RV : Property
{
	public Transform ModelContainer;

	public Transform FXContainer;

	public UnityEvent onSetExploded;

	private bool NetworkInitialize___EarlyScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted;

	public bool _isExploded { get; private set; }

	protected override void Start()
	{
		base.Start();
		InvokeRepeating("UpdateVariables", 0f, 0.5f);
		NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance._onSleepStart.AddListener(OnSleep);
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		_ = _isExploded;
	}

	private void UpdateVariables()
	{
		if (!InstanceFinder.IsServer || _isExploded)
		{
			return;
		}
		Pot[] array = (from x in BuildableItems
			where x is Pot
			select x as Pot).ToArray();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int num5 = 0; num5 < array.Length; num5++)
		{
			if (array[num5].IsFilledWithSoil)
			{
				num++;
			}
			if (array[num5].NormalizedWaterLevel > 0.9f)
			{
				num2++;
			}
			if (array[num5].Plant != null)
			{
				num3++;
			}
			if ((bool)array[num5].AppliedAdditives.Find((Additive x) => x.AdditiveName == "Speed Grow"))
			{
				num4++;
			}
		}
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("RV_Soil_Pots", num.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("RV_Watered_Pots", num2.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("RV_Seed_Pots", num3.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("RV_SpeedGrow_Pots", num4.ToString());
	}

	public void Ransack()
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		Debug.Log("Ransacking RV");
		foreach (BuildableItem buildableItem in BuildableItems)
		{
			IItemSlotOwner itemSlotOwner = null;
			if (buildableItem is IItemSlotOwner)
			{
				itemSlotOwner = buildableItem as IItemSlotOwner;
			}
			else
			{
				StorageEntity component = buildableItem.GetComponent<StorageEntity>();
				if (component != null)
				{
					itemSlotOwner = component;
				}
			}
			if (itemSlotOwner == null)
			{
				continue;
			}
			for (int i = 0; i < itemSlotOwner.ItemSlots.Count; i++)
			{
				if (itemSlotOwner.ItemSlots[i].ItemInstance != null && itemSlotOwner.ItemSlots[i].ItemInstance is ProductItemInstance)
				{
					itemSlotOwner.ItemSlots[i].SetQuantity(0);
				}
			}
		}
	}

	public override bool ShouldSave()
	{
		if (_isExploded)
		{
			return false;
		}
		return base.ShouldSave();
	}

	[TargetRpc]
	public void SetExploded(NetworkConnection conn)
	{
		RpcWriter___Target_SetExploded_328543758(conn);
	}

	public void SetExploded()
	{
		_isExploded = true;
		if (onSetExploded != null)
		{
			onSetExploded.Invoke();
		}
	}

	private void OnSleep()
	{
		if (FXContainer != null)
		{
			FXContainer.gameObject.SetActive(value: false);
		}
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterTargetRpc(5u, RpcReader___Target_SetExploded_328543758);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EProperty_002ERVAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Target_SetExploded_328543758(NetworkConnection conn)
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
			SendTargetRpc(5u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetExploded_328543758(NetworkConnection conn)
	{
		SetExploded();
	}

	private void RpcReader___Target_SetExploded_328543758(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized)
		{
			RpcLogic___SetExploded_328543758(base.LocalConnection);
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
