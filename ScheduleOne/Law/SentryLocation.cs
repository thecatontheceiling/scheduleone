using System.Collections.Generic;
using ScheduleOne.Police;
using UnityEngine;

namespace ScheduleOne.Law;

public class SentryLocation : MonoBehaviour
{
	[Header("References")]
	public List<Transform> StandPoints = new List<Transform>();

	[Header("Info")]
	public List<PoliceOfficer> AssignedOfficers = new List<PoliceOfficer>();
}
