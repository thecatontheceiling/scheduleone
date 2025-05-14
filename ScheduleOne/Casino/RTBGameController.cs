using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.Casino.UI;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Casino;

public class RTBGameController : CasinoGameController
{
	public enum EStage
	{
		WaitingForPlayers = 0,
		RedOrBlack = 1,
		HigherOrLower = 2,
		InsideOrOutside = 3,
		Suit = 4
	}

	public const int BET_MINIMUM = 10;

	public const int BET_MAXIMUM = 500;

	public const float ANSWER_MAX_TIME = 6f;

	[Header("References")]
	public Transform PlayCameraTransform;

	public Transform FocusedCameraTransform;

	public PlayingCard[] Cards;

	public Transform[] CardDefaultPositions;

	public Transform ActiveCardPosition;

	public Transform[] DockedCardPositions;

	public Action<EStage> onStageChange;

	public Action<string, string[]> onQuestionReady;

	public Action onQuestionDone;

	public Action onLocalPlayerCorrect;

	public Action onLocalPlayerIncorrect;

	public Action onLocalPlayerBetChange;

	public Action onLocalPlayerExitRound;

	private List<Player> playersInCurrentRound = new List<Player>();

	private List<PlayingCard.CardData> cardsInDeck = new List<PlayingCard.CardData>();

	private List<PlayingCard.CardData> drawnCards = new List<PlayingCard.CardData>();

	private bool NetworkInitialize___EarlyScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted;

	public EStage CurrentStage { get; private set; }

	public bool IsQuestionActive { get; private set; }

	public float LocalPlayerBet { get; private set; } = 10f;

	public float LocalPlayerBetMultiplier { get; private set; } = 1f;

	public float MultipliedLocalPlayerBet => LocalPlayerBet * LocalPlayerBetMultiplier;

	public float RemainingAnswerTime { get; private set; } = 6f;

	public bool IsLocalPlayerInCurrentRound => playersInCurrentRound.Contains(Player.Local);

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ECasino_002ERTBGameController_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Open()
	{
		base.Open();
		Singleton<RTBInterface>.Instance.Open(this);
	}

	protected override void Close()
	{
		if (IsLocalPlayerInCurrentRound)
		{
			RemoveLocalPlayerFromGame(payout: true);
		}
		Singleton<RTBInterface>.Instance.Close();
		base.Close();
	}

	protected override void Exit(ExitAction action)
	{
		if (!action.Used && base.IsOpen)
		{
			if (action.exitType == ExitType.Escape && IsLocalPlayerInCurrentRound)
			{
				action.Used = true;
				RemoveLocalPlayerFromGame(payout: true);
			}
			base.Exit(action);
		}
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (InstanceFinder.IsServer && CurrentStage == EStage.WaitingForPlayers && AreAllPlayersReady())
		{
			for (int i = 0; i < Players.CurrentPlayerCount; i++)
			{
				AddPlayerToCurrentRound(Players.GetPlayer(i).NetworkObject);
			}
			SetStage(EStage.RedOrBlack);
		}
	}

	[ObserversRpc(RunLocally = true)]
	private void SetStage(EStage stage)
	{
		RpcWriter___Observers_SetStage_2502303021(stage);
		RpcLogic___SetStage_2502303021(stage);
	}

	private void RunRound(EStage stage)
	{
		SetBetMultiplier(GetNetBetMultiplier(stage - 1));
		StartCoroutine(RunRound());
		IEnumerator RunRound()
		{
			if (IsLocalPlayerInCurrentRound)
			{
				if (stage == EStage.RedOrBlack)
				{
					NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(0f - LocalPlayerBet);
					Players.SetPlayerScore(Player.Local, Mathf.RoundToInt(LocalPlayerBet));
					base.LocalPlayerData.SetData("Ready", value: false);
				}
				PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(PlayCameraTransform.position, PlayCameraTransform.rotation, 0.8f);
				base.LocalPlayerData.SetData("Answer", 0f);
			}
			yield return new WaitForSeconds(0.4f);
			PlayingCard activeCard = null;
			if (stage == EStage.RedOrBlack)
			{
				activeCard = Cards[0];
			}
			else if (stage == EStage.HigherOrLower)
			{
				activeCard = Cards[1];
			}
			else if (stage == EStage.InsideOrOutside)
			{
				activeCard = Cards[2];
			}
			else if (stage == EStage.Suit)
			{
				activeCard = Cards[3];
			}
			if (InstanceFinder.IsServer)
			{
				activeCard.GlideTo(ActiveCardPosition.position, ActiveCardPosition.rotation);
			}
			yield return new WaitForSeconds(1f);
			GetQuestionsAndAnswers(stage, out var question, out var answers);
			IsQuestionActive = true;
			if (IsLocalPlayerInCurrentRound && onQuestionReady != null)
			{
				onQuestionReady(question, answers);
			}
			RemainingAnswerTime = 6f;
			while (RemainingAnswerTime > 0f && (!InstanceFinder.IsServer || GetAnsweredPlayersCount() != playersInCurrentRound.Count))
			{
				yield return new WaitForEndOfFrame();
				RemainingAnswerTime -= Time.deltaTime;
			}
			if (InstanceFinder.IsServer)
			{
				QuestionDone();
			}
			else
			{
				yield return new WaitUntil(() => !IsQuestionActive || IsCurrentRoundEmpty());
			}
			if (IsLocalPlayerInCurrentRound)
			{
				PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(FocusedCameraTransform.position, FocusedCameraTransform.rotation, 0.8f);
			}
			yield return new WaitForSeconds(0.7f);
			if (InstanceFinder.IsServer)
			{
				PlayingCard.CardData card = PullCardFromDeck();
				activeCard.SetCard(card.Suit, card.Value);
				activeCard.SetFaceUp(faceUp: true);
				SetBetMultiplier(GetNetBetMultiplier(stage));
				float answerIndex = GetAnswerIndex(stage, card);
				NotifyAnswer(answerIndex);
			}
			yield return new WaitForSeconds(2.5f);
			if (InstanceFinder.IsServer)
			{
				if (IsCurrentRoundEmpty())
				{
					EndGame();
				}
				else if (CurrentStage == EStage.Suit)
				{
					EndGame();
				}
				else
				{
					int num = drawnCards.Count - 1;
					activeCard.GlideTo(DockedCardPositions[num].position, DockedCardPositions[num].rotation);
					SetStage(CurrentStage + 1);
				}
			}
		}
	}

	[ObserversRpc(RunLocally = true)]
	private void SetBetMultiplier(float multiplier)
	{
		RpcWriter___Observers_SetBetMultiplier_431000436(multiplier);
		RpcLogic___SetBetMultiplier_431000436(multiplier);
	}

	[ObserversRpc(RunLocally = true)]
	private void EndGame()
	{
		RpcWriter___Observers_EndGame_2166136261();
		RpcLogic___EndGame_2166136261();
	}

	public void RemoveLocalPlayerFromGame(bool payout, float cameraDelay = 0f)
	{
		RequestRemovePlayerFromCurrentRound(Player.Local.NetworkObject);
		Players.SetPlayerScore(Player.Local, 0);
		if (payout)
		{
			NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(LocalPlayerBet * LocalPlayerBetMultiplier);
		}
		if (onLocalPlayerExitRound != null)
		{
			onLocalPlayerExitRound();
		}
		StartCoroutine(Wait());
		IEnumerator Wait()
		{
			yield return new WaitForSeconds(cameraDelay);
			if (base.IsOpen && !IsLocalPlayerInCurrentRound)
			{
				PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(localDefaultCameraTransform.position, localDefaultCameraTransform.rotation, 0.5f);
			}
		}
	}

	private bool IsCurrentRoundEmpty()
	{
		return playersInCurrentRound.Count == 0;
	}

	private float GetAnswerIndex(EStage stage, PlayingCard.CardData card)
	{
		switch (stage)
		{
		case EStage.RedOrBlack:
			if (card.Suit == PlayingCard.ECardSuit.Hearts || card.Suit == PlayingCard.ECardSuit.Diamonds)
			{
				return 1f;
			}
			return 2f;
		case EStage.HigherOrLower:
		{
			PlayingCard.CardData card4 = drawnCards[drawnCards.Count - 2];
			if (GetCardNumberValue(card) >= GetCardNumberValue(card4))
			{
				return 1f;
			}
			return 2f;
		}
		case EStage.InsideOrOutside:
		{
			PlayingCard.CardData card2 = drawnCards[drawnCards.Count - 2];
			PlayingCard.CardData card3 = drawnCards[drawnCards.Count - 3];
			int num = Mathf.Min(GetCardNumberValue(card2), GetCardNumberValue(card3));
			int num2 = Mathf.Max(GetCardNumberValue(card2), GetCardNumberValue(card3));
			int cardNumberValue = GetCardNumberValue(card);
			if (cardNumberValue >= num && cardNumberValue <= num2)
			{
				return 1f;
			}
			return 2f;
		}
		case EStage.Suit:
			switch (card.Suit)
			{
			case PlayingCard.ECardSuit.Spades:
				return 4f;
			case PlayingCard.ECardSuit.Hearts:
				return 1f;
			case PlayingCard.ECardSuit.Diamonds:
				return 3f;
			case PlayingCard.ECardSuit.Clubs:
				return 2f;
			}
			break;
		}
		Console.LogError("GetAnswerIndex not implemented for stage " + stage);
		return 0f;
	}

	[ObserversRpc(RunLocally = true)]
	private void NotifyAnswer(float answerIndex)
	{
		RpcWriter___Observers_NotifyAnswer_431000436(answerIndex);
		RpcLogic___NotifyAnswer_431000436(answerIndex);
	}

	[ObserversRpc(RunLocally = true)]
	private void QuestionDone()
	{
		RpcWriter___Observers_QuestionDone_2166136261();
		RpcLogic___QuestionDone_2166136261();
	}

	private void GetQuestionsAndAnswers(EStage stage, out string question, out string[] answers)
	{
		question = "";
		answers = new string[0];
		if (stage == EStage.RedOrBlack)
		{
			question = "What will the next card be?";
			answers = new string[2] { "Red", "Black" };
		}
		if (stage == EStage.HigherOrLower)
		{
			question = "Will the next card be higher or lower?";
			answers = new string[2] { "Higher", "Lower" };
		}
		if (stage == EStage.InsideOrOutside)
		{
			question = "Will the next card be inside or outside?";
			answers = new string[2] { "Inside", "Outside" };
		}
		if (stage == EStage.Suit)
		{
			question = "What will the suit of the next card be?";
			answers = new string[4] { "Hearts", "Clubs", "Diamonds", "Spades" };
		}
	}

	private void ResetCards()
	{
		for (int i = 0; i < Cards.Length; i++)
		{
			Cards[i].SetFaceUp(faceUp: false);
			Cards[i].GlideTo(CardDefaultPositions[i].position, CardDefaultPositions[i].rotation);
		}
		cardsInDeck = new List<PlayingCard.CardData>();
		for (int j = 0; j < 4; j++)
		{
			for (int k = 0; k < 13; k++)
			{
				PlayingCard.CardData item = new PlayingCard.CardData
				{
					Suit = (PlayingCard.ECardSuit)j,
					Value = (PlayingCard.ECardValue)(k + 1)
				};
				cardsInDeck.Add(item);
			}
		}
		drawnCards.Clear();
	}

	[ObserversRpc(RunLocally = true)]
	private void AddPlayerToCurrentRound(NetworkObject player)
	{
		RpcWriter___Observers_AddPlayerToCurrentRound_3323014238(player);
		RpcLogic___AddPlayerToCurrentRound_3323014238(player);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	private void RequestRemovePlayerFromCurrentRound(NetworkObject player)
	{
		RpcWriter___Server_RequestRemovePlayerFromCurrentRound_3323014238(player);
		RpcLogic___RequestRemovePlayerFromCurrentRound_3323014238(player);
	}

	[ObserversRpc(RunLocally = true)]
	private void RemovePlayerFromCurrentRound(NetworkObject player)
	{
		RpcWriter___Observers_RemovePlayerFromCurrentRound_3323014238(player);
		RpcLogic___RemovePlayerFromCurrentRound_3323014238(player);
	}

	private PlayingCard.CardData PullCardFromDeck()
	{
		PlayingCard.CardData cardData = cardsInDeck[UnityEngine.Random.Range(0, cardsInDeck.Count)];
		cardsInDeck.Remove(cardData);
		drawnCards.Add(cardData);
		return cardData;
	}

	public void SetLocalPlayerBet(float bet)
	{
		if (base.IsOpen)
		{
			LocalPlayerBet = bet;
			if (onLocalPlayerBetChange != null)
			{
				onLocalPlayerBetChange();
			}
		}
	}

	public bool AreAllPlayersReady()
	{
		if (Players.CurrentPlayerCount == 0)
		{
			return false;
		}
		return GetPlayersReadyCount() == Players.CurrentPlayerCount;
	}

	public int GetPlayersReadyCount()
	{
		int num = 0;
		for (int i = 0; i < Players.CurrentPlayerCount; i++)
		{
			if (!(Players.GetPlayer(i) == null) && Players.GetPlayerData(i).GetData<bool>("Ready"))
			{
				num++;
			}
		}
		return num;
	}

	public void SetLocalPlayerAnswer(float answer)
	{
		base.LocalPlayerData.SetData("Answer", answer);
	}

	public int GetAnsweredPlayersCount()
	{
		int num = 0;
		for (int i = 0; i < Players.CurrentPlayerCount; i++)
		{
			if (!(Players.GetPlayer(i) == null) && playersInCurrentRound.Contains(Players.GetPlayer(i)) && Players.GetPlayerData(i).GetData<float>("Answer") > 0.1f)
			{
				num++;
			}
		}
		return num;
	}

	public void ToggleLocalPlayerReady()
	{
		bool data = base.LocalPlayerData.GetData<bool>("Ready");
		data = !data;
		base.LocalPlayerData.SetData("Ready", data);
	}

	private int GetCardNumberValue(PlayingCard.CardData card)
	{
		if (card.Value == PlayingCard.ECardValue.Ace)
		{
			return 14;
		}
		return (int)card.Value;
	}

	public static float GetNetBetMultiplier(EStage stage)
	{
		return stage switch
		{
			EStage.RedOrBlack => 2f, 
			EStage.HigherOrLower => 3f, 
			EStage.InsideOrOutside => 4f, 
			EStage.Suit => 20f, 
			_ => 1f, 
		};
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterObserversRpc(0u, RpcReader___Observers_SetStage_2502303021);
			RegisterObserversRpc(1u, RpcReader___Observers_SetBetMultiplier_431000436);
			RegisterObserversRpc(2u, RpcReader___Observers_EndGame_2166136261);
			RegisterObserversRpc(3u, RpcReader___Observers_NotifyAnswer_431000436);
			RegisterObserversRpc(4u, RpcReader___Observers_QuestionDone_2166136261);
			RegisterObserversRpc(5u, RpcReader___Observers_AddPlayerToCurrentRound_3323014238);
			RegisterServerRpc(6u, RpcReader___Server_RequestRemovePlayerFromCurrentRound_3323014238);
			RegisterObserversRpc(7u, RpcReader___Observers_RemovePlayerFromCurrentRound_3323014238);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECasino_002ERTBGameControllerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Observers_SetStage_2502303021(EStage stage)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002ERTBGameController_002FEStageFishNet_002ESerializing_002EGenerated(writer, stage);
			SendObserversRpc(0u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetStage_2502303021(EStage stage)
	{
		CurrentStage = stage;
		if (IsLocalPlayerInCurrentRound || InstanceFinder.IsServer)
		{
			if (stage == EStage.RedOrBlack)
			{
				RunRound(EStage.RedOrBlack);
			}
			if (stage == EStage.HigherOrLower)
			{
				RunRound(EStage.HigherOrLower);
			}
			if (stage == EStage.InsideOrOutside)
			{
				RunRound(EStage.InsideOrOutside);
			}
			if (stage == EStage.Suit)
			{
				RunRound(EStage.Suit);
			}
			if (onStageChange != null)
			{
				onStageChange(stage);
			}
		}
	}

	private void RpcReader___Observers_SetStage_2502303021(PooledReader PooledReader0, Channel channel)
	{
		EStage stage = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002ERTBGameController_002FEStageFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetStage_2502303021(stage);
		}
	}

	private void RpcWriter___Observers_SetBetMultiplier_431000436(float multiplier)
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
			writer.WriteSingle(multiplier);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetBetMultiplier_431000436(float multiplier)
	{
		LocalPlayerBetMultiplier = multiplier;
	}

	private void RpcReader___Observers_SetBetMultiplier_431000436(PooledReader PooledReader0, Channel channel)
	{
		float multiplier = PooledReader0.ReadSingle();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetBetMultiplier_431000436(multiplier);
		}
	}

	private void RpcWriter___Observers_EndGame_2166136261()
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
			SendObserversRpc(2u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___EndGame_2166136261()
	{
		if (IsLocalPlayerInCurrentRound)
		{
			RemoveLocalPlayerFromGame(payout: true);
		}
		ResetCards();
		SetStage(EStage.WaitingForPlayers);
	}

	private void RpcReader___Observers_EndGame_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___EndGame_2166136261();
		}
	}

	private void RpcWriter___Observers_NotifyAnswer_431000436(float answerIndex)
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
			writer.WriteSingle(answerIndex);
			SendObserversRpc(3u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___NotifyAnswer_431000436(float answerIndex)
	{
		if (!IsLocalPlayerInCurrentRound)
		{
			return;
		}
		if (base.LocalPlayerData.GetData<float>("Answer") == answerIndex)
		{
			Console.Log("Correct answer!");
			Players.SetPlayerScore(Player.Local, Mathf.RoundToInt(MultipliedLocalPlayerBet));
			if (onLocalPlayerCorrect != null)
			{
				onLocalPlayerCorrect();
			}
		}
		else
		{
			Console.Log("Incorrect answer!");
			RemoveLocalPlayerFromGame(payout: false, 2f);
			if (onLocalPlayerIncorrect != null)
			{
				onLocalPlayerIncorrect();
			}
		}
	}

	private void RpcReader___Observers_NotifyAnswer_431000436(PooledReader PooledReader0, Channel channel)
	{
		float answerIndex = PooledReader0.ReadSingle();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___NotifyAnswer_431000436(answerIndex);
		}
	}

	private void RpcWriter___Observers_QuestionDone_2166136261()
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
			SendObserversRpc(4u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___QuestionDone_2166136261()
	{
		if (IsLocalPlayerInCurrentRound && IsQuestionActive)
		{
			if (base.LocalPlayerData.GetData<float>("Answer") == 0f)
			{
				SetLocalPlayerAnswer(1f);
			}
			IsQuestionActive = false;
			if (onQuestionDone != null)
			{
				onQuestionDone();
			}
		}
	}

	private void RpcReader___Observers_QuestionDone_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___QuestionDone_2166136261();
		}
	}

	private void RpcWriter___Observers_AddPlayerToCurrentRound_3323014238(NetworkObject player)
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
			SendObserversRpc(5u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___AddPlayerToCurrentRound_3323014238(NetworkObject player)
	{
		Player component = player.GetComponent<Player>();
		if (!(component == null) && !playersInCurrentRound.Contains(component))
		{
			playersInCurrentRound.Add(component);
		}
	}

	private void RpcReader___Observers_AddPlayerToCurrentRound_3323014238(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject player = PooledReader0.ReadNetworkObject();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___AddPlayerToCurrentRound_3323014238(player);
		}
	}

	private void RpcWriter___Server_RequestRemovePlayerFromCurrentRound_3323014238(NetworkObject player)
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
			writer.WriteNetworkObject(player);
			SendServerRpc(6u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___RequestRemovePlayerFromCurrentRound_3323014238(NetworkObject player)
	{
		RemovePlayerFromCurrentRound(player);
	}

	private void RpcReader___Server_RequestRemovePlayerFromCurrentRound_3323014238(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkObject player = PooledReader0.ReadNetworkObject();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___RequestRemovePlayerFromCurrentRound_3323014238(player);
		}
	}

	private void RpcWriter___Observers_RemovePlayerFromCurrentRound_3323014238(NetworkObject player)
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
			SendObserversRpc(7u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___RemovePlayerFromCurrentRound_3323014238(NetworkObject player)
	{
		if (!(player == null))
		{
			Player component = player.GetComponent<Player>();
			if (!(component == null) && playersInCurrentRound.Contains(component))
			{
				playersInCurrentRound.Remove(component);
			}
		}
	}

	private void RpcReader___Observers_RemovePlayerFromCurrentRound_3323014238(PooledReader PooledReader0, Channel channel)
	{
		NetworkObject player = PooledReader0.ReadNetworkObject();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___RemovePlayerFromCurrentRound_3323014238(player);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ECasino_002ERTBGameController_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		ResetCards();
	}
}
