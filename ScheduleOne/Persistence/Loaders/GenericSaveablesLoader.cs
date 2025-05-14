using System;
using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class GenericSaveablesLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (Directory.Exists(mainPath))
		{
			string[] files = Directory.GetFiles(mainPath);
			for (int i = 0; i < files.Length; i++)
			{
				if (TryLoadFile(files[i], out var contents, autoAddExtension: false))
				{
					GenericSaveData genericSaveData = null;
					try
					{
						genericSaveData = JsonUtility.FromJson<GenericSaveData>(contents);
					}
					catch (Exception ex)
					{
						Debug.LogError("Error loading generic save data: " + ex.Message);
					}
					if (genericSaveData != null)
					{
						Singleton<GenericSaveablesManager>.Instance.LoadSaveable(genericSaveData);
					}
				}
			}
		}
		if (TryLoadFile(mainPath, out var contents2))
		{
			GenericSaveablesData genericSaveablesData = JsonUtility.FromJson<GenericSaveablesData>(contents2);
			if (genericSaveablesData == null)
			{
				return;
			}
			GenericSaveData[] saveables = genericSaveablesData.Saveables;
			foreach (GenericSaveData genericSaveData2 in saveables)
			{
				if (genericSaveData2 != null)
				{
					Singleton<GenericSaveablesManager>.Instance.LoadSaveable(genericSaveData2);
				}
			}
		}
		else
		{
			Console.LogWarning("Failed to load generic saveables data from: " + mainPath);
		}
	}
}
