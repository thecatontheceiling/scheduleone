using UnityEngine;

namespace ScheduleOne.Tools;

public class EditionConditionalObject : MonoBehaviour
{
	public enum EType
	{
		ActiveInDemo = 0,
		ActiveInFullGame = 1
	}

	public EType type;

	private void Awake()
	{
		if (type == EType.ActiveInDemo)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
