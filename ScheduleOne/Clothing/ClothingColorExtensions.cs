using System;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace ScheduleOne.Clothing;

public static class ClothingColorExtensions
{
	public static Color GetActualColor(this EClothingColor color)
	{
		return Singleton<ClothingUtility>.Instance.GetColorData(color).ActualColor;
	}

	public static Color GetLabelColor(this EClothingColor color)
	{
		return Singleton<ClothingUtility>.Instance.GetColorData(color).LabelColor;
	}

	public static string GetLabel(this EClothingColor color)
	{
		return color.ToString();
	}

	public static EClothingColor GetClothingColor(Color color)
	{
		foreach (EClothingColor value in Enum.GetValues(typeof(EClothingColor)))
		{
			if (ColorEquals(value.GetActualColor(), color))
			{
				return value;
			}
		}
		Color color2 = color;
		Console.LogError("Could not find clothing color for color " + color2.ToString());
		return EClothingColor.White;
	}

	public static bool ColorEquals(Color a, Color b, float tolerance = 0.004f)
	{
		if (a.r > b.r + tolerance)
		{
			return false;
		}
		if (a.g > b.g + tolerance)
		{
			return false;
		}
		if (a.b > b.b + tolerance)
		{
			return false;
		}
		if (a.r < b.r - tolerance)
		{
			return false;
		}
		if (a.g < b.g - tolerance)
		{
			return false;
		}
		if (a.b < b.b - tolerance)
		{
			return false;
		}
		return true;
	}
}
