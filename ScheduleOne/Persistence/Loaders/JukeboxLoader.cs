using ScheduleOne.EntityFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Persistence.Loaders;

public class JukeboxLoader : GridItemLoader
{
	public override string ItemType => typeof(JukeboxData).Name;

	public override void Load(string mainPath)
	{
		GridItem gridItem = LoadAndCreate(mainPath);
		if (gridItem == null)
		{
			Console.LogWarning("Failed to load grid item");
			return;
		}
		Jukebox jukebox = gridItem as Jukebox;
		if (jukebox == null)
		{
			Console.LogWarning("Failed to cast grid item to Jukebox");
			return;
		}
		JukeboxData data = GetData<JukeboxData>(mainPath);
		if (data == null)
		{
			Console.LogWarning("Failed to load jukebox data");
			return;
		}
		Console.Log($"Loaded jukebox data: {data}");
		jukebox.SetJukeboxState(null, data.State, setTrackTime: true, setSync: true);
	}
}
