using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Persistence;
using ScheduleOne.Variables;
using UnityEngine;

namespace ScheduleOne.NPCs.CharacterClasses;

public class Fixer : NPC
{
	public const int ADDITIONAL_SIGNING_FEE_1 = 100;

	public const int ADDITIONAL_SIGNING_FEE_2 = 250;

	public const int MAX_SIGNING_FEE = 500;

	public const int ADDITIONAL_FEE_THRESHOLD = 5;

	public DialogueContainer GreetingDialogue;

	public string GreetedVariable = "FixerGreeted";

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted;

	protected override void Start()
	{
		base.Start();
		Singleton<LoadManager>.Instance.onLoadComplete.AddListener(Loaded);
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

	public static float GetAdditionalSigningFee()
	{
		int num = Mathf.RoundToInt(NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("LifetimeEmployeesRecruited"));
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			num2 = ((i > 5) ? (num2 + 250f) : (num2 + 100f));
		}
		return Mathf.Min(num2, 500f);
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ECharacterClasses_002EFixerAssembly_002DCSharp_002Edll_Excuted = true;
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
