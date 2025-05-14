using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Money;
using UnityEngine;

namespace ScheduleOne.Dialogue;

public class DialogueController_Dealer : DialogueController
{
	public Dealer Dealer { get; private set; }

	protected override void Start()
	{
		base.Start();
		Dealer = npc as Dealer;
	}

	public override string ModifyDialogueText(string dialogueLabel, string dialogueText)
	{
		if (DialogueHandler.activeDialogue.name == "Supplier_Recruitment" && dialogueLabel == "ENTRY")
		{
			dialogueText = dialogueText.Replace("<SIGNING_FEE>", "<color=#54E717>" + MoneyManager.FormatAmount(Dealer.SigningFee) + "</color>");
			dialogueText = dialogueText.Replace("<CUT>", "<color=#54E717>" + Mathf.RoundToInt(Dealer.Cut * 100f) + "%</color>");
		}
		return base.ModifyDialogueText(dialogueLabel, dialogueText);
	}

	public override string ModifyChoiceText(string choiceLabel, string choiceText)
	{
		if (DialogueHandler.activeDialogue.name == "Supplier_Recruitment" && choiceLabel == "CONFIRM")
		{
			choiceText = choiceText.Replace("<SIGNING_FEE>", "<color=#54E717>" + MoneyManager.FormatAmount(Dealer.SigningFee) + "</color>");
		}
		return base.ModifyChoiceText(choiceLabel, choiceText);
	}

	public override bool CheckChoice(string choiceLabel, out string invalidReason)
	{
		if (DialogueHandler.activeDialogue.name == "Supplier_Recruitment" && choiceLabel == "CONFIRM" && NetworkSingleton<MoneyManager>.Instance.cashBalance < Dealer.SigningFee)
		{
			invalidReason = "Insufficient cash";
			return false;
		}
		return base.CheckChoice(choiceLabel, out invalidReason);
	}

	public override void ChoiceCallback(string choiceLabel)
	{
		if (DialogueHandler.activeDialogue.name == "Supplier_Recruitment" && choiceLabel == "CONFIRM")
		{
			NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(0f - Dealer.SigningFee);
			Dealer.InitialRecruitment();
		}
		base.ChoiceCallback(choiceLabel);
	}
}
