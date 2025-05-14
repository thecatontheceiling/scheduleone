using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.Variables;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.NPCs.CharacterClasses;

public class Marco : NPC
{
	public Transform VehicleRecoveryPoint;

	public VehicleDetector VehicleDetector;

	public DialogueContainer RecoveryConversation;

	public DialogueContainer GreetingDialogue;

	public string GreetedVariable = "MarcoGreeted";

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ENPCs_002ECharacterClasses_002EMarco_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Start()
	{
		base.Start();
		Singleton<VehicleModMenu>.Instance.onPaintPurchased.AddListener(delegate
		{
			dialogueHandler.ShowWorldspaceDialogue_5s("Thanks buddy");
		});
		Singleton<LoadManager>.Instance.onLoadComplete.AddListener(Loaded);
	}

	private bool ShouldShowRecoverVehicle(bool enabled)
	{
		return Player.Local.LastDrivenVehicle != null;
	}

	private bool RecoverVehicleValid(out string reason)
	{
		if (Player.Local.LastDrivenVehicle == null)
		{
			reason = "You have no vehicle to recover";
			return false;
		}
		if (Player.Local.LastDrivenVehicle.isOccupied)
		{
			reason = "Someone is in the vehicle";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	private bool RepaintVehicleValid(out string reason)
	{
		if (VehicleDetector.closestVehicle == null)
		{
			reason = "Vehicle must be parked inside the shop";
			return false;
		}
		if (VehicleDetector.closestVehicle.isOccupied)
		{
			reason = "Someone is in the vehicle";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	private void RecoverVehicle()
	{
		LandVehicle lastDrivenVehicle = Player.Local.LastDrivenVehicle;
		if (!(lastDrivenVehicle == null))
		{
			lastDrivenVehicle.AlignTo(VehicleRecoveryPoint, EParkingAlignment.RearToKerb, network: true);
		}
	}

	private void Loaded()
	{
		Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(Loaded);
		if (!NetworkSingleton<VariableDatabase>.Instance.GetValue<bool>(GreetedVariable))
		{
			EnableGreeting();
		}
	}

	private void EnableGreeting()
	{
		dialogueHandler.GetComponent<DialogueController>().OverrideContainer = GreetingDialogue;
		dialogueHandler.onConversationStart.AddListener(SetGreeted);
	}

	private void SetGreeted()
	{
		dialogueHandler.onConversationStart.RemoveListener(SetGreeted);
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue(GreetedVariable, true.ToString());
		dialogueHandler.GetComponent<DialogueController>().OverrideContainer = null;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMarcoAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002ECharacterClasses_002EMarco_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		DialogueController.DialogueChoice dialogueChoice = new DialogueController.DialogueChoice();
		dialogueChoice.ChoiceText = "My vehicle is stuck";
		dialogueChoice.Enabled = true;
		dialogueChoice.shouldShowCheck = ShouldShowRecoverVehicle;
		dialogueChoice.isValidCheck = RecoverVehicleValid;
		dialogueChoice.Conversation = RecoveryConversation;
		dialogueChoice.onChoosen.AddListener(RecoverVehicle);
		DialogueController.DialogueChoice dialogueChoice2 = new DialogueController.DialogueChoice();
		dialogueChoice2.ChoiceText = "I'd like to repaint my vehicle";
		dialogueChoice2.Enabled = true;
		dialogueChoice2.isValidCheck = RepaintVehicleValid;
		dialogueChoice2.onChoosen.AddListener(delegate
		{
			Singleton<VehicleModMenu>.Instance.Open(VehicleDetector.closestVehicle);
		});
		dialogueHandler.GetComponent<DialogueController>().Choices.Add(dialogueChoice2);
		dialogueHandler.GetComponent<DialogueController>().Choices.Add(dialogueChoice);
	}
}
