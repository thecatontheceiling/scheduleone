using System.Collections.Generic;
using ScheduleOne.ItemFramework;

namespace ScheduleOne.Management;

public class ManagementItemFilter
{
	public enum EMode
	{
		Whitelist = 0,
		Blacklist = 1
	}

	public EMode Mode { get; private set; } = EMode.Blacklist;

	public List<ItemDefinition> Items { get; private set; } = new List<ItemDefinition>();

	public ManagementItemFilter(EMode mode)
	{
		Mode = mode;
		Items = new List<ItemDefinition>();
	}

	public void SetMode(EMode mode)
	{
		Mode = mode;
	}

	public void AddItem(ItemDefinition item)
	{
		Items.Add(item);
	}

	public void RemoveItem(ItemDefinition item)
	{
		Items.Remove(item);
	}

	public bool Contains(ItemDefinition item)
	{
		return Items.Contains(item);
	}

	public bool DoesItemMeetFilter(ItemInstance item)
	{
		if (Mode != EMode.Whitelist)
		{
			return !Items.Contains(item.Definition);
		}
		return Items.Contains(item.Definition);
	}

	public string GetDescription()
	{
		if (Mode == EMode.Blacklist)
		{
			if (Items.Count == 0)
			{
				return "All";
			}
			return Items.Count + " blacklisted";
		}
		if (Items.Count == 0)
		{
			return "None";
		}
		return Items.Count + " whitelisted";
	}
}
