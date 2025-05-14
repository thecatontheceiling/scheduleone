using ScheduleOne.Economy;

namespace ScheduleOne.Dialogue;

public class DialogueController_Supplier : DialogueController
{
	public Supplier Supplier { get; private set; }

	protected override void Start()
	{
		base.Start();
		Supplier = npc as Supplier;
	}
}
