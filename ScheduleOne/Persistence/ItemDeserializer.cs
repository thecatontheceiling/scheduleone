using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.ItemLoaders;
using UnityEngine;

namespace ScheduleOne.Persistence;

public static class ItemDeserializer
{
	public static ItemInstance LoadItem(string itemString)
	{
		ItemData itemData = null;
		try
		{
			itemData = JsonUtility.FromJson<ItemData>(itemString);
		}
		catch (Exception ex)
		{
			Console.LogError("Failed to deserialize ItemData from JSON: " + itemString + "\nException: " + ex);
			return null;
		}
		if (itemData == null)
		{
			Console.LogWarning("Failed to deserialize ItemData from JSON: " + itemString);
			return null;
		}
		ItemLoader itemLoader = Singleton<LoadManager>.Instance.GetItemLoader(itemData.DataType);
		if (itemLoader == null)
		{
			Console.LogError("Failed to find item loader for " + itemData.DataType);
			return null;
		}
		return itemLoader.LoadItem(itemString);
	}
}
