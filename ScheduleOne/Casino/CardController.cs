using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using UnityEngine;

namespace ScheduleOne.Casino;

public class CardController : NetworkBehaviour
{
	private List<PlayingCard> cards = new List<PlayingCard>();

	private Dictionary<string, PlayingCard> cardDictionary = new Dictionary<string, PlayingCard>();

	private bool NetworkInitialize___EarlyScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted;

	public virtual void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ECasino_002ECardController_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendCardValue(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
	{
		RpcWriter___Server_SendCardValue_3709737967(cardId, suit, value);
		RpcLogic___SendCardValue_3709737967(cardId, suit, value);
	}

	[ObserversRpc(RunLocally = true)]
	private void SetCardValue(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
	{
		RpcWriter___Observers_SetCardValue_3709737967(cardId, suit, value);
		RpcLogic___SetCardValue_3709737967(cardId, suit, value);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendCardFaceUp(string cardId, bool faceUp)
	{
		RpcWriter___Server_SendCardFaceUp_310431262(cardId, faceUp);
		RpcLogic___SendCardFaceUp_310431262(cardId, faceUp);
	}

	[ObserversRpc(RunLocally = true)]
	private void SetCardFaceUp(string cardId, bool faceUp)
	{
		RpcWriter___Observers_SetCardFaceUp_310431262(cardId, faceUp);
		RpcLogic___SetCardFaceUp_310431262(cardId, faceUp);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendCardGlide(string cardId, Vector3 position, Quaternion rotation, float glideTime)
	{
		RpcWriter___Server_SendCardGlide_2833372058(cardId, position, rotation, glideTime);
		RpcLogic___SendCardGlide_2833372058(cardId, position, rotation, glideTime);
	}

	[ObserversRpc(RunLocally = true)]
	private void SetCardGlide(string cardId, Vector3 position, Quaternion rotation, float glideTime)
	{
		RpcWriter___Observers_SetCardGlide_2833372058(cardId, position, rotation, glideTime);
		RpcLogic___SetCardGlide_2833372058(cardId, position, rotation, glideTime);
	}

	private PlayingCard GetCard(string cardId)
	{
		return cardDictionary[cardId];
	}

	public virtual void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted = true;
			RegisterServerRpc(0u, RpcReader___Server_SendCardValue_3709737967);
			RegisterObserversRpc(1u, RpcReader___Observers_SetCardValue_3709737967);
			RegisterServerRpc(2u, RpcReader___Server_SendCardFaceUp_310431262);
			RegisterObserversRpc(3u, RpcReader___Observers_SetCardFaceUp_310431262);
			RegisterServerRpc(4u, RpcReader___Server_SendCardGlide_2833372058);
			RegisterObserversRpc(5u, RpcReader___Observers_SetCardGlide_2833372058);
		}
	}

	public virtual void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECasino_002ECardControllerAssembly_002DCSharp_002Edll_Excuted = true;
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendCardValue_3709737967(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
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
			writer.WriteString(cardId);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002EPlayingCard_002FECardSuitFishNet_002ESerializing_002EGenerated(writer, suit);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002EPlayingCard_002FECardValueFishNet_002ESerializing_002EGenerated(writer, value);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendCardValue_3709737967(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
	{
		SetCardValue(cardId, suit, value);
	}

	private void RpcReader___Server_SendCardValue_3709737967(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string cardId = PooledReader0.ReadString();
		PlayingCard.ECardSuit suit = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002EPlayingCard_002FECardSuitFishNet_002ESerializing_002EGenerateds(PooledReader0);
		PlayingCard.ECardValue value = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002EPlayingCard_002FECardValueFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendCardValue_3709737967(cardId, suit, value);
		}
	}

	private void RpcWriter___Observers_SetCardValue_3709737967(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
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
			writer.WriteString(cardId);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002EPlayingCard_002FECardSuitFishNet_002ESerializing_002EGenerated(writer, suit);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002EPlayingCard_002FECardValueFishNet_002ESerializing_002EGenerated(writer, value);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetCardValue_3709737967(string cardId, PlayingCard.ECardSuit suit, PlayingCard.ECardValue value)
	{
		PlayingCard card = GetCard(cardId);
		if (card != null)
		{
			card.SetCard(suit, value, network: false);
		}
	}

	private void RpcReader___Observers_SetCardValue_3709737967(PooledReader PooledReader0, Channel channel)
	{
		string cardId = PooledReader0.ReadString();
		PlayingCard.ECardSuit suit = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002EPlayingCard_002FECardSuitFishNet_002ESerializing_002EGenerateds(PooledReader0);
		PlayingCard.ECardValue value = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002EPlayingCard_002FECardValueFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetCardValue_3709737967(cardId, suit, value);
		}
	}

	private void RpcWriter___Server_SendCardFaceUp_310431262(string cardId, bool faceUp)
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
			writer.WriteString(cardId);
			writer.WriteBoolean(faceUp);
			SendServerRpc(2u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendCardFaceUp_310431262(string cardId, bool faceUp)
	{
		SetCardFaceUp(cardId, faceUp);
	}

	private void RpcReader___Server_SendCardFaceUp_310431262(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string cardId = PooledReader0.ReadString();
		bool faceUp = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendCardFaceUp_310431262(cardId, faceUp);
		}
	}

	private void RpcWriter___Observers_SetCardFaceUp_310431262(string cardId, bool faceUp)
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
			writer.WriteString(cardId);
			writer.WriteBoolean(faceUp);
			SendObserversRpc(3u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetCardFaceUp_310431262(string cardId, bool faceUp)
	{
		PlayingCard card = GetCard(cardId);
		if (card != null)
		{
			card.SetFaceUp(faceUp, network: false);
		}
	}

	private void RpcReader___Observers_SetCardFaceUp_310431262(PooledReader PooledReader0, Channel channel)
	{
		string cardId = PooledReader0.ReadString();
		bool faceUp = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetCardFaceUp_310431262(cardId, faceUp);
		}
	}

	private void RpcWriter___Server_SendCardGlide_2833372058(string cardId, Vector3 position, Quaternion rotation, float glideTime)
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
			writer.WriteString(cardId);
			writer.WriteVector3(position);
			writer.WriteQuaternion(rotation);
			writer.WriteSingle(glideTime);
			SendServerRpc(4u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendCardGlide_2833372058(string cardId, Vector3 position, Quaternion rotation, float glideTime)
	{
		SetCardGlide(cardId, position, rotation, glideTime);
	}

	private void RpcReader___Server_SendCardGlide_2833372058(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string cardId = PooledReader0.ReadString();
		Vector3 position = PooledReader0.ReadVector3();
		Quaternion rotation = PooledReader0.ReadQuaternion();
		float glideTime = PooledReader0.ReadSingle();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendCardGlide_2833372058(cardId, position, rotation, glideTime);
		}
	}

	private void RpcWriter___Observers_SetCardGlide_2833372058(string cardId, Vector3 position, Quaternion rotation, float glideTime)
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
			writer.WriteString(cardId);
			writer.WriteVector3(position);
			writer.WriteQuaternion(rotation);
			writer.WriteSingle(glideTime);
			SendObserversRpc(5u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetCardGlide_2833372058(string cardId, Vector3 position, Quaternion rotation, float glideTime)
	{
		PlayingCard card = GetCard(cardId);
		if (card != null)
		{
			card.GlideTo(position, rotation, glideTime, network: false);
		}
	}

	private void RpcReader___Observers_SetCardGlide_2833372058(PooledReader PooledReader0, Channel channel)
	{
		string cardId = PooledReader0.ReadString();
		Vector3 position = PooledReader0.ReadVector3();
		Quaternion rotation = PooledReader0.ReadQuaternion();
		float glideTime = PooledReader0.ReadSingle();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetCardGlide_2833372058(cardId, position, rotation, glideTime);
		}
	}

	private void Awake_UserLogic_ScheduleOne_002ECasino_002ECardController_Assembly_002DCSharp_002Edll()
	{
		cards = new List<PlayingCard>(GetComponentsInChildren<PlayingCard>());
		foreach (PlayingCard card in cards)
		{
			card.SetCardController(this);
			if (cardDictionary.ContainsKey(card.CardID))
			{
				Debug.LogError("Card ID " + card.CardID + " already exists in the dictionary.");
			}
			else
			{
				cardDictionary.Add(card.CardID, card);
			}
		}
	}
}
