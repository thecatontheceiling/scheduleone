using System;
using System.Collections.Generic;
using EasyButtons;
using ScheduleOne.Property;
using UnityEngine;

namespace ScheduleOne.Building;

public class Surface : MonoBehaviour, IGUIDRegisterable
{
	public enum ESurfaceType
	{
		Wall = 0,
		Roof = 1
	}

	public enum EFace
	{
		Front = 0,
		Back = 1,
		Top = 2,
		Bottom = 3,
		Left = 4,
		Right = 5
	}

	[Header("Settings")]
	public ESurfaceType SurfaceType;

	public List<EFace> ValidFaces = new List<EFace> { EFace.Front };

	public ScheduleOne.Property.Property ParentProperty;

	[SerializeField]
	protected string BakedGUID = string.Empty;

	public Guid GUID { get; protected set; }

	public Transform Container => ParentProperty.Container.transform;

	[Button]
	public void RegenerateGUID()
	{
		BakedGUID = Guid.NewGuid().ToString();
	}

	private void OnValidate()
	{
		if (ParentProperty == null)
		{
			ParentProperty = GetComponentInParent<ScheduleOne.Property.Property>();
		}
		if (string.IsNullOrEmpty(BakedGUID))
		{
			RegenerateGUID();
		}
	}

	protected virtual void Awake()
	{
		if (!GUIDManager.IsGUIDValid(BakedGUID))
		{
			Console.LogError(base.gameObject.name + "'s baked GUID is not valid! Bad.");
		}
		if (GUIDManager.IsGUIDAlreadyRegistered(new Guid(BakedGUID)))
		{
			Console.LogError(base.gameObject.name + "'s baked GUID is already registered! Bad.", this);
		}
		GUID = new Guid(BakedGUID);
		GUIDManager.RegisterObject(this);
	}

	public void SetGUID(Guid guid)
	{
		GUID = guid;
		GUIDManager.RegisterObject(this);
	}

	public Vector3 GetRelativePosition(Vector3 worldPosition)
	{
		return base.transform.InverseTransformPoint(worldPosition);
	}

	public Quaternion GetRelativeRotation(Quaternion worldRotation)
	{
		return Quaternion.Inverse(base.transform.rotation) * worldRotation;
	}

	public bool IsFrontFace(Vector3 point, Collider collider)
	{
		return collider.transform.InverseTransformPoint(point).z > 0f;
	}

	public bool IsPointValid(Vector3 point, Collider hitCollider)
	{
		Vector3 vector = Vector3.zero;
		if (hitCollider is BoxCollider)
		{
			vector = (hitCollider as BoxCollider).center;
		}
		else if (hitCollider is MeshCollider)
		{
			vector = (hitCollider as MeshCollider).sharedMesh.bounds.center;
		}
		Vector3 vector2 = hitCollider.transform.InverseTransformPoint(point) - vector;
		using (List<EFace>.Enumerator enumerator = ValidFaces.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
				case EFace.Front:
					if (vector2.z >= 0f)
					{
						return true;
					}
					break;
				case EFace.Back:
					if (vector2.z <= 0f)
					{
						return true;
					}
					break;
				case EFace.Top:
					if (vector2.y >= 0f)
					{
						return true;
					}
					break;
				case EFace.Bottom:
					if (vector2.y <= 0f)
					{
						return true;
					}
					break;
				case EFace.Left:
					if (vector2.x <= 0f)
					{
						return true;
					}
					break;
				case EFace.Right:
					if (vector2.x >= 0f)
					{
						return true;
					}
					break;
				}
			}
		}
		return false;
	}
}
