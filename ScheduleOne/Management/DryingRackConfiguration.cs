using ScheduleOne.Employees;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Management;

public class DryingRackConfiguration : EntityConfiguration
{
	public NPCField AssignedBotanist;

	public QualityField TargetQuality;

	public ObjectField Destination;

	public DryingRack Rack { get; protected set; }

	public TransitRoute DestinationRoute { get; protected set; }

	public DryingRackConfiguration(ConfigurationReplicator replicator, IConfigurable configurable, DryingRack rack)
		: base(replicator, configurable)
	{
		Rack = rack;
		AssignedBotanist = new NPCField(this);
		AssignedBotanist.TypeRequirement = typeof(Botanist);
		AssignedBotanist.onNPCChanged.AddListener(delegate
		{
			InvokeChanged();
		});
		TargetQuality = new QualityField(this);
		TargetQuality.onValueChanged.AddListener(delegate
		{
			InvokeChanged();
		});
		TargetQuality.SetValue(EQuality.Premium, network: false);
		Destination = new ObjectField(this);
		Destination.objectFilter = DestinationFilter;
		Destination.onObjectChanged.AddListener(delegate
		{
			InvokeChanged();
		});
		Destination.onObjectChanged.AddListener(DestinationChanged);
		Destination.DrawTransitLine = true;
	}

	public override void Destroy()
	{
		base.Destroy();
		if (DestinationRoute != null)
		{
			DestinationRoute.Destroy();
			DestinationRoute = null;
		}
	}

	private void DestinationChanged(BuildableItem item)
	{
		if (DestinationRoute != null)
		{
			DestinationRoute.Destroy();
			DestinationRoute = null;
		}
		if (Destination.SelectedObject != null)
		{
			DestinationRoute = new TransitRoute(Rack, Destination.SelectedObject as ITransitEntity);
			if (base.IsSelected)
			{
				DestinationRoute.SetVisualsActive(active: true);
			}
		}
		else
		{
			DestinationRoute = null;
		}
	}

	public bool DestinationFilter(BuildableItem obj, out string reason)
	{
		reason = "";
		if (obj is ITransitEntity && (obj as ITransitEntity).Selectable && obj != Rack)
		{
			return true;
		}
		return false;
	}

	public override void Selected()
	{
		base.Selected();
		if (DestinationRoute != null)
		{
			DestinationRoute.SetVisualsActive(active: true);
		}
	}

	public override void Deselected()
	{
		base.Deselected();
		if (DestinationRoute != null)
		{
			DestinationRoute.SetVisualsActive(active: false);
		}
	}

	public override bool ShouldSave()
	{
		if (Destination.SelectedObject != null)
		{
			return true;
		}
		return base.ShouldSave();
	}

	public override string GetSaveString()
	{
		return new DryingRackConfigurationData(TargetQuality.GetData(), Destination.GetData()).GetJson();
	}
}
