using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.Employees;
using ScheduleOne.EntityFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Management;

public class BotanistConfiguration : EntityConfiguration
{
	public ObjectField Bed;

	public ObjectField Supplies;

	public ObjectListField AssignedStations;

	public List<Pot> AssignedPots = new List<Pot>();

	public List<DryingRack> AssignedRacks = new List<DryingRack>();

	public Botanist botanist { get; protected set; }

	public BedItem bedItem { get; private set; }

	public BotanistConfiguration(ConfigurationReplicator replicator, IConfigurable configurable, Botanist _botanist)
		: base(replicator, configurable)
	{
		botanist = _botanist;
		Bed = new ObjectField(this);
		Bed.TypeRequirements = new List<Type> { typeof(BedItem) };
		Bed.onObjectChanged.AddListener(BedChanged);
		Bed.objectFilter = BedItem.IsBedValid;
		Supplies = new ObjectField(this);
		Supplies.TypeRequirements = new List<Type> { typeof(PlaceableStorageEntity) };
		Supplies.onObjectChanged.AddListener(delegate
		{
			InvokeChanged();
		});
		AssignedStations = new ObjectListField(this);
		AssignedStations.MaxItems = botanist.MaxAssignedPots;
		AssignedStations.TypeRequirements = new List<Type>
		{
			typeof(Pot),
			typeof(DryingRack)
		};
		AssignedStations.onListChanged.AddListener(delegate
		{
			InvokeChanged();
		});
		AssignedStations.onListChanged.AddListener(AssignedPotsChanged);
		AssignedStations.objectFilter = IsStationValid;
	}

	public override void Destroy()
	{
		base.Destroy();
		Bed.SetObject(null, network: false);
		foreach (Pot assignedPot in AssignedPots)
		{
			(assignedPot.Configuration as PotConfiguration).AssignedBotanist.SetNPC(null, network: false);
		}
		foreach (DryingRack assignedRack in AssignedRacks)
		{
			(assignedRack.Configuration as DryingRackConfiguration).AssignedBotanist.SetNPC(null, network: false);
		}
	}

	private bool IsStationValid(BuildableItem obj, out string reason)
	{
		reason = string.Empty;
		Pot pot = obj as Pot;
		DryingRack dryingRack = obj as DryingRack;
		if (pot != null)
		{
			PotConfiguration potConfiguration = pot.Configuration as PotConfiguration;
			if (potConfiguration.AssignedBotanist.SelectedNPC != null && potConfiguration.AssignedBotanist.SelectedNPC != botanist)
			{
				reason = "Already assigned to " + potConfiguration.AssignedBotanist.SelectedNPC.fullName;
				return false;
			}
			return true;
		}
		if (dryingRack != null)
		{
			DryingRackConfiguration dryingRackConfiguration = dryingRack.Configuration as DryingRackConfiguration;
			if (dryingRackConfiguration.AssignedBotanist.SelectedNPC != null && dryingRackConfiguration.AssignedBotanist.SelectedNPC != botanist)
			{
				reason = "Already assigned to " + dryingRackConfiguration.AssignedBotanist.SelectedNPC.fullName;
				return false;
			}
			return true;
		}
		reason = "Not a pot or drying rack";
		return false;
	}

	public void AssignedPotsChanged(List<BuildableItem> objects)
	{
		for (int i = 0; i < AssignedPots.Count; i++)
		{
			if (!objects.Contains(AssignedPots[i]))
			{
				Pot pot = AssignedPots[i];
				AssignedPots.RemoveAt(i);
				i--;
				if ((pot.Configuration as PotConfiguration).AssignedBotanist.SelectedNPC == botanist)
				{
					(pot.Configuration as PotConfiguration).AssignedBotanist.SetNPC(null, network: false);
				}
			}
		}
		for (int j = 0; j < objects.Count; j++)
		{
			if (objects[j] is Pot)
			{
				if (!AssignedPots.Contains(objects[j]))
				{
					Pot pot2 = objects[j] as Pot;
					AssignedPots.Add(pot2);
					if ((pot2.Configuration as PotConfiguration).AssignedBotanist.SelectedNPC != botanist)
					{
						(pot2.Configuration as PotConfiguration).AssignedBotanist.SetNPC(botanist, network: false);
					}
				}
			}
			else if (objects[j] is DryingRack && !AssignedRacks.Contains(objects[j]))
			{
				DryingRack dryingRack = objects[j] as DryingRack;
				AssignedRacks.Add(dryingRack);
				if ((dryingRack.Configuration as DryingRackConfiguration).AssignedBotanist.SelectedNPC != botanist)
				{
					(dryingRack.Configuration as DryingRackConfiguration).AssignedBotanist.SetNPC(botanist, network: false);
				}
			}
		}
	}

	public override bool ShouldSave()
	{
		if (AssignedPots.Count > 0)
		{
			return true;
		}
		if (AssignedRacks.Count > 0)
		{
			return true;
		}
		if (Supplies.SelectedObject != null)
		{
			return true;
		}
		if (Bed.SelectedObject != null)
		{
			return true;
		}
		return base.ShouldSave();
	}

	public override string GetSaveString()
	{
		return new BotanistConfigurationData(Bed.GetData(), Supplies.GetData(), AssignedStations.GetData()).GetJson();
	}

	private void BedChanged(BuildableItem newItem)
	{
		BedItem bedItem = this.bedItem;
		if (bedItem != null)
		{
			bedItem.Bed.SetAssignedEmployee(null);
		}
		this.bedItem = ((newItem != null) ? (newItem as BedItem) : null);
		if (this.bedItem != null)
		{
			this.bedItem.Bed.SetAssignedEmployee(botanist);
		}
		InvokeChanged();
	}
}
