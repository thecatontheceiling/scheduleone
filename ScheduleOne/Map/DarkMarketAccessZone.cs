using ScheduleOne.DevUtilities;

namespace ScheduleOne.Map;

public class DarkMarketAccessZone : TimedAccessZone
{
	protected override bool GetIsOpen()
	{
		if (!NetworkSingleton<DarkMarket>.Instance.IsOpen || !NetworkSingleton<DarkMarket>.Instance.Unlocked)
		{
			return false;
		}
		return base.GetIsOpen();
	}
}
