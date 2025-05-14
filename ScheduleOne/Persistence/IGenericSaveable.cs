using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Persistence;

public interface IGenericSaveable
{
	Guid GUID { get; }

	void InitializeSaveable()
	{
		if (!Singleton<GenericSaveablesManager>.InstanceExists)
		{
			Console.LogError("GenericSaveablesManager does not exist in scene.");
		}
		else
		{
			Singleton<GenericSaveablesManager>.Instance.RegisterSaveable(this);
		}
	}

	void Load(GenericSaveData data);

	GenericSaveData GetSaveData();
}
