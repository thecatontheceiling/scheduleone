using System;
using System.IO;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Product;
using ScheduleOne.UI;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class ProductManagerLoader : Loader
{
	public override void Load(string mainPath)
	{
		string contents;
		bool flag = TryLoadFile(mainPath, out contents);
		if (!flag)
		{
			TryLoadFile(Path.Combine(mainPath, "Products"), out contents);
		}
		string text = Path.Combine(mainPath, "CreatedProducts");
		if (Directory.Exists(text) && !flag)
		{
			Console.LogWarning("Loading legacy product data from " + text);
			WeedProductLoader weedProductLoader = new WeedProductLoader();
			MethProductLoader methProductLoader = new MethProductLoader();
			CocaineProductLoader cocaineProductLoader = new CocaineProductLoader();
			string[] files = Directory.GetFiles(text);
			for (int i = 0; i < files.Length; i++)
			{
				if (!TryLoadFile(files[i], out var contents2, autoAddExtension: false))
				{
					continue;
				}
				ProductData productData = null;
				try
				{
					productData = JsonUtility.FromJson<ProductData>(contents2);
				}
				catch (Exception ex)
				{
					Debug.LogError("Error loading product data: " + ex.Message);
				}
				if (productData == null)
				{
					continue;
				}
				bool flag2 = false;
				if (string.IsNullOrEmpty(productData.Name))
				{
					Console.LogWarning("Product name is empty; generating random name");
					if (Singleton<NewMixScreen>.InstanceExists)
					{
						productData.Name = Singleton<NewMixScreen>.Instance.GenerateUniqueName();
					}
					else
					{
						productData.Name = "Product " + UnityEngine.Random.Range(0, 1000);
					}
					flag2 = true;
				}
				if (string.IsNullOrEmpty(productData.ID))
				{
					Console.LogWarning("Product ID is empty; generating from name");
					productData.ID = ProductManager.MakeIDFileSafe(productData.Name);
					flag2 = true;
				}
				if (flag2)
				{
					try
					{
						File.WriteAllText(files[i], productData.GetJson());
					}
					catch (Exception ex2)
					{
						Console.LogError("Error saving modified product data: " + ex2.Message);
					}
				}
				switch (productData.DrugType)
				{
				case EDrugType.Marijuana:
					weedProductLoader.Load(files[i]);
					break;
				case EDrugType.Methamphetamine:
					methProductLoader.Load(files[i]);
					break;
				case EDrugType.Cocaine:
					cocaineProductLoader.Load(files[i]);
					break;
				default:
					Console.LogError("Unknown drug type: " + productData.DrugType);
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(contents))
		{
			ProductManagerData productManagerData = JsonUtility.FromJson<ProductManagerData>(contents);
			if (productManagerData == null)
			{
				return;
			}
			LoadProducts(productManagerData);
			if (productManagerData.DiscoveredProducts != null)
			{
				for (int j = 0; j < productManagerData.DiscoveredProducts.Length; j++)
				{
					if (productManagerData.DiscoveredProducts[j] != null)
					{
						NetworkSingleton<ProductManager>.Instance.SetProductDiscovered(null, productManagerData.DiscoveredProducts[j], autoList: false);
					}
				}
			}
			if (productManagerData.ListedProducts != null)
			{
				for (int k = 0; k < productManagerData.ListedProducts.Length; k++)
				{
					if (productManagerData.ListedProducts[k] != null)
					{
						NetworkSingleton<ProductManager>.Instance.SetProductListed(null, productManagerData.ListedProducts[k], listed: true);
					}
				}
			}
			if (productManagerData.FavouritedProducts != null)
			{
				for (int l = 0; l < productManagerData.FavouritedProducts.Length; l++)
				{
					if (productManagerData.FavouritedProducts[l] != null)
					{
						NetworkSingleton<ProductManager>.Instance.SetProductFavourited(null, productManagerData.FavouritedProducts[l], fav: true);
					}
				}
			}
			if (productManagerData.ActiveMixOperation != null && productManagerData.ActiveMixOperation.ProductID != string.Empty)
			{
				NetworkSingleton<ProductManager>.Instance.SendMixOperation(productManagerData.ActiveMixOperation, productManagerData.IsMixComplete);
			}
			if (productManagerData.MixRecipes != null)
			{
				for (int m = 0; m < productManagerData.MixRecipes.Length; m++)
				{
					if (productManagerData.MixRecipes[m] != null)
					{
						try
						{
							MixRecipeData mixRecipeData = productManagerData.MixRecipes[m];
							NetworkSingleton<ProductManager>.Instance.CreateMixRecipe(null, mixRecipeData.Product, mixRecipeData.Mixer, mixRecipeData.Output);
						}
						catch (Exception ex3)
						{
							Console.LogError("Error loading mix recipe: " + ex3.Message);
						}
					}
				}
			}
			if (productManagerData.ProductPrices == null)
			{
				return;
			}
			for (int n = 0; n < productManagerData.ProductPrices.Length; n++)
			{
				if (productManagerData.ProductPrices[n] != null)
				{
					StringIntPair stringIntPair = productManagerData.ProductPrices[n];
					ProductDefinition item = Registry.GetItem<ProductDefinition>(stringIntPair.String);
					if (item != null)
					{
						NetworkSingleton<ProductManager>.Instance.SetPrice(null, item.ID, stringIntPair.Int);
					}
				}
			}
		}
		else
		{
			Console.LogWarning("Did not find product data file in " + mainPath);
		}
	}

	private void SanitizeProductData(ProductData data)
	{
		if (data == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(data.Name))
		{
			Console.LogWarning("Product name is empty; generating random name");
			if (Singleton<NewMixScreen>.InstanceExists)
			{
				data.Name = Singleton<NewMixScreen>.Instance.GenerateUniqueName();
			}
			else
			{
				data.Name = "Product " + UnityEngine.Random.Range(0, 1000);
			}
		}
		if (string.IsNullOrEmpty(data.ID))
		{
			Console.LogWarning("Product ID is empty; generating from name");
			data.ID = ProductManager.MakeIDFileSafe(data.Name);
		}
	}

	private void LoadProducts(ProductManagerData productData)
	{
		if (productData == null)
		{
			return;
		}
		if (productData.CreatedWeed != null)
		{
			WeedProductData[] createdWeed = productData.CreatedWeed;
			foreach (WeedProductData weedProductData in createdWeed)
			{
				if (weedProductData != null)
				{
					SanitizeProductData(weedProductData);
					NetworkSingleton<ProductManager>.Instance.CreateWeed_Server(weedProductData.Name, weedProductData.ID, weedProductData.DrugType, weedProductData.Properties.ToList(), weedProductData.AppearanceSettings);
				}
			}
		}
		if (productData.CreatedMeth != null)
		{
			MethProductData[] createdMeth = productData.CreatedMeth;
			foreach (MethProductData methProductData in createdMeth)
			{
				if (methProductData != null)
				{
					SanitizeProductData(methProductData);
					NetworkSingleton<ProductManager>.Instance.CreateMeth_Server(methProductData.Name, methProductData.ID, methProductData.DrugType, methProductData.Properties.ToList(), methProductData.AppearanceSettings);
				}
			}
		}
		if (productData.CreatedCocaine == null)
		{
			return;
		}
		CocaineProductData[] createdCocaine = productData.CreatedCocaine;
		foreach (CocaineProductData cocaineProductData in createdCocaine)
		{
			if (cocaineProductData != null)
			{
				SanitizeProductData(cocaineProductData);
				NetworkSingleton<ProductManager>.Instance.CreateCocaine_Server(cocaineProductData.Name, cocaineProductData.ID, cocaineProductData.DrugType, cocaineProductData.Properties.ToList(), cocaineProductData.AppearanceSettings);
			}
		}
	}
}
