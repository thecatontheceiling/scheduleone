using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Dialogue;

public class DialogueController_Dan : DialogueController
{
	public ItemDefinition ItemToGive;

	protected override void Start()
	{
		base.Start();
		if (ItemToGive == null)
		{
			Debug.LogWarning("ItemToGive is not set in the inspector.");
		}
	}

	public override string ModifyDialogueText(string dialogueLabel, string dialogueText)
	{
		if (dialogueLabel == "GIVE_ITEM" && ItemToGive != null)
		{
			PlayerSingleton<PlayerInventory>.Instance.AddItemToInventory(ItemToGive.GetDefaultInstance());
		}
		return base.ModifyDialogueText(dialogueLabel, dialogueText);
	}
}
