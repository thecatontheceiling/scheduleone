using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;

namespace ScheduleOne.Persistence;

public class GenericSaveablesManager : Singleton<GenericSaveablesManager>, IBaseSaveable, ISaveable
{
	protected List<IGenericSaveable> Saveables = new List<IGenericSaveable>();

	private GenericSaveablesLoader loader = new GenericSaveablesLoader();

	public string SaveFolderName => "GenericSaveables";

	public string SaveFileName => "GenericSaveables";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; } = true;

	protected override void Awake()
	{
		base.Awake();
		InitializeSaveable();
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	public void RegisterSaveable(IGenericSaveable saveable)
	{
		if (!Saveables.Contains(saveable))
		{
			Saveables.Add(saveable);
		}
	}

	public virtual string GetSaveString()
	{
		List<GenericSaveData> list = new List<GenericSaveData>();
		for (int i = 0; i < Saveables.Count; i++)
		{
			if (Saveables[i] != null)
			{
				list.Add(Saveables[i].GetSaveData());
			}
		}
		return new GenericSaveablesData(list.ToArray()).GetJson();
	}

	public void LoadSaveable(GenericSaveData data)
	{
		if (!GUIDManager.IsGUIDValid(data.GUID))
		{
			Console.LogWarning("Invalid GUID found in generic save data: " + data.GUID);
			return;
		}
		Guid guid = new Guid(data.GUID);
		IGenericSaveable genericSaveable = Saveables.Find((IGenericSaveable x) => x.GUID == guid);
		if (genericSaveable == null)
		{
			Guid guid2 = guid;
			Console.LogWarning("No saveable found with GUID: " + guid2.ToString());
		}
		else
		{
			genericSaveable.Load(data);
		}
	}
}
