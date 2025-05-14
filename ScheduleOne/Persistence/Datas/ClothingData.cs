using System;
using ScheduleOne.Clothing;

namespace ScheduleOne.Persistence.Datas;

[Serializable]
public class ClothingData : ItemData
{
	public EClothingColor Color;

	public ClothingData(string iD, int quantity, EClothingColor color)
		: base(iD, quantity)
	{
		Color = color;
	}
}
