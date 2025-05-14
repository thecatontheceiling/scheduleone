using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using FluffyUnderware.DevTools.Extensions;
using ScheduleOne.PlayerScripts;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Casino;

public class CasinoGamePlayers : NetworkBehaviour
{
	public int PlayerLimit = 4;

	private Player[] Players;

	public UnityEvent onPlayerListChanged;

	public UnityEvent onPlayerScoresChanged;

	private Dictionary<Player, int> playerScores = new Dictionary<Player, int>();

	private Dictionary<Player, CasinoGamePlayerData> playerDatas = new Dictionary<Player, CasinoGamePlayerData>();

	private bool NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted;

	public int CurrentPlayerCount => Players.Count((Player p) => p != null);

	public virtual void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ECasino_002ECasinoGamePlayers_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (CurrentPlayerCount <= 0)
		{
			return;
		}
		SetPlayerList(connection, GetPlayerObjects());
		Player[] players = Players;
		foreach (Player player in players)
		{
			if (!(player == null) && playerScores[player] != 0)
			{
				SetPlayerScore(connection, player.NetworkObject, playerScores[player]);
			}
		}
	}

	public void AddPlayer(Player player)
	{
		RequestAddPlayer(player.NetworkObject);
	}

	public void RemovePlayer(Player player)
	{
		RequestRemovePlayer(player.NetworkObject);
	}

	public void SetPlayerScore(Player player, int score)
	{
		RequestSetScore(player.NetworkObject, score);
	}

	public int GetPlayerScore(Player player)
	{
		if (player == null)
		{
			return 0;
		}
		if (playerScores.ContainsKey(player))
		{
			return playerScores[player];
		}
		return 0;
	}

	public Player GetPlayer(int index)
	{
		if (index < Players.Length)
		{
			return Players[index];
		}
		return null;
	}

	public int GetPlayerIndex(Player player)
	{
		return Players.IndexOf(player);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	private void RequestAddPlayer(NetworkObject playerObject)
	{
		RpcWriter___Server_RequestAddPlayer_3323014238(playerObject);
		RpcLogic___RequestAddPlayer_3323014238(playerObject);
	}

	private void AddPlayerToArray(Player player)
	{
		for (int i = 0; i < PlayerLimit; i++)
		{
			if (Players[i] == null)
			{
				Players[i] = player;
				break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestRemovePlayer(NetworkObject playerObject)
	{
		RpcWriter___Server_RequestRemovePlayer_3323014238(playerObject);
	}

	private void RemovePlayerFromArray(Player player)
	{
		for (int i = 0; i < PlayerLimit; i++)
		{
			if (Players[i] == player)
			{
				Players[i] = null;
				break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestSetScore(NetworkObject playerObject, int score)
	{
		RpcWriter___Server_RequestSetScore_4172557123(playerObject, score);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	private void SetPlayerScore(NetworkConnection conn, NetworkObject playerObject, int score)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetPlayerScore_1865307316(conn, playerObject, score);
			RpcLogic___SetPlayerScore_1865307316(conn, playerObject, score);
		}
		else
		{
			RpcWriter___Target_SetPlayerScore_1865307316(conn, playerObject, score);
		}
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	private void SetPlayerList(NetworkConnection conn, NetworkObject[] playerObjects)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetPlayerList_204172449(conn, playerObjects);
			RpcLogic___SetPlayerList_204172449(conn, playerObjects);
		}
		else
		{
			RpcWriter___Target_SetPlayerList_204172449(conn, playerObjects);
		}
	}

	public CasinoGamePlayerData GetPlayerData()
	{
		return GetPlayerData(Player.Local);
	}

	public CasinoGamePlayerData GetPlayerData(Player player)
	{
		if (!playerDatas.ContainsKey(player))
		{
			playerDatas.Add(player, new CasinoGamePlayerData(this, player));
		}
		return playerDatas[player];
	}

	public CasinoGamePlayerData GetPlayerData(int index)
	{
		if (index < Players.Length && Players[index] != null)
		{
			return GetPlayerData(Players[index]);
		}
		return null;
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendPlayerBool(NetworkObject playerObject, string key, bool value)
	{
		RpcWriter___Server_SendPlayerBool_77262511(playerObject, key, value);
		RpcLogic___SendPlayerBool_77262511(playerObject, key, value);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	private void ReceivePlayerBool(NetworkConnection conn, NetworkObject playerObject, string key, bool value)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_ReceivePlayerBool_1748594478(conn, playerObject, key, value);
			RpcLogic___ReceivePlayerBool_1748594478(conn, playerObject, key, value);
		}
		else
		{
			RpcWriter___Target_ReceivePlayerBool_1748594478(conn, playerObject, key, value);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendPlayerFloat(NetworkObject playerObject, string key, float value)
	{
		RpcWriter___Server_SendPlayerFloat_2931762093(playerObject, key, value);
		RpcLogic___SendPlayerFloat_2931762093(playerObject, key, value);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	private void ReceivePlayerFloat(NetworkConnection conn, NetworkObject playerObject, string key, float value)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_ReceivePlayerFloat_2317689966(conn, playerObject, key, value);
			RpcLogic___ReceivePlayerFloat_2317689966(conn, playerObject, key, value);
		}
		else
		{
			RpcWriter___Target_ReceivePlayerFloat_2317689966(conn, playerObject, key, value);
		}
	}

	private NetworkObject[] GetPlayerObjects()
	{
		NetworkObject[] array = new NetworkObject[PlayerLimit];
		for (int i = 0; i < PlayerLimit; i++)
		{
			if (Players[i] != null)
			{
				array[i] = Players[i].NetworkObject;
			}
		}
		return array;
	}

	public virtual void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted = true;
			RegisterServerRpc(0u, RpcReader___Server_RequestAddPlayer_3323014238);
			RegisterServerRpc(1u, RpcReader___Server_RequestRemovePlayer_3323014238);
			RegisterServerRpc(2u, RpcReader___Server_RequestSetScore_4172557123);
			RegisterObserversRpc(3u, RpcReader___Observers_SetPlayerScore_1865307316);
			RegisterTargetRpc(4u, RpcReader___Target_SetPlayerScore_1865307316);
			RegisterObserversRpc(5u, RpcReader___Observers_SetPlayerList_204172449);
			RegisterTargetRpc(6u, RpcReader___Target_SetPlayerList_204172449);
			RegisterServerRpc(7u, RpcReader___Server_SendPlayerBool_77262511);
			RegisterObserversRpc(8u, RpcReader___Observers_ReceivePlayerBool_1748594478);
			RegisterTargetRpc(9u, RpcReader___Target_ReceivePlayerBool_1748594478);
			RegisterServerRpc(10u, RpcReader___Server_SendPlayerFloat_2931762093);
			RegisterObserversRpc(11u, RpcReader___Observers_ReceivePlayerFloat_2317689966);
			RegisterTargetRpc(12u, RpcReader___Target_ReceivePlayerFloat_2317689966);
		}
	}

	public virtual void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGamePlayersAssembly_002DCSharp_002Edll_Excuted = true;
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_RequestAddPlayer_3323014238(NetworkObject playerObject)
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
			writer.WriteNetworkObject(playerObject);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___RequestAddPlayer_3323014238(NetworkObject playerObject)
	{
		Player component = playerObject.GetComponent<Player>();
		if (component != null && !Players.Contains(component))
		{
			AddPlayerToArray(component);
			if (!playerScores.ContainsKey(component))
			{
				playerScores.Add(component, 0);
			}
			if (!playerDatas.ContainsKey(component))
			{
				playerDatas.Add(component, new CasinoGamePlayerData(this, component));
			}
		}
		SetPlayerList(null, GetPlayerObjects());
	}

	private void RpcReader___Server_RequestAddPlayer_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___RequestAddPlayer_3323014238(playerObject);
		}
	}

	private void RpcWriter___Server_RequestRemovePlayer_3323014238(NetworkObject playerObject)
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
			writer.WriteNetworkObject(playerObject);
			SendServerRpc(1u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___RequestRemovePlayer_3323014238(NetworkObject playerObject)
	{
		Player component = playerObject.GetComponent<Player>();
		if (component != null && Players.Contains(component))
		{
			RemovePlayerFromArray(component);
		}
		SetPlayerList(null, GetPlayerObjects());
	}

	private void RpcReader___Server_RequestRemovePlayer_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized)
		{
			RpcLogic___RequestRemovePlayer_3323014238(playerObject);
		}
	}

	private void RpcWriter___Server_RequestSetScore_4172557123(NetworkObject playerObject, int score)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteInt32(score);
			SendServerRpc(2u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___RequestSetScore_4172557123(NetworkObject playerObject, int score)
	{
		SetPlayerScore(null, playerObject, score);
	}

	private void RpcReader___Server_RequestSetScore_4172557123(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		int score = PooledReader0.ReadInt32();
		if (base.IsServerInitialized)
		{
			RpcLogic___RequestSetScore_4172557123(playerObject, score);
		}
	}

	private void RpcWriter___Observers_SetPlayerScore_1865307316(NetworkConnection conn, NetworkObject playerObject, int score)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteInt32(score);
			SendObserversRpc(3u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetPlayerScore_1865307316(NetworkConnection conn, NetworkObject playerObject, int score)
	{
		Player component = playerObject.GetComponent<Player>();
		if (!(component == null))
		{
			if (!playerScores.ContainsKey(component))
			{
				playerScores.Add(component, score);
			}
			else
			{
				playerScores[component] = score;
			}
			if (onPlayerScoresChanged != null)
			{
				onPlayerScoresChanged.Invoke();
			}
		}
	}

	private void RpcReader___Observers_SetPlayerScore_1865307316(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		int score = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetPlayerScore_1865307316(null, playerObject, score);
		}
	}

	private void RpcWriter___Target_SetPlayerScore_1865307316(NetworkConnection conn, NetworkObject playerObject, int score)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteInt32(score);
			SendTargetRpc(4u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetPlayerScore_1865307316(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		int score = PooledReader0.ReadInt32();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetPlayerScore_1865307316(base.LocalConnection, playerObject, score);
		}
	}

	private void RpcWriter___Observers_SetPlayerList_204172449(NetworkConnection conn, NetworkObject[] playerObjects)
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
			GeneratedWriters___Internal.Write___FishNet_002EObject_002ENetworkObject_005B_005DFishNet_002ESerializing_002EGenerated(writer, playerObjects);
			SendObserversRpc(5u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetPlayerList_204172449(NetworkConnection conn, NetworkObject[] playerObjects)
	{
		Players = new Player[PlayerLimit];
		for (int i = 0; i < PlayerLimit; i++)
		{
			Players[i] = null;
		}
		for (int j = 0; j < playerObjects.Length; j++)
		{
			if (playerObjects[j] == null)
			{
				continue;
			}
			Player component = playerObjects[j].GetComponent<Player>();
			if (component != null)
			{
				Players[j] = component;
				if (!playerScores.ContainsKey(component))
				{
					playerScores.Add(component, 0);
				}
				if (!playerDatas.ContainsKey(component))
				{
					playerDatas.Add(component, new CasinoGamePlayerData(this, component));
				}
			}
		}
		if (onPlayerListChanged != null)
		{
			onPlayerListChanged.Invoke();
		}
	}

	private void RpcReader___Observers_SetPlayerList_204172449(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject[] playerObjects = GeneratedReaders___Internal.Read___FishNet_002EObject_002ENetworkObject_005B_005DFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetPlayerList_204172449(null, playerObjects);
		}
	}

	private void RpcWriter___Target_SetPlayerList_204172449(NetworkConnection conn, NetworkObject[] playerObjects)
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
			GeneratedWriters___Internal.Write___FishNet_002EObject_002ENetworkObject_005B_005DFishNet_002ESerializing_002EGenerated(writer, playerObjects);
			SendTargetRpc(6u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetPlayerList_204172449(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject[] playerObjects = GeneratedReaders___Internal.Read___FishNet_002EObject_002ENetworkObject_005B_005DFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___SetPlayerList_204172449(base.LocalConnection, playerObjects);
		}
	}

	private void RpcWriter___Server_SendPlayerBool_77262511(NetworkObject playerObject, string key, bool value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteBoolean(value);
			SendServerRpc(7u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendPlayerBool_77262511(NetworkObject playerObject, string key, bool value)
	{
		ReceivePlayerBool(null, playerObject, key, value);
	}

	private void RpcReader___Server_SendPlayerBool_77262511(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		bool value = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendPlayerBool_77262511(playerObject, key, value);
		}
	}

	private void RpcWriter___Observers_ReceivePlayerBool_1748594478(NetworkConnection conn, NetworkObject playerObject, string key, bool value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteBoolean(value);
			SendObserversRpc(8u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___ReceivePlayerBool_1748594478(NetworkConnection conn, NetworkObject playerObject, string key, bool value)
	{
		Player component = playerObject.GetComponent<Player>();
		if (!(component == null))
		{
			if (!playerDatas.ContainsKey(component))
			{
				playerDatas.Add(component, new CasinoGamePlayerData(this, component));
			}
			playerDatas[component].SetData(key, value, network: false);
		}
	}

	private void RpcReader___Observers_ReceivePlayerBool_1748594478(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		bool value = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___ReceivePlayerBool_1748594478(null, playerObject, key, value);
		}
	}

	private void RpcWriter___Target_ReceivePlayerBool_1748594478(NetworkConnection conn, NetworkObject playerObject, string key, bool value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteBoolean(value);
			SendTargetRpc(9u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_ReceivePlayerBool_1748594478(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		bool value = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceivePlayerBool_1748594478(base.LocalConnection, playerObject, key, value);
		}
	}

	private void RpcWriter___Server_SendPlayerFloat_2931762093(NetworkObject playerObject, string key, float value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteSingle(value);
			SendServerRpc(10u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendPlayerFloat_2931762093(NetworkObject playerObject, string key, float value)
	{
		ReceivePlayerFloat(null, playerObject, key, value);
	}

	private void RpcReader___Server_SendPlayerFloat_2931762093(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendPlayerFloat_2931762093(playerObject, key, value);
		}
	}

	private void RpcWriter___Observers_ReceivePlayerFloat_2317689966(NetworkConnection conn, NetworkObject playerObject, string key, float value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteSingle(value);
			SendObserversRpc(11u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___ReceivePlayerFloat_2317689966(NetworkConnection conn, NetworkObject playerObject, string key, float value)
	{
		Player component = playerObject.GetComponent<Player>();
		if (!(component == null))
		{
			if (!playerDatas.ContainsKey(component))
			{
				playerDatas.Add(component, new CasinoGamePlayerData(this, component));
			}
			playerDatas[component].SetData(key, value, network: false);
		}
	}

	private void RpcReader___Observers_ReceivePlayerFloat_2317689966(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___ReceivePlayerFloat_2317689966(null, playerObject, key, value);
		}
	}

	private void RpcWriter___Target_ReceivePlayerFloat_2317689966(NetworkConnection conn, NetworkObject playerObject, string key, float value)
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
			writer.WriteNetworkObject(playerObject);
			writer.WriteString(key);
			writer.WriteSingle(value);
			SendTargetRpc(12u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_ReceivePlayerFloat_2317689966(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject playerObject = PooledReader0.ReadNetworkObject();
		string key = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsClientInitialized)
		{
			RpcLogic___ReceivePlayerFloat_2317689966(base.LocalConnection, playerObject, key, value);
		}
	}

	private void Awake_UserLogic_ScheduleOne_002ECasino_002ECasinoGamePlayers_Assembly_002DCSharp_002Edll()
	{
		Players = new Player[PlayerLimit];
	}
}
