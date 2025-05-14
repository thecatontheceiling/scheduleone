using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.Building;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Property;
using UnityEngine;

namespace ScheduleOne.EntityFramework;

public class SurfaceItem : BuildableItem
{
	[Header("Settings")]
	public List<Surface.ESurfaceType> ValidSurfaceTypes = new List<Surface.ESurfaceType>
	{
		Surface.ESurfaceType.Wall,
		Surface.ESurfaceType.Roof
	};

	public bool AllowRotation = true;

	protected Vector3 RelativePosition = Vector3.zero;

	protected Quaternion RelativeRotation = Quaternion.identity;

	private bool NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted;

	public Surface ParentSurface { get; protected set; }

	public float RotationIncrement { get; } = 45f;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EEntityFramework_002ESurfaceItem_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (base.Initialized && base.LocallyBuilt)
		{
			StartCoroutine(WaitForDataSend());
		}
		IEnumerator WaitForDataSend()
		{
			yield return new WaitUntil(() => base.NetworkObject.IsSpawned);
			SendSurfaceItemData(base.ItemInstance, base.GUID.ToString(), ParentSurface.GUID.ToString(), RelativePosition, RelativeRotation);
		}
	}

	protected override void SendInitToClient(NetworkConnection conn)
	{
		InitializeSurfaceItem(conn, base.ItemInstance, base.GUID.ToString(), ParentSurface.GUID.ToString(), RelativePosition, RelativeRotation);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendSurfaceItemData(ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		RpcWriter___Server_SendSurfaceItemData_2652836379(instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
	}

	[TargetRpc]
	[ObserversRpc(RunLocally = true)]
	public virtual void InitializeSurfaceItem(NetworkConnection conn, ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_InitializeSurfaceItem_2932264618(conn, instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
			RpcLogic___InitializeSurfaceItem_2932264618(conn, instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
		}
		else
		{
			RpcWriter___Target_InitializeSurfaceItem_2932264618(conn, instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
		}
	}

	public virtual void InitializeSurfaceItem(ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		SetTransformData(parentSurfaceGUID, relativePosition, relativeRotation);
		if (ParentSurface == null)
		{
			DestroyItem(callOnServer: false);
			return;
		}
		ScheduleOne.Property.Property parentProperty = ParentSurface.ParentProperty;
		if (parentProperty == null)
		{
			Console.LogError("Failed to find parent property for " + base.gameObject.name);
		}
		else
		{
			base.InitializeBuildableItem(instance, GUID, parentProperty.PropertyCode);
		}
	}

	protected virtual void SetTransformData(string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		Surface surface = GUIDManager.GetObject<Surface>(new Guid(parentSurfaceGUID));
		if (surface == null)
		{
			Console.LogError("Failed to find parent surface for " + base.gameObject.name);
			return;
		}
		ParentSurface = surface;
		RelativePosition = relativePosition;
		RelativeRotation = relativeRotation;
		base.transform.position = surface.transform.TransformPoint(relativePosition);
		base.transform.rotation = surface.transform.rotation * relativeRotation;
		if (base.NetworkObject.IsSpawned)
		{
			base.transform.SetParent(ParentSurface.Container.transform);
		}
		else
		{
			StartCoroutine(Routine());
		}
		IEnumerator Routine()
		{
			yield return new WaitUntil(() => base.NetworkObject.IsSpawned);
			base.transform.SetParent(ParentSurface.Container.transform);
		}
	}

	protected override ScheduleOne.Property.Property GetProperty(Transform searchTransform = null)
	{
		return base.GetProperty(searchTransform);
	}

	public override string GetSaveString()
	{
		return new SurfaceItemData(base.GUID, base.ItemInstance, 25, ParentSurface.GUID.ToString(), RelativePosition, RelativeRotation).GetJson();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(5u, RpcReader___Server_SendSurfaceItemData_2652836379);
			RegisterTargetRpc(6u, RpcReader___Target_InitializeSurfaceItem_2932264618);
			RegisterObserversRpc(7u, RpcReader___Observers_InitializeSurfaceItem_2932264618);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEntityFramework_002ESurfaceItemAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendSurfaceItemData_2652836379(ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
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
			writer.WriteItemInstance(instance);
			writer.WriteString(GUID);
			writer.WriteString(parentSurfaceGUID);
			writer.WriteVector3(relativePosition);
			writer.WriteQuaternion(relativeRotation);
			SendServerRpc(5u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendSurfaceItemData_2652836379(ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		InitializeSurfaceItem(null, instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
	}

	private void RpcReader___Server_SendSurfaceItemData_2652836379(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		ItemInstance instance = PooledReader0.ReadItemInstance();
		string gUID = PooledReader0.ReadString();
		string parentSurfaceGUID = PooledReader0.ReadString();
		Vector3 relativePosition = PooledReader0.ReadVector3();
		Quaternion relativeRotation = PooledReader0.ReadQuaternion();
		if (base.IsServerInitialized)
		{
			RpcLogic___SendSurfaceItemData_2652836379(instance, gUID, parentSurfaceGUID, relativePosition, relativeRotation);
		}
	}

	private void RpcWriter___Target_InitializeSurfaceItem_2932264618(NetworkConnection conn, ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
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
			writer.WriteItemInstance(instance);
			writer.WriteString(GUID);
			writer.WriteString(parentSurfaceGUID);
			writer.WriteVector3(relativePosition);
			writer.WriteQuaternion(relativeRotation);
			SendTargetRpc(6u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	public virtual void RpcLogic___InitializeSurfaceItem_2932264618(NetworkConnection conn, ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
	{
		InitializeSurfaceItem(instance, GUID, parentSurfaceGUID, relativePosition, relativeRotation);
	}

	private void RpcReader___Target_InitializeSurfaceItem_2932264618(PooledReader PooledReader0, Channel channel)
	{
		ItemInstance instance = PooledReader0.ReadItemInstance();
		string gUID = PooledReader0.ReadString();
		string parentSurfaceGUID = PooledReader0.ReadString();
		Vector3 relativePosition = PooledReader0.ReadVector3();
		Quaternion relativeRotation = PooledReader0.ReadQuaternion();
		if (base.IsClientInitialized)
		{
			RpcLogic___InitializeSurfaceItem_2932264618(base.LocalConnection, instance, gUID, parentSurfaceGUID, relativePosition, relativeRotation);
		}
	}

	private void RpcWriter___Observers_InitializeSurfaceItem_2932264618(NetworkConnection conn, ItemInstance instance, string GUID, string parentSurfaceGUID, Vector3 relativePosition, Quaternion relativeRotation)
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
			writer.WriteItemInstance(instance);
			writer.WriteString(GUID);
			writer.WriteString(parentSurfaceGUID);
			writer.WriteVector3(relativePosition);
			writer.WriteQuaternion(relativeRotation);
			SendObserversRpc(7u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_InitializeSurfaceItem_2932264618(PooledReader PooledReader0, Channel channel)
	{
		ItemInstance instance = PooledReader0.ReadItemInstance();
		string gUID = PooledReader0.ReadString();
		string parentSurfaceGUID = PooledReader0.ReadString();
		Vector3 relativePosition = PooledReader0.ReadVector3();
		Quaternion relativeRotation = PooledReader0.ReadQuaternion();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___InitializeSurfaceItem_2932264618(null, instance, gUID, parentSurfaceGUID, relativePosition, relativeRotation);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EEntityFramework_002ESurfaceItem_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
	}
}
