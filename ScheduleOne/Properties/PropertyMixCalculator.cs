using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.Product;
using ScheduleOne.Properties.MixMaps;
using ScheduleOne.StationFramework;
using UnityEngine;

namespace ScheduleOne.Properties;

public static class PropertyMixCalculator
{
	private class Reaction
	{
		public Property Existing;

		public Property Output;
	}

	public const int MAX_PROPERTIES = 8;

	public const float MAX_DELTA_DIFFERENCE = 0.5f;

	public static List<Property> MixProperties(List<Property> existingProperties, Property newProperty, EDrugType drugType)
	{
		StationRecipe recipe = NetworkSingleton<ProductManager>.Instance.GetRecipe(existingProperties, newProperty);
		if (recipe != null)
		{
			Console.Log("Existing recipe found! for " + recipe.Product.Item.Name);
			return (recipe.Product.Item as ProductDefinition).Properties;
		}
		Vector2 vector = newProperty.MixDirection * newProperty.MixMagnitude;
		MixerMap mixerMap = NetworkSingleton<ProductManager>.Instance.GetMixerMap(drugType);
		List<Reaction> list = new List<Reaction>();
		for (int i = 0; i < existingProperties.Count; i++)
		{
			Vector2 point = mixerMap.GetEffect(existingProperties[i]).Position + vector;
			Property property = mixerMap.GetEffectAtPoint(point)?.Property;
			if (property != null)
			{
				Reaction item = new Reaction
				{
					Existing = existingProperties[i],
					Output = property
				};
				list.Add(item);
			}
		}
		List<Property> list2 = new List<Property>(existingProperties);
		foreach (Reaction item2 in list)
		{
			if (!list2.Contains(item2.Output))
			{
				list2[list2.IndexOf(item2.Existing)] = item2.Output;
			}
		}
		if (!list2.Contains(newProperty) && list2.Count < 8)
		{
			list2.Add(newProperty);
		}
		return list2.Distinct().ToList();
	}

	public static void Shuffle<T>(List<T> list, int seed)
	{
		System.Random random = new System.Random(seed);
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = random.Next(num + 1);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}
}
