using System;
using ScheduleOne.Vehicles.Modification;
using UnityEngine;

namespace ScheduleOne.Persistence.Datas;

[Serializable]
public class VehicleData : SaveData
{
	public string GUID;

	public string VehicleCode;

	public Vector3 Position;

	public Quaternion Rotation;

	public string Color;

	public ItemSet VehicleContents;

	public VehicleData(Guid guid, string code, Vector3 pos, Quaternion rot, EVehicleColor col, ItemSet vehicleContents)
	{
		GUID = guid.ToString();
		VehicleCode = code;
		Position = pos;
		Rotation = rot;
		Color = col.ToString();
		VehicleContents = vehicleContents;
	}
}
