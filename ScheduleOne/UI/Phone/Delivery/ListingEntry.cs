using ScheduleOne.Money;
using ScheduleOne.UI.Shop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.Delivery;

public class ListingEntry : MonoBehaviour
{
	[Header("References")]
	public Image Icon;

	public Text ItemNameLabel;

	public Text ItemPriceLabel;

	public InputField QuantityInput;

	public Button IncrementButton;

	public Button DecrementButton;

	public RectTransform LockedContainer;

	public UnityEvent onQuantityChanged;

	public ShopListing MatchingListing { get; private set; }

	public int SelectedQuantity { get; private set; }

	public void Initialize(ShopListing match)
	{
		MatchingListing = match;
		Icon.sprite = MatchingListing.Item.Icon;
		ItemNameLabel.text = MatchingListing.Item.Name;
		ItemPriceLabel.text = MoneyManager.FormatAmount(MatchingListing.Price);
		QuantityInput.onSubmit.AddListener(OnQuantityInputSubmitted);
		QuantityInput.onEndEdit.AddListener(delegate
		{
			ValidateInput();
		});
		IncrementButton.onClick.AddListener(delegate
		{
			ChangeQuantity(1);
		});
		DecrementButton.onClick.AddListener(delegate
		{
			ChangeQuantity(-1);
		});
		QuantityInput.SetTextWithoutNotify(SelectedQuantity.ToString());
		RefreshLocked();
	}

	public void RefreshLocked()
	{
		if (MatchingListing.Item.IsPurchasable)
		{
			LockedContainer.gameObject.SetActive(value: false);
		}
		else
		{
			LockedContainer.gameObject.SetActive(value: true);
		}
	}

	public void SetQuantity(int quant, bool notify = true)
	{
		if (!MatchingListing.Item.IsPurchasable)
		{
			quant = 0;
		}
		SelectedQuantity = Mathf.Clamp(quant, 0, 999);
		QuantityInput.SetTextWithoutNotify(SelectedQuantity.ToString());
		if (notify && onQuantityChanged != null)
		{
			onQuantityChanged.Invoke();
		}
	}

	private void ChangeQuantity(int change)
	{
		SetQuantity(SelectedQuantity + change);
	}

	private void OnQuantityInputSubmitted(string value)
	{
		if (int.TryParse(value, out var result))
		{
			SetQuantity(result);
		}
		else
		{
			SetQuantity(0);
		}
	}

	private void ValidateInput()
	{
		OnQuantityInputSubmitted(QuantityInput.text);
	}
}
