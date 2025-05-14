using UnityEngine;

namespace ScheduleOne.AvatarFramework;

public class Eyebrow : MonoBehaviour
{
	public enum ESide
	{
		Right = 0,
		Left = 1
	}

	private const float eyebrowHeightMultiplier = 0.01f;

	[SerializeField]
	private Vector3 EyebrowDefaultScale;

	[SerializeField]
	private Vector3 EyebrowDefaultLocalPos;

	[SerializeField]
	protected ESide Side;

	[SerializeField]
	protected Transform Model;

	[SerializeField]
	protected MeshRenderer Rend;

	[Header("Eyebrow Data - Readonly")]
	[SerializeField]
	private Color col;

	[SerializeField]
	private float scale = 1f;

	[SerializeField]
	private float thickness = 1f;

	[SerializeField]
	private float restingAngle;

	public void SetScale(float _scale)
	{
		scale = _scale;
		Model.localScale = new Vector3(EyebrowDefaultScale.x, EyebrowDefaultScale.y, EyebrowDefaultScale.z * thickness) * scale;
	}

	public void SetThickness(float thickness)
	{
		this.thickness = thickness;
		SetScale(scale);
	}

	public void SetRestingAngle(float _angle)
	{
		restingAngle = _angle;
		base.transform.localRotation = Quaternion.Euler(base.transform.localEulerAngles.x, base.transform.localEulerAngles.y, restingAngle * ((Side == ESide.Left) ? (-1f) : 1f));
	}

	public void SetRestingHeight(float normalizedHeight)
	{
		normalizedHeight = Mathf.Clamp(normalizedHeight, -1.1f, 1.5f);
		Model.transform.localPosition = new Vector3(EyebrowDefaultLocalPos.x, EyebrowDefaultLocalPos.y + normalizedHeight * 0.01f, EyebrowDefaultLocalPos.z);
	}

	public void SetColor(Color _col)
	{
		col = _col;
		Rend.material.color = col;
	}
}
