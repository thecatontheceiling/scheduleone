using System;
using UnityEngine;

namespace AdvancedPeopleSystem;

[Serializable]
public class MinMaxFacialBlendshapes
{
	public string name;

	[Range(-100f, 100f)]
	public float Min;

	[Range(-100f, 100f)]
	public float Max;

	public float GetRandom()
	{
		return UnityEngine.Random.Range(Min, Max);
	}
}
