using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.UI;
using TMPro;
using UnityEngine;

namespace ScheduleOne.EntityFramework;

public class LabelledSurfaceItem : SurfaceItem
{
	public int MaxCharacters = 100;

	[Header("References")]
	public TextMeshPro Label;

	private bool NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted;

	public string Message { get; private set; } = "Your Message Here";

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (!connection.IsHost)
		{
			SetMessage(connection, Message);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendMessageToServer(string message)
	{
		RpcWriter___Server_SendMessageToServer_3615296227(message);
		RpcLogic___SendMessageToServer_3615296227(message);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetMessage(NetworkConnection conn, string message)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetMessage_2971853958(conn, message);
			RpcLogic___SetMessage_2971853958(conn, message);
		}
		else
		{
			RpcWriter___Target_SetMessage_2971853958(conn, message);
		}
	}

	public void Interacted()
	{
		Singleton<TextInputScreen>.Instance.Open("Edit Sign Message", Message, MessageSubmitted, MaxCharacters);
	}

	private void MessageSubmitted(string message)
	{
		SendMessageToServer(message);
	}

	public override string GetSaveString()
	{
		return new LabelledSurfaceItemData(base.GUID, base.ItemInstance, 0, base.ParentSurface.GUID.ToString(), RelativePosition, RelativeRotation, Message).GetJson();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(8u, RpcReader___Server_SendMessageToServer_3615296227);
			RegisterObserversRpc(9u, RpcReader___Observers_SetMessage_2971853958);
			RegisterTargetRpc(10u, RpcReader___Target_SetMessage_2971853958);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EEntityFramework_002ELabelledSurfaceItemAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendMessageToServer_3615296227(string message)
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
			writer.WriteString(message);
			SendServerRpc(8u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendMessageToServer_3615296227(string message)
	{
		SetMessage(null, message);
	}

	private void RpcReader___Server_SendMessageToServer_3615296227(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string message = PooledReader0.ReadString();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendMessageToServer_3615296227(message);
		}
	}

	private void RpcWriter___Observers_SetMessage_2971853958(NetworkConnection conn, string message)
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
			writer.WriteString(message);
			SendObserversRpc(9u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetMessage_2971853958(NetworkConnection conn, string message)
	{
		Message = message;
		Label.text = message;
		base.HasChanged = true;
	}

	private void RpcReader___Observers_SetMessage_2971853958(PooledReader PooledReader0, Channel channel)
	{
		string message = PooledReader0.ReadString();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetMessage_2971853958(null, message);
		}
	}

	private void RpcWriter___Target_SetMessage_2971853958(NetworkConnection conn, string message)
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
			writer.WriteString(message);
			SendTargetRpc(10u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetMessage_2971853958(PooledReader PooledReader0, Channel channel)
	{
		string message = PooledReader0.ReadString();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetMessage_2971853958(base.LocalConnection, message);
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
