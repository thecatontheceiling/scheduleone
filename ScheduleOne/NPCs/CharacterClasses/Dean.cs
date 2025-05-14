using ScheduleOne.Dialogue;

namespace ScheduleOne.NPCs.CharacterClasses;

public class Dean : NPC
{
	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ENPCs_002ECharacterClasses_002EDean_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	public bool TattooChoiceValid(out string reason)
	{
		reason = string.Empty;
		return true;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EDeanAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002ECharacterClasses_002EDean_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		dialogueHandler.GetComponent<DialogueController>().Choices[0].isValidCheck = TattooChoiceValid;
	}
}
