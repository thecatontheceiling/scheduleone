namespace ScheduleOne.ItemFramework;

public class ItemFilter_LegalStatus : ItemFilter
{
	public ELegalStatus RequiredLegalStatus;

	public ItemFilter_LegalStatus(ELegalStatus requiredLegalStatus)
	{
		RequiredLegalStatus = requiredLegalStatus;
	}

	public override bool DoesItemMatchFilter(ItemInstance instance)
	{
		if (instance == null)
		{
			return false;
		}
		if (instance.Definition.legalStatus != RequiredLegalStatus)
		{
			return false;
		}
		return base.DoesItemMatchFilter(instance);
	}
}
