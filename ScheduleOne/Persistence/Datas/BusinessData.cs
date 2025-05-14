using System;

namespace ScheduleOne.Persistence.Datas;

[Serializable]
public class BusinessData : PropertyData
{
	public LaunderOperationData[] LaunderingOperations;

	public BusinessData(string propertyCode, bool isOwned, bool[] switchStates, LaunderOperationData[] launderingOperations, bool[] toggleableStates)
		: base(propertyCode, isOwned, switchStates, toggleableStates)
	{
		LaunderingOperations = launderingOperations;
	}
}
