using UnityEngine;

namespace ScheduleOne.PlayerTasks;

public class RotateRigidbodyToTarget : MonoBehaviour
{
	public Rigidbody Rigidbody;

	public Vector3 TargetRotation;

	public float RotationForce = 1f;

	public Transform Bitch;

	public void FixedUpdate()
	{
		Bitch.localRotation = Quaternion.Euler(TargetRotation);
		Quaternion rotation = Bitch.rotation;
		Quaternion quaternion = rotation * Quaternion.Inverse(base.transform.rotation);
		Vector3 vector = Vector3.Normalize(new Vector3(quaternion.x, quaternion.y, quaternion.z)) * RotationForce;
		float num = Mathf.Clamp01(Quaternion.Angle(base.transform.rotation, rotation) / 90f);
		Rigidbody.AddTorque(vector * num, ForceMode.Acceleration);
	}
}
