using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Trash;

public class TrashBag : TrashItem
{
	public TrashContent Content { get; private set; } = new TrashContent();

	public void LoadContent(TrashContentData data)
	{
		Content.LoadFromData(data);
	}

	public override TrashItemData GetData()
	{
		return new TrashBagData(ID, base.GUID.ToString(), base.transform.position, base.transform.rotation, Content.GetData());
	}
}
