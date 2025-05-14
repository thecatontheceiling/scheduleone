using ScheduleOne.AvatarFramework;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScheduleOne.PlayerScripts;

public class ViewmodelAvatar : Singleton<ViewmodelAvatar>
{
	public ScheduleOne.AvatarFramework.Avatar ParentAvatar;

	public Animator Animator;

	public ScheduleOne.AvatarFramework.Avatar Avatar;

	public Transform RightHandContainer;

	private Vector3 baseOffset;

	public bool IsVisible { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		baseOffset = base.transform.localPosition;
		SetVisibility(isVisible: false);
		if (ParentAvatar.CurrentSettings != null)
		{
			SetAppearance(ParentAvatar.CurrentSettings);
		}
		ParentAvatar.onSettingsLoaded.AddListener(delegate
		{
			SetAppearance(ParentAvatar.CurrentSettings);
		});
	}

	public void SetVisibility(bool isVisible)
	{
		SetOffset(Vector3.zero);
		IsVisible = isVisible;
		base.gameObject.SetActive(isVisible);
	}

	public void SetAppearance(AvatarSettings settings)
	{
		AvatarSettings avatarSettings = Object.Instantiate(settings);
		avatarSettings.Height = 0.25f;
		Avatar.LoadAvatarSettings(avatarSettings);
		LayerUtility.SetLayerRecursively(base.gameObject, LayerMask.NameToLayer("Viewmodel"));
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (meshRenderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
			{
				meshRenderer.enabled = false;
			}
			else
			{
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren2)
		{
			if (skinnedMeshRenderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
			{
				skinnedMeshRenderer.enabled = false;
			}
			else
			{
				skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	public void SetAnimatorController(RuntimeAnimatorController controller)
	{
		Animator.runtimeAnimatorController = controller;
	}

	public void SetOffset(Vector3 offset)
	{
		base.transform.localPosition = baseOffset + offset;
	}
}
