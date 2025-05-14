using System.Collections.Generic;
using ScheduleOne.Management;
using ScheduleOne.Management.UI;
using UnityEngine;

namespace ScheduleOne.UI.Management;

public class PackagerConfigPanel : ConfigPanel
{
	[Header("References")]
	public ObjectFieldUI BedUI;

	public ObjectListFieldUI StationsUI;

	public RouteListFieldUI RoutesUI;

	public override void Bind(List<EntityConfiguration> configs)
	{
		List<ObjectField> list = new List<ObjectField>();
		List<ObjectListField> list2 = new List<ObjectListField>();
		List<RouteListField> list3 = new List<RouteListField>();
		foreach (PackagerConfiguration config in configs)
		{
			if (config == null)
			{
				Console.LogError("Failed to cast EntityConfiguration to PackagerConfiguration");
				return;
			}
			list.Add(config.Bed);
			list2.Add(config.Stations);
			list3.Add(config.Routes);
		}
		BedUI.Bind(list);
		StationsUI.Bind(list2);
		RoutesUI.Bind(list3);
	}
}
