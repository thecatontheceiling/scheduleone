using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScheduleOne.Casino.UI;

public class BlackjackInterface : Singleton<BlackjackInterface>
{
	[Header("References")]
	public Canvas Canvas;

	public CasinoGamePlayerDisplay PlayerDisplay;

	public RectTransform BetContainer;

	public TextMeshProUGUI BetTitleLabel;

	public Slider BetSlider;

	public TextMeshProUGUI BetAmount;

	public Button ReadyButton;

	public TextMeshProUGUI ReadyLabel;

	public RectTransform WaitingContainer;

	public TextMeshProUGUI WaitingLabel;

	public TextMeshProUGUI DealerScoreLabel;

	public TextMeshProUGUI PlayerScoreLabel;

	public Button HitButton;

	public Button StandButton;

	public Animation InputContainerAnimation;

	public CanvasGroup InputContainerCanvasGroup;

	public AnimationClip InputContainerFadeIn;

	public AnimationClip InputContainerFadeOut;

	public RectTransform SelectionIndicator;

	public Animation ScoresContainerAnimation;

	public CanvasGroup ScoresContainerCanvasGroup;

	public TextMeshProUGUI PositiveOutcomeLabel;

	public TextMeshProUGUI PayoutLabel;

	public UnityEvent onBust;

	public UnityEvent onBlackjack;

	public UnityEvent onWin;

	public UnityEvent onLose;

	public UnityEvent onPush;

	public BlackjackGameController CurrentGame { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		BetSlider.onValueChanged.AddListener(BetSliderChanged);
		ReadyButton.onClick.AddListener(ReadyButtonClicked);
		HitButton.onClick.AddListener(HitClicked);
		StandButton.onClick.AddListener(StandClicked);
		InputContainerCanvasGroup.alpha = 0f;
		InputContainerCanvasGroup.interactable = false;
		ScoresContainerCanvasGroup.alpha = 0f;
		Canvas.enabled = false;
	}

	private void FixedUpdate()
	{
		if (CurrentGame == null)
		{
			return;
		}
		bool data = CurrentGame.LocalPlayerData.GetData<bool>("Ready");
		BetSlider.interactable = CurrentGame.CurrentStage == BlackjackGameController.EStage.WaitingForPlayers && !data;
		if (data)
		{
			BetTitleLabel.text = "Waiting for other players...";
		}
		else
		{
			BetTitleLabel.text = "Place your bet and press 'ready'";
		}
		if (CurrentGame.CurrentStage == BlackjackGameController.EStage.WaitingForPlayers)
		{
			BetContainer.gameObject.SetActive(value: true);
			RefreshReadyButton();
		}
		else
		{
			BetContainer.gameObject.SetActive(value: false);
		}
		PlayerScoreLabel.text = CurrentGame.LocalPlayerScore.ToString();
		if (CurrentGame.CurrentStage == BlackjackGameController.EStage.DealerTurn || CurrentGame.CurrentStage == BlackjackGameController.EStage.Ending)
		{
			DealerScoreLabel.text = CurrentGame.DealerScore.ToString();
		}
		else
		{
			DealerScoreLabel.text = CurrentGame.DealerScore + "+?";
		}
		if (CurrentGame.CurrentStage == BlackjackGameController.EStage.PlayerTurn && CurrentGame.PlayerTurn != null)
		{
			if (CurrentGame.PlayerTurn.IsLocalPlayer)
			{
				WaitingLabel.text = "Your turn!";
			}
			else
			{
				WaitingLabel.text = "Waiting for " + CurrentGame.PlayerTurn.PlayerName + "...";
			}
			WaitingContainer.gameObject.SetActive(value: true);
		}
		else if (CurrentGame.CurrentStage == BlackjackGameController.EStage.DealerTurn)
		{
			WaitingLabel.text = "Dealer's turn...";
			WaitingContainer.gameObject.SetActive(value: true);
		}
		else
		{
			WaitingContainer.gameObject.SetActive(value: false);
		}
	}

	public void Open(BlackjackGameController game)
	{
		CurrentGame = game;
		BlackjackGameController currentGame = CurrentGame;
		currentGame.onLocalPlayerBetChange = (Action)Delegate.Combine(currentGame.onLocalPlayerBetChange, new Action(RefreshDisplayedBet));
		BlackjackGameController currentGame2 = CurrentGame;
		currentGame2.onLocalPlayerExitRound = (Action)Delegate.Combine(currentGame2.onLocalPlayerExitRound, new Action(LocalPlayerExitRound));
		BlackjackGameController currentGame3 = CurrentGame;
		currentGame3.onInitialCardsDealt = (Action)Delegate.Combine(currentGame3.onInitialCardsDealt, new Action(ShowScores));
		BlackjackGameController currentGame4 = CurrentGame;
		currentGame4.onLocalPlayerReadyForInput = (Action)Delegate.Combine(currentGame4.onLocalPlayerReadyForInput, new Action(LocalPlayerReadyForInput));
		BlackjackGameController currentGame5 = CurrentGame;
		currentGame5.onLocalPlayerBust = (Action)Delegate.Combine(currentGame5.onLocalPlayerBust, new Action(OnLocalPlayerBust));
		BlackjackGameController currentGame6 = CurrentGame;
		currentGame6.onLocalPlayerRoundCompleted = (Action<BlackjackGameController.EPayoutType>)Delegate.Combine(currentGame6.onLocalPlayerRoundCompleted, new Action<BlackjackGameController.EPayoutType>(OnLocalPlayerRoundCompleted));
		PlayerDisplay.Bind(game.Players);
		Singleton<InputPromptsCanvas>.Instance.LoadModule("exitonly");
		Canvas.enabled = true;
		BetSlider.SetValueWithoutNotify(0f);
		game.SetLocalPlayerBet(10f);
		RefreshDisplayedBet();
		RefreshDisplayedBet();
	}

	public void Close()
	{
		if (CurrentGame != null)
		{
			BlackjackGameController currentGame = CurrentGame;
			currentGame.onLocalPlayerBetChange = (Action)Delegate.Remove(currentGame.onLocalPlayerBetChange, new Action(RefreshDisplayedBet));
			BlackjackGameController currentGame2 = CurrentGame;
			currentGame2.onLocalPlayerExitRound = (Action)Delegate.Remove(currentGame2.onLocalPlayerExitRound, new Action(LocalPlayerExitRound));
			BlackjackGameController currentGame3 = CurrentGame;
			currentGame3.onInitialCardsDealt = (Action)Delegate.Remove(currentGame3.onInitialCardsDealt, new Action(ShowScores));
			BlackjackGameController currentGame4 = CurrentGame;
			currentGame4.onLocalPlayerReadyForInput = (Action)Delegate.Remove(currentGame4.onLocalPlayerReadyForInput, new Action(LocalPlayerReadyForInput));
			BlackjackGameController currentGame5 = CurrentGame;
			currentGame5.onLocalPlayerBust = (Action)Delegate.Remove(currentGame5.onLocalPlayerBust, new Action(OnLocalPlayerBust));
			BlackjackGameController currentGame6 = CurrentGame;
			currentGame6.onLocalPlayerRoundCompleted = (Action<BlackjackGameController.EPayoutType>)Delegate.Remove(currentGame6.onLocalPlayerRoundCompleted, new Action<BlackjackGameController.EPayoutType>(OnLocalPlayerRoundCompleted));
		}
		CurrentGame = null;
		PlayerDisplay.Unbind();
		Singleton<InputPromptsCanvas>.Instance.UnloadModule();
		Canvas.enabled = false;
	}

	private void BetSliderChanged(float newValue)
	{
		CurrentGame.SetLocalPlayerBet(GetBetFromSliderValue(newValue));
		RefreshDisplayedBet();
	}

	private float GetBetFromSliderValue(float sliderVal)
	{
		return Mathf.Lerp(10f, 1000f, Mathf.Pow(sliderVal, 2f));
	}

	private void RefreshDisplayedBet()
	{
		BetAmount.text = MoneyManager.FormatAmount(CurrentGame.LocalPlayerBet);
		BetSlider.SetValueWithoutNotify(Mathf.Sqrt(Mathf.InverseLerp(10f, 1000f, CurrentGame.LocalPlayerBet)));
	}

	private void RefreshReadyButton()
	{
		if (NetworkSingleton<MoneyManager>.Instance.cashBalance >= CurrentGame.LocalPlayerBet)
		{
			ReadyButton.interactable = true;
			BetAmount.color = new Color32(84, 231, 23, byte.MaxValue);
		}
		else
		{
			ReadyButton.interactable = false;
			BetAmount.color = new Color32(231, 52, 23, byte.MaxValue);
		}
		if (CurrentGame.LocalPlayerData.GetData<bool>("Ready"))
		{
			ReadyLabel.text = "Cancel";
		}
		else
		{
			ReadyLabel.text = "Ready";
		}
	}

	private void LocalPlayerReadyForInput()
	{
		SelectionIndicator.gameObject.SetActive(value: false);
		InputContainerCanvasGroup.interactable = true;
		InputContainerAnimation.Play(InputContainerFadeIn.name);
	}

	private void ShowScores()
	{
		ScoresContainerAnimation.Play(InputContainerFadeIn.name);
	}

	private void HideScores()
	{
		ScoresContainerAnimation.Play(InputContainerFadeOut.name);
	}

	private void HitClicked()
	{
		SelectionIndicator.transform.position = HitButton.transform.position;
		SelectionIndicator.gameObject.SetActive(value: true);
		CurrentGame.LocalPlayerData.SetData("Action", 1f);
		InputContainerCanvasGroup.interactable = false;
		InputContainerAnimation.Play(InputContainerFadeOut.name);
	}

	private void StandClicked()
	{
		SelectionIndicator.transform.position = StandButton.transform.position;
		SelectionIndicator.gameObject.SetActive(value: true);
		CurrentGame.LocalPlayerData.SetData("Action", 2f);
		InputContainerCanvasGroup.interactable = false;
		InputContainerAnimation.Play(InputContainerFadeOut.name);
	}

	private void LocalPlayerExitRound()
	{
		HideScores();
		if (InputContainerCanvasGroup.alpha > 0f)
		{
			InputContainerCanvasGroup.interactable = false;
			InputContainerAnimation.Play(InputContainerFadeOut.name);
		}
	}

	private void ReadyButtonClicked()
	{
		CurrentGame.ToggleLocalPlayerReady();
	}

	private void OnLocalPlayerBust()
	{
		if (onBust != null)
		{
			onBust.Invoke();
		}
	}

	private void OnLocalPlayerRoundCompleted(BlackjackGameController.EPayoutType payout)
	{
		float payout2 = CurrentGame.GetPayout(CurrentGame.LocalPlayerBet, payout);
		PayoutLabel.text = MoneyManager.FormatAmount(payout2);
		switch (payout)
		{
		case BlackjackGameController.EPayoutType.None:
			if (!CurrentGame.IsLocalPlayerBust && onLose != null)
			{
				onLose.Invoke();
			}
			break;
		case BlackjackGameController.EPayoutType.Blackjack:
			PositiveOutcomeLabel.text = "Blackjack!";
			if (onBlackjack != null)
			{
				onBlackjack.Invoke();
			}
			break;
		case BlackjackGameController.EPayoutType.Win:
			PositiveOutcomeLabel.text = "Win!";
			if (onWin != null)
			{
				onWin.Invoke();
			}
			break;
		case BlackjackGameController.EPayoutType.Push:
			if (onPush != null)
			{
				onPush.Invoke();
			}
			break;
		}
	}
}
