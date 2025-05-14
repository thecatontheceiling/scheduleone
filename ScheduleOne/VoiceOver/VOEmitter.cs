using ScheduleOne.Audio;
using UnityEngine;

namespace ScheduleOne.VoiceOver;

[RequireComponent(typeof(AudioSourceController))]
public class VOEmitter : MonoBehaviour
{
	public const float PitchVariation = 0.05f;

	[SerializeField]
	private VODatabase Database;

	[Range(0.5f, 2f)]
	public float PitchMultiplier = 1f;

	private float runtimePitchMultiplier = 1f;

	protected AudioSourceController audioSourceController;

	private VODatabase defaultVODatabase;

	protected virtual void Awake()
	{
		audioSourceController = GetComponent<AudioSourceController>();
	}

	public virtual void Play(EVOLineType lineType)
	{
		if (!audioSourceController.gameObject.activeInHierarchy)
		{
			return;
		}
		if (Database == null)
		{
			Console.LogError("Database is not set on VOEmitter.");
			return;
		}
		AudioClip randomClip = Database.GetRandomClip(lineType);
		if (randomClip == null)
		{
			Console.LogError("No clip found for line type: " + lineType);
			return;
		}
		audioSourceController.Stop();
		audioSourceController.AudioSource.clip = randomClip;
		audioSourceController.VolumeMultiplier = Database.VolumeMultiplier * Database.GetEntry(lineType).VolumeMultiplier;
		audioSourceController.PitchMultiplier = (PitchMultiplier + Random.Range(-0.05f, 0.05f)) * runtimePitchMultiplier;
		audioSourceController.Play();
	}

	public void SetRuntimePitchMultiplier(float pitchMultiplier)
	{
		runtimePitchMultiplier = pitchMultiplier;
	}

	public void SetDatabase(VODatabase database, bool writeDefault = true)
	{
		Database = database;
		if (writeDefault)
		{
			defaultVODatabase = database;
		}
	}

	public void ResetDatabase()
	{
		SetDatabase(defaultVODatabase, writeDefault: false);
	}
}
