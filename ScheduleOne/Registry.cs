using System;
using System.Collections.Generic;
using EasyButtons;
using FishNet.Object;
using ScheduleOne.ConstructableScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Levelling;
using ScheduleOne.Persistence;
using UnityEngine;

namespace ScheduleOne;

public class Registry : PersistentSingleton<Registry>
{
	[Serializable]
	public class ObjectRegister
	{
		public string ID;

		public string AssetPath;

		public NetworkObject Prefab;
	}

	[Serializable]
	public class ItemRegister
	{
		public string ID;

		public string AssetPath;

		public ItemDefinition Definition;
	}

	[SerializeField]
	private List<ObjectRegister> ObjectRegistry = new List<ObjectRegister>();

	[SerializeField]
	private List<ItemRegister> ItemRegistry = new List<ItemRegister>();

	[SerializeField]
	private List<ItemRegister> ItemsAddedAtRuntime = new List<ItemRegister>();

	private Dictionary<int, ItemRegister> ItemDictionary = new Dictionary<int, ItemRegister>();

	private Dictionary<string, string> itemIDAliases = new Dictionary<string, string> { { "viagra", "viagor" } };

	public List<SeedDefinition> Seeds = new List<SeedDefinition>();

	protected override void Awake()
	{
		base.Awake();
		if (Singleton<Registry>.Instance == null || Singleton<Registry>.Instance != this)
		{
			return;
		}
		foreach (ItemRegister item in ItemRegistry)
		{
			if (ItemDictionary.ContainsKey(GetHash(item.ID)))
			{
				Console.LogError("Duplicate item ID: " + item.ID);
			}
			else
			{
				AddToItemDictionary(item);
			}
		}
	}

	public static GameObject GetPrefab(string id)
	{
		return Singleton<Registry>.Instance.ObjectRegistry.Find((ObjectRegister x) => x.ID.ToLower() == id.ToString())?.Prefab.gameObject;
	}

	public static ItemDefinition GetItem(string ID)
	{
		return Singleton<Registry>.Instance._GetItem(ID);
	}

	public static bool ItemExists(string ID)
	{
		return Singleton<Registry>.Instance._GetItem(ID, warnIfNonExistent: false) != null;
	}

	public static T GetItem<T>(string ID) where T : ItemDefinition
	{
		return Singleton<Registry>.Instance._GetItem(ID) as T;
	}

	public ItemDefinition _GetItem(string ID, bool warnIfNonExistent = true)
	{
		if (string.IsNullOrEmpty(ID))
		{
			return null;
		}
		if (itemIDAliases.ContainsKey(ID.ToLower()))
		{
			ID = itemIDAliases[ID.ToLower()];
		}
		int hash = GetHash(ID);
		if (!ItemDictionary.ContainsKey(hash))
		{
			if (Singleton<LoadManager>.InstanceExists && !Singleton<LoadManager>.Instance.IsLoading && warnIfNonExistent)
			{
				Console.LogError("Item '" + ID + "' not found in registry! (Hash = " + hash + ")");
			}
			return null;
		}
		return ItemDictionary[hash]?.Definition;
	}

	public static Constructable GetConstructable(string id)
	{
		GameObject prefab = GetPrefab(id);
		if (!(prefab != null))
		{
			return null;
		}
		return prefab.GetComponent<Constructable>();
	}

	private static int GetHash(string ID)
	{
		return ID.ToLower().GetHashCode();
	}

	private static string RemoveAssetsAndPrefab(string originalString)
	{
		int num = originalString.IndexOf("Assets/");
		if (num != -1)
		{
			originalString = originalString.Substring(num + "Assets/".Length);
		}
		int num2 = originalString.LastIndexOf(".prefab");
		if (num2 != -1)
		{
			originalString = originalString.Substring(0, num2);
		}
		return originalString;
	}

	protected override void Start()
	{
		base.Start();
		Singleton<LoadManager>.Instance.onPreSceneChange.AddListener(RemoveRuntimeItems);
	}

	public void AddToRegistry(ItemDefinition item)
	{
		Console.Log("Adding " + item.ID + "(Hash = " + GetHash(item.ID) + ") to registry: " + item);
		ItemRegister itemRegister = new ItemRegister
		{
			Definition = item,
			ID = item.ID,
			AssetPath = string.Empty
		};
		ItemRegistry.Add(itemRegister);
		AddToItemDictionary(itemRegister);
		if (Application.isPlaying)
		{
			ItemsAddedAtRuntime.Add(new ItemRegister
			{
				Definition = item,
				ID = item.ID,
				AssetPath = string.Empty
			});
		}
	}

	private void AddToItemDictionary(ItemRegister reg)
	{
		int hash = GetHash(reg.ID);
		if (ItemDictionary.ContainsKey(hash))
		{
			Console.LogError("Duplicate item ID: " + reg.ID);
		}
		else
		{
			ItemDictionary.Add(hash, reg);
		}
	}

	private void RemoveItemFromDictionary(ItemRegister reg)
	{
		int hash = GetHash(reg.ID);
		ItemDictionary.Remove(hash);
	}

	public void RemoveRuntimeItems()
	{
		foreach (ItemRegister item in new List<ItemRegister>(ItemsAddedAtRuntime))
		{
			RemoveFromRegistry(item.Definition);
		}
		ItemsAddedAtRuntime.Clear();
		Console.Log("Removed runtime items from registry");
	}

	public void RemoveFromRegistry(ItemDefinition item)
	{
		ItemRegister itemRegister = ItemRegistry.Find((ItemRegister x) => x.Definition == item);
		if (itemRegister != null)
		{
			ItemRegistry.Remove(itemRegister);
			RemoveItemFromDictionary(itemRegister);
		}
	}

	[Button]
	public void LogOrderedUnlocks()
	{
		List<ItemDefinition> list = new List<ItemDefinition>();
		for (int i = 0; i < ItemRegistry.Count; i++)
		{
			if ((ItemRegistry[i].Definition as StorableItemDefinition).RequiresLevelToPurchase)
			{
				list.Add(ItemRegistry[i].Definition);
			}
		}
		list.Sort((ItemDefinition x, ItemDefinition y) => (x as StorableItemDefinition).RequiredRank.CompareTo((y as StorableItemDefinition).RequiredRank));
		Console.Log("Ordered Unlocks:");
		foreach (ItemDefinition item in list)
		{
			string iD = item.ID;
			FullRank requiredRank = (item as StorableItemDefinition).RequiredRank;
			Console.Log(iD + " - " + requiredRank.ToString());
		}
	}
}
