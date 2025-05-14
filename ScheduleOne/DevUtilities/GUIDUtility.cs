using System;
using EasyButtons;
using UnityEngine;

namespace ScheduleOne.DevUtilities;

public class GUIDUtility : MonoBehaviour
{
	[Button]
	public void GenerateGUID()
	{
		Console.Log(Guid.NewGuid().ToString());
	}
}
