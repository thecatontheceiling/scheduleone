using System;

namespace ScheduleOne.Law;

[Serializable]
public class Evading : Crime
{
	public override string CrimeName { get; protected set; } = "Evading arrest";
}
