using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence;
using UnityEngine;

namespace ScheduleOne.UI.Shop;

[Serializable]
public class ShopListing
{
	[Serializable]
	public class CategoryInstance
	{
		public EShopCategory Category;
	}

	public enum ERestockRate
	{
		Daily = 0,
		Weekly = 1,
		Never = 2
	}

	public string name;

	public StorableItemDefinition Item;

	[Header("Pricing")]
	[SerializeField]
	protected bool OverridePrice;

	[SerializeField]
	protected float OverriddenPrice = 10f;

	[Header("Stock")]
	public bool LimitedStock;

	public int DefaultStock = -1;

	public ERestockRate RestockRate;

	[Header("Settings")]
	public bool EnforceMinimumGameCreationVersion;

	public float MinimumGameCreationVersion = 27f;

	public bool CanBeDelivered;

	[Header("Color")]
	public bool UseIconTint;

	public Color IconTint = Color.white;

	public Action onStockChanged;

	public bool IsInStock => true;

	public float Price
	{
		get
		{
			if (!OverridePrice)
			{
				return Item.BasePurchasePrice;
			}
			return OverriddenPrice;
		}
	}

	public bool IsUnlimitedStock => !LimitedStock;

	public ShopInterface Shop { get; private set; }

	public int CurrentStock { get; protected set; }

	public int QuantityInCart { get; private set; }

	public int CurrentStockMinusCart => CurrentStock - QuantityInCart;

	public void Initialize(ShopInterface shop)
	{
		Shop = shop;
	}

	public void Restock(bool network)
	{
		SetStock(DefaultStock);
	}

	public void RemoveStock(int quantity)
	{
		SetStock(CurrentStock - quantity);
	}

	public void SetStock(int quantity, bool network = true)
	{
		if (!IsUnlimitedStock)
		{
			if (network && NetworkSingleton<ShopManager>.InstanceExists && Shop != null)
			{
				NetworkSingleton<ShopManager>.Instance.SendStock(Shop.ShopCode, Item.ID, quantity);
			}
			CurrentStock = quantity;
			if (CurrentStock < 0)
			{
				CurrentStock = 0;
			}
			if (onStockChanged != null)
			{
				onStockChanged();
			}
		}
	}

	public virtual bool ShouldShow()
	{
		if (EnforceMinimumGameCreationVersion && SaveManager.GetVersionNumber(Singleton<MetadataManager>.Instance.CreationVersion) < MinimumGameCreationVersion)
		{
			return false;
		}
		return true;
	}

	public virtual bool DoesListingMatchCategoryFilter(EShopCategory category)
	{
		if (category != EShopCategory.All)
		{
			return Item.ShopCategories.Find((CategoryInstance x) => x.Category == category) != null;
		}
		return true;
	}

	public virtual bool DoesListingMatchSearchTerm(string searchTerm)
	{
		return Item.Name.ToLower().Contains(searchTerm.ToLower());
	}

	public void SetQuantityInCart(int quantity)
	{
		QuantityInCart = quantity;
		if (onStockChanged != null)
		{
			onStockChanged();
		}
	}
}
