using ScheduleOne.Product;

namespace ScheduleOne.ItemFramework;

public class ItemFilter_Dryable : ItemFilter
{
	public override bool DoesItemMatchFilter(ItemInstance instance)
	{
		if (!IsItemDryable(instance))
		{
			return false;
		}
		return base.DoesItemMatchFilter(instance);
	}

	public static bool IsItemDryable(ItemInstance instance)
	{
		if (instance == null)
		{
			return false;
		}
		if (instance is ProductItemInstance productItemInstance && (productItemInstance.Definition as ProductDefinition).DrugType == EDrugType.Marijuana && productItemInstance.AppliedPackaging == null && productItemInstance.Quality < EQuality.Heavenly)
		{
			return true;
		}
		if (instance.ID == "cocaleaf" && (instance as QualityItemInstance).Quality < EQuality.Heavenly)
		{
			return true;
		}
		return false;
	}
}
