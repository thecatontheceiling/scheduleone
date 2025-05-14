using System;
using ScheduleOne.ItemFramework;

namespace ScheduleOne.ObjectScripts;

[Serializable]
public class DryingOperation
{
	public string ItemID;

	public int Quantity;

	public EQuality StartQuality;

	public int Time;

	public DryingOperation(string itemID, int quantity, EQuality startQuality, int time)
	{
		ItemID = itemID;
		Quantity = quantity;
		StartQuality = startQuality;
		Time = time;
	}

	public DryingOperation()
	{
	}

	public void IncreaseQuality()
	{
		StartQuality++;
		Time = 0;
	}

	public QualityItemInstance GetQualityItemInstance()
	{
		QualityItemInstance obj = Registry.GetItem(ItemID).GetDefaultInstance(Quantity) as QualityItemInstance;
		obj.SetQuality(StartQuality);
		return obj;
	}

	public EQuality GetQuality()
	{
		if (Time >= 720)
		{
			return StartQuality + 1;
		}
		return StartQuality;
	}
}
