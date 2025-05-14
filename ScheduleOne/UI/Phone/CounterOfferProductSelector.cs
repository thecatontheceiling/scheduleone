using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone;

public class CounterOfferProductSelector : MonoBehaviour
{
	public const int ENTRIES_PER_PAGE = 25;

	public RectTransform Container;

	public InputField SearchBar;

	public RectTransform ProductContainer;

	public Text PageLabel;

	public GameObject ProductEntryPrefab;

	public Action<ProductDefinition> onProductPreviewed;

	public Action<ProductDefinition> onProductSelected;

	private List<RectTransform> productEntries = new List<RectTransform>();

	private Dictionary<ProductDefinition, RectTransform> productEntriesDict = new Dictionary<ProductDefinition, RectTransform>();

	private string searchTerm = string.Empty;

	private int pageIndex;

	private int pageCount;

	private List<ProductDefinition> results = new List<ProductDefinition>();

	private ProductDefinition lastPreviewedResult;

	public bool IsOpen { get; private set; }

	public void Awake()
	{
		SearchBar.onValueChanged.AddListener(SetSearchTerm);
	}

	public void Open()
	{
		IsOpen = true;
		Container.gameObject.SetActive(value: true);
		EnsureAllEntriesExist();
		SetSearchTerm(string.Empty);
		SearchBar.ActivateInputField();
	}

	public void Close()
	{
		IsOpen = false;
		Container.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (IsOpen && GameInput.GetButtonDown(GameInput.ButtonCode.Submit) && lastPreviewedResult != null)
		{
			ProductSelected(lastPreviewedResult);
		}
	}

	public void SetSearchTerm(string search)
	{
		searchTerm = search.ToLower();
		SearchBar.SetTextWithoutNotify(searchTerm);
		RebuildResultsList();
		if (search != string.Empty && results.Count > 0)
		{
			ProductHovered(results[0]);
		}
	}

	private void RebuildResultsList()
	{
		results = GetMatchingProducts(searchTerm);
		results.Sort(delegate(ProductDefinition a, ProductDefinition b)
		{
			int num = a.DrugType.CompareTo(b.DrugType);
			return (num != 0) ? num : a.Name.CompareTo(b.Name);
		});
		Console.Log($"Found {results.Count} results for {searchTerm}");
		pageCount = Mathf.CeilToInt((float)results.Count / 25f);
		SetPage(pageIndex);
	}

	private List<ProductDefinition> GetMatchingProducts(string searchTerm)
	{
		List<ProductDefinition> list = new List<ProductDefinition>();
		List<EDrugType> list2 = new List<EDrugType>();
		foreach (EDrugType value in Enum.GetValues(typeof(EDrugType)))
		{
			if (searchTerm.ToLower().Contains(value.ToString().ToLower()))
			{
				list2.Add(value);
			}
		}
		if (searchTerm.ToLower().Contains("weed"))
		{
			list2.Add(EDrugType.Marijuana);
		}
		if (searchTerm.ToLower().Contains("coke"))
		{
			list2.Add(EDrugType.Cocaine);
		}
		if (searchTerm.ToLower().Contains("meth"))
		{
			list2.Add(EDrugType.Methamphetamine);
		}
		foreach (ProductDefinition discoveredProduct in ProductManager.DiscoveredProducts)
		{
			if (list2.Contains(discoveredProduct.DrugType))
			{
				list.Add(discoveredProduct);
			}
			else if (discoveredProduct.Name.ToLower().Contains(searchTerm))
			{
				list.Add(discoveredProduct);
			}
		}
		return list;
	}

	private void EnsureAllEntriesExist()
	{
		foreach (ProductDefinition discoveredProduct in ProductManager.DiscoveredProducts)
		{
			if (!productEntriesDict.ContainsKey(discoveredProduct))
			{
				CreateProductEntry(discoveredProduct);
			}
		}
	}

	private void CreateProductEntry(ProductDefinition product)
	{
		if (!productEntriesDict.ContainsKey(product))
		{
			RectTransform component = UnityEngine.Object.Instantiate(ProductEntryPrefab, ProductContainer).GetComponent<RectTransform>();
			component.Find("Icon").GetComponent<Image>().sprite = product.Icon;
			component.GetComponent<Button>().onClick.AddListener(delegate
			{
				ProductSelected(product);
			});
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener(delegate
			{
				ProductHovered(product);
			});
			component.gameObject.AddComponent<EventTrigger>().triggers.Add(entry);
			productEntries.Add(component);
			productEntriesDict.Add(product, component);
		}
	}

	public void ChangePage(int change)
	{
		SetPage(pageIndex + change);
	}

	private void SetPage(int page)
	{
		pageIndex = Mathf.Clamp(page, 0, Mathf.Max(pageCount - 1, 0));
		int num = pageIndex * 25;
		int num2 = Mathf.Min(num + 25, results.Count);
		Console.Log($"Page {pageIndex + 1} / {pageCount} ({num} - {num2})");
		List<ProductDefinition> range = results.GetRange(num, num2 - num);
		List<ProductDefinition> list = productEntriesDict.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			RectTransform rectTransform = productEntriesDict[list[i]];
			if (range.Contains(list[i]))
			{
				rectTransform.gameObject.SetActive(value: true);
			}
			else
			{
				rectTransform.gameObject.SetActive(value: false);
			}
		}
		for (int j = 0; j < range.Count; j++)
		{
			productEntriesDict[range[j]].SetSiblingIndex(j);
		}
		PageLabel.text = $"{pageIndex + 1} / {pageCount}";
	}

	private void ProductHovered(ProductDefinition def)
	{
		if (onProductPreviewed != null)
		{
			onProductPreviewed(def);
		}
		lastPreviewedResult = def;
	}

	private void ProductSelected(ProductDefinition def)
	{
		if (onProductSelected != null)
		{
			onProductSelected(def);
		}
		Close();
	}

	public bool IsMouseOverSelector()
	{
		bool flag = RectTransformUtility.RectangleContainsScreenPoint(Container, GameInput.MousePosition, PlayerSingleton<PlayerCamera>.Instance.OverlayCamera);
		Console.Log($"Mouse over selector: {flag}");
		return flag;
	}
}
