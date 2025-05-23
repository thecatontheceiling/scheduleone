using UnityEngine;

namespace Beautify.Universal;

public class SphereAnimator : MonoBehaviour
{
	private Rigidbody rb;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		if (base.transform.position.z < 2.5f)
		{
			rb.AddForce(Vector3.forward * 200f * Time.fixedDeltaTime);
		}
		else if (base.transform.position.z > 8f)
		{
			rb.AddForce(Vector3.back * 200f * Time.fixedDeltaTime);
		}
	}
}
