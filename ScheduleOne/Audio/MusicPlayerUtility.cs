using ScheduleOne.DevUtilities;
using UnityEngine;

namespace ScheduleOne.Audio;

public class MusicPlayerUtility : MonoBehaviour
{
	public void PlayTrack(string trackName)
	{
		Singleton<MusicPlayer>.Instance.SetTrackEnabled(trackName, enabled: true);
	}

	public void StopTracks()
	{
		Singleton<MusicPlayer>.Instance.StopAndDisableTracks();
	}
}
