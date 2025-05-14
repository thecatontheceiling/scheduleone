using System.Collections.Generic;
using ScheduleOne.PlayerScripts;

namespace ScheduleOne.Casino;

public class CasinoGamePlayerData
{
	protected Dictionary<string, bool> bools = new Dictionary<string, bool>();

	protected Dictionary<string, float> floats = new Dictionary<string, float>();

	public CasinoGamePlayers Parent { get; private set; }

	public Player Player { get; private set; }

	public CasinoGamePlayerData(CasinoGamePlayers parent, Player player)
	{
		Parent = parent;
		Player = player;
		bools = new Dictionary<string, bool>();
		floats = new Dictionary<string, float>();
	}

	public T GetData<T>(string key)
	{
		if (typeof(T) == typeof(bool))
		{
			if (bools.ContainsKey(key))
			{
				return (T)(object)bools[key];
			}
		}
		else if (typeof(T) == typeof(float) && floats.ContainsKey(key))
		{
			return (T)(object)floats[key];
		}
		return default(T);
	}

	public void SetData<T>(string key, T value, bool network = true)
	{
		if (network)
		{
			if (typeof(T) == typeof(bool))
			{
				Parent.SendPlayerBool(Player.NetworkObject, key, (bool)(object)value);
			}
			else if (typeof(T) == typeof(float))
			{
				Parent.SendPlayerFloat(Player.NetworkObject, key, (float)(object)value);
			}
		}
		if (typeof(T) == typeof(bool))
		{
			if (bools.ContainsKey(key))
			{
				bools[key] = (bool)(object)value;
			}
			else
			{
				bools.Add(key, (bool)(object)value);
			}
		}
		else if (typeof(T) == typeof(float))
		{
			if (floats.ContainsKey(key))
			{
				floats[key] = (float)(object)value;
			}
			else
			{
				floats.Add(key, (float)(object)value);
			}
		}
	}
}
