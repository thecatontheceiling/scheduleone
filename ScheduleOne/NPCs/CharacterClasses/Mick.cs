using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Variables;

namespace ScheduleOne.NPCs.CharacterClasses;

public class Mick : NPC
{
	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted;

	protected override void Start()
	{
		base.Start();
		dialogueHandler.GetComponent<DialogueController>().Choices[0].isValidCheck = CanPawn;
	}

	private bool CanPawn(out string reason)
	{
		reason = string.Empty;
		if (NetworkSingleton<VariableDatabase>.Instance.GetValue<bool>("PawnShopAngeredToday"))
		{
			reason = "Mick doesn't want to do business with you right now.";
			return false;
		}
		return true;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EMickAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
