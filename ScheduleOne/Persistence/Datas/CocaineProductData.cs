using System;
using ScheduleOne.Product;

namespace ScheduleOne.Persistence.Datas;

[Serializable]
public class CocaineProductData : ProductData
{
	public CocaineAppearanceSettings AppearanceSettings;

	public CocaineProductData(string name, string id, EDrugType drugType, string[] properties, CocaineAppearanceSettings appearanceSettings)
		: base(name, id, drugType, properties)
	{
		AppearanceSettings = appearanceSettings;
	}
}
