using ScheduleOne.DevUtilities;

namespace ScheduleOne.Persistence.Datas;

public class GameData : SaveData
{
	public string OrganisationName;

	public int Seed;

	public GameSettings Settings;

	public GameData(string organisationName, int seed, GameSettings settings)
	{
		OrganisationName = organisationName;
		Seed = seed;
		Settings = settings;
	}

	public GameData()
	{
		OrganisationName = "Organisation";
		Seed = 0;
	}
}
