using System;
using ScheduleOne.Money;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScheduleOne.UI.Shop;

public class ListingUI : MonoBehaviour
{
	public static Color32 PriceLabelColor_Normal = new Color32(90, 185, 90, byte.MaxValue);

	public static Color32 PriceLabelColor_NoStock = new Color32(165, 70, 60, byte.MaxValue);

	[Header("Colors")]
	public Color32 StockLabelDefault = new Color32(40, 40, 40, byte.MaxValue);

	public Color32 StockLabelNone = new Color32(185, 55, 55, byte.MaxValue);

	[Header("References")]
	public Image Icon;

	public TextMeshProUGUI NameLabel;

	public TextMeshProUGUI PriceLabel;

	public TextMeshProUGUI StockLabel;

	public GameObject LockedContainer;

	public Button BuyButton;

	public Button DropdownButton;

	public EventTrigger Trigger;

	public RectTransform DetailPanelAnchor;

	public RectTransform DropdownAnchor;

	public RectTransform TopDropdownAnchor;

	public Action hoverStart;

	public Action hoverEnd;

	public Action onClicked;

	public Action onDropdownClicked;

	public ShopListing Listing { get; protected set; }

	public virtual void Initialize(ShopListing listing)
	{
		Listing = listing;
		Icon.sprite = listing.Item.Icon;
		Icon.color = (listing.UseIconTint ? listing.IconTint : Color.white);
		NameLabel.text = listing.Item.Name;
		UpdatePrice();
		UpdateStock();
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerEnter;
		entry.callback.AddListener(delegate
		{
			HoverStart();
		});
		Trigger.triggers.Add(entry);
		EventTrigger.Entry entry2 = new EventTrigger.Entry();
		entry2.eventID = EventTriggerType.PointerExit;
		entry2.callback.AddListener(delegate
		{
			HoverEnd();
		});
		Trigger.triggers.Add(entry2);
		listing.onStockChanged = (Action)Delegate.Combine(listing.onStockChanged, new Action(StockChanged));
		BuyButton.onClick.AddListener(Clicked);
		DropdownButton.onClick.AddListener(DropdownClicked);
		UpdateLockStatus();
	}

	public virtual RectTransform GetIconCopy(RectTransform parent)
	{
		return UnityEngine.Object.Instantiate(Icon.gameObject, parent).GetComponent<RectTransform>();
	}

	public void Update()
	{
		UpdateButtons();
	}

	private void Clicked()
	{
		if (onClicked != null)
		{
			onClicked();
		}
	}

	private void DropdownClicked()
	{
		if (onDropdownClicked != null)
		{
			onDropdownClicked();
		}
	}

	private void HoverStart()
	{
		if (hoverStart != null)
		{
			hoverStart();
		}
	}

	private void HoverEnd()
	{
		if (hoverEnd != null)
		{
			hoverEnd();
		}
	}

	private void StockChanged()
	{
		UpdateButtons();
		UpdatePrice();
		UpdateStock();
	}

	private void UpdatePrice()
	{
		PriceLabel.text = MoneyManager.FormatAmount(Listing.Price);
		PriceLabel.color = PriceLabelColor_Normal;
	}

	private void UpdateStock()
	{
		if (StockLabel == null)
		{
			return;
		}
		if (Listing.IsUnlimitedStock)
		{
			StockLabel.enabled = false;
			return;
		}
		int currentStockMinusCart = Listing.CurrentStockMinusCart;
		StockLabel.text = currentStockMinusCart + " / " + Listing.DefaultStock;
		if (currentStockMinusCart > 0)
		{
			StockLabel.color = StockLabelDefault;
		}
		else
		{
			StockLabel.text = "Out of stock";
			StockLabel.color = StockLabelNone;
		}
		if (currentStockMinusCart == 1 && Listing.RestockRate == ShopListing.ERestockRate.Never)
		{
			StockLabel.text = "1 of 1";
		}
		StockLabel.enabled = true;
	}

	private void UpdateButtons()
	{
		bool interactable = CanAddToCart();
		BuyButton.interactable = interactable;
		DropdownButton.interactable = interactable;
	}

	public bool CanAddToCart()
	{
		if (!Listing.IsUnlimitedStock)
		{
			return Listing.CurrentStockMinusCart > 0;
		}
		return true;
	}

	public void UpdateLockStatus()
	{
		LockedContainer.gameObject.SetActive(!Listing.Item.IsPurchasable);
	}
}
