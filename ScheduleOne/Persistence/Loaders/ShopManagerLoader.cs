using System;
using System.IO;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.UI.Shop;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class ShopManagerLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents))
		{
			ShopManagerData shopManagerData = null;
			try
			{
				shopManagerData = JsonUtility.FromJson<ShopManagerData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading data: " + ex.Message);
			}
			if (shopManagerData == null)
			{
				return;
			}
			ShopData[] shops = shopManagerData.Shops;
			foreach (ShopData shopData in shops)
			{
				if (shopData != null)
				{
					ShopInterface shopInterface = ShopInterface.AllShops.Find((ShopInterface x) => x.ShopCode == shopData.ShopCode);
					if (shopInterface == null)
					{
						Debug.LogError("Failed to load shop data: Shop not found: " + shopData.ShopCode);
						break;
					}
					shopInterface.Load(shopData);
				}
			}
		}
		else if (Directory.Exists(mainPath))
		{
			Console.Log("Loading legacy shops at: " + mainPath);
			ShopLoader loader = new ShopLoader();
			string[] files = Directory.GetFiles(mainPath);
			for (int num = 0; num < files.Length; num++)
			{
				Console.Log("Loading shop file: " + files[num]);
				new LoadRequest(files[num], loader);
			}
		}
	}
}
