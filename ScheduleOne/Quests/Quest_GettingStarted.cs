using ScheduleOne.Economy;
using ScheduleOne.NPCs.CharacterClasses;

namespace ScheduleOne.Quests;

public class Quest_GettingStarted : Quest
{
	public float CashAmount = 375f;

	public DeadDrop CashDrop;

	public UncleNelson Nelson;

	protected override void MinPass()
	{
		base.MinPass();
	}

	public override void SetQuestState(EQuestState state, bool network = true)
	{
		base.SetQuestState(state, network);
	}
}
