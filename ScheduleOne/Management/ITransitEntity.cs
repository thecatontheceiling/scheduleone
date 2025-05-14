using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs;
using UnityEngine;

namespace ScheduleOne.Management;

public interface ITransitEntity
{
	public enum ESlotType
	{
		Input = 0,
		Output = 1,
		Both = 2
	}

	string Name { get; }

	List<ItemSlot> InputSlots { get; set; }

	List<ItemSlot> OutputSlots { get; set; }

	Transform LinkOrigin { get; }

	Transform[] AccessPoints { get; }

	bool Selectable { get; }

	bool IsAcceptingItems { get; }

	bool IsDestroyed { get; }

	Guid GUID { get; }

	void ShowOutline(Color color);

	void HideOutline();

	void InsertItemIntoInput(ItemInstance item, NPC inserter = null)
	{
		if (GetInputCapacityForItem(item, inserter) < item.Quantity)
		{
			Console.LogWarning("ITransitEntity InsertItem() called but item won't fit!");
			return;
		}
		int num = item.Quantity;
		for (int i = 0; i < InputSlots.Count; i++)
		{
			if (InputSlots[i].IsLocked || InputSlots[i].IsAddLocked)
			{
				continue;
			}
			int capacityForItem = InputSlots[i].GetCapacityForItem(item);
			if (capacityForItem > 0)
			{
				int num2 = Mathf.Min(capacityForItem, num);
				if (InputSlots[i].ItemInstance == null)
				{
					InputSlots[i].SetStoredItem(item);
				}
				else
				{
					InputSlots[i].ChangeQuantity(num2);
				}
				num -= num2;
			}
			if (num <= 0)
			{
				break;
			}
		}
	}

	void InsertItemIntoOutput(ItemInstance item, NPC inserter = null)
	{
		if (GetOutputCapacityForItem(item, inserter) < item.Quantity)
		{
			Console.LogWarning("ITransitEntity InsertItem() called but item won't fit!");
			return;
		}
		int num = item.Quantity;
		for (int i = 0; i < OutputSlots.Count; i++)
		{
			if (OutputSlots[i].IsLocked || OutputSlots[i].IsAddLocked)
			{
				continue;
			}
			int capacityForItem = OutputSlots[i].GetCapacityForItem(item);
			if (capacityForItem > 0)
			{
				int num2 = Mathf.Min(capacityForItem, num);
				if (OutputSlots[i].ItemInstance == null)
				{
					OutputSlots[i].SetStoredItem(item);
				}
				else
				{
					OutputSlots[i].ChangeQuantity(num2);
				}
				num -= num2;
			}
			if (num <= 0)
			{
				break;
			}
		}
	}

	int GetInputCapacityForItem(ItemInstance item, NPC asker = null)
	{
		int num = 0;
		NetworkObject networkObject = ((asker != null) ? asker.NetworkObject : null);
		for (int i = 0; i < InputSlots.Count; i++)
		{
			if (InputSlots[i].IsLocked || InputSlots[i].IsAddLocked)
			{
				bool flag = false;
				if (networkObject != null && InputSlots[i].ActiveLock != null && InputSlots[i].ActiveLock.LockOwner == networkObject)
				{
					flag = true;
				}
				if (!flag)
				{
					continue;
				}
			}
			num += InputSlots[i].GetCapacityForItem(item);
		}
		return num;
	}

	int GetOutputCapacityForItem(ItemInstance item, NPC asker = null)
	{
		int num = 0;
		NetworkObject networkObject = ((asker != null) ? asker.NetworkObject : null);
		for (int i = 0; i < OutputSlots.Count; i++)
		{
			if (OutputSlots[i].IsLocked || OutputSlots[i].IsAddLocked)
			{
				bool flag = false;
				if (networkObject != null && OutputSlots[i].ActiveLock != null && OutputSlots[i].ActiveLock.LockOwner == networkObject)
				{
					flag = true;
				}
				if (!flag)
				{
					continue;
				}
			}
			num += OutputSlots[i].GetCapacityForItem(item);
		}
		return num;
	}

	ItemSlot GetOutputItemContainer(ItemInstance item)
	{
		return OutputSlots.FirstOrDefault((ItemSlot x) => x.ItemInstance == item);
	}

	List<ItemSlot> ReserveInputSlotsForItem(ItemInstance item, NetworkObject locker)
	{
		List<ItemSlot> list = new List<ItemSlot>();
		int num = item.Quantity;
		for (int i = 0; i < InputSlots.Count; i++)
		{
			int capacityForItem = InputSlots[i].GetCapacityForItem(item);
			if (capacityForItem != 0)
			{
				int num2 = Mathf.Min(capacityForItem, num);
				num -= num2;
				InputSlots[i].ApplyLock(locker, "Employee is about to place an item here");
				list.Add(InputSlots[i]);
				if (num <= 0)
				{
					break;
				}
			}
		}
		return list;
	}

	void RemoveSlotLocks(NetworkObject locker)
	{
		for (int i = 0; i < InputSlots.Count; i++)
		{
			if (InputSlots[i].ActiveLock != null && InputSlots[i].ActiveLock.LockOwner == locker)
			{
				InputSlots[i].RemoveLock();
			}
		}
	}

	ItemSlot GetFirstSlotContainingItem(string id, ESlotType searchType)
	{
		if (searchType == ESlotType.Output || searchType == ESlotType.Both)
		{
			for (int i = 0; i < OutputSlots.Count; i++)
			{
				if (OutputSlots[i].ItemInstance != null && OutputSlots[i].ItemInstance.ID == id)
				{
					return OutputSlots[i];
				}
			}
		}
		if (searchType == ESlotType.Input || searchType == ESlotType.Both)
		{
			for (int j = 0; j < InputSlots.Count; j++)
			{
				if (InputSlots[j].ItemInstance != null && InputSlots[j].ItemInstance.ID == id)
				{
					return InputSlots[j];
				}
			}
		}
		return null;
	}

	ItemSlot GetFirstSlotContainingTemplateItem(ItemInstance templateItem, ESlotType searchType)
	{
		if (searchType == ESlotType.Output || searchType == ESlotType.Both)
		{
			for (int i = 0; i < OutputSlots.Count; i++)
			{
				if (OutputSlots[i].ItemInstance != null && OutputSlots[i].ItemInstance.CanStackWith(templateItem, checkQuantities: false))
				{
					return OutputSlots[i];
				}
			}
		}
		if (searchType == ESlotType.Input || searchType == ESlotType.Both)
		{
			for (int j = 0; j < InputSlots.Count; j++)
			{
				if (InputSlots[j].ItemInstance != null && InputSlots[j].ItemInstance.CanStackWith(templateItem, checkQuantities: false))
				{
					return InputSlots[j];
				}
			}
		}
		return null;
	}
}
