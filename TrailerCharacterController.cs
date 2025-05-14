using System.Collections;
using EasyButtons;
using UnityEngine;

public class TrailerCharacterController : MonoBehaviour
{
	public Transform StartPos;

	public Transform EndPos;

	public Transform Character;

	public float WalkSpeed = 2f;

	private Coroutine routine;

	private void Awake()
	{
		if (Input.GetKeyDown(KeyCode.LeftCurlyBracket))
		{
			Play();
		}
		if (Input.GetKeyDown(KeyCode.RightCurlyBracket))
		{
			Stop();
		}
	}

	[Button]
	public void Play()
	{
		Stop();
		routine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			float lerpTime = Vector3.Distance(StartPos.position, EndPos.position) / WalkSpeed;
			float t = 0f;
			while (true)
			{
				Character.transform.position = Vector3.Lerp(StartPos.position, EndPos.position, t / lerpTime);
				Character.transform.rotation = StartPos.rotation;
				t += Time.deltaTime;
				if (t >= lerpTime)
				{
					break;
				}
				yield return new WaitForEndOfFrame();
			}
			Character.transform.position = EndPos.position;
		}
	}

	[Button]
	public void Stop()
	{
		if (routine != null)
		{
			StopCoroutine(routine);
			routine = null;
		}
	}
}
