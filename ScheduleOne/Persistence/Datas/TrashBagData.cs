using UnityEngine;

namespace ScheduleOne.Persistence.Datas;

public class TrashBagData : TrashItemData
{
	public TrashBagData(string trashID, string guid, Vector3 position, Quaternion rotation, TrashContentData contents)
		: base(trashID, guid, position, rotation)
	{
		DataType = "TrashBagData";
		Contents = contents;
	}
}
