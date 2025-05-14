using System.Collections.Generic;

namespace ScheduleOne.ItemFramework;

public class ItemFilter_ID : ItemFilter
{
	public bool IsWhitelist = true;

	public List<string> IDs = new List<string>();

	public ItemFilter_ID(List<string> ids)
	{
		IDs = ids;
	}

	public override bool DoesItemMatchFilter(ItemInstance instance)
	{
		if (instance == null)
		{
			return false;
		}
		if (IsWhitelist)
		{
			if (!IDs.Contains(instance.ID))
			{
				return false;
			}
		}
		else if (IDs.Contains(instance.ID))
		{
			return false;
		}
		return base.DoesItemMatchFilter(instance);
	}
}
