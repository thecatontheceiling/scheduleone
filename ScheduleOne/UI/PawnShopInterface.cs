using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Items;
using ScheduleOne.Variables;
using ScheduleOne.VoiceOver;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI;

public class PawnShopInterface : Singleton<PawnShopInterface>
{
	public enum EState
	{
		WaitingForOffer = 0,
		Negotiating = 1
	}

	public enum EPlayerResponse
	{
		None = 0,
		Accept = 1,
		Counter = 2,
		Cancel = 3
	}

	public enum EShopResponse
	{
		Accept = 0,
		Counter = 1,
		Refusal = 2
	}

	public const float PAYMENT_MIN = 1f;

	public const float PAYMENT_MAX = 999999f;

	public const float THINK_TIME = 0.75f;

	public const float MIN_VALUE_MULTIPLIER = 0.5f;

	public const float MAX_VALUE_MULTIPLIER = 2f;

	public const int PAWN_SLOT_COUNT = 5;

	private EState CurrentState;

	private EPlayerResponse PlayerResponse;

	private int CurrentNegotiationRound;

	private float InitialShopOffer;

	private float LastShopOffer;

	private float LastRefusedAmount;

	public NPC PawnShopNPC;

	public AnimationCurve RandomCurve;

	[Header("References")]
	public Canvas Canvas;

	public RectTransform Container;

	public ItemSlotUI[] Slots;

	public TextMeshProUGUI[] ValueRangeLabels;

	public TextMeshProUGUI TotalValueLabel;

	public Button StartButton;

	public Animation Step1Animation;

	public CanvasGroup Step1CanvasGroup;

	public Animation Step2Animation;

	public CanvasGroup Step2CanvasGroup;

	public AnimationClip FadeInAnim;

	public AnimationClip FadeOutAnim;

	public TMP_InputField OfferInputField;

	public Slider AngerSlider;

	public TextMeshProUGUI AcceptCounterButtonLabel;

	[Header("Settings")]
	public string[] OfferLines;

	public string[] ThinkLines;

	public string[] AcceptLines;

	public string[] CounterLines;

	public string[] RefusalLines;

	public string[] DealFinalizedLines;

	public string[] AngeredLines;

	public string[] CrashOutLines;

	private ItemSlot[] PawnSlots;

	private Coroutine routine;

	public bool IsOpen { get; private set; }

	public float SelectedPayment { get; private set; }

	public float NPCAnger { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		PawnSlots = new ItemSlot[5];
		for (int i = 0; i < 5; i++)
		{
			PawnSlots[i] = new ItemSlot();
			PawnSlots[i].AddFilter(new ItemFilter_LegalStatus(ELegalStatus.Legal));
			ItemFilter_ID itemFilter_ID = new ItemFilter_ID(new List<string> { "cash" });
			itemFilter_ID.IsWhitelist = false;
			PawnSlots[i].AddFilter(itemFilter_ID);
			ItemSlot obj = PawnSlots[i];
			obj.onItemDataChanged = (Action)Delegate.Combine(obj.onItemDataChanged, new Action(PawnSlotChanged));
			Slots[i].AssignSlot(PawnSlots[i]);
		}
		GameInput.RegisterExitListener(Exit, 3);
		StartButton.onClick.AddListener(StartButtonPressed);
		Canvas.enabled = false;
		Container.gameObject.SetActive(value: false);
	}

	protected override void Start()
	{
		base.Start();
		TimeManager timeManager = NetworkSingleton<TimeManager>.Instance;
		timeManager.onMinutePass = (Action)Delegate.Remove(timeManager.onMinutePass, new Action(OnMinPass));
		TimeManager timeManager2 = NetworkSingleton<TimeManager>.Instance;
		timeManager2.onMinutePass = (Action)Delegate.Combine(timeManager2.onMinutePass, new Action(OnMinPass));
		TimeManager timeManager3 = NetworkSingleton<TimeManager>.Instance;
		timeManager3.onDayPass = (Action)Delegate.Remove(timeManager3.onDayPass, new Action(OnDayPass));
		TimeManager timeManager4 = NetworkSingleton<TimeManager>.Instance;
		timeManager4.onDayPass = (Action)Delegate.Combine(timeManager4.onDayPass, new Action(OnDayPass));
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkSingleton<TimeManager>.InstanceExists)
		{
			TimeManager timeManager = NetworkSingleton<TimeManager>.Instance;
			timeManager.onMinutePass = (Action)Delegate.Remove(timeManager.onMinutePass, new Action(OnMinPass));
			TimeManager timeManager2 = NetworkSingleton<TimeManager>.Instance;
			timeManager2.onDayPass = (Action)Delegate.Remove(timeManager2.onDayPass, new Action(OnDayPass));
		}
	}

	public void Open()
	{
		IsOpen = true;
		PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(enabled: true);
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
		PlayerSingleton<PlayerMovement>.Instance.canMove = false;
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: false);
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
		Singleton<ItemUIManager>.Instance.SetDraggingEnabled(enabled: true);
		Singleton<ItemUIManager>.Instance.EnableQuickMove(new List<ItemSlot>(PlayerSingleton<PlayerInventory>.Instance.GetAllInventorySlots()), PawnSlots.ToList());
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: false);
		UpdateValueRangeLabels();
		CurrentState = EState.WaitingForOffer;
		ResetUI();
		Canvas.enabled = true;
		Container.gameObject.SetActive(value: true);
	}

	public void Close(bool returnItemsToPlayer)
	{
		ResetUI();
		IsOpen = false;
		Canvas.enabled = false;
		Container.gameObject.SetActive(value: false);
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: true);
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		PlayerSingleton<PlayerCamera>.Instance.SetDoFActive(active: false, 0f);
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: true);
		Singleton<ItemUIManager>.Instance.SetDraggingEnabled(enabled: false);
		if (routine != null)
		{
			StopCoroutine(routine);
			routine = null;
		}
		ItemSlot[] pawnSlots;
		if (returnItemsToPlayer)
		{
			pawnSlots = PawnSlots;
			foreach (ItemSlot itemSlot in pawnSlots)
			{
				if (itemSlot.ItemInstance != null)
				{
					PlayerSingleton<PlayerInventory>.Instance.AddItemToInventory(itemSlot.ItemInstance.GetCopy());
				}
			}
		}
		pawnSlots = PawnSlots;
		for (int i = 0; i < pawnSlots.Length; i++)
		{
			pawnSlots[i].ClearStoredInstance();
		}
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: true);
	}

	private void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen && action.exitType == ExitType.Escape)
		{
			action.Used = true;
			if (CurrentState == EState.Negotiating)
			{
				EndNegotiation();
			}
			else
			{
				Close(returnItemsToPlayer: true);
			}
		}
	}

	private void OnMinPass()
	{
		ChangeAnger(-0.0013888889f);
	}

	private void OnDayPass()
	{
		SetAngeredToday(angered: false);
	}

	private void Update()
	{
		if (!IsOpen)
		{
			return;
		}
		if (Player.Local.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
		{
			Close(returnItemsToPlayer: true);
			return;
		}
		if (CurrentState == EState.WaitingForOffer)
		{
			StartButton.interactable = GetPawnItems().Count > 0;
		}
		else if (CurrentState == EState.Negotiating)
		{
			if (Mathf.Abs(SelectedPayment - LastShopOffer) <= 0.5f)
			{
				AcceptCounterButtonLabel.text = "ACCEPT";
			}
			else
			{
				AcceptCounterButtonLabel.text = "COUNTER";
			}
		}
		AngerSlider.value = Mathf.Lerp(AngerSlider.value, 0.1f + NPCAnger * 0.9f, Time.deltaTime * 2f);
	}

	private List<ItemInstance> GetPawnItems()
	{
		List<ItemInstance> list = new List<ItemInstance>();
		ItemSlot[] pawnSlots = PawnSlots;
		foreach (ItemSlot itemSlot in pawnSlots)
		{
			if (itemSlot.ItemInstance != null)
			{
				list.Add(itemSlot.ItemInstance);
			}
		}
		return list;
	}

	private void PawnSlotChanged()
	{
		UpdateValueRangeLabels();
	}

	private void UpdateValueRangeLabels()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < PawnSlots.Length; i++)
		{
			if (PawnSlots[i].ItemInstance == null)
			{
				ValueRangeLabels[i].enabled = false;
				continue;
			}
			StorableItemDefinition storableItemDefinition = PawnSlots[i].ItemInstance.Definition as StorableItemDefinition;
			float num3 = storableItemDefinition.BasePurchasePrice * storableItemDefinition.ResellMultiplier * (float)PawnSlots[i].ItemInstance.Quantity;
			float num4 = num3 * 0.5f;
			float num5 = num3 * 2f;
			ValueRangeLabels[i].text = $"{MoneyManager.FormatAmount(num4)} - {MoneyManager.FormatAmount(num5)}";
			num += num4;
			num2 += num5;
		}
		TotalValueLabel.text = "Total: <color=#FFD755>" + $"{MoneyManager.FormatAmount(num)} - {MoneyManager.FormatAmount(num2)}" + "</color>";
	}

	public void StartButtonPressed()
	{
		StartNegotiation();
	}

	private void StartNegotiation()
	{
		if (CurrentState == EState.WaitingForOffer)
		{
			CurrentState = EState.Negotiating;
			CurrentNegotiationRound = 0;
			LastRefusedAmount = float.MaxValue;
			routine = StartCoroutine(NegotiationRoutine());
		}
		IEnumerator NegotiationRoutine()
		{
			Step1Animation.Play(FadeOutAnim.name);
			Think();
			yield return new WaitForSeconds(Step1Animation[FadeOutAnim.name].length);
			yield return new WaitForSeconds(0.75f);
			InitialShopOffer = RoundOffer(GetTotalValue());
			SetOffer(InitialShopOffer);
			SetPlayerResponse(EPlayerResponse.None);
			Step2Animation.Play(FadeInAnim.name);
			yield return new WaitUntil(() => PlayerResponse != EPlayerResponse.None);
			switch (PlayerResponse)
			{
			case EPlayerResponse.Accept:
				FinalizeDeal(InitialShopOffer);
				yield break;
			case EPlayerResponse.Cancel:
				EndNegotiation();
				yield break;
			}
			while (true)
			{
				Step2Animation.Play(FadeOutAnim.name);
				float counter;
				float angerChange;
				EShopResponse shopResponse = EvaluateCounter(LastShopOffer, SelectedPayment, out counter, out angerChange);
				Console.Log("Shop response: " + shopResponse.ToString() + " - Counter: " + counter + " - Anger change: " + angerChange);
				ChangeAnger(angerChange);
				if (NPCAnger >= 1f)
				{
					break;
				}
				Think();
				yield return new WaitForSeconds(Step1Animation[FadeOutAnim.name].length);
				yield return new WaitForSeconds(0.75f);
				SetOffer(counter);
				SetPlayerResponse(EPlayerResponse.None);
				PlayShopResponse(shopResponse, counter);
				Step2Animation.Play(FadeInAnim.name);
				yield return new WaitUntil(() => PlayerResponse != EPlayerResponse.None);
				switch (PlayerResponse)
				{
				case EPlayerResponse.Accept:
					FinalizeDeal(SelectedPayment);
					yield break;
				case EPlayerResponse.Cancel:
					EndNegotiation();
					yield break;
				}
				CurrentNegotiationRound++;
			}
		}
	}

	private void PlayShopResponse(EShopResponse response, float counter)
	{
		switch (response)
		{
		case EShopResponse.Accept:
		{
			string text2 = AcceptLines[UnityEngine.Random.Range(0, AcceptLines.Length)];
			PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text2, 30f);
			break;
		}
		case EShopResponse.Counter:
			CounterLines[UnityEngine.Random.Range(0, CounterLines.Length)].Replace("<AMOUNT>", MoneyManager.FormatAmount(counter));
			break;
		case EShopResponse.Refusal:
		{
			string text = RefusalLines[UnityEngine.Random.Range(0, RefusalLines.Length)];
			text = text.Replace("<AMOUNT>", MoneyManager.FormatAmount(counter));
			PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text, 30f);
			PawnShopNPC.PlayVO(EVOLineType.No);
			break;
		}
		}
	}

	private EShopResponse EvaluateCounter(float lastShopOffer, float playerOffer, out float counterAmount, out float angerChange)
	{
		counterAmount = playerOffer;
		angerChange = 0f;
		float num = playerOffer / InitialShopOffer;
		float num2 = playerOffer / lastShopOffer;
		Console.Log("Original ratio: " + num + " - Last ratio: " + num2);
		float num3 = Mathf.Clamp01(2f - (num + num2) / 2f);
		float num4 = Mathf.Clamp(num3, 0f, 0.9f);
		num4 *= Mathf.Clamp01(1f - NPCAnger * 0.5f);
		num4 *= Mathf.Clamp01(1f - (float)CurrentNegotiationRound * 0.1f);
		Console.Log("Accept chance: " + num4);
		float num5 = UnityEngine.Random.Range(0f, 1f);
		angerChange = Mathf.Clamp01(1f - num3) * 0.7f;
		if (playerOffer <= lastShopOffer)
		{
			return EShopResponse.Accept;
		}
		if (playerOffer >= LastRefusedAmount)
		{
			counterAmount = lastShopOffer;
			return EShopResponse.Refusal;
		}
		if (num5 <= num4)
		{
			float num6 = Mathf.Sqrt(num4);
			if (UnityEngine.Random.Range(0f, 1f) <= num6)
			{
				angerChange *= 0.5f;
				return EShopResponse.Accept;
			}
			angerChange *= 0.75f;
			float offer = Mathf.Lerp(lastShopOffer, playerOffer, UnityEngine.Random.Range(0f, num3));
			counterAmount = RoundOffer(offer);
			return EShopResponse.Counter;
		}
		LastRefusedAmount = playerOffer;
		counterAmount = lastShopOffer;
		return EShopResponse.Refusal;
	}

	private void EndNegotiation()
	{
		if (routine != null)
		{
			StopCoroutine(routine);
			routine = null;
		}
		CurrentState = EState.WaitingForOffer;
		PawnShopNPC.dialogueHandler.HideWorldspaceDialogue();
		ResetUI();
	}

	public void PaymentSubmitted(string value)
	{
		if (float.TryParse(value, out var result))
		{
			SetSelectedPayment(result);
		}
		else
		{
			SetSelectedPayment(SelectedPayment);
		}
	}

	public void ChangePayment(float change)
	{
		SetSelectedPayment(SelectedPayment + change);
	}

	public void SetSelectedPayment(float amount)
	{
		Console.Log("Setting selected payment: " + amount);
		SelectedPayment = Mathf.RoundToInt(Mathf.Clamp(amount, 1f, 999999f));
		OfferInputField.SetTextWithoutNotify(SelectedPayment.ToString());
	}

	public void SetPlayerResponse(EPlayerResponse response)
	{
		Console.Log("Player response: " + response);
		PlayerResponse = response;
	}

	public void AcceptOrCounter()
	{
		if (Mathf.Abs(SelectedPayment - LastShopOffer) <= 0.5f)
		{
			SetPlayerResponse(EPlayerResponse.Accept);
		}
		else
		{
			SetPlayerResponse(EPlayerResponse.Counter);
		}
	}

	public void Cancel()
	{
		SetPlayerResponse(EPlayerResponse.Cancel);
	}

	private void ChangeAnger(float change)
	{
		NPCAnger = Mathf.Clamp01(NPCAnger + change);
		if (NPCAnger >= 0.8f)
		{
			PawnShopNPC.Avatar.EmotionManager.AddEmotionOverride("Angry", "pawn_angry", 0f, 2);
		}
		else if (NPCAnger >= 0.5f)
		{
			PawnShopNPC.Avatar.EmotionManager.AddEmotionOverride("Annoyed", "pawn_annoyed", 0f, 1);
		}
		else
		{
			PawnShopNPC.Avatar.EmotionManager.RemoveEmotionOverride("pawn_angry");
			PawnShopNPC.Avatar.EmotionManager.RemoveEmotionOverride("pawn_annoyed");
		}
		if (NPCAnger >= 1f)
		{
			Console.Log("NPC is angry! Closing shop.");
			SetAngeredToday(angered: true);
			if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
			{
				string text = AngeredLines[UnityEngine.Random.Range(0, AngeredLines.Length)];
				PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text, 5f);
			}
			else
			{
				string text2 = CrashOutLines[UnityEngine.Random.Range(0, CrashOutLines.Length)];
				PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text2, 5f);
				PawnShopNPC.behaviour.CombatBehaviour.SetTarget(null, Player.Local.NetworkObject);
				PawnShopNPC.behaviour.CombatBehaviour.Enable_Networked(null);
			}
			PawnShopNPC.PlayVO(EVOLineType.Angry);
			Close(returnItemsToPlayer: true);
		}
	}

	private void SetAngeredToday(bool angered)
	{
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("PawnShopAngeredToday", angered.ToString());
	}

	private void Think()
	{
		string text = ThinkLines[UnityEngine.Random.Range(0, ThinkLines.Length)];
		PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text, 3f);
		PawnShopNPC.PlayVO(EVOLineType.Think);
	}

	private void SetOffer(float amount)
	{
		Console.Log("Setting offer: " + amount);
		string text = OfferLines[UnityEngine.Random.Range(0, OfferLines.Length)];
		text = text.Replace("<AMOUNT>", MoneyManager.FormatAmount(amount));
		LastShopOffer = amount;
		SetSelectedPayment(amount);
		PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text, 30f);
	}

	private void FinalizeDeal(float amount)
	{
		NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(amount, visualizeChange: true, playCashSound: true);
		string text = DealFinalizedLines[UnityEngine.Random.Range(0, DealFinalizedLines.Length)];
		PawnShopNPC.dialogueHandler.ShowWorldspaceDialogue(text, 5f);
		PawnShopNPC.PlayVO(EVOLineType.Acknowledge);
		Close(returnItemsToPlayer: false);
	}

	private float GetTotalValue()
	{
		float num = 0f;
		ItemSlot[] pawnSlots = PawnSlots;
		foreach (ItemSlot itemSlot in pawnSlots)
		{
			if (itemSlot.ItemInstance != null)
			{
				num += GetItemValue(itemSlot.ItemInstance);
			}
		}
		return num;
	}

	private float RoundOffer(float offer)
	{
		if (offer <= 25f)
		{
			return offer;
		}
		if (offer <= 100f)
		{
			return Mathf.Round(offer / 5f) * 5f;
		}
		if (offer <= 1000f)
		{
			return Mathf.Round(offer / 10f) * 10f;
		}
		if (offer <= 10000f)
		{
			return Mathf.Round(offer / 50f) * 50f;
		}
		if (offer <= 100000f)
		{
			return Mathf.Round(offer / 100f) * 100f;
		}
		if (offer <= 1000000f)
		{
			return Mathf.Round(offer / 500f) * 500f;
		}
		return Mathf.Round(offer / 1000f) * 1000f;
	}

	private float GetItemValue(ItemInstance item)
	{
		StorableItemDefinition storableItemDefinition = item.Definition as StorableItemDefinition;
		float num = storableItemDefinition.BasePurchasePrice * storableItemDefinition.ResellMultiplier * (float)item.Quantity;
		int hashCode = (item.Name[0].ToString() + NetworkSingleton<TimeManager>.Instance.DayIndex).GetHashCode();
		float time = Mathf.Lerp(0.5f, 2f, Mathf.InverseLerp(-2.1474836E+09f, 2.1474836E+09f, hashCode));
		float num2 = RandomCurve.Evaluate(time);
		Console.Log("Value multiplier: " + time + " -> " + num2);
		return num * num2;
	}

	private void ResetUI()
	{
		Step1CanvasGroup.alpha = 1f;
		Step1CanvasGroup.interactable = true;
		Step1CanvasGroup.blocksRaycasts = true;
		Step2CanvasGroup.alpha = 0f;
		Step2CanvasGroup.interactable = false;
		Step2CanvasGroup.blocksRaycasts = false;
	}
}
