using System;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace ScheduleOne.Persistence.Datas;

public class LabelledSurfaceItemData : SurfaceItemData
{
	public string Message;

	public LabelledSurfaceItemData(Guid guid, ItemInstance item, int loadOrder, string parentSurfaceGUID, Vector3 pos, Quaternion rot, string message)
		: base(guid, item, loadOrder, parentSurfaceGUID, pos, rot)
	{
		Message = message;
	}
}
