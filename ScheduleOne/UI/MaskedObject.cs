using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScheduleOne.UI;

public class MaskedObject : UIBehaviour
{
	[SerializeField]
	private CanvasRenderer canvasRendererToClip;

	public bool includeChildren;

	[SerializeField]
	private Canvas rootCanvas;

	[SerializeField]
	private RectTransform maskRectTransform;

	private bool initialized;

	private List<CanvasRenderer> canvasRenderersToClip = new List<CanvasRenderer>();

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		if (initialized)
		{
			SetTargetClippingRect();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		Initialize(rootCanvas, maskRectTransform);
	}

	protected override void Start()
	{
		canvasRenderersToClip.Add(canvasRendererToClip);
		if (includeChildren)
		{
			CanvasRenderer[] componentsInChildren = GetComponentsInChildren<CanvasRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] != canvasRendererToClip)
				{
					canvasRenderersToClip.Add(componentsInChildren[i]);
				}
			}
		}
		SetTargetClippingRect();
	}

	public void Initialize(Canvas rootCanvas, RectTransform maskRectTransform)
	{
		this.rootCanvas = rootCanvas;
		this.maskRectTransform = maskRectTransform;
		SetTargetClippingRect();
		initialized = true;
	}

	private void SetTargetClippingRect()
	{
		Rect rect = maskRectTransform.rect;
		rect.center += (Vector2)rootCanvas.transform.InverseTransformPoint(maskRectTransform.position);
		foreach (CanvasRenderer item in canvasRenderersToClip)
		{
			item.EnableRectClipping(rect);
		}
	}
}
