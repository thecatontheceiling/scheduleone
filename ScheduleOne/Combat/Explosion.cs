using System.Collections.Generic;
using FishNet;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Noise;
using UnityEngine;

namespace ScheduleOne.Combat;

public class Explosion : MonoBehaviour
{
	public AudioSourceController Sound;

	public void Initialize(Vector3 origin, ExplosionData data)
	{
		base.transform.position = origin;
		Sound.Play();
		float num = Mathf.Max(data.DamageRadius, data.PushForceRadius);
		NoiseUtility.EmitNoise(origin, ENoiseType.Explosion, num * 4f, base.gameObject);
		List<IDamageable> list = new List<IDamageable>();
		if (InstanceFinder.IsServer)
		{
			Collider[] array = Physics.OverlapSphere(origin, num);
			foreach (Collider collider in array)
			{
				IDamageable componentInParent = collider.GetComponentInParent<IDamageable>();
				if (componentInParent == null || list.Contains(componentInParent))
				{
					continue;
				}
				Console.Log("Explosion hit " + componentInParent?.ToString() + " at " + collider.transform.position.ToString());
				RaycastHit hitInfo = default(RaycastHit);
				if (Vector3.Distance(origin, collider.transform.position) < 1f)
				{
					hitInfo.point = origin;
				}
				else
				{
					if (!Physics.Raycast(origin, collider.transform.position - origin, out hitInfo, num, NetworkSingleton<CombatManager>.Instance.ExplosionLayerMask))
					{
						Debug.DrawLine(origin, collider.transform.position, Color.green, 5f);
						continue;
					}
					Debug.DrawLine(origin, hitInfo.point, Color.red, 5f);
					if (hitInfo.collider != collider && hitInfo.collider.GetComponentInParent<IDamageable>() != componentInParent)
					{
						continue;
					}
				}
				list.Add(componentInParent);
				float num2 = Vector3.Distance(origin, collider.transform.position);
				float impactDamage = Mathf.Lerp(data.MaxDamage, 0f, Mathf.Clamp01(num2 / data.DamageRadius));
				float impactForce = Mathf.Lerp(data.MaxPushForce, 0f, Mathf.Clamp01(num2 / data.PushForceRadius));
				Impact impact = new Impact(hitInfo, hitInfo.point, (hitInfo.point - origin).normalized, impactForce, impactDamage, EImpactType.Explosion, null, Random.Range(0, int.MaxValue));
				componentInParent.ReceiveImpact(impact);
			}
		}
		Console.Log("Explosion hit " + list.Count + " damageables.");
	}
}
