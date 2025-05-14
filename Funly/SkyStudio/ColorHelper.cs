using UnityEngine;

namespace Funly.SkyStudio;

public abstract class ColorHelper
{
	public static Color ColorWithHex(uint hex)
	{
		return ColorWithHexAlpha((hex << 8) | 0xFF);
	}

	public static Color ColorWithHexAlpha(uint hex)
	{
		float r = (float)((hex >> 24) & 0xFF) / 255f;
		float g = (float)((hex >> 16) & 0xFF) / 255f;
		float b = (float)((hex >> 8) & 0xFF) / 255f;
		float a = (float)(hex & 0xFF) / 255f;
		return new Color(r, g, b, a);
	}
}
