using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Messaging;
using ScheduleOne.Money;
using ScheduleOne.Product;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone;

public class CounterofferInterface : MonoBehaviour
{
	public const int COUNTEROFFER_SUCCESS_XP = 5;

	public const int MinQuantity = 1;

	public int MaxQuantity = 50;

	public const float MinPrice = 1f;

	public const float MaxPrice = 9999f;

	public float IconAlignment = 0.2f;

	public GameObject ProductEntryPrefab;

	[Header("References")]
	public GameObject Container;

	public Text TitleLabel;

	public Button ConfirmButton;

	public Image ProductIcon;

	public Text ProductLabel;

	public RectTransform ProductLabelRect;

	public InputField PriceInput;

	public Text FairPriceLabel;

	public CounterOfferProductSelector ProductSelector;

	private Action<ProductDefinition, int, float> orderConfirmedCallback;

	private ProductDefinition selectedProduct;

	private int quantity;

	private float price;

	private Dictionary<ProductDefinition, RectTransform> productEntries = new Dictionary<ProductDefinition, RectTransform>();

	private bool mouseUp;

	private MSGConversation conversation;

	public bool IsOpen { get; private set; }

	private void Awake()
	{
		CounterOfferProductSelector productSelector = ProductSelector;
		productSelector.onProductPreviewed = (Action<ProductDefinition>)Delegate.Combine(productSelector.onProductPreviewed, new Action<ProductDefinition>(DisplayProduct));
		CounterOfferProductSelector productSelector2 = ProductSelector;
		productSelector2.onProductSelected = (Action<ProductDefinition>)Delegate.Combine(productSelector2.onProductSelected, new Action<ProductDefinition>(SetProduct));
	}

	private void Start()
	{
		GameInput.RegisterExitListener(Exit, 4);
		Close();
	}

	private void Update()
	{
		if (ProductSelector.IsOpen && GameInput.GetButtonUp(GameInput.ButtonCode.PrimaryClick) && mouseUp && !ProductSelector.IsMouseOverSelector())
		{
			ProductSelector.Close();
		}
		if (!GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
		{
			mouseUp = true;
		}
	}

	public void Open(ProductDefinition product, int quantity, float price, MSGConversation _conversation, Action<ProductDefinition, int, float> _orderConfirmedCallback)
	{
		IsOpen = true;
		selectedProduct = product;
		this.quantity = Mathf.Clamp(quantity, 1, MaxQuantity);
		this.price = price;
		conversation = _conversation;
		MSGConversation mSGConversation = conversation;
		mSGConversation.onMessageRendered = (Action)Delegate.Combine(mSGConversation.onMessageRendered, new Action(Close));
		orderConfirmedCallback = _orderConfirmedCallback;
		Container.gameObject.SetActive(value: true);
		SetProduct(product);
		PriceInput.text = price.ToString();
	}

	public void Close()
	{
		IsOpen = false;
		if (conversation != null)
		{
			MSGConversation mSGConversation = conversation;
			mSGConversation.onMessageRendered = (Action)Delegate.Remove(mSGConversation.onMessageRendered, new Action(Close));
		}
		if (ProductSelector.IsOpen)
		{
			ProductSelector.Close();
		}
		Container.gameObject.SetActive(value: false);
	}

	public void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen)
		{
			action.Used = true;
			Close();
		}
	}

	public void Send()
	{
		if (float.TryParse(PriceInput.text, out var result))
		{
			price = result;
		}
		price = Mathf.Clamp(price, 1f, 9999f);
		PriceInput.SetTextWithoutNotify(price.ToString());
		if (orderConfirmedCallback != null)
		{
			orderConfirmedCallback(selectedProduct, quantity, price);
		}
		Close();
	}

	private void UpdateFairPrice()
	{
		float amount = selectedProduct.MarketValue * (float)quantity;
		FairPriceLabel.text = "Fair price: " + MoneyManager.FormatAmount(amount);
	}

	private void SetProduct(ProductDefinition newProduct)
	{
		selectedProduct = newProduct;
		DisplayProduct(newProduct);
		UpdateFairPrice();
		ProductSelector.Close();
	}

	private void DisplayProduct(ProductDefinition tempProduct)
	{
		ProductIcon.sprite = tempProduct.Icon;
		UpdatePriceQuantityLabel(tempProduct.Name);
	}

	public void ChangeQuantity(int change)
	{
		quantity = Mathf.Clamp(quantity + change, 1, MaxQuantity);
		UpdatePriceQuantityLabel(selectedProduct.Name);
		UpdateFairPrice();
	}

	private void UpdatePriceQuantityLabel(string productName)
	{
		ProductLabel.text = quantity + "x " + productName;
		float value = 0f - ProductLabel.preferredWidth / 2f + 20f;
		ProductLabelRect.anchoredPosition = new Vector2(Mathf.Clamp(value, -120f, float.MaxValue), ProductLabelRect.anchoredPosition.y);
	}

	public void ChangePrice(float change)
	{
		price = Mathf.Clamp(price + change, 1f, 9999f);
		PriceInput.SetTextWithoutNotify(price.ToString());
	}

	public void PriceSubmitted(string value)
	{
		if (float.TryParse(value, out var result))
		{
			price = result;
		}
		else
		{
			price = 0f;
		}
		price = Mathf.Clamp(price, 1f, 9999f);
		PriceInput.SetTextWithoutNotify(price.ToString());
	}

	public void OpenProductSelector()
	{
		if (mouseUp)
		{
			mouseUp = false;
			ProductSelector.Open();
		}
	}
}
