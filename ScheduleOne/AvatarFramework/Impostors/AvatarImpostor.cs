using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.AvatarFramework.Impostors;

public class AvatarImpostor : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	private Transform cachedCamera;

	public bool HasTexture { get; private set; }

	private Transform Camera
	{
		get
		{
			if (cachedCamera == null)
			{
				cachedCamera = PlayerSingleton<PlayerCamera>.Instance?.transform;
			}
			return cachedCamera;
		}
	}

	public void SetAvatarSettings(AvatarSettings settings)
	{
		Texture2D impostorTexture = settings.ImpostorTexture;
		if (impostorTexture != null)
		{
			meshRenderer.material.mainTexture = impostorTexture;
			HasTexture = true;
		}
	}

	private void LateUpdate()
	{
		Realign();
	}

	private void Realign()
	{
		if (Camera != null)
		{
			Vector3 position = Camera.position;
			position.y = base.transform.position.y;
			Vector3 forward = base.transform.position - position;
			base.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	public void EnableImpostor()
	{
		base.gameObject.SetActive(value: true);
		Realign();
	}

	public void DisableImpostor()
	{
		base.gameObject.SetActive(value: false);
	}
}
