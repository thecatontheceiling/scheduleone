using System;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Product;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class MethProductLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents, autoAddExtension: false))
		{
			MethProductData methProductData = null;
			try
			{
				methProductData = JsonUtility.FromJson<MethProductData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading product data: " + ex.Message);
			}
			if (methProductData != null)
			{
				NetworkSingleton<ProductManager>.Instance.CreateMeth_Server(methProductData.Name, methProductData.ID, methProductData.DrugType, methProductData.Properties.ToList(), methProductData.AppearanceSettings);
			}
		}
	}
}
