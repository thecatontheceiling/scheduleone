using System;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class ShopLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents, autoAddExtension: false))
		{
			Console.Log("Loading shop file a: " + mainPath);
			ShopData data = null;
			try
			{
				data = JsonUtility.FromJson<ShopData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to load shop data: " + ex.Message);
			}
			if (data != null)
			{
				Console.Log("Found shop data");
				ShopInterface shopInterface = ShopInterface.AllShops.Find((ShopInterface x) => x.ShopCode == data.ShopCode);
				if (shopInterface == null)
				{
					Debug.LogError("Failed to load shop data: Shop not found: " + data.ShopCode);
				}
				else
				{
					shopInterface.Load(data);
				}
			}
		}
		else
		{
			Console.Log("Failed to load shop file: " + mainPath);
		}
	}
}
