namespace LiquidVolumeFX;

public static class DetailExtensions
{
	public static bool allowsRefraction(this DETAIL detail)
	{
		return detail != DETAIL.DefaultNoFlask;
	}

	public static bool usesFlask(this DETAIL detail)
	{
		if (detail != DETAIL.Simple)
		{
			return detail == DETAIL.Default;
		}
		return true;
	}
}
