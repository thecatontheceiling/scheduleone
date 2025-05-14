using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Properties;
using ScheduleOne.Properties.MixMaps;
using ScheduleOne.StationFramework;
using ScheduleOne.UI;
using ScheduleOne.Variables;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Product;

public class ProductManager : NetworkSingleton<ProductManager>, IBaseSaveable, ISaveable
{
	public const int MIN_PRICE = 1;

	public const int MAX_PRICE = 999;

	public Action<ProductDefinition> onProductDiscovered;

	public static List<ProductDefinition> DiscoveredProducts = new List<ProductDefinition>();

	public static List<ProductDefinition> ListedProducts = new List<ProductDefinition>();

	public static List<ProductDefinition> FavouritedProducts = new List<ProductDefinition>();

	public List<ProductDefinition> AllProducts = new List<ProductDefinition>();

	public List<ProductDefinition> DefaultKnownProducts = new List<ProductDefinition>();

	public List<PropertyItemDefinition> ValidMixIngredients = new List<PropertyItemDefinition>();

	public AnimationCurve SampleSuccessCurve;

	[Header("Default Products")]
	public WeedDefinition DefaultWeed;

	public CocaineDefinition DefaultCocaine;

	public MethDefinition DefaultMeth;

	[Header("Mix Maps")]
	public MixerMap WeedMixMap;

	public MixerMap MethMixMap;

	public MixerMap CokeMixMap;

	private List<ProductDefinition> createdProducts = new List<ProductDefinition>();

	public Action<NewMixOperation> onMixCompleted;

	public Action<ProductDefinition> onNewProductCreated;

	public Action<ProductDefinition> onProductListed;

	public Action<ProductDefinition> onProductDelisted;

	public Action<ProductDefinition> onProductFavourited;

	public Action<ProductDefinition> onProductUnfavourited;

	public UnityEvent onFirstSampleRejection;

	public UnityEvent onSecondUniqueProductCreated;

	public List<string> ProductNames = new List<string>();

	private List<StationRecipe> mixRecipes = new List<StationRecipe>();

	public Action<StationRecipe> onMixRecipeAdded;

	private Dictionary<ProductDefinition, float> ProductPrices = new Dictionary<ProductDefinition, float>();

	private ProductDefinition highestValueProduct;

	private ProductManagerLoader loader = new ProductManagerLoader();

	private bool NetworkInitialize___EarlyScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted;

	public static bool MethDiscovered => DiscoveredProducts.Any((ProductDefinition p) => p.ID == "meth");

	public static bool CocaineDiscovered => DiscoveredProducts.Any((ProductDefinition p) => p.ID == "cocaine");

	public static bool IsAcceptingOrders { get; private set; } = true;

	public NewMixOperation CurrentMixOperation { get; private set; }

	public bool IsMixingInProgress => CurrentMixOperation != null;

	public bool IsMixComplete { get; private set; }

	public float TimeSinceProductListingChanged { get; private set; }

	public string SaveFolderName => "Products";

	public string SaveFileName => "Products";

	public Loader Loader => loader;

	public bool ShouldSaveUnderFolder => false;

	public List<string> LocalExtraFiles { get; set; } = new List<string>();

	public List<string> LocalExtraFolders { get; set; } = new List<string>();

	public bool HasChanged { get; set; } = true;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EProduct_002EProductManager_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Start()
	{
		base.Start();
		Singleton<LoadManager>.Instance.onPreSceneChange.AddListener(Clean);
		foreach (ProductDefinition defaultKnownProduct in DefaultKnownProducts)
		{
			defaultKnownProduct.OnValidate();
			if (highestValueProduct == null || defaultKnownProduct.MarketValue > highestValueProduct.MarketValue)
			{
				highestValueProduct = defaultKnownProduct;
			}
		}
		foreach (ProductDefinition allProduct in AllProducts)
		{
			if (!ProductNames.Contains(allProduct.Name))
			{
				ProductNames.Add(allProduct.Name);
			}
			if (!ProductPrices.ContainsKey(allProduct))
			{
				ProductPrices.Add(allProduct, allProduct.MarketValue);
			}
		}
		NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance._onSleepEnd.AddListener(OnNewDay);
		TimeManager timeManager = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance;
		timeManager.onMinutePass = (Action)Delegate.Combine(timeManager.onMinutePass, new Action(OnMinPass));
		foreach (PropertyItemDefinition validMixIngredient in ValidMixIngredients)
		{
			for (int i = 0; i < validMixIngredient.Properties.Count; i++)
			{
				if (!Singleton<PropertyUtility>.Instance.AllProperties.Contains(validMixIngredient.Properties[i]))
				{
					Console.LogError("Mixer " + validMixIngredient.Name + " has property " + validMixIngredient.Properties[i]?.ToString() + " that is not in the valid properties list");
				}
			}
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		for (int i = 0; i < DefaultKnownProducts.Count; i++)
		{
			SetProductDiscovered(null, DefaultKnownProducts[i].ID, autoList: false);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		RefreshHighestValueProduct();
	}

	private void Update()
	{
		TimeSinceProductListingChanged += Time.deltaTime;
	}

	private void Clean()
	{
		DiscoveredProducts.Clear();
		ListedProducts.Clear();
		FavouritedProducts.Clear();
		IsAcceptingOrders = true;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetMethDiscovered()
	{
		RpcWriter___Server_SetMethDiscovered_2166136261();
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetCocaineDiscovered()
	{
		RpcWriter___Server_SetCocaineDiscovered_2166136261();
	}

	public virtual void InitializeSaveable()
	{
		Singleton<SaveManager>.Instance.RegisterSaveable(this);
	}

	public MixerMap GetMixerMap(EDrugType type)
	{
		switch (type)
		{
		case EDrugType.Marijuana:
			return WeedMixMap;
		case EDrugType.Methamphetamine:
			return MethMixMap;
		case EDrugType.Cocaine:
			return CokeMixMap;
		default:
			Console.LogError("No mixer map found for " + type);
			return null;
		}
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		foreach (ProductDefinition createdProduct in createdProducts)
		{
			if (createdProduct is WeedDefinition)
			{
				WeedDefinition weedDefinition = createdProduct as WeedDefinition;
				WeedAppearanceSettings appearance = new WeedAppearanceSettings(weedDefinition.MainMat.color, weedDefinition.SecondaryMat.color, weedDefinition.LeafMat.color, weedDefinition.StemMat.color);
				List<string> list = new List<string>();
				foreach (ScheduleOne.Properties.Property property in weedDefinition.Properties)
				{
					list.Add(property.ID);
				}
				CreateWeed(connection, createdProduct.Name, createdProduct.ID, EDrugType.Marijuana, list, appearance);
			}
			else if (createdProduct is MethDefinition)
			{
				MethDefinition obj = createdProduct as MethDefinition;
				MethAppearanceSettings appearanceSettings = obj.AppearanceSettings;
				List<string> list2 = new List<string>();
				foreach (ScheduleOne.Properties.Property property2 in obj.Properties)
				{
					list2.Add(property2.ID);
				}
				CreateMeth(connection, createdProduct.Name, createdProduct.ID, EDrugType.Methamphetamine, list2, appearanceSettings);
			}
			else
			{
				if (!(createdProduct is CocaineDefinition))
				{
					continue;
				}
				CocaineDefinition obj2 = createdProduct as CocaineDefinition;
				CocaineAppearanceSettings appearanceSettings2 = obj2.AppearanceSettings;
				List<string> list3 = new List<string>();
				foreach (ScheduleOne.Properties.Property property3 in obj2.Properties)
				{
					list3.Add(property3.ID);
				}
				CreateCocaine(connection, createdProduct.Name, createdProduct.ID, EDrugType.Cocaine, list3, appearanceSettings2);
			}
		}
		for (int i = 0; i < mixRecipes.Count; i++)
		{
			CreateMixRecipe(null, mixRecipes[i].Ingredients[1].Items[0].ID, mixRecipes[i].Ingredients[0].Items[0].ID, mixRecipes[i].Product.Item.ID);
		}
		for (int j = 0; j < DiscoveredProducts.Count; j++)
		{
			SetProductDiscovered(connection, DiscoveredProducts[j].ID, autoList: false);
		}
		for (int k = 0; k < ListedProducts.Count; k++)
		{
			SetProductListed(connection, ListedProducts[k].ID, listed: true);
		}
		for (int l = 0; l < FavouritedProducts.Count; l++)
		{
			SetProductFavourited(connection, FavouritedProducts[l].ID, fav: true);
		}
		foreach (KeyValuePair<ProductDefinition, float> productPrice in ProductPrices)
		{
			SetPrice(connection, productPrice.Key.ID, productPrice.Value);
		}
	}

	private void OnMinPass()
	{
		if (!NetworkSingleton<VariableDatabase>.InstanceExists || GameManager.IS_TUTORIAL || NetworkSingleton<VariableDatabase>.Instance.GetValue<bool>("SecondUniqueProductDiscovered"))
		{
			return;
		}
		float value = NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("Inventory_OGKush");
		if (NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("Inventory_Weed_Count") > value)
		{
			NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("SecondUniqueProductDiscovered", true.ToString());
			if (onSecondUniqueProductCreated != null)
			{
				onSecondUniqueProductCreated.Invoke();
			}
		}
	}

	private void OnNewDay()
	{
		if (InstanceFinder.IsServer && CurrentMixOperation != null && !IsMixComplete)
		{
			SetMixOperation(CurrentMixOperation, complete: true);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SetProductListed(string productID, bool listed)
	{
		RpcWriter___Server_SetProductListed_310431262(productID, listed);
		RpcLogic___SetProductListed_310431262(productID, listed);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetProductListed(NetworkConnection conn, string productID, bool listed)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetProductListed_619441887(conn, productID, listed);
			RpcLogic___SetProductListed_619441887(conn, productID, listed);
		}
		else
		{
			RpcWriter___Target_SetProductListed_619441887(conn, productID, listed);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SetProductFavourited(string productID, bool listed)
	{
		RpcWriter___Server_SetProductFavourited_310431262(productID, listed);
		RpcLogic___SetProductFavourited_310431262(productID, listed);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetProductFavourited(NetworkConnection conn, string productID, bool fav)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetProductFavourited_619441887(conn, productID, fav);
			RpcLogic___SetProductFavourited_619441887(conn, productID, fav);
		}
		else
		{
			RpcWriter___Target_SetProductFavourited_619441887(conn, productID, fav);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void DiscoverProduct(string productID)
	{
		RpcWriter___Server_DiscoverProduct_3615296227(productID);
		RpcLogic___DiscoverProduct_3615296227(productID);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetProductDiscovered(NetworkConnection conn, string productID, bool autoList)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetProductDiscovered_619441887(conn, productID, autoList);
			RpcLogic___SetProductDiscovered_619441887(conn, productID, autoList);
		}
		else
		{
			RpcWriter___Target_SetProductDiscovered_619441887(conn, productID, autoList);
		}
	}

	public void SetIsAcceptingOrder(bool accepting)
	{
		IsAcceptingOrders = accepting;
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void CreateWeed_Server(string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		RpcWriter___Server_CreateWeed_Server_2331775230(name, id, type, properties, appearance);
		RpcLogic___CreateWeed_Server_2331775230(name, id, type, properties, appearance);
	}

	[TargetRpc]
	[ObserversRpc(RunLocally = true)]
	private void CreateWeed(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_CreateWeed_1777266891(conn, name, id, type, properties, appearance);
			RpcLogic___CreateWeed_1777266891(conn, name, id, type, properties, appearance);
		}
		else
		{
			RpcWriter___Target_CreateWeed_1777266891(conn, name, id, type, properties, appearance);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void CreateCocaine_Server(string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		RpcWriter___Server_CreateCocaine_Server_891166717(name, id, type, properties, appearance);
		RpcLogic___CreateCocaine_Server_891166717(name, id, type, properties, appearance);
	}

	[TargetRpc]
	[ObserversRpc(RunLocally = true)]
	private void CreateCocaine(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_CreateCocaine_1327282946(conn, name, id, type, properties, appearance);
			RpcLogic___CreateCocaine_1327282946(conn, name, id, type, properties, appearance);
		}
		else
		{
			RpcWriter___Target_CreateCocaine_1327282946(conn, name, id, type, properties, appearance);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void CreateMeth_Server(string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		RpcWriter___Server_CreateMeth_Server_4251728555(name, id, type, properties, appearance);
		RpcLogic___CreateMeth_Server_4251728555(name, id, type, properties, appearance);
	}

	[TargetRpc]
	[ObserversRpc(RunLocally = true)]
	private void CreateMeth(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_CreateMeth_1869045686(conn, name, id, type, properties, appearance);
			RpcLogic___CreateMeth_1869045686(conn, name, id, type, properties, appearance);
		}
		else
		{
			RpcWriter___Target_CreateMeth_1869045686(conn, name, id, type, properties, appearance);
		}
	}

	private void RefreshHighestValueProduct()
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		for (int i = 0; i < DiscoveredProducts.Count; i++)
		{
			if (highestValueProduct == null || DiscoveredProducts[i].MarketValue > highestValueProduct.MarketValue)
			{
				highestValueProduct = DiscoveredProducts[i];
			}
		}
		float marketValue = highestValueProduct.MarketValue;
		if (marketValue >= 100f)
		{
			Singleton<AchievementManager>.Instance.UnlockAchievement(AchievementManager.EAchievement.MASTER_CHEF);
		}
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("HighestValueProduct", marketValue.ToString());
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendMixRecipe(string product, string mixer, string output)
	{
		RpcWriter___Server_SendMixRecipe_852232071(product, mixer, output);
		RpcLogic___SendMixRecipe_852232071(product, mixer, output);
	}

	[TargetRpc]
	[ObserversRpc(RunLocally = true)]
	public void CreateMixRecipe(NetworkConnection conn, string product, string mixer, string output)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_CreateMixRecipe_1410895574(conn, product, mixer, output);
			RpcLogic___CreateMixRecipe_1410895574(conn, product, mixer, output);
		}
		else
		{
			RpcWriter___Target_CreateMixRecipe_1410895574(conn, product, mixer, output);
		}
	}

	public StationRecipe GetRecipe(string product, string mixer)
	{
		return mixRecipes.Find((StationRecipe r) => r.Product.Item.ID == product && r.Ingredients[0].Items[0].ID == mixer);
	}

	public StationRecipe GetRecipe(List<ScheduleOne.Properties.Property> productProperties, ScheduleOne.Properties.Property mixerProperty)
	{
		foreach (StationRecipe mixRecipe in mixRecipes)
		{
			if (mixRecipe == null || mixRecipe.Ingredients.Count < 2)
			{
				continue;
			}
			ItemDefinition item = mixRecipe.Ingredients[0].Item;
			ItemDefinition item2 = mixRecipe.Ingredients[1].Item;
			if (item == null || item2 == null)
			{
				continue;
			}
			List<ScheduleOne.Properties.Property> list = (item as PropertyItemDefinition)?.Properties;
			List<ScheduleOne.Properties.Property> list2 = (item2 as PropertyItemDefinition)?.Properties;
			if (item2 is ProductDefinition)
			{
				list = (item2 as PropertyItemDefinition)?.Properties;
				list2 = (item as PropertyItemDefinition)?.Properties;
			}
			if (list.Count != productProperties.Count || list2.Count != 1)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < productProperties.Count; i++)
			{
				if (!list.Contains(productProperties[i]))
				{
					flag = false;
					break;
				}
			}
			if (flag && !(list2[0] != mixerProperty))
			{
				return mixRecipe;
			}
		}
		return null;
	}

	[TargetRpc]
	private void GiveItem(NetworkConnection conn, string id)
	{
		RpcWriter___Target_GiveItem_2971853958(conn, id);
	}

	public ProductDefinition GetKnownProduct(EDrugType type, List<ScheduleOne.Properties.Property> properties)
	{
		foreach (ProductDefinition allProduct in AllProducts)
		{
			if (allProduct.DrugTypes[0].DrugType != type || allProduct.Properties.Count != properties.Count)
			{
				continue;
			}
			for (int i = 0; i < properties.Count && allProduct.Properties.Contains(properties[i]); i++)
			{
				if (i == properties.Count - 1)
				{
					return allProduct;
				}
			}
		}
		return null;
	}

	public float GetPrice(ProductDefinition product)
	{
		if (product == null)
		{
			Console.LogError("Product is null");
			return 1f;
		}
		if (ProductPrices.ContainsKey(product))
		{
			return Mathf.Clamp(ProductPrices[product], 1f, 999f);
		}
		Console.LogError("Price not found for product: " + product.ID + ". Returning market value");
		return Mathf.Clamp(product.MarketValue, 1f, 999f);
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendPrice(string productID, float value)
	{
		RpcWriter___Server_SendPrice_606697822(productID, value);
		RpcLogic___SendPrice_606697822(productID, value);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetPrice(NetworkConnection conn, string productID, float value)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetPrice_4077118173(conn, productID, value);
			RpcLogic___SetPrice_4077118173(conn, productID, value);
		}
		else
		{
			RpcWriter___Target_SetPrice_4077118173(conn, productID, value);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendMixOperation(NewMixOperation operation, bool complete)
	{
		RpcWriter___Server_SendMixOperation_3670976965(operation, complete);
		RpcLogic___SendMixOperation_3670976965(operation, complete);
	}

	[ObserversRpc(RunLocally = true)]
	private void SetMixOperation(NewMixOperation operation, bool complete)
	{
		RpcWriter___Observers_SetMixOperation_3670976965(operation, complete);
		RpcLogic___SetMixOperation_3670976965(operation, complete);
	}

	public string FinishAndNameMix(string productID, string ingredientID, string mixName)
	{
		if (!IsMixNameValid(mixName))
		{
			Console.LogError("Invalid mix name. Using random name");
			mixName = Singleton<NewMixScreen>.Instance.GenerateUniqueName();
		}
		string id = mixName.ToLower().Replace(" ", string.Empty);
		id = MakeIDFileSafe(id);
		id = id.Replace(" ", string.Empty);
		id = id.Replace("(", string.Empty);
		id = id.Replace(")", string.Empty);
		id = id.Replace("'", string.Empty);
		id = id.Replace("\"", string.Empty);
		id = id.Replace(":", string.Empty);
		id = id.Replace(";", string.Empty);
		id = id.Replace(",", string.Empty);
		id = id.Replace(".", string.Empty);
		id = id.Replace("!", string.Empty);
		id = id.Replace("?", string.Empty);
		FinishAndNameMix(productID, ingredientID, mixName, id);
		if (!InstanceFinder.IsServer)
		{
			SendFinishAndNameMix(productID, ingredientID, mixName, id);
		}
		return id;
	}

	public static string MakeIDFileSafe(string id)
	{
		id = id.ToLower();
		id = id.Replace(" ", string.Empty);
		id = id.Replace("(", string.Empty);
		id = id.Replace(")", string.Empty);
		id = id.Replace("'", string.Empty);
		id = id.Replace("\"", string.Empty);
		id = id.Replace(":", string.Empty);
		id = id.Replace(";", string.Empty);
		id = id.Replace(",", string.Empty);
		id = id.Replace(".", string.Empty);
		id = id.Replace("!", string.Empty);
		id = id.Replace("?", string.Empty);
		return id;
	}

	public static bool IsMixNameValid(string mixName)
	{
		if (string.IsNullOrEmpty(mixName))
		{
			return false;
		}
		return true;
	}

	[ObserversRpc(RunLocally = true)]
	private void FinishAndNameMix(string productID, string ingredientID, string mixName, string mixID)
	{
		RpcWriter___Observers_FinishAndNameMix_4237212381(productID, ingredientID, mixName, mixID);
		RpcLogic___FinishAndNameMix_4237212381(productID, ingredientID, mixName, mixID);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SendFinishAndNameMix(string productID, string ingredientID, string mixName, string mixID)
	{
		RpcWriter___Server_SendFinishAndNameMix_4237212381(productID, ingredientID, mixName, mixID);
	}

	public static float CalculateProductValue(ProductDefinition product, float baseValue)
	{
		return CalculateProductValue(baseValue, product.Properties);
	}

	public static float CalculateProductValue(float baseValue, List<ScheduleOne.Properties.Property> properties)
	{
		float num = baseValue;
		float num2 = 1f;
		for (int i = 0; i < properties.Count; i++)
		{
			num += (float)properties[i].ValueChange;
			num += baseValue * properties[i].AddBaseValueMultiple;
			num2 *= properties[i].ValueMultiplier;
		}
		num *= num2;
		return Mathf.RoundToInt(num);
	}

	public virtual string GetSaveString()
	{
		string[] array = new string[DiscoveredProducts.Count];
		for (int i = 0; i < DiscoveredProducts.Count; i++)
		{
			if (!(DiscoveredProducts[i] == null))
			{
				array[i] = DiscoveredProducts[i].ID;
			}
		}
		string[] array2 = new string[ListedProducts.Count];
		for (int j = 0; j < ListedProducts.Count; j++)
		{
			if (!(ListedProducts[j] == null))
			{
				array2[j] = ListedProducts[j].ID;
			}
		}
		string[] array3 = new string[FavouritedProducts.Count];
		for (int k = 0; k < FavouritedProducts.Count; k++)
		{
			if (!(FavouritedProducts[k] == null))
			{
				array3[k] = FavouritedProducts[k].ID;
			}
		}
		MixRecipeData[] array4 = new MixRecipeData[mixRecipes.Count];
		for (int l = 0; l < mixRecipes.Count; l++)
		{
			if (mixRecipes[l] == null)
			{
				continue;
			}
			if (mixRecipes[l].Ingredients.Count < 2)
			{
				Console.LogWarning("Mix recipe has less than 2 ingredients");
			}
			else if (mixRecipes[l].Product != null)
			{
				try
				{
					array4[l] = new MixRecipeData(mixRecipes[l].Ingredients[1].Items[0].ID, mixRecipes[l].Ingredients[0].Items[0].ID, mixRecipes[l].Product.Item.ID);
				}
				catch (Exception ex)
				{
					Console.LogError("Failed to save mix recipe: " + ex);
				}
			}
		}
		StringIntPair[] array5 = new StringIntPair[ProductPrices.Count];
		for (int m = 0; m < AllProducts.Count; m++)
		{
			if (!(AllProducts[m] == null))
			{
				float num = 0f;
				array5[m] = new StringIntPair(i: Mathf.RoundToInt((!ProductPrices.ContainsKey(AllProducts[m])) ? AllProducts[m].MarketValue : ProductPrices[AllProducts[m]]), str: AllProducts[m].ID);
			}
		}
		List<WeedProductData> list = new List<WeedProductData>();
		List<MethProductData> list2 = new List<MethProductData>();
		List<CocaineProductData> list3 = new List<CocaineProductData>();
		for (int n = 0; n < createdProducts.Count; n++)
		{
			if (!(createdProducts[n] == null))
			{
				switch (createdProducts[n].DrugType)
				{
				case EDrugType.Marijuana:
					list.Add((createdProducts[n] as WeedDefinition).GetSaveData() as WeedProductData);
					break;
				case EDrugType.Methamphetamine:
					list2.Add((createdProducts[n] as MethDefinition).GetSaveData() as MethProductData);
					break;
				case EDrugType.Cocaine:
					list3.Add((createdProducts[n] as CocaineDefinition).GetSaveData() as CocaineProductData);
					break;
				default:
					Console.LogError("Product type not supported: " + createdProducts[n].DrugType);
					break;
				}
			}
		}
		return new ProductManagerData(array, array2, CurrentMixOperation, IsMixComplete, array4, array5, array3, list.ToArray(), list2.ToArray(), list3.ToArray()).GetJson();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(0u, RpcReader___Server_SetMethDiscovered_2166136261);
			RegisterServerRpc(1u, RpcReader___Server_SetCocaineDiscovered_2166136261);
			RegisterServerRpc(2u, RpcReader___Server_SetProductListed_310431262);
			RegisterObserversRpc(3u, RpcReader___Observers_SetProductListed_619441887);
			RegisterTargetRpc(4u, RpcReader___Target_SetProductListed_619441887);
			RegisterServerRpc(5u, RpcReader___Server_SetProductFavourited_310431262);
			RegisterObserversRpc(6u, RpcReader___Observers_SetProductFavourited_619441887);
			RegisterTargetRpc(7u, RpcReader___Target_SetProductFavourited_619441887);
			RegisterServerRpc(8u, RpcReader___Server_DiscoverProduct_3615296227);
			RegisterObserversRpc(9u, RpcReader___Observers_SetProductDiscovered_619441887);
			RegisterTargetRpc(10u, RpcReader___Target_SetProductDiscovered_619441887);
			RegisterServerRpc(11u, RpcReader___Server_CreateWeed_Server_2331775230);
			RegisterTargetRpc(12u, RpcReader___Target_CreateWeed_1777266891);
			RegisterObserversRpc(13u, RpcReader___Observers_CreateWeed_1777266891);
			RegisterServerRpc(14u, RpcReader___Server_CreateCocaine_Server_891166717);
			RegisterTargetRpc(15u, RpcReader___Target_CreateCocaine_1327282946);
			RegisterObserversRpc(16u, RpcReader___Observers_CreateCocaine_1327282946);
			RegisterServerRpc(17u, RpcReader___Server_CreateMeth_Server_4251728555);
			RegisterTargetRpc(18u, RpcReader___Target_CreateMeth_1869045686);
			RegisterObserversRpc(19u, RpcReader___Observers_CreateMeth_1869045686);
			RegisterServerRpc(20u, RpcReader___Server_SendMixRecipe_852232071);
			RegisterTargetRpc(21u, RpcReader___Target_CreateMixRecipe_1410895574);
			RegisterObserversRpc(22u, RpcReader___Observers_CreateMixRecipe_1410895574);
			RegisterTargetRpc(23u, RpcReader___Target_GiveItem_2971853958);
			RegisterServerRpc(24u, RpcReader___Server_SendPrice_606697822);
			RegisterObserversRpc(25u, RpcReader___Observers_SetPrice_4077118173);
			RegisterTargetRpc(26u, RpcReader___Target_SetPrice_4077118173);
			RegisterServerRpc(27u, RpcReader___Server_SendMixOperation_3670976965);
			RegisterObserversRpc(28u, RpcReader___Observers_SetMixOperation_3670976965);
			RegisterObserversRpc(29u, RpcReader___Observers_FinishAndNameMix_4237212381);
			RegisterServerRpc(30u, RpcReader___Server_SendFinishAndNameMix_4237212381);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EProduct_002EProductManagerAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SetMethDiscovered_2166136261()
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			SendServerRpc(0u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetMethDiscovered_2166136261()
	{
		SetProductDiscovered(null, "meth", autoList: false);
	}

	private void RpcReader___Server_SetMethDiscovered_2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		if (base.IsServerInitialized)
		{
			RpcLogic___SetMethDiscovered_2166136261();
		}
	}

	private void RpcWriter___Server_SetCocaineDiscovered_2166136261()
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			SendServerRpc(1u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetCocaineDiscovered_2166136261()
	{
		SetProductDiscovered(null, "cocaine", autoList: false);
	}

	private void RpcReader___Server_SetCocaineDiscovered_2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		if (base.IsServerInitialized)
		{
			RpcLogic___SetCocaineDiscovered_2166136261();
		}
	}

	private void RpcWriter___Server_SetProductListed_310431262(string productID, bool listed)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(listed);
			SendServerRpc(2u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetProductListed_310431262(string productID, bool listed)
	{
		SetProductListed(null, productID, listed);
	}

	private void RpcReader___Server_SetProductListed_310431262(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string productID = PooledReader0.ReadString();
		bool listed = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetProductListed_310431262(productID, listed);
		}
	}

	private void RpcWriter___Observers_SetProductListed_619441887(NetworkConnection conn, string productID, bool listed)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(listed);
			SendObserversRpc(3u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetProductListed_619441887(NetworkConnection conn, string productID, bool listed)
	{
		ProductDefinition productDefinition = AllProducts.Find((ProductDefinition p) => p.ID == productID);
		if (productDefinition == null)
		{
			Console.LogWarning("SetProductListed: product is not found (" + productID + ")");
			return;
		}
		if (!DiscoveredProducts.Contains(productDefinition))
		{
			Console.LogWarning("SetProductListed: product is not yet discovered");
		}
		if (listed)
		{
			if (!ListedProducts.Contains(productDefinition))
			{
				ListedProducts.Add(productDefinition);
			}
		}
		else if (ListedProducts.Contains(productDefinition))
		{
			ListedProducts.Remove(productDefinition);
		}
		if (NetworkSingleton<VariableDatabase>.InstanceExists && InstanceFinder.IsServer)
		{
			NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("ListedProductsCount", ListedProducts.Count.ToString());
			NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("OGKushListed", (ListedProducts.Find((ProductDefinition x) => x.ID == "ogkush") != null).ToString());
		}
		HasChanged = true;
		TimeSinceProductListingChanged = 0f;
		if (listed)
		{
			if (onProductListed != null)
			{
				onProductListed(productDefinition);
			}
		}
		else if (onProductDelisted != null)
		{
			onProductDelisted(productDefinition);
		}
	}

	private void RpcReader___Observers_SetProductListed_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool listed = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetProductListed_619441887(null, productID, listed);
		}
	}

	private void RpcWriter___Target_SetProductListed_619441887(NetworkConnection conn, string productID, bool listed)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(listed);
			SendTargetRpc(4u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetProductListed_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool listed = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetProductListed_619441887(base.LocalConnection, productID, listed);
		}
	}

	private void RpcWriter___Server_SetProductFavourited_310431262(string productID, bool listed)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(listed);
			SendServerRpc(5u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SetProductFavourited_310431262(string productID, bool listed)
	{
		SetProductFavourited(null, productID, listed);
	}

	private void RpcReader___Server_SetProductFavourited_310431262(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string productID = PooledReader0.ReadString();
		bool listed = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SetProductFavourited_310431262(productID, listed);
		}
	}

	private void RpcWriter___Observers_SetProductFavourited_619441887(NetworkConnection conn, string productID, bool fav)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(fav);
			SendObserversRpc(6u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetProductFavourited_619441887(NetworkConnection conn, string productID, bool fav)
	{
		ProductDefinition productDefinition = AllProducts.Find((ProductDefinition p) => p.ID == productID);
		if (productDefinition == null)
		{
			Console.LogWarning("SetProductFavourited: product is not found (" + productID + ")");
			return;
		}
		if (!DiscoveredProducts.Contains(productDefinition))
		{
			Console.LogWarning("SetProductFavourited: product is not yet discovered");
		}
		if (fav)
		{
			if (!FavouritedProducts.Contains(productDefinition))
			{
				FavouritedProducts.Add(productDefinition);
			}
		}
		else if (FavouritedProducts.Contains(productDefinition))
		{
			FavouritedProducts.Remove(productDefinition);
		}
		HasChanged = true;
		if (fav)
		{
			if (onProductFavourited != null)
			{
				onProductFavourited(productDefinition);
			}
		}
		else if (onProductUnfavourited != null)
		{
			onProductUnfavourited(productDefinition);
		}
	}

	private void RpcReader___Observers_SetProductFavourited_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool fav = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetProductFavourited_619441887(null, productID, fav);
		}
	}

	private void RpcWriter___Target_SetProductFavourited_619441887(NetworkConnection conn, string productID, bool fav)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(fav);
			SendTargetRpc(7u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetProductFavourited_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool fav = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetProductFavourited_619441887(base.LocalConnection, productID, fav);
		}
	}

	private void RpcWriter___Server_DiscoverProduct_3615296227(string productID)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			SendServerRpc(8u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___DiscoverProduct_3615296227(string productID)
	{
		SetProductDiscovered(null, productID, autoList: true);
	}

	private void RpcReader___Server_DiscoverProduct_3615296227(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string productID = PooledReader0.ReadString();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___DiscoverProduct_3615296227(productID);
		}
	}

	private void RpcWriter___Observers_SetProductDiscovered_619441887(NetworkConnection conn, string productID, bool autoList)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(autoList);
			SendObserversRpc(9u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetProductDiscovered_619441887(NetworkConnection conn, string productID, bool autoList)
	{
		ProductDefinition productDefinition = AllProducts.Find((ProductDefinition p) => p.ID == productID);
		if (productDefinition == null)
		{
			Console.LogWarning("SetProductDiscovered: product is not found");
			return;
		}
		if (!DiscoveredProducts.Contains(productDefinition))
		{
			DiscoveredProducts.Add(productDefinition);
			if (autoList)
			{
				SetProductListed(productID, listed: true);
			}
			if (onProductDiscovered != null)
			{
				onProductDiscovered(productDefinition);
			}
		}
		HasChanged = true;
	}

	private void RpcReader___Observers_SetProductDiscovered_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool autoList = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetProductDiscovered_619441887(null, productID, autoList);
		}
	}

	private void RpcWriter___Target_SetProductDiscovered_619441887(NetworkConnection conn, string productID, bool autoList)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteBoolean(autoList);
			SendTargetRpc(10u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetProductDiscovered_619441887(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		bool autoList = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetProductDiscovered_619441887(base.LocalConnection, productID, autoList);
		}
	}

	private void RpcWriter___Server_CreateWeed_Server_2331775230(string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendServerRpc(11u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___CreateWeed_Server_2331775230(string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		CreateWeed(null, name, id, type, properties, appearance);
	}

	private void RpcReader___Server_CreateWeed_Server_2331775230(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		WeedAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___CreateWeed_Server_2331775230(text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Target_CreateWeed_1777266891(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendTargetRpc(12u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___CreateWeed_1777266891(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		if (!Registry.ItemExists(id))
		{
			WeedDefinition weedDefinition = UnityEngine.Object.Instantiate(DefaultWeed);
			weedDefinition.name = name;
			weedDefinition.Name = name;
			weedDefinition.Description = string.Empty;
			weedDefinition.ID = id;
			weedDefinition.Initialize(Singleton<PropertyUtility>.Instance.GetProperties(properties), new List<EDrugType> { type }, appearance);
			AllProducts.Add(weedDefinition);
			ProductPrices.Add(weedDefinition, weedDefinition.MarketValue);
			ProductNames.Add(name);
			createdProducts.Add(weedDefinition);
			Singleton<Registry>.Instance.AddToRegistry(weedDefinition);
			weedDefinition.Icon = Singleton<ProductIconManager>.Instance.GenerateIcons(id);
			if (weedDefinition.Icon == null)
			{
				Console.LogError("Failed to generate icons for " + name);
			}
			SetProductDiscovered(null, id, autoList: false);
			RefreshHighestValueProduct();
			if (onNewProductCreated != null)
			{
				onNewProductCreated(weedDefinition);
			}
		}
	}

	private void RpcReader___Target_CreateWeed_1777266891(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		WeedAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___CreateWeed_1777266891(base.LocalConnection, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Observers_CreateWeed_1777266891(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, WeedAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendObserversRpc(13u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_CreateWeed_1777266891(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		WeedAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EWeedAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___CreateWeed_1777266891(null, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Server_CreateCocaine_Server_891166717(string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendServerRpc(14u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___CreateCocaine_Server_891166717(string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		CreateCocaine(null, name, id, type, properties, appearance);
	}

	private void RpcReader___Server_CreateCocaine_Server_891166717(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		CocaineAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___CreateCocaine_Server_891166717(text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Target_CreateCocaine_1327282946(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendTargetRpc(15u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___CreateCocaine_1327282946(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		if (Registry.GetItem(id) != null)
		{
			Console.LogError("Product with ID " + id + " already exists");
			return;
		}
		CocaineDefinition cocaineDefinition = UnityEngine.Object.Instantiate(DefaultCocaine);
		cocaineDefinition.name = name;
		cocaineDefinition.Name = name;
		cocaineDefinition.Description = string.Empty;
		cocaineDefinition.ID = id;
		cocaineDefinition.Initialize(Singleton<PropertyUtility>.Instance.GetProperties(properties), new List<EDrugType> { type }, appearance);
		AllProducts.Add(cocaineDefinition);
		ProductPrices.Add(cocaineDefinition, cocaineDefinition.MarketValue);
		ProductNames.Add(name);
		createdProducts.Add(cocaineDefinition);
		Singleton<Registry>.Instance.AddToRegistry(cocaineDefinition);
		cocaineDefinition.Icon = Singleton<ProductIconManager>.Instance.GenerateIcons(id);
		if (cocaineDefinition.Icon == null)
		{
			Console.LogError("Failed to generate icons for " + name);
		}
		SetProductDiscovered(null, id, autoList: false);
		RefreshHighestValueProduct();
		if (onNewProductCreated != null)
		{
			onNewProductCreated(cocaineDefinition);
		}
	}

	private void RpcReader___Target_CreateCocaine_1327282946(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		CocaineAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___CreateCocaine_1327282946(base.LocalConnection, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Observers_CreateCocaine_1327282946(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, CocaineAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendObserversRpc(16u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_CreateCocaine_1327282946(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		CocaineAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002ECocaineAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___CreateCocaine_1327282946(null, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Server_CreateMeth_Server_4251728555(string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendServerRpc(17u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___CreateMeth_Server_4251728555(string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		CreateMeth(null, name, id, type, properties, appearance);
	}

	private void RpcReader___Server_CreateMeth_Server_4251728555(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		MethAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___CreateMeth_Server_4251728555(text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Target_CreateMeth_1869045686(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendTargetRpc(18u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___CreateMeth_1869045686(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		if (Registry.GetItem(id) != null)
		{
			Console.LogError("Product with ID " + id + " already exists");
			return;
		}
		MethDefinition methDefinition = UnityEngine.Object.Instantiate(DefaultMeth);
		methDefinition.name = name;
		methDefinition.Name = name;
		methDefinition.Description = string.Empty;
		methDefinition.ID = id;
		methDefinition.Initialize(Singleton<PropertyUtility>.Instance.GetProperties(properties), new List<EDrugType> { type }, appearance);
		AllProducts.Add(methDefinition);
		ProductPrices.Add(methDefinition, methDefinition.MarketValue);
		ProductNames.Add(name);
		createdProducts.Add(methDefinition);
		Singleton<Registry>.Instance.AddToRegistry(methDefinition);
		methDefinition.Icon = Singleton<ProductIconManager>.Instance.GenerateIcons(id);
		if (methDefinition.Icon == null)
		{
			Console.LogError("Failed to generate icons for " + name);
		}
		SetProductDiscovered(null, id, autoList: false);
		RefreshHighestValueProduct();
		if (onNewProductCreated != null)
		{
			onNewProductCreated(methDefinition);
		}
	}

	private void RpcReader___Target_CreateMeth_1869045686(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		MethAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized)
		{
			RpcLogic___CreateMeth_1869045686(base.LocalConnection, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Observers_CreateMeth_1869045686(NetworkConnection conn, string name, string id, EDrugType type, List<string> properties, MethAppearanceSettings appearance)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(name);
			writer.WriteString(id);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerated(writer, type);
			GeneratedWriters___Internal.Write___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerated(writer, properties);
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerated(writer, appearance);
			SendObserversRpc(19u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_CreateMeth_1869045686(PooledReader PooledReader0, Channel channel)
	{
		string text = PooledReader0.ReadString();
		string id = PooledReader0.ReadString();
		EDrugType type = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EEDrugTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
		List<string> properties = GeneratedReaders___Internal.Read___System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003EFishNet_002ESerializing_002EGenerateds(PooledReader0);
		MethAppearanceSettings appearance = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002EMethAppearanceSettingsFishNet_002ESerializing_002EGenerateds(PooledReader0);
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___CreateMeth_1869045686(null, text, id, type, properties, appearance);
		}
	}

	private void RpcWriter___Server_SendMixRecipe_852232071(string product, string mixer, string output)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(product);
			writer.WriteString(mixer);
			writer.WriteString(output);
			SendServerRpc(20u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendMixRecipe_852232071(string product, string mixer, string output)
	{
		CreateMixRecipe(null, product, mixer, output);
	}

	private void RpcReader___Server_SendMixRecipe_852232071(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string product = PooledReader0.ReadString();
		string mixer = PooledReader0.ReadString();
		string output = PooledReader0.ReadString();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendMixRecipe_852232071(product, mixer, output);
		}
	}

	private void RpcWriter___Target_CreateMixRecipe_1410895574(NetworkConnection conn, string product, string mixer, string output)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(product);
			writer.WriteString(mixer);
			writer.WriteString(output);
			SendTargetRpc(21u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	public void RpcLogic___CreateMixRecipe_1410895574(NetworkConnection conn, string product, string mixer, string output)
	{
		if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(mixer) || string.IsNullOrEmpty(output))
		{
			Console.LogError("Invalid mix recipe: Product:" + product + " Mixer:" + mixer + " Output:" + output);
			return;
		}
		StationRecipe stationRecipe = null;
		for (int i = 0; i < mixRecipes.Count; i++)
		{
			if (!(mixRecipes[i] == null) && mixRecipes[i].Product != null && mixRecipes[i].Ingredients.Count >= 2)
			{
				string iD = mixRecipes[i].Ingredients[0].Items[0].ID;
				string iD2 = mixRecipes[i].Ingredients[1].Items[0].ID;
				string iD3 = mixRecipes[i].Product.Item.ID;
				if (iD == product && iD2 == mixer && iD3 == output)
				{
					stationRecipe = mixRecipes[i];
					break;
				}
				if (iD2 == product && iD == mixer && iD3 == output)
				{
					stationRecipe = mixRecipes[i];
					break;
				}
			}
		}
		if (stationRecipe != null)
		{
			Console.LogWarning("Mix recipe already exists");
			return;
		}
		StationRecipe stationRecipe2 = ScriptableObject.CreateInstance<StationRecipe>();
		ItemDefinition item = Registry.GetItem(product);
		ItemDefinition item2 = Registry.GetItem(mixer);
		if (item == null)
		{
			Console.LogError("Product not found: " + product);
			return;
		}
		if (item2 == null)
		{
			Console.LogError("Mixer not found: " + mixer);
			return;
		}
		stationRecipe2.Ingredients = new List<StationRecipe.IngredientQuantity>();
		stationRecipe2.Ingredients.Add(new StationRecipe.IngredientQuantity
		{
			Items = new List<ItemDefinition> { item },
			Quantity = 1
		});
		stationRecipe2.Ingredients.Add(new StationRecipe.IngredientQuantity
		{
			Items = new List<ItemDefinition> { item2 },
			Quantity = 1
		});
		ItemDefinition item3 = Registry.GetItem(output);
		if (item3 == null)
		{
			Console.LogError("Output item not found: " + output);
			return;
		}
		stationRecipe2.Product = new StationRecipe.ItemQuantity
		{
			Item = item3,
			Quantity = 1
		};
		stationRecipe2.RecipeTitle = stationRecipe2.Product.Item.Name;
		stationRecipe2.Unlocked = true;
		mixRecipes.Add(stationRecipe2);
		if (onMixRecipeAdded != null)
		{
			onMixRecipeAdded(stationRecipe2);
		}
		ProductDefinition productDefinition = stationRecipe2.Product.Item as ProductDefinition;
		if (productDefinition != null)
		{
			productDefinition.AddRecipe(stationRecipe2);
		}
		else
		{
			Console.LogError("Product is not a product definition: " + product);
		}
		HasChanged = true;
	}

	private void RpcReader___Target_CreateMixRecipe_1410895574(PooledReader PooledReader0, Channel channel)
	{
		string product = PooledReader0.ReadString();
		string mixer = PooledReader0.ReadString();
		string output = PooledReader0.ReadString();
		if (base.IsClientInitialized)
		{
			RpcLogic___CreateMixRecipe_1410895574(base.LocalConnection, product, mixer, output);
		}
	}

	private void RpcWriter___Observers_CreateMixRecipe_1410895574(NetworkConnection conn, string product, string mixer, string output)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(product);
			writer.WriteString(mixer);
			writer.WriteString(output);
			SendObserversRpc(22u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcReader___Observers_CreateMixRecipe_1410895574(PooledReader PooledReader0, Channel channel)
	{
		string product = PooledReader0.ReadString();
		string mixer = PooledReader0.ReadString();
		string output = PooledReader0.ReadString();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___CreateMixRecipe_1410895574(null, product, mixer, output);
		}
	}

	private void RpcWriter___Target_GiveItem_2971853958(NetworkConnection conn, string id)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(id);
			SendTargetRpc(23u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcLogic___GiveItem_2971853958(NetworkConnection conn, string id)
	{
		PlayerSingleton<PlayerInventory>.Instance.AddItemToInventory(Registry.GetItem(id).GetDefaultInstance());
	}

	private void RpcReader___Target_GiveItem_2971853958(PooledReader PooledReader0, Channel channel)
	{
		string id = PooledReader0.ReadString();
		if (base.IsClientInitialized)
		{
			RpcLogic___GiveItem_2971853958(base.LocalConnection, id);
		}
	}

	private void RpcWriter___Server_SendPrice_606697822(string productID, float value)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteSingle(value);
			SendServerRpc(24u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendPrice_606697822(string productID, float value)
	{
		SetPrice(null, productID, value);
	}

	private void RpcReader___Server_SendPrice_606697822(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string productID = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendPrice_606697822(productID, value);
		}
	}

	private void RpcWriter___Observers_SetPrice_4077118173(NetworkConnection conn, string productID, float value)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteSingle(value);
			SendObserversRpc(25u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetPrice_4077118173(NetworkConnection conn, string productID, float value)
	{
		ProductDefinition item = Registry.GetItem<ProductDefinition>(productID);
		if (item == null)
		{
			Console.LogError("Product not found: " + productID);
			return;
		}
		value = Mathf.RoundToInt(Mathf.Clamp(value, 1f, 999f));
		if (!ProductPrices.ContainsKey(item))
		{
			ProductPrices.Add(item, value);
		}
		else
		{
			ProductPrices[item] = value;
		}
	}

	private void RpcReader___Observers_SetPrice_4077118173(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetPrice_4077118173(null, productID, value);
		}
	}

	private void RpcWriter___Target_SetPrice_4077118173(NetworkConnection conn, string productID, float value)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteSingle(value);
			SendTargetRpc(26u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetPrice_4077118173(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		float value = PooledReader0.ReadSingle();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetPrice_4077118173(base.LocalConnection, productID, value);
		}
	}

	private void RpcWriter___Server_SendMixOperation_3670976965(NewMixOperation operation, bool complete)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002ENewMixOperationFishNet_002ESerializing_002EGenerated(writer, operation);
			writer.WriteBoolean(complete);
			SendServerRpc(27u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendMixOperation_3670976965(NewMixOperation operation, bool complete)
	{
		SetMixOperation(operation, complete);
	}

	private void RpcReader___Server_SendMixOperation_3670976965(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		NewMixOperation operation = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002ENewMixOperationFishNet_002ESerializing_002EGenerateds(PooledReader0);
		bool complete = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendMixOperation_3670976965(operation, complete);
		}
	}

	private void RpcWriter___Observers_SetMixOperation_3670976965(NewMixOperation operation, bool complete)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			GeneratedWriters___Internal.Write___ScheduleOne_002EProduct_002ENewMixOperationFishNet_002ESerializing_002EGenerated(writer, operation);
			writer.WriteBoolean(complete);
			SendObserversRpc(28u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___SetMixOperation_3670976965(NewMixOperation operation, bool complete)
	{
		CurrentMixOperation = operation;
		IsMixComplete = complete;
		if (CurrentMixOperation != null && IsMixComplete && onMixCompleted != null)
		{
			onMixCompleted(CurrentMixOperation);
		}
	}

	private void RpcReader___Observers_SetMixOperation_3670976965(PooledReader PooledReader0, Channel channel)
	{
		NewMixOperation operation = GeneratedReaders___Internal.Read___ScheduleOne_002EProduct_002ENewMixOperationFishNet_002ESerializing_002EGenerateds(PooledReader0);
		bool complete = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetMixOperation_3670976965(operation, complete);
		}
	}

	private void RpcWriter___Observers_FinishAndNameMix_4237212381(string productID, string ingredientID, string mixName, string mixID)
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteString(ingredientID);
			writer.WriteString(mixName);
			writer.WriteString(mixID);
			SendObserversRpc(29u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___FinishAndNameMix_4237212381(string productID, string ingredientID, string mixName, string mixID)
	{
		if (AllProducts.Find((ProductDefinition p) => p.ID == mixID) != null)
		{
			return;
		}
		ProductDefinition productDefinition = Registry.GetItem(productID) as ProductDefinition;
		PropertyItemDefinition propertyItemDefinition = Registry.GetItem(ingredientID) as PropertyItemDefinition;
		if (productDefinition == null || propertyItemDefinition == null)
		{
			Debug.LogError("Product or mixer not found");
			return;
		}
		List<ScheduleOne.Properties.Property> list = PropertyMixCalculator.MixProperties(productDefinition.Properties, propertyItemDefinition.Properties[0], productDefinition.DrugType);
		List<string> list2 = new List<string>();
		foreach (ScheduleOne.Properties.Property item in list)
		{
			list2.Add(item.ID);
		}
		switch (productDefinition.DrugType)
		{
		case EDrugType.Marijuana:
			CreateWeed(null, mixName, mixID, EDrugType.Marijuana, list2, WeedDefinition.GetAppearanceSettings(list));
			break;
		case EDrugType.Methamphetamine:
			CreateMeth(null, mixName, mixID, EDrugType.Methamphetamine, list2, MethDefinition.GetAppearanceSettings(list));
			break;
		case EDrugType.Cocaine:
			CreateCocaine(null, mixName, mixID, EDrugType.Cocaine, list2, CocaineDefinition.GetAppearanceSettings(list));
			break;
		default:
			Console.LogError("Drug type not supported");
			break;
		}
	}

	private void RpcReader___Observers_FinishAndNameMix_4237212381(PooledReader PooledReader0, Channel channel)
	{
		string productID = PooledReader0.ReadString();
		string ingredientID = PooledReader0.ReadString();
		string mixName = PooledReader0.ReadString();
		string mixID = PooledReader0.ReadString();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___FinishAndNameMix_4237212381(productID, ingredientID, mixName, mixID);
		}
	}

	private void RpcWriter___Server_SendFinishAndNameMix_4237212381(string productID, string ingredientID, string mixName, string mixID)
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			writer.WriteString(productID);
			writer.WriteString(ingredientID);
			writer.WriteString(mixName);
			writer.WriteString(mixID);
			SendServerRpc(30u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___SendFinishAndNameMix_4237212381(string productID, string ingredientID, string mixName, string mixID)
	{
		FinishAndNameMix(productID, ingredientID, mixName, mixID);
		CreateMixRecipe(null, productID, ingredientID, mixID);
	}

	private void RpcReader___Server_SendFinishAndNameMix_4237212381(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		string productID = PooledReader0.ReadString();
		string ingredientID = PooledReader0.ReadString();
		string mixName = PooledReader0.ReadString();
		string mixID = PooledReader0.ReadString();
		if (base.IsServerInitialized)
		{
			RpcLogic___SendFinishAndNameMix_4237212381(productID, ingredientID, mixName, mixID);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EProduct_002EProductManager_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		InitializeSaveable();
	}
}
