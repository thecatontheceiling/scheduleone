using System.Collections.Generic;
using ScheduleOne.Management;
using ScheduleOne.Management.UI;
using UnityEngine;

namespace ScheduleOne.UI.Management;

public class DryingRackConfigPanel : ConfigPanel
{
	[Header("References")]
	public QualityFieldUI QualityUI;

	public ObjectFieldUI DestinationUI;

	public override void Bind(List<EntityConfiguration> configs)
	{
		List<QualityField> list = new List<QualityField>();
		List<ObjectField> list2 = new List<ObjectField>();
		foreach (DryingRackConfiguration config in configs)
		{
			if (config == null)
			{
				Console.LogError("Failed to cast EntityConfiguration to DryingRackConfiguration");
				return;
			}
			list.Add(config.TargetQuality);
			list2.Add(config.Destination);
		}
		QualityUI.Bind(list);
		DestinationUI.Bind(list2);
	}
}
