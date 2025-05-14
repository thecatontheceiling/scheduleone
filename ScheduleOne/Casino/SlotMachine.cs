using System;
using System.Collections;
using EasyButtons;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Casino;

public class SlotMachine : NetworkBehaviour
{
	public enum ESymbol
	{
		Cherry = 0,
		Lemon = 1,
		Grape = 2,
		Watermelon = 3,
		Bell = 4,
		Seven = 5
	}

	public enum EOutcome
	{
		Jackpot = 0,
		BigWin = 1,
		SmallWin = 2,
		MiniWin = 3,
		NoWin = 4
	}

	public static int[] BetAmounts = new int[5] { 5, 10, 25, 50, 100 };

	[Header("References")]
	public InteractableObject DownButton;

	public InteractableObject UpButton;

	public InteractableObject HandleIntObj;

	public TextMeshPro BetAmountLabel;

	public SlotReel[] Reels;

	public AudioSourceController SpinLoop;

	public Animation ScreenAnimation;

	public ParticleSystem[] JackpotParticles;

	[Header("Win Animations")]
	public TextMeshProUGUI[] WinAmountLabels;

	public AnimationClip MiniWinAnimation;

	public AnimationClip SmallWinAnimation;

	public AnimationClip BigWinAnimation;

	public AnimationClip JackpotAnimation;

	public AudioSourceController MiniWinSound;

	public AudioSourceController SmallWinSound;

	public AudioSourceController BigWinSound;

	public AudioSourceController JackpotSound;

	public UnityEvent onDownPressed;

	public UnityEvent onUpPressed;

	public UnityEvent onHandlePulled;

	private int currentBetIndex = 1;

	private bool NetworkInitialize___EarlyScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted;

	public bool IsSpinning { get; private set; }

	private int currentBetAmount => BetAmounts[currentBetIndex];

	public virtual void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ECasino_002ESlotMachine_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (currentBetIndex != 1)
		{
			SetBetIndex(connection, currentBetIndex);
		}
	}

	private void DownHovered()
	{
		if (IsSpinning)
		{
			DownButton.SetInteractableState(InteractableObject.EInteractableState.Disabled);
			return;
		}
		DownButton.SetInteractableState(InteractableObject.EInteractableState.Default);
		DownButton.SetMessage("Decrease bet");
	}

	private void DownInteracted()
	{
		if (onDownPressed != null)
		{
			onDownPressed.Invoke();
		}
		SendBetIndex(currentBetIndex - 1);
	}

	private void UpHovered()
	{
		if (IsSpinning)
		{
			UpButton.SetInteractableState(InteractableObject.EInteractableState.Disabled);
			return;
		}
		UpButton.SetInteractableState(InteractableObject.EInteractableState.Default);
		UpButton.SetMessage("Increase bet");
	}

	private void UpInteracted()
	{
		if (onUpPressed != null)
		{
			onUpPressed.Invoke();
		}
		SendBetIndex(currentBetIndex + 1);
	}

	private void HandleHovered()
	{
		if (IsSpinning)
		{
			HandleIntObj.SetInteractableState(InteractableObject.EInteractableState.Disabled);
			return;
		}
		int num = currentBetAmount;
		if (NetworkSingleton<MoneyManager>.Instance.cashBalance < (float)num)
		{
			HandleIntObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
			HandleIntObj.SetMessage("Insufficient cash");
		}
		else
		{
			HandleIntObj.SetInteractableState(InteractableObject.EInteractableState.Default);
			HandleIntObj.SetMessage("Pull handle");
		}
	}

	[Button]
	public void HandleInteracted()
	{
		if (!IsSpinning)
		{
			if (onHandlePulled != null)
			{
				onHandlePulled.Invoke();
			}
			NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-currentBetAmount);
			SendStartSpin(Player.Local.LocalConnection, currentBetAmount);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	private void SendBetIndex(int index)
	{
		RpcWriter___Server_SendBetIndex_3316948804(index);
		RpcLogic___SendBetIndex_3316948804(index);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetBetIndex(NetworkConnection conn, int index)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetBetIndex_2681120339(conn, index);
			RpcLogic___SetBetIndex_2681120339(conn, index);
		}
		else
		{
			RpcWriter___Target_SetBetIndex_2681120339(conn, index);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendStartSpin(NetworkConnection spinner, int betAmount)
	{
		RpcWriter___Server_SendStartSpin_2681120339(spinner, betAmount);
		RpcLogic___SendStartSpin_2681120339(spinner, betAmount);
	}

	[ObserversRpc(RunLocally = true)]
	public void StartSpin(NetworkConnection spinner, ESymbol[] symbols, int betAmount)
	{
		RpcWriter___Observers_StartSpin_2659526290(spinner, symbols, betAmount);
		RpcLogic___StartSpin_2659526290(spinner, symbols, betAmount);
	}

	private EOutcome EvaluateOutcome(ESymbol[] outcome)
	{
		if (IsUniform(outcome))
		{
			if (outcome[0] == ESymbol.Seven)
			{
				return EOutcome.Jackpot;
			}
			if (outcome[0] == ESymbol.Bell)
			{
				return EOutcome.BigWin;
			}
			if (IsFruit(outcome[0]))
			{
				return EOutcome.SmallWin;
			}
		}
		if (IsAllFruit(outcome))
		{
			return EOutcome.MiniWin;
		}
		return EOutcome.NoWin;
	}

	private int GetWinAmount(EOutcome outcome, int betAmount)
	{
		return outcome switch
		{
			EOutcome.Jackpot => betAmount * 100, 
			EOutcome.BigWin => betAmount * 25, 
			EOutcome.SmallWin => betAmount * 10, 
			EOutcome.MiniWin => betAmount * 2, 
			_ => 0, 
		};
	}

	private void DisplayOutcome(EOutcome outcome, int winAmount)
	{
		TextMeshProUGUI[] winAmountLabels = WinAmountLabels;
		for (int i = 0; i < winAmountLabels.Length; i++)
		{
			winAmountLabels[i].text = MoneyManager.FormatAmount(winAmount);
		}
		switch (outcome)
		{
		case EOutcome.Jackpot:
		{
			ScreenAnimation.Play(JackpotAnimation.name);
			ParticleSystem[] jackpotParticles = JackpotParticles;
			for (int i = 0; i < jackpotParticles.Length; i++)
			{
				jackpotParticles[i].Play();
			}
			break;
		}
		case EOutcome.BigWin:
			ScreenAnimation.Play(BigWinAnimation.name);
			BigWinSound.Play();
			break;
		case EOutcome.SmallWin:
			ScreenAnimation.Play(SmallWinAnimation.name);
			SmallWinSound.Play();
			break;
		case EOutcome.MiniWin:
			ScreenAnimation.Play(MiniWinAnimation.name);
			MiniWinSound.Play();
			break;
		}
	}

	public static ESymbol GetRandomSymbol()
	{
		if (Application.isEditor)
		{
			return ESymbol.Seven;
		}
		return (ESymbol)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ESymbol)).Length);
	}

	private bool IsFruit(ESymbol symbol)
	{
		if (symbol != ESymbol.Cherry && symbol != ESymbol.Lemon && symbol != ESymbol.Grape)
		{
			return symbol == ESymbol.Watermelon;
		}
		return true;
	}

	private bool IsAllFruit(ESymbol[] symbols)
	{
		for (int i = 0; i < symbols.Length; i++)
		{
			if (!IsFruit(symbols[i]))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsUniform(ESymbol[] symbols)
	{
		for (int i = 1; i < symbols.Length; i++)
		{
			if (symbols[i] != symbols[i - 1])
			{
				return false;
			}
		}
		return true;
	}

	[Button]
	public void SimulateMany()
	{
		int num = 100;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int i = 0; i < num; i++)
		{
			num2--;
			ESymbol[] array = new ESymbol[Reels.Length];
			for (int j = 0; j < Reels.Length; j++)
			{
				array[j] = GetRandomSymbol();
			}
			EOutcome eOutcome = EvaluateOutcome(array);
			if (eOutcome == EOutcome.MiniWin)
			{
				num4++;
			}
			if (eOutcome == EOutcome.SmallWin)
			{
				num3++;
			}
			if (eOutcome == EOutcome.BigWin)
			{
				num5++;
			}
			if (eOutcome == EOutcome.Jackpot)
			{
				num6++;
			}
			int winAmount = GetWinAmount(eOutcome, 1);
			num2 += winAmount;
		}
		Console.Log("Simulated " + num + " spins. Net win: " + num2);
		Console.Log("Mini wins: " + num4 + " Small wins: " + num3 + " Big wins: " + num5 + " Jackpots: " + num6);
	}

	public virtual void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted = true;
			RegisterServerRpc(0u, RpcReader___Server_SendBetIndex_3316948804);
			RegisterObserversRpc(1u, RpcReader___Observers_SetBetIndex_2681120339);
			RegisterTargetRpc(2u, RpcReader___Target_SetBetIndex_2681120339);
			RegisterServerRpc(3u, RpcReader___Server_SendStartSpin_2681120339);
			RegisterObserversRpc(4u, RpcReader___Observers_StartSpin_2659526290);
		}
	}

	public virtual void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECasino_002ESlotMachineAssembly_002DCSharp_002Edll_Excuted = true;
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendBetIndex_3316948804(int index)
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
			writer.WriteInt32(index);
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___SendBetIndex_3316948804(int index)
	{
		SetBetIndex(null, index);
	}

	private void RpcReader___Server_SendBetIndex_3316948804(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		int index = PooledReader0.ReadInt32();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendBetIndex_3316948804(index);
		}
	}

	private void RpcWriter___Observers_SetBetIndex_2681120339(NetworkConnection conn, int index)
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
			writer.WriteInt32(index);
			SendObserversRpc(1u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetBetIndex_2681120339(NetworkConnection conn, int index)
	{
		currentBetIndex = Mathf.Clamp(index, 0, BetAmounts.Length - 1);
		BetAmountLabel.text = MoneyManager.FormatAmount(currentBetAmount);
	}

	private void RpcReader___Observers_SetBetIndex_2681120339(PooledReader PooledReader0, Channel channel)
	{
		int index = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetBetIndex_2681120339(null, index);
		}
	}

	private void RpcWriter___Target_SetBetIndex_2681120339(NetworkConnection conn, int index)
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
			writer.WriteInt32(index);
			SendTargetRpc(2u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetBetIndex_2681120339(PooledReader PooledReader0, Channel channel)
	{
		int index = PooledReader0.ReadInt32();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetBetIndex_2681120339(base.LocalConnection, index);
		}
	}

	private void RpcWriter___Server_SendStartSpin_2681120339(NetworkConnection spinner, int betAmount)
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
			writer.WriteNetworkConnection(spinner);
			writer.WriteInt32(betAmount);
			SendServerRpc(3u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendStartSpin_2681120339(NetworkConnection spinner, int betAmount)
	{
		ESymbol[] array = new ESymbol[Reels.Length];
		for (int i = 0; i < Reels.Length; i++)
		{
			array[i] = GetRandomSymbol();
		}
		StartSpin(spinner, array, betAmount);
	}

	private void RpcReader___Server_SendStartSpin_2681120339(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NetworkConnection spinner = PooledReader0.ReadNetworkConnection();
		int betAmount = PooledReader0.ReadInt32();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendStartSpin_2681120339(spinner, betAmount);
		}
	}

	private void RpcWriter___Observers_StartSpin_2659526290(NetworkConnection spinner, ESymbol[] symbols, int betAmount)
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
			writer.WriteNetworkConnection(spinner);
			GeneratedWriters___Internal.Write___ScheduleOne_002ECasino_002ESlotMachine_002FESymbol_005B_005DFishNet_002ESerializing_002EGenerated(writer, symbols);
			writer.WriteInt32(betAmount);
			SendObserversRpc(4u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___StartSpin_2659526290(NetworkConnection spinner, ESymbol[] symbols, int betAmount)
	{
		if (!IsSpinning)
		{
			IsSpinning = true;
			Singleton<CoroutineService>.Instance.StartCoroutine(Spin());
		}
		IEnumerator Spin()
		{
			for (int i = 0; i < Reels.Length; i++)
			{
				yield return new WaitForSeconds(0.2f);
				Reels[i].Spin();
				if (i == 0)
				{
					SpinLoop.Play();
				}
			}
			yield return new WaitForSeconds(0.5f);
			EOutcome outcome = EvaluateOutcome(symbols);
			for (int i = 0; i < Reels.Length; i++)
			{
				if (i == Reels.Length - 1 && outcome != EOutcome.Jackpot && symbols[i - 1] == symbols[i - 2])
				{
					yield return new WaitForSeconds(0.3f);
				}
				yield return new WaitForSeconds(0.6f);
				if (outcome == EOutcome.Jackpot)
				{
					if (i == 0)
					{
						JackpotSound.Play();
					}
					else
					{
						yield return new WaitForSeconds(0.35f);
					}
				}
				Reels[i].Stop(symbols[i]);
			}
			SpinLoop.Stop();
			int winAmount = GetWinAmount(outcome, betAmount);
			if (spinner.IsLocalClient)
			{
				NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(winAmount);
			}
			DisplayOutcome(outcome, winAmount);
			IsSpinning = false;
		}
	}

	private void RpcReader___Observers_StartSpin_2659526290(PooledReader PooledReader0, Channel channel)
	{
		NetworkConnection spinner = PooledReader0.ReadNetworkConnection();
		ESymbol[] symbols = GeneratedReaders___Internal.Read___ScheduleOne_002ECasino_002ESlotMachine_002FESymbol_005B_005DFishNet_002ESerializing_002EGenerateds(PooledReader0);
		int betAmount = PooledReader0.ReadInt32();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___StartSpin_2659526290(spinner, symbols, betAmount);
		}
	}

	private void Awake_UserLogic_ScheduleOne_002ECasino_002ESlotMachine_Assembly_002DCSharp_002Edll()
	{
		DownButton.onHovered.AddListener(DownHovered);
		DownButton.onInteractStart.AddListener(DownInteracted);
		UpButton.onHovered.AddListener(UpHovered);
		UpButton.onInteractStart.AddListener(UpInteracted);
		HandleIntObj.onHovered.AddListener(HandleHovered);
		HandleIntObj.onInteractStart.AddListener(HandleInteracted);
		SetBetIndex(null, currentBetIndex);
	}
}
