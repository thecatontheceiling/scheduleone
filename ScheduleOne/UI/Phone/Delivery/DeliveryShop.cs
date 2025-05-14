using System.Collections.Generic;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.Property;
using ScheduleOne.UI.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.Delivery;

public class DeliveryShop : MonoBehaviour
{
	public const int DELIVERY_VEHICLE_SLOT_CAPACITY = 16;

	public const int DELIVERY_TIME_MIN = 60;

	public const int DELIVERY_TIME_MAX = 360;

	public const int DELIVERY_TIME_ITEM_COUNT_DIVISOR = 160;

	[Header("References")]
	public Image HeaderImage;

	public Button HeaderButton;

	public RectTransform ContentsContainer;

	public RectTransform ListingContainer;

	public Text DeliveryFeeLabel;

	public Text ItemTotalLabel;

	public Text OrderTotalLabel;

	public Button OrderButton;

	public Text OrderButtonNote;

	public Dropdown DestinationDropdown;

	public Dropdown LoadingDockDropdown;

	[Header("Settings")]
	public string MatchingShopInterfaceName = "ShopInterface";

	public float DeliveryFee = 200f;

	public bool AvailableByDefault;

	public ListingEntry ListingEntryPrefab;

	public Sprite HeaderImage_Hidden;

	public Sprite HeaderImage_Expanded;

	public RectTransform HeaderArrow;

	private List<ListingEntry> listingEntries = new List<ListingEntry>();

	private ScheduleOne.Property.Property destinationProperty;

	private int loadingDockIndex;

	public ShopInterface MatchingShop { get; private set; }

	public bool IsExpanded { get; private set; }

	public bool IsAvailable { get; private set; }

	private void Start()
	{
		MatchingShop = ShopInterface.AllShops.Find((ShopInterface x) => x.ShopName == MatchingShopInterfaceName);
		if (MatchingShop == null)
		{
			Debug.LogError("Could not find shop interface with name " + MatchingShopInterfaceName);
			return;
		}
		foreach (ShopListing listing in MatchingShop.Listings)
		{
			if (listing.CanBeDelivered)
			{
				ListingEntry listingEntry = Object.Instantiate(ListingEntryPrefab, ListingContainer);
				listingEntry.Initialize(listing);
				listingEntry.onQuantityChanged.AddListener(RefreshCart);
				listingEntries.Add(listingEntry);
			}
		}
		DeliveryFeeLabel.text = MoneyManager.FormatAmount(DeliveryFee);
		int num = Mathf.CeilToInt((float)listingEntries.Count / 2f);
		ContentsContainer.sizeDelta = new Vector2(ContentsContainer.sizeDelta.x, 230f + (float)num * 60f);
		HeaderButton.onClick.AddListener(delegate
		{
			SetIsExpanded(!IsExpanded);
		});
		OrderButton.onClick.AddListener(OrderPressed);
		DestinationDropdown.onValueChanged.AddListener(DestinationDropdownSelected);
		LoadingDockDropdown.onValueChanged.AddListener(LoadingDockDropdownSelected);
		SetIsExpanded(expanded: false);
		if (AvailableByDefault)
		{
			SetIsAvailable();
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
		MatchingShop.DeliveryVehicle.Deactivate();
	}

	private void FixedUpdate()
	{
		if (IsExpanded && PlayerSingleton<DeliveryApp>.Instance.isOpen)
		{
			RefreshOrderButton();
		}
	}

	public void SetIsExpanded(bool expanded)
	{
		IsExpanded = expanded;
		ContentsContainer.gameObject.SetActive(IsExpanded);
		HeaderImage.sprite = (IsExpanded ? HeaderImage_Expanded : HeaderImage_Hidden);
		HeaderArrow.localRotation = (IsExpanded ? Quaternion.Euler(0f, 0f, 270f) : Quaternion.Euler(0f, 0f, 180f));
		PlayerSingleton<DeliveryApp>.Instance.RefreshContent();
	}

	public void SetIsAvailable()
	{
		IsAvailable = true;
		base.gameObject.SetActive(value: true);
		PlayerSingleton<DeliveryApp>.Instance.RefreshContent();
	}

	public void OrderPressed()
	{
		if (!CanOrder(out var reason))
		{
			Debug.LogWarning("Cannot order: " + reason);
			return;
		}
		float orderTotal = GetOrderTotal();
		List<StringIntPair> list = new List<StringIntPair>();
		foreach (ListingEntry listingEntry in listingEntries)
		{
			if (listingEntry.SelectedQuantity > 0)
			{
				list.Add(new StringIntPair(listingEntry.MatchingListing.Item.ID, listingEntry.SelectedQuantity));
			}
		}
		int orderItemCount = GetOrderItemCount();
		int timeUntilArrival = Mathf.RoundToInt(Mathf.Lerp(60f, 360f, Mathf.Clamp01((float)orderItemCount / 160f)));
		DeliveryInstance delivery = new DeliveryInstance(GUIDManager.GenerateUniqueGUID().ToString(), MatchingShopInterfaceName, destinationProperty.PropertyCode, loadingDockIndex - 1, list.ToArray(), EDeliveryStatus.InTransit, timeUntilArrival);
		NetworkSingleton<DeliveryManager>.Instance.SendDelivery(delivery);
		NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction("Delivery from " + MatchingShop.ShopName, 0f - orderTotal, 1f, string.Empty);
		PlayerSingleton<DeliveryApp>.Instance.PlayOrderSubmittedAnim();
		ResetCart();
	}

	public void RefreshShop()
	{
		RefreshCart();
		RefreshOrderButton();
		RefreshDestinationUI();
		RefreshLoadingDockUI();
		RefreshEntryOrder();
		RefreshEntriesLocked();
	}

	public void ResetCart()
	{
		foreach (ListingEntry listingEntry in listingEntries)
		{
			listingEntry.SetQuantity(0, notify: false);
		}
		RefreshCart();
		RefreshOrderButton();
	}

	private void RefreshCart()
	{
		ItemTotalLabel.text = MoneyManager.FormatAmount(GetCartCost());
		OrderTotalLabel.text = MoneyManager.FormatAmount(GetOrderTotal());
	}

	private void RefreshOrderButton()
	{
		if (CanOrder(out var reason))
		{
			OrderButton.interactable = true;
			OrderButtonNote.enabled = false;
		}
		else
		{
			OrderButton.interactable = false;
			OrderButtonNote.text = reason;
			OrderButtonNote.enabled = true;
		}
	}

	public bool CanOrder(out string reason)
	{
		reason = string.Empty;
		if (HasActiveDelivery())
		{
			reason = "Delivery already in progress";
			return false;
		}
		float cartCost = GetCartCost();
		if (GetOrderTotal() > NetworkSingleton<MoneyManager>.Instance.SyncAccessor_onlineBalance)
		{
			reason = "Insufficient online balance";
			return false;
		}
		if (destinationProperty == null)
		{
			reason = "Select a destination";
			return false;
		}
		if (destinationProperty.LoadingDockCount == 0)
		{
			reason = "Selected destination has no loading docks";
			return false;
		}
		if (loadingDockIndex == 0)
		{
			reason = "Select a loading dock";
			return false;
		}
		if (!WillCartFitInVehicle())
		{
			reason = "Order is too large for delivery vehicle";
			return false;
		}
		return cartCost > 0f;
	}

	public bool HasActiveDelivery()
	{
		if (destinationProperty == null)
		{
			return false;
		}
		return NetworkSingleton<DeliveryManager>.Instance.GetActiveShopDelivery(this) != null;
	}

	public bool WillCartFitInVehicle()
	{
		int num = 0;
		foreach (ListingEntry listingEntry in listingEntries)
		{
			if (listingEntry.SelectedQuantity != 0)
			{
				int num2 = listingEntry.SelectedQuantity;
				int stackLimit = listingEntry.MatchingListing.Item.StackLimit;
				while (num2 > 0)
				{
					num2 = ((num2 > stackLimit) ? (num2 - stackLimit) : 0);
					num++;
				}
			}
		}
		return num <= 16;
	}

	public void RefreshDestinationUI()
	{
		ScheduleOne.Property.Property property = destinationProperty;
		destinationProperty = null;
		DestinationDropdown.ClearOptions();
		List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
		list.Add(new Dropdown.OptionData("-"));
		List<ScheduleOne.Property.Property> potentialDestinations = GetPotentialDestinations();
		int num = 0;
		for (int i = 0; i < potentialDestinations.Count; i++)
		{
			list.Add(new Dropdown.OptionData(potentialDestinations[i].PropertyName));
			if (potentialDestinations[i] == property)
			{
				num = i + 1;
			}
		}
		DestinationDropdown.AddOptions(list);
		DestinationDropdown.SetValueWithoutNotify(num);
		DestinationDropdownSelected(num);
	}

	private void DestinationDropdownSelected(int index)
	{
		if (index > 0 && index <= GetPotentialDestinations().Count)
		{
			destinationProperty = GetPotentialDestinations()[index - 1];
			if (loadingDockIndex == 0 && destinationProperty.LoadingDockCount > 0)
			{
				loadingDockIndex = 1;
			}
		}
		else
		{
			destinationProperty = null;
		}
		RefreshLoadingDockUI();
	}

	private List<ScheduleOne.Property.Property> GetPotentialDestinations()
	{
		return new List<ScheduleOne.Property.Property>(ScheduleOne.Property.Property.OwnedProperties);
	}

	public void RefreshLoadingDockUI()
	{
		int value = loadingDockIndex;
		loadingDockIndex = 0;
		LoadingDockDropdown.ClearOptions();
		List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
		list.Add(new Dropdown.OptionData("-"));
		if (destinationProperty != null)
		{
			for (int i = 0; i < destinationProperty.LoadingDockCount; i++)
			{
				list.Add(new Dropdown.OptionData((i + 1).ToString()));
			}
		}
		LoadingDockDropdown.AddOptions(list);
		int num = Mathf.Clamp(value, 0, list.Count - 1);
		LoadingDockDropdown.SetValueWithoutNotify(num);
		LoadingDockDropdownSelected(num);
	}

	private void LoadingDockDropdownSelected(int index)
	{
		loadingDockIndex = index;
	}

	private float GetCartCost()
	{
		float num = 0f;
		foreach (ListingEntry listingEntry in listingEntries)
		{
			num += (float)listingEntry.SelectedQuantity * listingEntry.MatchingListing.Price;
		}
		return num;
	}

	private float GetOrderTotal()
	{
		return GetCartCost() + DeliveryFee;
	}

	private int GetOrderItemCount()
	{
		int num = 0;
		foreach (ListingEntry listingEntry in listingEntries)
		{
			num += listingEntry.SelectedQuantity;
		}
		return num;
	}

	private void RefreshEntryOrder()
	{
		List<ListingEntry> list = new List<ListingEntry>();
		List<ListingEntry> list2 = new List<ListingEntry>();
		foreach (ListingEntry listingEntry in listingEntries)
		{
			if (!listingEntry.MatchingListing.Item.IsPurchasable)
			{
				list2.Add(listingEntry);
			}
			else
			{
				list.Add(listingEntry);
			}
		}
		list.AddRange(list2);
		for (int i = 0; i < list.Count; i++)
		{
			list[i].transform.SetSiblingIndex(i);
		}
	}

	private void RefreshEntriesLocked()
	{
		foreach (ListingEntry listingEntry in listingEntries)
		{
			listingEntry.RefreshLocked();
		}
	}
}
