using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;

namespace ScheduleOne.Packaging;

public class FilledPackaging_Equippable : Equippable_Viewmodel
{
	public FilledPackagingVisuals Visuals;

	public override void Equip(ItemInstance item)
	{
		base.Equip(item);
		(item as ProductItemInstance).SetupPackagingVisuals(Visuals);
	}
}
