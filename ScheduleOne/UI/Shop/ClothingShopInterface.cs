using System.Collections.Generic;
using System.Linq;
using ScheduleOne.Clothing;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.Variables;
using UnityEngine;

namespace ScheduleOne.UI.Shop;

public class ClothingShopInterface : ShopInterface
{
	public ShopColorPicker ColorPicker;

	private ShopListing _selectedListing;

	protected override void Start()
	{
		base.Start();
		ColorPicker.onColorPicked.AddListener(ColorPicked);
	}

	public override void ListingClicked(ListingUI listingUI)
	{
		if (listingUI.Listing.Item.IsPurchasable)
		{
			if ((listingUI.Listing.Item as ClothingDefinition).Colorable)
			{
				_selectedListing = listingUI.Listing;
				ColorPicker.Open(listingUI.Listing.Item);
			}
			else
			{
				base.ListingClicked(listingUI);
			}
		}
	}

	protected override void Exit(ExitAction action)
	{
		if (!action.Used)
		{
			if (ColorPicker != null && ColorPicker.IsOpen)
			{
				action.Used = true;
				ColorPicker.Close();
			}
			base.Exit(action);
		}
	}

	private void ColorPicked(EClothingColor color)
	{
		if (_selectedListing != null)
		{
			ClothingShopListing clothingShopListing = new ClothingShopListing();
			clothingShopListing.Item = _selectedListing.Item;
			clothingShopListing.Color = color;
			Cart.AddItem(clothingShopListing, 1);
			AddItemSound.Play();
		}
	}

	public override bool HandoverItems()
	{
		List<ItemSlot> availableSlots = GetAvailableSlots();
		List<ShopListing> list = Cart.cartDictionary.Keys.ToList();
		bool result = true;
		for (int i = 0; i < list.Count; i++)
		{
			NetworkSingleton<VariableDatabase>.Instance.NotifyItemAcquired(list[i].Item.ID, Cart.cartDictionary[list[i]]);
			int num = Cart.cartDictionary[list[i]];
			ClothingInstance clothingInstance = list[i].Item.GetDefaultInstance() as ClothingInstance;
			clothingInstance.Color = EClothingColor.White;
			if (list[i] is ClothingShopListing)
			{
				Console.Log("Color: " + (list[i] as ClothingShopListing).Color);
				clothingInstance.Color = (list[i] as ClothingShopListing).Color;
			}
			for (int j = 0; j < availableSlots.Count; j++)
			{
				if (num <= 0)
				{
					break;
				}
				int capacityForItem = availableSlots[j].GetCapacityForItem(clothingInstance);
				if (capacityForItem != 0)
				{
					int num2 = Mathf.Min(capacityForItem, num);
					availableSlots[j].AddItem(clothingInstance.GetCopy(num2));
					num -= num2;
				}
			}
			if (num > 0)
			{
				Debug.LogWarning("Failed to handover all items in cart: " + clothingInstance.Name);
				result = false;
			}
		}
		return result;
	}
}
