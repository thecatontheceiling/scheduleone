using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Building;

public class BuildUpdate_Surface : BuildUpdate_Base
{
	public GameObject GhostModel;

	public SurfaceItem BuildableItemClass;

	public ItemInstance ItemInstance;

	public float CurrentRotation;

	[Header("Settings")]
	public LayerMask DetectionMask;

	protected bool validPosition;

	protected Material currentGhostMaterial;

	protected Surface hoveredValidSurface;

	private float detectionRange => Mathf.Max(BuildableItemClass.HoldDistance, 4f);

	protected virtual void Start()
	{
		LateUpdate();
	}

	protected virtual void Update()
	{
		CheckRotation();
		if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) && validPosition)
		{
			Place();
		}
	}

	protected virtual void LateUpdate()
	{
		validPosition = false;
		GhostModel.transform.up = Vector3.up;
		PositionObjectInFrontOfPlayer(BuildableItemClass.HoldDistance, sanitizeForward: true);
		Surface surface = null;
		if (PlayerSingleton<PlayerCamera>.Instance.LookRaycast_ExcludeBuildables(detectionRange, out var hit, DetectionMask))
		{
			surface = hit.collider.GetComponentInParent<Surface>();
		}
		if (IsSurfaceValidForItem(surface, hit.collider, hit.point))
		{
			hoveredValidSurface = surface;
			validPosition = true;
		}
		else
		{
			hoveredValidSurface = null;
		}
		if ((!Application.isEditor || !Input.GetKey(KeyCode.LeftAlt)) && BuildableItemClass.GetPenetration(out var x, out var z, out var y))
		{
			if (Vector3.Distance(GhostModel.transform.position - GhostModel.transform.right * x, PlayerSingleton<PlayerCamera>.Instance.transform.position) < Vector3.Distance(GhostModel.transform.position - GhostModel.transform.forward * z, PlayerSingleton<PlayerCamera>.Instance.transform.position))
			{
				GhostModel.transform.position -= GhostModel.transform.right * x;
				if (BuildableItemClass.GetPenetration(out x, out z, out y))
				{
					GhostModel.transform.position -= GhostModel.transform.forward * z;
				}
			}
			else
			{
				GhostModel.transform.position -= GhostModel.transform.forward * z;
				if (BuildableItemClass.GetPenetration(out x, out z, out y))
				{
					GhostModel.transform.position -= GhostModel.transform.right * x;
				}
			}
			GhostModel.transform.position -= GhostModel.transform.up * y;
		}
		UpdateMaterials();
	}

	protected void PositionObjectInFrontOfPlayer(float dist, bool sanitizeForward)
	{
		Vector3 forward = PlayerSingleton<PlayerCamera>.Instance.transform.forward;
		if (sanitizeForward)
		{
			forward.y = 0f;
		}
		Vector3 vector = PlayerSingleton<PlayerCamera>.Instance.transform.position + forward * dist;
		Vector3 forward2 = (PlayerSingleton<PlayerCamera>.Instance.transform.position - vector).normalized;
		if (PlayerSingleton<PlayerCamera>.Instance.LookRaycast_ExcludeBuildables(detectionRange, out var hit, DetectionMask))
		{
			vector = hit.point;
			forward2 = hit.normal;
		}
		else if (BuildableItemClass.MidAirCenterPoint != null)
		{
			vector += -GhostModel.transform.InverseTransformPoint(BuildableItemClass.MidAirCenterPoint.transform.position);
		}
		Quaternion quaternion = Quaternion.LookRotation(forward2, Vector3.up);
		GhostModel.transform.rotation = quaternion * Quaternion.Inverse(BuildableItemClass.BuildPoint.transform.rotation);
		GhostModel.transform.RotateAround(BuildableItemClass.BuildPoint.transform.position, BuildableItemClass.BuildPoint.transform.forward, CurrentRotation);
		GhostModel.transform.position = vector - GhostModel.transform.InverseTransformPoint(BuildableItemClass.BuildPoint.transform.position);
	}

	private bool IsSurfaceValidForItem(Surface surface, Collider hitCollider, Vector3 hitPoint)
	{
		if (surface == null)
		{
			return false;
		}
		if (!BuildableItemClass.ValidSurfaceTypes.Contains(surface.SurfaceType))
		{
			return false;
		}
		if (surface.ParentProperty == null || !surface.ParentProperty.IsOwned)
		{
			return false;
		}
		if (!surface.IsPointValid(hitPoint, hitCollider))
		{
			return false;
		}
		return true;
	}

	protected void CheckRotation()
	{
		if (!BuildableItemClass.AllowRotation)
		{
			CurrentRotation = 0f;
			return;
		}
		if (GameInput.GetButtonDown(GameInput.ButtonCode.RotateLeft) && !GameInput.IsTyping)
		{
			CurrentRotation -= BuildableItemClass.RotationIncrement;
		}
		if (GameInput.GetButtonDown(GameInput.ButtonCode.RotateRight) && !GameInput.IsTyping)
		{
			CurrentRotation += BuildableItemClass.RotationIncrement;
		}
	}

	protected void UpdateMaterials()
	{
		Material material = Singleton<BuildManager>.Instance.ghostMaterial_White;
		if (!validPosition)
		{
			material = Singleton<BuildManager>.Instance.ghostMaterial_Red;
		}
		if (currentGhostMaterial != material)
		{
			currentGhostMaterial = material;
			Singleton<BuildManager>.Instance.ApplyMaterial(GhostModel, material);
		}
	}

	protected virtual void Place()
	{
		Mathf.RoundToInt(CurrentRotation);
		Vector3 relativePosition = hoveredValidSurface.GetRelativePosition(GhostModel.transform.position);
		Quaternion relativeRotation = hoveredValidSurface.GetRelativeRotation(GhostModel.transform.rotation);
		Singleton<BuildManager>.Instance.CreateSurfaceItem(ItemInstance.GetCopy(1), hoveredValidSurface, relativePosition, relativeRotation);
		PlayerSingleton<PlayerInventory>.Instance.equippedSlot.ChangeQuantity(-1);
		Singleton<BuildManager>.Instance.PlayBuildSound((ItemInstance.Definition as BuildableItemDefinition).BuildSoundType, GhostModel.transform.position);
	}
}
