using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.ProductManagerApp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScheduleOne.Product;

public class ProductEntry : MonoBehaviour
{
	public Color SelectedColor;

	public Color DeselectedColor;

	public Color FavouritedColor;

	public Color UnfavouritedColor;

	[Header("References")]
	public Button Button;

	public Image Frame;

	public Image Icon;

	public RectTransform Tick;

	public RectTransform Cross;

	public EventTrigger Trigger;

	public Button FavouriteButton;

	public Image FavouriteIcon;

	public UnityEvent onHovered;

	private bool destroyed;

	public ProductDefinition Definition { get; private set; }

	public void Initialize(ProductDefinition definition)
	{
		Definition = definition;
		Icon.sprite = definition.Icon;
		Button.onClick.AddListener(Clicked);
		FavouriteButton.onClick.AddListener(FavouriteClicked);
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerEnter;
		entry.callback.AddListener(delegate
		{
			onHovered.Invoke();
		});
		Trigger.triggers.Add(entry);
		UpdateListed();
		UpdateFavourited();
		UpdateDiscovered(Definition);
		ProductManager instance = NetworkSingleton<ProductManager>.Instance;
		instance.onProductDiscovered = (Action<ProductDefinition>)Delegate.Combine(instance.onProductDiscovered, new Action<ProductDefinition>(UpdateDiscovered));
		ProductManager instance2 = NetworkSingleton<ProductManager>.Instance;
		instance2.onProductListed = (Action<ProductDefinition>)Delegate.Combine(instance2.onProductListed, new Action<ProductDefinition>(ProductListedOrDelisted));
		ProductManager instance3 = NetworkSingleton<ProductManager>.Instance;
		instance3.onProductDelisted = (Action<ProductDefinition>)Delegate.Combine(instance3.onProductDelisted, new Action<ProductDefinition>(ProductListedOrDelisted));
		ProductManager instance4 = NetworkSingleton<ProductManager>.Instance;
		instance4.onProductFavourited = (Action<ProductDefinition>)Delegate.Combine(instance4.onProductFavourited, new Action<ProductDefinition>(ProductFavouritedOrUnFavourited));
		ProductManager instance5 = NetworkSingleton<ProductManager>.Instance;
		instance5.onProductUnfavourited = (Action<ProductDefinition>)Delegate.Combine(instance5.onProductUnfavourited, new Action<ProductDefinition>(ProductFavouritedOrUnFavourited));
	}

	public void Destroy()
	{
		destroyed = true;
		base.gameObject.SetActive(value: false);
		UnityEngine.Object.DestroyImmediate(base.gameObject);
	}

	private void OnDestroy()
	{
		ProductManager instance = NetworkSingleton<ProductManager>.Instance;
		instance.onProductDiscovered = (Action<ProductDefinition>)Delegate.Remove(instance.onProductDiscovered, new Action<ProductDefinition>(UpdateDiscovered));
		ProductManager instance2 = NetworkSingleton<ProductManager>.Instance;
		instance2.onProductListed = (Action<ProductDefinition>)Delegate.Remove(instance2.onProductListed, new Action<ProductDefinition>(ProductListedOrDelisted));
		ProductManager instance3 = NetworkSingleton<ProductManager>.Instance;
		instance3.onProductDelisted = (Action<ProductDefinition>)Delegate.Remove(instance3.onProductDelisted, new Action<ProductDefinition>(ProductListedOrDelisted));
		ProductManager instance4 = NetworkSingleton<ProductManager>.Instance;
		instance4.onProductFavourited = (Action<ProductDefinition>)Delegate.Remove(instance4.onProductFavourited, new Action<ProductDefinition>(ProductFavouritedOrUnFavourited));
		ProductManager instance5 = NetworkSingleton<ProductManager>.Instance;
		instance5.onProductUnfavourited = (Action<ProductDefinition>)Delegate.Remove(instance5.onProductUnfavourited, new Action<ProductDefinition>(ProductFavouritedOrUnFavourited));
	}

	private void Clicked()
	{
		PlayerSingleton<ProductManagerApp>.Instance.SelectProduct(this);
		UpdateListed();
	}

	private void FavouriteClicked()
	{
		if (ProductManager.DiscoveredProducts.Contains(Definition))
		{
			if (ProductManager.FavouritedProducts.Contains(Definition))
			{
				NetworkSingleton<ProductManager>.Instance.SetProductFavourited(Definition.ID, listed: false);
			}
			else
			{
				NetworkSingleton<ProductManager>.Instance.SetProductFavourited(Definition.ID, listed: true);
			}
		}
	}

	private void ProductListedOrDelisted(ProductDefinition def)
	{
		if (def == Definition)
		{
			UpdateListed();
		}
	}

	public void UpdateListed()
	{
		if (!destroyed && !(this == null) && !(base.gameObject == null))
		{
			if (ProductManager.ListedProducts.Contains(Definition))
			{
				Frame.color = SelectedColor;
				Tick.gameObject.SetActive(value: true);
				Cross.gameObject.SetActive(value: false);
			}
			else
			{
				Frame.color = DeselectedColor;
				Tick.gameObject.SetActive(value: false);
				Cross.gameObject.SetActive(value: true);
			}
		}
	}

	private void ProductFavouritedOrUnFavourited(ProductDefinition def)
	{
		if (def == Definition)
		{
			UpdateFavourited();
		}
	}

	public void UpdateFavourited()
	{
		if (!destroyed && !(this == null) && !(base.gameObject == null))
		{
			if (ProductManager.FavouritedProducts.Contains(Definition))
			{
				FavouriteIcon.color = FavouritedColor;
			}
			else
			{
				FavouriteIcon.color = UnfavouritedColor;
			}
		}
	}

	public void UpdateDiscovered(ProductDefinition def)
	{
		if (def == null)
		{
			Console.LogWarning(def?.ToString() + " productDefinition is null");
		}
		if (def.ID == Definition.ID)
		{
			if (ProductManager.DiscoveredProducts.Contains(Definition))
			{
				Icon.color = Color.white;
			}
			else
			{
				Icon.color = Color.black;
			}
			UpdateListed();
		}
	}
}
