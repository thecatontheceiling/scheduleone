using System;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Audio;

public class Zone : MonoBehaviour
{
	public const float UPDATE_INTERVAL = 0.25f;

	public Transform PointContainer;

	public bool IsClosed = true;

	public float VerticalSize = 5f;

	[Header("Debug")]
	public Color ZoneColor = Color.white;

	private Vector3[] points;

	public float LocalPlayerDistance { get; protected set; }

	private void Awake()
	{
		points = GetPoints();
		InvokeRepeating("Recalculate", 0f, 0.25f);
	}

	public void Recalculate()
	{
		if (PlayerSingleton<PlayerCamera>.InstanceExists)
		{
			Vector3 position = PlayerSingleton<PlayerCamera>.Instance.transform.position;
			float num = 0f;
			Vector3 zero = Vector3.zero;
			if (IsClosed && DoBoundsContainPoint(position) && IsPointInsidePolygon(points, position))
			{
				num = 0f;
			}
			else
			{
				zero = GetClosestPointOnPolygon(points, position);
				zero.y = position.y;
				num = Vector3.Distance(zero, position);
			}
			float f = 0f;
			Vector3 vector = base.transform.InverseTransformPoint(position);
			if (vector.y > VerticalSize)
			{
				f = vector.y - VerticalSize;
			}
			LocalPlayerDistance = Mathf.Sqrt(Mathf.Pow(num, 2f) + Mathf.Pow(f, 2f));
		}
	}

	private void OnDrawGizmos()
	{
		if (PointContainer.childCount >= 2)
		{
			Vector3[] array = GetPoints();
			for (int i = 0; i < array.Length - 1; i++)
			{
				Vector3 vector = array[i];
				Vector3 vector2 = array[i + 1];
				Debug.DrawLine(vector, vector2, ZoneColor);
				Debug.DrawLine(vector + Vector3.up * VerticalSize, vector2 + Vector3.up * VerticalSize, ZoneColor);
				Gizmos.color = ZoneColor;
				Gizmos.DrawSphere(vector, 0.5f);
			}
			if (IsClosed)
			{
				Debug.DrawLine(array[^1], array[0], ZoneColor);
				Debug.DrawLine(array[^1] + Vector3.up * VerticalSize, array[0] + Vector3.up * VerticalSize, ZoneColor);
			}
		}
	}

	private Vector3[] GetPoints()
	{
		if (PointContainer == null)
		{
			return new Vector3[0];
		}
		Vector3[] array = new Vector3[PointContainer.childCount];
		for (int i = 0; i < PointContainer.childCount; i++)
		{
			array[i] = PointContainer.GetChild(i).position;
		}
		return array;
	}

	private bool DoBoundsContainPoint(Vector3 point)
	{
		Tuple<Vector3, Vector3> boundingPoints = GetBoundingPoints();
		if (point.x >= boundingPoints.Item1.x && point.x <= boundingPoints.Item2.x && point.z >= boundingPoints.Item1.z)
		{
			return point.z <= boundingPoints.Item2.z;
		}
		return false;
	}

	private Tuple<Vector3, Vector3> GetBoundingPoints()
	{
		Vector3[] source = GetPoints();
		float x = source.Select((Vector3 p) => p.x).Max();
		float x2 = source.Select((Vector3 p) => p.x).Min();
		float z = source.Select((Vector3 p) => p.z).Max();
		float z2 = source.Select((Vector3 p) => p.z).Min();
		return new Tuple<Vector3, Vector3>(new Vector3(x2, 0f, z2), new Vector3(x, VerticalSize, z));
	}

	private bool IsPointInsidePolygon(Vector3[] polyPoints, Vector3 point)
	{
		Vector2[] array = new Vector2[polyPoints.Length];
		for (int i = 0; i < polyPoints.Length; i++)
		{
			array[i] = new Vector2(polyPoints[i].x, polyPoints[i].z);
		}
		return CalculateWindingNumber(array, new Vector2(point.x, point.z)) != 0;
	}

	private int CalculateWindingNumber(Vector2[] polygon, Vector2 point)
	{
		int num = 0;
		for (int i = 0; i < polygon.Length; i++)
		{
			Vector2 start = polygon[i];
			Vector2 end = polygon[(i + 1) % polygon.Length];
			if (IsPointOnSegment(start, end, point))
			{
				return 0;
			}
			if (start.y <= point.y)
			{
				if (end.y > point.y && IsLeft(start, end, point) > 0)
				{
					num++;
				}
			}
			else if (end.y <= point.y && IsLeft(start, end, point) < 0)
			{
				num--;
			}
		}
		return num;
		static float CrossProduct(Vector2 vector2, Vector2 vector3, Vector2 vector)
		{
			return (vector.x - vector2.x) * (vector3.y - vector2.y) - (vector.y - vector2.y) * (vector3.x - vector2.x);
		}
		static float DotProduct(Vector2 vector2, Vector2 vector3, Vector2 vector)
		{
			return (vector.x - vector2.x) * (vector3.x - vector2.x) + (vector.y - vector2.y) * (vector3.y - vector2.y);
		}
		static int IsLeft(Vector2 start2, Vector2 end2, Vector2 point2)
		{
			float num2 = CrossProduct(start2, end2, point2);
			if (Mathf.Abs(num2) < 0.001f)
			{
				return 0;
			}
			if (num2 > 0f)
			{
				return 1;
			}
			return -1;
		}
		static bool IsPointOnSegment(Vector2 vector, Vector2 vector2, Vector2 point2)
		{
			if (Mathf.Abs(CrossProduct(vector, vector2, point2)) > 0.001f)
			{
				return false;
			}
			float num2 = DotProduct(vector, vector2, point2);
			if (num2 < 0f)
			{
				return false;
			}
			float sqrMagnitude = (vector2 - vector).sqrMagnitude;
			if (num2 > sqrMagnitude)
			{
				return false;
			}
			return true;
		}
	}

	private Vector3 GetClosestPointOnPolygon(Vector3[] polyPoints, Vector3 point)
	{
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		for (int i = 0; i < polyPoints.Length - 1; i++)
		{
			Vector3 lineStart = polyPoints[i];
			Vector3 lineEnd = polyPoints[i + 1];
			Vector3 vector = ProjectPointOnLineSegment(lineStart, lineEnd, point);
			float num2 = Vector3.Distance(point, vector);
			if (num2 < num)
			{
				num = num2;
				result = vector;
			}
		}
		if (IsClosed)
		{
			Vector3 lineStart2 = polyPoints[^1];
			Vector3 lineEnd2 = polyPoints[0];
			Vector3 vector2 = ProjectPointOnLineSegment(lineStart2, lineEnd2, point);
			float num3 = Vector3.Distance(point, vector2);
			if (num3 < num)
			{
				num = num3;
				result = vector2;
			}
		}
		return result;
		static Vector3 ProjectPointOnLineSegment(Vector3 vector5, Vector3 vector4, Vector3 vector6)
		{
			Vector3 vector3 = vector4 - vector5;
			float magnitude = vector3.magnitude;
			vector3.Normalize();
			float value = Vector3.Dot(vector6 - vector5, vector3);
			value = Mathf.Clamp(value, 0f, magnitude);
			return vector5 + vector3 * value;
		}
	}
}
