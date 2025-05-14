using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
	private IEnumerator FetchActiveVote()
	{
		string uri = "https://your-backend-url.com/active_vote";
		using UnityWebRequest request = UnityWebRequest.Get(uri);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			JObject jObject = JObject.Parse(request.downloadHandler.text);
			Debug.Log("Active Vote: " + jObject.ToString());
		}
		else
		{
			Debug.LogError("Failed to fetch vote: " + request.downloadHandler.text);
		}
	}
}
