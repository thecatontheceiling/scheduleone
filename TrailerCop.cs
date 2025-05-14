using EasyButtons;
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.NPCs;
using ScheduleOne.Police;
using UnityEngine;

public class TrailerCop : MonoBehaviour
{
	public PoliceOfficer Officer;

	public Transform StartPoint;

	public Transform EndPoint;

	public Transform FaceTarget;

	public AvatarEquippable Equippable;

	public float Speed = 0.3f;

	public bool RaiseWeapon;

	public Transform ShootTarget;

	[Button]
	public void Play()
	{
		Officer.Movement.Warp(StartPoint.position);
		Officer.SetEquippable_Networked(null, Equippable.AssetPath);
		if (RaiseWeapon)
		{
			Officer.SendEquippableMessage_Networked(null, "Raise");
		}
		Officer.Avatar.EmotionManager.AddEmotionOverride("Angry", "trailercop", 0f, 100);
		Officer.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("trailercop", 10, Speed));
		Officer.Movement.SetDestination(EndPoint.position);
	}

	private void Update()
	{
		if (Officer.Movement.IsMoving)
		{
			Officer.Avatar.LookController.OverrideLookTarget(FaceTarget.position, 10, rotateBody: true);
		}
	}

	public void Shoot()
	{
		Officer.SendEquippableMessage_Networked_Vector(null, "Shoot", ShootTarget.position);
	}
}
