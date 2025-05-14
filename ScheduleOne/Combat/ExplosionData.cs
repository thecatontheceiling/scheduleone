namespace ScheduleOne.Combat;

public struct ExplosionData
{
	public float DamageRadius;

	public float MaxDamage;

	public float PushForceRadius;

	public float MaxPushForce;

	public static readonly ExplosionData DefaultSmall = new ExplosionData(6f, 200f, 500f);

	public ExplosionData(float damageRadius, float maxDamage, float maxPushForce)
	{
		DamageRadius = damageRadius;
		MaxDamage = maxDamage;
		PushForceRadius = damageRadius * 2f;
		MaxPushForce = maxPushForce;
	}
}
