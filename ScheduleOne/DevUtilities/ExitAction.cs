namespace ScheduleOne.DevUtilities;

public class ExitAction
{
	public ExitType exitType;

	private bool used;

	public bool Used
	{
		get
		{
			return used;
		}
		set
		{
			if (value)
			{
				Use();
			}
		}
	}

	public void Use()
	{
		used = true;
	}
}
