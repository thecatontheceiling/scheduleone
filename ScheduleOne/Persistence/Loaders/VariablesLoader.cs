using System;
using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Variables;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class VariablesLoader : Loader
{
	public override void Load(string mainPath)
	{
		Console.Log("Loading variables at: " + mainPath);
		if (TryLoadFile(mainPath, out var contents))
		{
			VariableCollectionData variableCollectionData = JsonUtility.FromJson<VariableCollectionData>(contents);
			if (variableCollectionData == null)
			{
				return;
			}
			VariableData[] variables = variableCollectionData.Variables;
			foreach (VariableData variableData in variables)
			{
				if (variableData != null)
				{
					NetworkSingleton<VariableDatabase>.Instance.LoadVariable(variableData);
				}
			}
		}
		else
		{
			if (!Directory.Exists(mainPath))
			{
				return;
			}
			Console.Log("Loading legacy variables");
			string[] files = Directory.GetFiles(mainPath);
			for (int j = 0; j < files.Length; j++)
			{
				if (TryLoadFile(files[j], out var contents2, autoAddExtension: false))
				{
					VariableData variableData2 = null;
					try
					{
						variableData2 = JsonUtility.FromJson<VariableData>(contents2);
					}
					catch (Exception ex)
					{
						Debug.LogError("Error loading quest data: " + ex.Message);
					}
					if (variableData2 != null)
					{
						NetworkSingleton<VariableDatabase>.Instance.LoadVariable(variableData2);
					}
				}
			}
		}
	}
}
