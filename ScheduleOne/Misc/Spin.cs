using UnityEngine;

namespace ScheduleOne.Misc;

public class Spin : MonoBehaviour
{
	public Vector3 Axis;

	public float Speed;

	private void Update()
	{
		base.transform.Rotate(Axis, Speed * Time.deltaTime, Space.Self);
	}
}
