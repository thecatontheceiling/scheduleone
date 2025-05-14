using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine.Events;

namespace ScheduleOne.Management;

public class RouteListField : ConfigField
{
	public List<AdvancedTransitRoute> Routes = new List<AdvancedTransitRoute>();

	public int MaxRoutes = 1;

	public UnityEvent<List<AdvancedTransitRoute>> onListChanged = new UnityEvent<List<AdvancedTransitRoute>>();

	public RouteListField(EntityConfiguration parentConfig)
		: base(parentConfig)
	{
	}

	public void SetList(List<AdvancedTransitRoute> list, bool network, bool bypassSequenceCheck = false)
	{
		if (!Routes.SequenceEqual(list) || bypassSequenceCheck)
		{
			Routes = new List<AdvancedTransitRoute>();
			Routes.AddRange(list);
			if (network)
			{
				base.ParentConfig.ReplicateField(this);
			}
			if (onListChanged != null)
			{
				onListChanged.Invoke(list);
			}
		}
	}

	public void Replicate()
	{
		Console.Log("Replicating route list field");
		SetList(Routes, network: true, bypassSequenceCheck: true);
	}

	public void AddItem(AdvancedTransitRoute item)
	{
		if (!Routes.Contains(item))
		{
			if (Routes.Count >= MaxRoutes)
			{
				Console.LogWarning("Route cannot be added to " + base.ParentConfig.GetType().Name + " because the maximum number of routes has been reached");
				return;
			}
			List<AdvancedTransitRoute> list = new List<AdvancedTransitRoute>(Routes);
			list.Add(item);
			SetList(list, network: true);
		}
	}

	public void RemoveItem(AdvancedTransitRoute item)
	{
		if (Routes.Contains(item))
		{
			List<AdvancedTransitRoute> list = new List<AdvancedTransitRoute>(Routes);
			list.Remove(item);
			SetList(list, network: true);
		}
	}

	public override bool IsValueDefault()
	{
		return Routes.Count == 0;
	}

	public RouteListData GetData()
	{
		List<AdvancedTransitRouteData> list = new List<AdvancedTransitRouteData>();
		for (int i = 0; i < Routes.Count; i++)
		{
			list.Add(Routes[i].GetData());
		}
		return new RouteListData(list);
	}

	public void Load(RouteListData data)
	{
		if (data == null)
		{
			return;
		}
		List<AdvancedTransitRoute> list = new List<AdvancedTransitRoute>();
		for (int i = 0; i < data.Routes.Count; i++)
		{
			if (string.IsNullOrEmpty(data.Routes[i].SourceGUID) || string.IsNullOrEmpty(data.Routes[i].DestinationGUID))
			{
				Console.LogWarning("Route data is missing source or destination GUID");
				continue;
			}
			ITransitEntity transitEntity = null;
			ITransitEntity transitEntity2 = null;
			try
			{
				transitEntity = GUIDManager.GetObject<ITransitEntity>(new Guid(data.Routes[i].SourceGUID));
				transitEntity2 = GUIDManager.GetObject<ITransitEntity>(new Guid(data.Routes[i].DestinationGUID));
			}
			catch (Exception ex)
			{
				Console.LogError("Error loading route: " + ex.Message);
				continue;
			}
			AdvancedTransitRoute advancedTransitRoute = new AdvancedTransitRoute(transitEntity, transitEntity2);
			advancedTransitRoute.Filter.SetMode(data.Routes[i].FilterMode);
			for (int j = 0; j < data.Routes[i].FilterItemIDs.Count; j++)
			{
				ItemDefinition itemDefinition = GUIDManager.GetObject<ItemDefinition>(new Guid(data.Routes[i].FilterItemIDs[j]));
				if (itemDefinition == null)
				{
					Console.LogWarning("Could not find item definition with GUID " + data.Routes[i].FilterItemIDs[j]);
				}
				else if (itemDefinition != null)
				{
					advancedTransitRoute.Filter.AddItem(itemDefinition);
				}
			}
			list.Add(advancedTransitRoute);
		}
		SetList(list, network: true);
	}
}
