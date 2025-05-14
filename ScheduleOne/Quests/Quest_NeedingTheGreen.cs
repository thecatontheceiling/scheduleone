using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;

namespace ScheduleOne.Quests;

public class Quest_NeedingTheGreen : Quest
{
	public Quest[] PrerequisiteQuests;

	public QuestEntry EarnEntry;

	public float LifetimeEarningsRequirement = 10000f;

	protected override void MinPass()
	{
		base.MinPass();
		string text = MoneyManager.FormatAmount(LifetimeEarningsRequirement);
		EarnEntry.SetEntryTitle("Earn " + text + " (" + MoneyManager.FormatAmount(NetworkSingleton<MoneyManager>.Instance.LifetimeEarnings) + " / " + text + ")");
		if (!InstanceFinder.IsServer || base.QuestState != EQuestState.Inactive)
		{
			return;
		}
		Quest[] prerequisiteQuests = PrerequisiteQuests;
		for (int i = 0; i < prerequisiteQuests.Length; i++)
		{
			if (prerequisiteQuests[i].QuestState != EQuestState.Completed)
			{
				return;
			}
		}
		Begin();
	}
}
