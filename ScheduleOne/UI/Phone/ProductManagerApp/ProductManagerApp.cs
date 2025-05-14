using System;
using System.Collections;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Product;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.ProductManagerApp;

public class ProductManagerApp : App<ProductManagerApp>
{
	[Serializable]
	public class ProductTypeContainer
	{
		public EDrugType DrugType;

		public RectTransform Container;

		public RectTransform NoneDisplay;

		public void RefreshNoneDisplay()
		{
			NoneDisplay.gameObject.SetActive(Container.childCount == 0);
		}
	}

	[Header("References")]
	public ProductTypeContainer FavouritesContainer;

	public List<ProductTypeContainer> ProductTypeContainers;

	public ProductAppDetailPanel DetailPanel;

	public RectTransform SelectionIndicator;

	public GameObject EntryPrefab;

	private List<ProductEntry> favouriteEntries = new List<ProductEntry>();

	private List<ProductEntry> entries = new List<ProductEntry>();

	private ProductEntry selectedEntry;

	protected override void Awake()
	{
		base.Awake();
		DetailPanel.SetActiveProduct(null);
	}

	protected override void Start()
	{
		base.Start();
		ProductManager productManager = NetworkSingleton<ProductManager>.Instance;
		productManager.onProductDiscovered = (Action<ProductDefinition>)Delegate.Combine(productManager.onProductDiscovered, new Action<ProductDefinition>(CreateEntry));
		ProductManager productManager2 = NetworkSingleton<ProductManager>.Instance;
		productManager2.onProductFavourited = (Action<ProductDefinition>)Delegate.Combine(productManager2.onProductFavourited, new Action<ProductDefinition>(ProductFavourited));
		ProductManager productManager3 = NetworkSingleton<ProductManager>.Instance;
		productManager3.onProductUnfavourited = (Action<ProductDefinition>)Delegate.Combine(productManager3.onProductUnfavourited, new Action<ProductDefinition>(ProductUnfavourited));
		foreach (ProductDefinition favouritedProduct in ProductManager.FavouritedProducts)
		{
			CreateFavouriteEntry(favouritedProduct);
		}
		foreach (ProductDefinition discoveredProduct in ProductManager.DiscoveredProducts)
		{
			CreateEntry(discoveredProduct);
		}
	}

	private void LateUpdate()
	{
		if (base.isOpen && selectedEntry != null)
		{
			SelectionIndicator.position = selectedEntry.transform.position;
		}
	}

	public virtual void CreateEntry(ProductDefinition definition)
	{
		ProductTypeContainer productTypeContainer = ProductTypeContainers.Find((ProductTypeContainer x) => x.DrugType == definition.DrugTypes[0].DrugType);
		ProductEntry component = UnityEngine.Object.Instantiate(EntryPrefab, productTypeContainer.Container).GetComponent<ProductEntry>();
		component.Initialize(definition);
		entries.Add(component);
		productTypeContainer.RefreshNoneDisplay();
		LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
	}

	private void ProductFavourited(ProductDefinition product)
	{
		CreateFavouriteEntry(product);
	}

	private void ProductUnfavourited(ProductDefinition product)
	{
		RemoveFavouriteEntry(product);
	}

	private void CreateFavouriteEntry(ProductDefinition definition)
	{
		if (!(favouriteEntries.Find((ProductEntry x) => x.Definition == definition) != null))
		{
			ProductEntry component = UnityEngine.Object.Instantiate(EntryPrefab, FavouritesContainer.Container).GetComponent<ProductEntry>();
			component.Initialize(definition);
			favouriteEntries.Add(component);
			FavouritesContainer.RefreshNoneDisplay();
			DelayedRebuildLayout();
		}
	}

	private void RemoveFavouriteEntry(ProductDefinition definition)
	{
		ProductEntry productEntry = favouriteEntries.Find((ProductEntry x) => x.Definition == definition);
		if (selectedEntry == productEntry)
		{
			selectedEntry = null;
			SelectionIndicator.gameObject.SetActive(value: false);
			DetailPanel.SetActiveProduct(null);
		}
		if (productEntry != null)
		{
			favouriteEntries.Remove(productEntry);
			productEntry.Destroy();
		}
		FavouritesContainer.RefreshNoneDisplay();
		DelayedRebuildLayout();
	}

	private void DelayedRebuildLayout()
	{
		StartCoroutine(Delay());
		IEnumerator Delay()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			yield return new WaitForEndOfFrame();
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			ContentSizeFitter[] componentsInChildren = GetComponentsInChildren<ContentSizeFitter>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
				componentsInChildren[i].enabled = true;
			}
		}
	}

	public void SelectProduct(ProductEntry entry)
	{
		selectedEntry = entry;
		DetailPanel.SetActiveProduct(entry.Definition);
		SelectionIndicator.position = entry.transform.position;
		SelectionIndicator.gameObject.SetActive(value: true);
	}

	public override void SetOpen(bool open)
	{
		base.SetOpen(open);
		VerticalLayoutGroup[] layoutGroups;
		if (open)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				entries[i].UpdateDiscovered(entries[i].Definition);
				entries[i].UpdateListed();
			}
			for (int j = 0; j < favouriteEntries.Count; j++)
			{
				favouriteEntries[j].UpdateDiscovered(favouriteEntries[j].Definition);
				favouriteEntries[j].UpdateListed();
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			base.gameObject.SetActive(value: false);
			base.gameObject.SetActive(value: true);
			layoutGroups = GetComponentsInChildren<VerticalLayoutGroup>();
			for (int k = 0; k < layoutGroups.Length; k++)
			{
				layoutGroups[k].enabled = false;
				layoutGroups[k].enabled = true;
			}
			if (selectedEntry != null)
			{
				DetailPanel.SetActiveProduct(selectedEntry.Definition);
			}
			StartCoroutine(Delay());
		}
		IEnumerator Delay()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			for (int l = 0; l < layoutGroups.Length; l++)
			{
				layoutGroups[l].enabled = false;
				layoutGroups[l].enabled = true;
			}
			yield return new WaitForEndOfFrame();
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			for (int m = 0; m < layoutGroups.Length; m++)
			{
				layoutGroups[m].enabled = false;
				layoutGroups[m].enabled = true;
			}
			yield return new WaitForEndOfFrame();
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
			for (int n = 0; n < layoutGroups.Length; n++)
			{
				layoutGroups[n].enabled = false;
				layoutGroups[n].enabled = true;
			}
		}
	}
}
