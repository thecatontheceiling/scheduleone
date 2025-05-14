using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.ItemFramework;

namespace ScheduleOne.DevUtilities;

public class PackingAlgorithm : Singleton<PackingAlgorithm>
{
	[Serializable]
	public class Rectangle
	{
		public string name;

		public int sizeX;

		public int sizeY;

		public bool flipped;

		public int actualSizeX
		{
			get
			{
				if (flipped)
				{
					return sizeY;
				}
				return sizeX;
			}
		}

		public int actualSizeY
		{
			get
			{
				if (flipped)
				{
					return sizeX;
				}
				return sizeY;
			}
		}

		public Rectangle(string _name, int x, int y)
		{
			name = _name;
			sizeX = x;
			sizeY = y;
		}
	}

	public class StoredItemData : Rectangle
	{
		public ItemInstance item;

		public int xPos;

		public int yPos;

		public float rotation
		{
			get
			{
				if (!flipped)
				{
					return 0f;
				}
				return 90f;
			}
		}

		public StoredItemData(string _name, int x, int y, ItemInstance _item)
			: base(_name, x, y)
		{
			item = _item;
		}
	}

	public class Coordinate
	{
		public int x;

		public int y;

		public Rectangle occupant;

		public Coordinate(int _x, int _y)
		{
			x = _x;
			y = _y;
		}
	}

	public List<Rectangle> rectsToPack = new List<Rectangle>();

	public List<StoredItemData> PackItems(List<ItemInstance> datas, int gridX, int gridY)
	{
		List<StoredItemData> list = new List<StoredItemData>();
		for (int i = 0; i < datas.Count; i++)
		{
			StorableItemDefinition storableItemDefinition = datas[i].Definition as StorableItemDefinition;
			if (!(storableItemDefinition == null))
			{
				StoredItemData item = new StoredItemData(storableItemDefinition.Name, storableItemDefinition.StoredItem.xSize, storableItemDefinition.StoredItem.ySize, datas[i]);
				list.Add(item);
			}
		}
		AttemptPack(list, gridX, gridY);
		return list;
	}

	public List<StoredItemData> AttemptPack(List<StoredItemData> rects, int gridX, int gridY)
	{
		List<StoredItemData> list = rects.OrderBy((StoredItemData o) => o.sizeX * o.sizeY).ToList();
		list.Reverse();
		Coordinate[,] array = new Coordinate[gridX, gridY];
		for (int num = 0; num < gridX; num++)
		{
			for (int num2 = 0; num2 < gridY; num2++)
			{
				array[num, num2] = new Coordinate(num, num2);
			}
		}
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			List<Coordinate> list2 = new List<Coordinate>();
			if (num3 == 0)
			{
				list2.Add(new Coordinate(0, 0));
			}
			for (int num4 = 0; num4 < gridX; num4++)
			{
				for (int num5 = 0; num5 < gridY; num5++)
				{
					if (array[num4, num5].occupant == null && DoesCoordinateHaveOccupiedAdjacent(array, new Coordinate(num4, num5), gridX, gridY))
					{
						list2.Add(new Coordinate(num4, num5));
					}
				}
			}
			int regionSize = GetRegionSize(array, gridX, gridY);
			int num6 = int.MaxValue;
			Coordinate coordinate = null;
			bool flipped = false;
			for (int num7 = 0; num7 < list2.Count; num7++)
			{
				bool flag = true;
				for (int num8 = 0; num8 < list[num3].actualSizeX; num8++)
				{
					for (int num9 = 0; num9 < list[num3].actualSizeY; num9++)
					{
						Coordinate coordinate2 = TransformCoordinatePoint(array, list2[num7], new Coordinate(num8, num9), gridX, gridY);
						if (coordinate2 == null)
						{
							flag = false;
						}
						else if (coordinate2.occupant != null)
						{
							flag = false;
						}
						if (!flag)
						{
							break;
						}
					}
					if (!flag)
					{
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
				for (int num10 = 0; num10 < list[num3].actualSizeX; num10++)
				{
					for (int num11 = 0; num11 < list[num3].actualSizeY; num11++)
					{
						TransformCoordinatePoint(array, list2[num7], new Coordinate(num10, num11), gridX, gridY).occupant = list[num3];
					}
				}
				int num12 = GetRegionSize(array, gridX, gridY) - regionSize;
				if (num12 < num6)
				{
					num6 = num12;
					coordinate = list2[num7];
					flipped = false;
				}
				for (int num13 = 0; num13 < list[num3].actualSizeX; num13++)
				{
					for (int num14 = 0; num14 < list[num3].actualSizeY; num14++)
					{
						TransformCoordinatePoint(array, list2[num7], new Coordinate(num13, num14), gridX, gridY).occupant = null;
					}
				}
			}
			for (int num15 = 0; num15 < list2.Count; num15++)
			{
				bool flag2 = true;
				list[num3].flipped = true;
				for (int num16 = 0; num16 < list[num3].actualSizeX; num16++)
				{
					for (int num17 = 0; num17 < list[num3].actualSizeY; num17++)
					{
						Coordinate coordinate3 = TransformCoordinatePoint(array, list2[num15], new Coordinate(num16, num17), gridX, gridY);
						if (coordinate3 == null)
						{
							flag2 = false;
						}
						else if (coordinate3.occupant != null)
						{
							flag2 = false;
						}
						if (!flag2)
						{
							break;
						}
					}
					if (!flag2)
					{
						break;
					}
				}
				if (!flag2)
				{
					continue;
				}
				for (int num18 = 0; num18 < list[num3].actualSizeX; num18++)
				{
					for (int num19 = 0; num19 < list[num3].actualSizeY; num19++)
					{
						TransformCoordinatePoint(array, list2[num15], new Coordinate(num18, num19), gridX, gridY).occupant = list[num3];
					}
				}
				int num20 = GetRegionSize(array, gridX, gridY) - regionSize;
				if (num20 < num6)
				{
					num6 = num20;
					coordinate = list2[num15];
					flipped = true;
				}
				for (int num21 = 0; num21 < list[num3].actualSizeX; num21++)
				{
					for (int num22 = 0; num22 < list[num3].actualSizeY; num22++)
					{
						TransformCoordinatePoint(array, list2[num15], new Coordinate(num21, num22), gridX, gridY).occupant = null;
					}
				}
			}
			if (coordinate == null)
			{
				Console.LogWarning("Unable to resolve rectangle position.");
				continue;
			}
			list[num3].flipped = flipped;
			for (int num23 = 0; num23 < list[num3].actualSizeX; num23++)
			{
				for (int num24 = 0; num24 < list[num3].actualSizeY; num24++)
				{
					TransformCoordinatePoint(array, coordinate, new Coordinate(num23, num24), gridX, gridY).occupant = list[num3];
				}
			}
			list[num3].xPos = coordinate.x;
			list[num3].yPos = coordinate.y;
		}
		return rects;
	}

	private bool DoesCoordinateHaveOccupiedAdjacent(Coordinate[,] grid, Coordinate coord, int gridX, int gridY)
	{
		Coordinate coordinate = new Coordinate(coord.x - 1, coord.y);
		if (IsCoordinateInBounds(coordinate, gridX, gridY) && grid[coordinate.x, coordinate.y].occupant != null)
		{
			return true;
		}
		Coordinate coordinate2 = new Coordinate(coord.x + 1, coord.y);
		if (IsCoordinateInBounds(coordinate2, gridX, gridY) && grid[coordinate2.x, coordinate2.y].occupant != null)
		{
			return true;
		}
		Coordinate coordinate3 = new Coordinate(coord.x, coord.y - 1);
		if (IsCoordinateInBounds(coordinate3, gridX, gridY) && grid[coordinate3.x, coordinate3.y].occupant != null)
		{
			return true;
		}
		Coordinate coordinate4 = new Coordinate(coord.x, coord.y + 1);
		if (IsCoordinateInBounds(coordinate4, gridX, gridY) && grid[coordinate4.x, coordinate4.y].occupant != null)
		{
			return true;
		}
		return false;
	}

	private bool IsCoordinateInBounds(Coordinate coord, int gridX, int gridY)
	{
		if (coord.x < 0 || coord.x >= gridX)
		{
			return false;
		}
		if (coord.y < 0 || coord.y >= gridY)
		{
			return false;
		}
		return true;
	}

	private void PrintGrid(Coordinate[,] grid, int gridX, int gridY)
	{
		string text = string.Empty;
		for (int i = 0; i < gridY; i++)
		{
			for (int j = 0; j < gridX; j++)
			{
				text = ((grid[j, gridY - i - 1].occupant != null) ? (text + grid[j, gridY - i - 1].occupant.name + ", ") : (text + "*, "));
			}
			text += "\n";
		}
		Console.Log(text);
	}

	private int GetRegionSize(Coordinate[,] grid, int gridX, int gridY)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < gridX; i++)
		{
			for (int j = 0; j < gridY; j++)
			{
				if (grid[i, j].occupant != null)
				{
					if (i > num3)
					{
						num3 = i;
					}
					if (j > num4)
					{
						num4 = j;
					}
				}
			}
		}
		return (num3 - num) * (num4 - num2);
	}

	private Coordinate TransformCoordinatePoint(Coordinate[,] grid, Coordinate baseCoordinate, Coordinate offset, int gridX, int gridY)
	{
		if (IsCoordinateInBounds(new Coordinate(baseCoordinate.x + offset.x, baseCoordinate.y + offset.y), gridX, gridY))
		{
			return grid[baseCoordinate.x + offset.x, baseCoordinate.y + offset.y];
		}
		return null;
	}
}
