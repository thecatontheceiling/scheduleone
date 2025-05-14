using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.NPCs.CharacterClasses;

namespace ScheduleOne.Dialogue;

public class DialogueHandler_VehicleSalesman : ControlledDialogueHandler
{
	public Jeremy Salesman;

	public Jeremy.DealershipListing selectedVehicle;

	protected override string ModifyChoiceText(string choiceLabel, string choiceText)
	{
		Jeremy.DealershipListing dealershipListing = Salesman.Listings.Find((Jeremy.DealershipListing x) => x.vehicleCode.ToLower() == choiceLabel.ToLower());
		if (choiceLabel == "BUY_CASH")
		{
			if (selectedVehicle != null)
			{
				choiceText = choiceText.Replace("(<PRICE>)", "<color=#19BEF0>(" + MoneyManager.FormatAmount(selectedVehicle.price) + ")</color>");
			}
		}
		else if (choiceLabel == "BUY_ONLINE")
		{
			if (selectedVehicle != null)
			{
				choiceText = choiceText.Replace("(<PRICE>)", "<color=#19BEF0>(" + MoneyManager.FormatAmount(selectedVehicle.price) + ")</color>");
			}
		}
		else if (dealershipListing != null)
		{
			choiceText = choiceText.Replace("(<PRICE>)", "<color=#19BEF0>(" + MoneyManager.FormatAmount(dealershipListing.price) + ")</color>");
		}
		return base.ModifyChoiceText(choiceLabel, choiceText);
	}

	public override bool CheckChoice(string choiceLabel, out string invalidReason)
	{
		if (choiceLabel == "BUY_CASH")
		{
			if (NetworkSingleton<MoneyManager>.Instance.cashBalance < selectedVehicle.price)
			{
				invalidReason = "Insufficient cash";
				return false;
			}
		}
		else if (choiceLabel == "BUY_ONLINE" && NetworkSingleton<MoneyManager>.Instance.SyncAccessor_onlineBalance < selectedVehicle.price)
		{
			invalidReason = "Insufficient balance";
			return false;
		}
		return base.CheckChoice(choiceLabel, out invalidReason);
	}

	protected override void ChoiceCallback(string choiceLabel)
	{
		if (choiceLabel == "BUY_CASH")
		{
			if (NetworkSingleton<MoneyManager>.Instance.cashBalance >= selectedVehicle.price)
			{
				NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(0f - selectedVehicle.price);
				Salesman.Dealership.SpawnVehicle(selectedVehicle.vehicleCode);
			}
			return;
		}
		if (choiceLabel == "BUY_ONLINE")
		{
			if (NetworkSingleton<MoneyManager>.Instance.SyncAccessor_onlineBalance >= selectedVehicle.price)
			{
				NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction(selectedVehicle.vehicleCode + " purchase", 0f - selectedVehicle.price, 1f, string.Empty);
				Salesman.Dealership.SpawnVehicle(selectedVehicle.vehicleCode);
			}
			return;
		}
		Jeremy.DealershipListing dealershipListing = Salesman.Listings.Find((Jeremy.DealershipListing x) => x.vehicleCode.ToLower() == choiceLabel.ToLower());
		if (dealershipListing != null)
		{
			selectedVehicle = dealershipListing;
		}
		base.ChoiceCallback(choiceLabel);
	}

	protected override int CheckBranch(string branchLabel)
	{
		if (branchLabel == "BRANCH_CAN_AFFORD")
		{
			if (selectedVehicle == null)
			{
				return 0;
			}
			if (NetworkSingleton<MoneyManager>.Instance.SyncAccessor_onlineBalance >= selectedVehicle.price)
			{
				return 1;
			}
			return 0;
		}
		return base.CheckBranch(branchLabel);
	}

	protected override void DialogueCallback(string choiceLabel)
	{
		base.DialogueCallback(choiceLabel);
	}

	protected override string ModifyDialogueText(string dialogueLabel, string dialogueText)
	{
		if (dialogueLabel == "CONFIRM")
		{
			return dialogueText.Replace("<VEHICLE>", selectedVehicle.vehicleName);
		}
		return base.ModifyDialogueText(dialogueLabel, dialogueText);
	}
}
