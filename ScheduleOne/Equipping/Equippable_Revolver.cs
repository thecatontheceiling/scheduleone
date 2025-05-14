using ScheduleOne.ItemFramework;
using UnityEngine;

namespace ScheduleOne.Equipping;

public class Equippable_Revolver : Equippable_RangedWeapon
{
	public Transform[] Bullets;

	public override void Equip(ItemInstance item)
	{
		base.Equip(item);
		SetDisplayedBullets(weaponItem.Value);
	}

	public override void Fire()
	{
		base.Fire();
		SetDisplayedBullets(weaponItem.Value);
	}

	public override void Reload()
	{
		base.Reload();
		SetDisplayedBullets(weaponItem.Value);
	}

	protected override void NotifyIncrementalReload()
	{
		base.NotifyIncrementalReload();
		SetDisplayedBullets(weaponItem.Value);
	}

	private void SetDisplayedBullets(int count)
	{
		for (int i = 0; i < Bullets.Length; i++)
		{
			Bullets[i].gameObject.SetActive(i < count);
		}
	}
}
