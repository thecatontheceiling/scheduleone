using UnityEngine;

namespace ScheduleOne.AvatarFramework;

public class EyebrowController : MonoBehaviour
{
	[Header("References")]
	public Eyebrow leftBrow;

	public Eyebrow rightBrow;

	public void ApplySettings(AvatarSettings settings)
	{
		SetLeftBrowRestingHeight(settings.EyebrowRestingHeight);
		SetRightBrowRestingHeight(settings.EyebrowRestingHeight);
		leftBrow.SetScale(settings.EyebrowScale);
		rightBrow.SetScale(settings.EyebrowScale);
		leftBrow.SetThickness(settings.EyebrowThickness);
		rightBrow.SetThickness(settings.EyebrowThickness);
		leftBrow.SetRestingAngle(settings.EyebrowRestingAngle);
		rightBrow.SetRestingAngle(settings.EyebrowRestingAngle);
		leftBrow.SetColor(settings.HairColor);
		rightBrow.SetColor(settings.HairColor);
	}

	public void SetLeftBrowRestingHeight(float normalizedHeight)
	{
		leftBrow.SetRestingHeight(normalizedHeight);
	}

	public void SetRightBrowRestingHeight(float normalizedHeight)
	{
		rightBrow.SetRestingHeight(normalizedHeight);
	}
}
