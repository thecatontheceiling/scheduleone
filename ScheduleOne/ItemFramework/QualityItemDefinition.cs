using System;
using UnityEngine;

namespace ScheduleOne.ItemFramework;

[Serializable]
[CreateAssetMenu(fileName = "StorableItemDefinition", menuName = "ScriptableObjects/QualityItemDefinition", order = 1)]
public class QualityItemDefinition : StorableItemDefinition
{
	[Header("Quality")]
	public EQuality DefaultQuality = EQuality.Standard;

	public override ItemInstance GetDefaultInstance(int quantity = 1)
	{
		return new QualityItemInstance(this, quantity, DefaultQuality);
	}
}
