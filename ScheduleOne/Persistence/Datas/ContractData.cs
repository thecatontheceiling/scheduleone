using System;
using ScheduleOne.Product;
using ScheduleOne.Quests;

namespace ScheduleOne.Persistence.Datas;

[Serializable]
public class ContractData : QuestData
{
	public string CustomerGUID;

	public float Payment;

	public ProductList ProductList;

	public string DeliveryLocationGUID;

	public QuestWindowConfig DeliveryWindow;

	public int PickupScheduleIndex;

	public GameDateTimeData AcceptTime;

	public ContractData(string guid, EQuestState state, bool isTracked, string title, string desc, bool isTimed, GameDateTimeData expiry, QuestEntryData[] entries, string customerGUID, float payment, ProductList productList, string deliveryLocationGUID, QuestWindowConfig deliveryWindow, int pickupScheduleIndex, GameDateTimeData acceptTime)
		: base(guid, state, isTracked, title, desc, isTimed, expiry, entries)
	{
		CustomerGUID = customerGUID;
		Payment = payment;
		ProductList = productList;
		DeliveryLocationGUID = deliveryLocationGUID;
		DeliveryWindow = deliveryWindow;
		PickupScheduleIndex = pickupScheduleIndex;
		AcceptTime = acceptTime;
	}
}
