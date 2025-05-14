using System.Collections.Generic;
using UnityEngine;

namespace ScheduleOne.AvatarFramework.Impostors;

public class ImpostorGenerator : MonoBehaviour
{
	[Header("References")]
	public Camera ImpostorCamera;

	public Avatar Avatar;

	[Header("Settings")]
	public List<AvatarSettings> GenerationQueue = new List<AvatarSettings>();

	[SerializeField]
	private Texture2D output;
}
