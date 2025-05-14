using System;
using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.NPCs;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Quests;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class QuestsLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents))
		{
			QuestManagerData questManagerData = null;
			try
			{
				questManagerData = JsonUtility.FromJson<QuestManagerData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading data: " + ex.Message);
			}
			if (questManagerData == null)
			{
				return;
			}
			if (questManagerData.Quests != null)
			{
				QuestData[] quests = questManagerData.Quests;
				foreach (QuestData questData in quests)
				{
					if (questData != null)
					{
						Quest quest = GUIDManager.GetObject<Quest>(new Guid(questData.GUID));
						if (quest == null)
						{
							Console.LogWarning("Failed to find quest with GUID: " + questData.GUID);
						}
						else
						{
							quest.Load(questData);
						}
					}
				}
			}
			if (questManagerData.DeaddropQuests != null)
			{
				DeaddropQuestData[] deaddropQuests = questManagerData.DeaddropQuests;
				foreach (DeaddropQuestData deaddropQuestData in deaddropQuests)
				{
					if (deaddropQuestData != null)
					{
						DeadDrop deadDrop = GUIDManager.GetObject<DeadDrop>(new Guid(deaddropQuestData.DeaddropGUID));
						if (deadDrop == null)
						{
							Console.LogWarning("Failed to find deaddrop with GUID: " + deaddropQuestData.DeaddropGUID);
						}
						else
						{
							NetworkSingleton<QuestManager>.Instance.CreateDeaddropCollectionQuest(deadDrop.GUID.ToString(), deaddropQuestData.GUID).Load(deaddropQuestData);
						}
					}
				}
			}
			if (questManagerData.Contracts == null)
			{
				return;
			}
			ContractData[] contracts = questManagerData.Contracts;
			foreach (ContractData contractData in contracts)
			{
				if (contractData != null)
				{
					NPC nPC = GUIDManager.GetObject<NPC>(new Guid(contractData.CustomerGUID));
					if (nPC == null)
					{
						Console.LogWarning("Failed to find customer with GUID: " + contractData.CustomerGUID);
					}
					else
					{
						NetworkSingleton<QuestManager>.Instance.CreateContract_Local(contractData.Title, contractData.Description, contractData.Entries, contractData.GUID, contractData.IsTracked, nPC.NetworkObject, contractData.Payment, contractData.ProductList, contractData.DeliveryLocationGUID, contractData.DeliveryWindow, contractData.Expires, new GameDateTime(contractData.ExpiryDate), contractData.PickupScheduleIndex, new GameDateTime(contractData.AcceptTime));
					}
				}
			}
		}
		else
		{
			if (!Directory.Exists(mainPath))
			{
				return;
			}
			Console.Log("Loading legacy quests from: " + mainPath);
			string[] files = Directory.GetFiles(mainPath);
			for (int j = 0; j < files.Length; j++)
			{
				if (!TryLoadFile(files[j], out var contents2, autoAddExtension: false))
				{
					continue;
				}
				QuestData questData2 = null;
				try
				{
					questData2 = JsonUtility.FromJson<QuestData>(contents2);
				}
				catch (Exception ex2)
				{
					Debug.LogError("Error loading quest data: " + ex2.Message);
				}
				if (questData2 == null)
				{
					continue;
				}
				Quest quest2 = null;
				if (questData2.DataType == "DeaddropQuestData")
				{
					DeaddropQuestData deaddropQuestData2 = null;
					try
					{
						deaddropQuestData2 = JsonUtility.FromJson<DeaddropQuestData>(contents2);
					}
					catch (Exception ex3)
					{
						Debug.LogError("Error loading quest data: " + ex3.Message);
					}
					if (deaddropQuestData2 == null)
					{
						continue;
					}
					DeadDrop deadDrop2 = GUIDManager.GetObject<DeadDrop>(new Guid(deaddropQuestData2.DeaddropGUID));
					if (deadDrop2 == null)
					{
						Console.LogWarning("Failed to find deaddrop with GUID: " + deaddropQuestData2.DeaddropGUID);
						continue;
					}
					quest2 = NetworkSingleton<QuestManager>.Instance.CreateDeaddropCollectionQuest(deadDrop2.GUID.ToString(), questData2.GUID);
				}
				else
				{
					quest2 = GUIDManager.GetObject<Quest>(new Guid(questData2.GUID));
				}
				if (quest2 == null)
				{
					Console.LogWarning("Failed to find quest with GUID: " + questData2.GUID);
				}
				else
				{
					quest2.Load(questData2);
				}
			}
			string path = Path.Combine(mainPath, "Contracts");
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files2 = Directory.GetFiles(path);
			for (int k = 0; k < files2.Length; k++)
			{
				if (!TryLoadFile(files2[k], out var contents3, autoAddExtension: false))
				{
					continue;
				}
				ContractData contractData2 = null;
				try
				{
					contractData2 = JsonUtility.FromJson<ContractData>(contents3);
				}
				catch (Exception ex4)
				{
					Debug.LogError("Error loading contract data: " + ex4.Message);
				}
				if (contractData2 != null)
				{
					NPC nPC2 = GUIDManager.GetObject<NPC>(new Guid(contractData2.CustomerGUID));
					if (nPC2 == null)
					{
						Console.LogWarning("Failed to find customer with GUID: " + contractData2.CustomerGUID);
					}
					else
					{
						NetworkSingleton<QuestManager>.Instance.CreateContract_Local(contractData2.Title, contractData2.Description, contractData2.Entries, contractData2.GUID, contractData2.IsTracked, nPC2.NetworkObject, contractData2.Payment, contractData2.ProductList, contractData2.DeliveryLocationGUID, contractData2.DeliveryWindow, contractData2.Expires, new GameDateTime(contractData2.ExpiryDate), contractData2.PickupScheduleIndex, new GameDateTime(contractData2.AcceptTime));
					}
				}
			}
		}
	}
}
