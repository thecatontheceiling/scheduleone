using System;
using System.Collections.Generic;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.Management;

public class AdvancedTransitRoute : TransitRoute
{
	public ManagementItemFilter Filter { get; private set; } = new ManagementItemFilter(ManagementItemFilter.EMode.Blacklist);

	public AdvancedTransitRoute(ITransitEntity source, ITransitEntity destination)
		: base(source, destination)
	{
	}

	public AdvancedTransitRoute(AdvancedTransitRouteData data)
		: base((!string.IsNullOrEmpty(data.SourceGUID)) ? GUIDManager.GetObject<ITransitEntity>(new Guid(data.SourceGUID)) : null, (!string.IsNullOrEmpty(data.DestinationGUID)) ? GUIDManager.GetObject<ITransitEntity>(new Guid(data.DestinationGUID)) : null)
	{
		Filter.SetMode(data.FilterMode);
		for (int i = 0; i < data.FilterItemIDs.Count; i++)
		{
			ItemDefinition item = Registry.GetItem(data.FilterItemIDs[i]);
			if (item != null)
			{
				Filter.AddItem(item);
			}
		}
	}

	public ItemInstance GetItemReadyToMove()
	{
		if (base.Source == null || base.Destination == null)
		{
			return null;
		}
		foreach (ItemSlot outputSlot in base.Source.OutputSlots)
		{
			if (outputSlot.ItemInstance != null && Filter.DoesItemMeetFilter(outputSlot.ItemInstance))
			{
				int inputCapacityForItem = base.Destination.GetInputCapacityForItem(outputSlot.ItemInstance);
				if (inputCapacityForItem > 0)
				{
					return outputSlot.ItemInstance.GetCopy(Mathf.Min(inputCapacityForItem, outputSlot.ItemInstance.Quantity));
				}
			}
		}
		return null;
	}

	public AdvancedTransitRouteData GetData()
	{
		List<string> list = new List<string>();
		foreach (ItemDefinition item in Filter.Items)
		{
			list.Add(item.ID);
		}
		string sourceGUID = string.Empty;
		string destinationGUID = string.Empty;
		if (base.Source != null)
		{
			sourceGUID = base.Source.GUID.ToString();
		}
		if (base.Destination != null)
		{
			destinationGUID = base.Destination.GUID.ToString();
		}
		return new AdvancedTransitRouteData(sourceGUID, destinationGUID, Filter.Mode, list);
	}
}
