using ScheduleOne.Money;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Shop;

public class CartEntry : MonoBehaviour
{
	[Header("References")]
	public TextMeshProUGUI NameLabel;

	public TextMeshProUGUI PriceLabel;

	public Button IncrementButton;

	public Button DecrementButton;

	public Button RemoveButton;

	public int Quantity { get; protected set; }

	public Cart Cart { get; protected set; }

	public ShopListing Listing { get; protected set; }

	public void Initialize(Cart cart, ShopListing listing, int quantity)
	{
		Cart = cart;
		Listing = listing;
		Quantity = quantity;
		IncrementButton.onClick.AddListener(delegate
		{
			ChangeAmount(1);
		});
		DecrementButton.onClick.AddListener(delegate
		{
			ChangeAmount(-1);
		});
		RemoveButton.onClick.AddListener(delegate
		{
			ChangeAmount(-999);
		});
		UpdateTitle();
		UpdatePrice();
	}

	public void SetQuantity(int quantity)
	{
		Quantity = quantity;
		UpdateTitle();
		UpdatePrice();
	}

	protected virtual void UpdateTitle()
	{
		NameLabel.text = Quantity + "x " + Listing.Item.Name;
	}

	private void UpdatePrice()
	{
		PriceLabel.text = MoneyManager.FormatAmount((float)Quantity * Listing.Price);
	}

	private void ChangeAmount(int change)
	{
		if (change > 0)
		{
			Cart.AddItem(Listing, change);
		}
		else if (change < 0)
		{
			Cart.RemoveItem(Listing, -change);
		}
	}
}
