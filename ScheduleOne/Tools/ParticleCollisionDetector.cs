using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Tools;

public class ParticleCollisionDetector : MonoBehaviour
{
	public UnityEvent<GameObject> onCollision = new UnityEvent<GameObject>();

	private ParticleSystem ps;

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
	}

	public void OnParticleCollision(GameObject other)
	{
		if (onCollision != null)
		{
			onCollision.Invoke(other);
		}
	}

	private void OnParticleTrigger()
	{
		Component collider = ps.trigger.GetCollider(0);
		if (collider != null && onCollision != null)
		{
			onCollision.Invoke(collider.gameObject);
		}
	}
}
