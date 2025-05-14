using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.Delivery;

public class DeliveryStatusDisplay : MonoBehaviour
{
	public GameObject ItemEntryPrefab;

	[Header("References")]
	public RectTransform Rect;

	public Text DestinationLabel;

	public Text ShopLabel;

	public Image StatusImage;

	public Text StatusLabel;

	public Tooltip StatusTooltip;

	public RectTransform ItemEntryContainer;

	[Header("Settings")]
	public Color StatusColor_Transit;

	public Color StatusColor_Waiting;

	public Color StatusColor_Arrived;

	public DeliveryInstance DeliveryInstance { get; private set; }

	public void AssignDelivery(DeliveryInstance instance)
	{
		DeliveryInstance = instance;
		DestinationLabel.text = DeliveryInstance.Destination.PropertyName + " [" + (DeliveryInstance.LoadingDockIndex + 1) + "]";
		ShopLabel.text = DeliveryInstance.StoreName;
		StringIntPair[] items = DeliveryInstance.Items;
		foreach (StringIntPair stringIntPair in items)
		{
			RectTransform component = Object.Instantiate(ItemEntryPrefab, ItemEntryContainer).GetComponent<RectTransform>();
			ItemDefinition item = Registry.GetItem(stringIntPair.String);
			component.Find("Label").GetComponent<Text>().text = stringIntPair.Int + "x " + item.Name;
		}
		int num = Mathf.CeilToInt((float)DeliveryInstance.Items.Length / 2f);
		Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, 70 + 20 * num);
		RefreshStatus();
	}

	public void RefreshStatus()
	{
		if (DeliveryInstance.Status == EDeliveryStatus.InTransit)
		{
			StatusImage.color = StatusColor_Transit;
			int timeUntilArrival = DeliveryInstance.TimeUntilArrival;
			int num = timeUntilArrival / 60;
			int num2 = timeUntilArrival % 60;
			StatusLabel.text = num + "h " + num2 + "m";
			StatusTooltip.text = "This delivery is currently in transit.";
		}
		else if (DeliveryInstance.Status == EDeliveryStatus.Waiting)
		{
			StatusImage.color = StatusColor_Waiting;
			StatusLabel.text = "Waiting";
			StatusTooltip.text = "This delivery is waiting for the loading dock " + (DeliveryInstance.LoadingDockIndex + 1) + " to be empty.";
		}
		else if (DeliveryInstance.Status == EDeliveryStatus.Arrived)
		{
			StatusImage.color = StatusColor_Arrived;
			StatusLabel.text = "Arrived";
			StatusTooltip.text = "This delivery has arrived and is ready to be unloaded.";
		}
	}
}
