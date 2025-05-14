using ScheduleOne.DevUtilities;
using ScheduleOne.Product;

namespace ScheduleOne.ItemFramework;

public class ItemFilter_MixingIngredient : ItemFilter
{
	public override bool DoesItemMatchFilter(ItemInstance instance)
	{
		if (instance == null)
		{
			return false;
		}
		ItemDefinition definition = instance.Definition;
		if (!(definition is PropertyItemDefinition))
		{
			return false;
		}
		PropertyItemDefinition item = definition as PropertyItemDefinition;
		if (!NetworkSingleton<ProductManager>.Instance.ValidMixIngredients.Contains(item))
		{
			return false;
		}
		return base.DoesItemMatchFilter(instance);
	}
}
