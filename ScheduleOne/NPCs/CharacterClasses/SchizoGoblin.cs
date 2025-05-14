using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.NPCs.CharacterClasses;

public class SchizoGoblin : NPC
{
	private Player targetPlayer;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted;

	[ObserversRpc]
	public void SetTargetPlayer(NetworkObject player)
	{
		RpcWriter___Observers_SetTargetPlayer_3323014238(player);
	}

	public void Activate()
	{
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterObserversRpc(35u, RpcReader___Observers_SetTargetPlayer_3323014238);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002ESchizoGoblinAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Observers_SetTargetPlayer_3323014238(NetworkObject player)
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
			writer.WriteNetworkObject(player);
			SendObserversRpc(35u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetTargetPlayer_3323014238(NetworkObject player)
	{
		targetPlayer = player.GetComponent<Player>();
		if (targetPlayer.IsLocalPlayer)
		{
			LayerUtility.SetLayerRecursively(base.gameObject, LayerMask.NameToLayer("NPC"));
		}
		else
		{
			LayerUtility.SetLayerRecursively(base.gameObject, LayerMask.NameToLayer("Invisible"));
		}
	}

	private void RpcReader___Observers_SetTargetPlayer_3323014238(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject player = PooledReader0.ReadNetworkObject();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetTargetPlayer_3323014238(player);
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
