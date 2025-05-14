using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.UI.Management;
using UnityEngine;

namespace ScheduleOne.Management;

public class TransitRoute
{
	protected TransitLineVisuals visuals;

	public Action<ITransitEntity> onSourceChange;

	public Action<ITransitEntity> onDestinationChange;

	public ITransitEntity Source { get; protected set; }

	public ITransitEntity Destination { get; protected set; }

	public TransitRoute(ITransitEntity source, ITransitEntity destination)
	{
		Source = source;
		Destination = destination;
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		instance.onFixedUpdate = (Action)Delegate.Combine(instance.onFixedUpdate, new Action(Update));
	}

	public void Destroy()
	{
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		instance.onFixedUpdate = (Action)Delegate.Remove(instance.onFixedUpdate, new Action(Update));
		if (visuals != null)
		{
			UnityEngine.Object.Destroy(visuals.gameObject);
		}
	}

	public void SetVisualsActive(bool active)
	{
		if (visuals == null)
		{
			visuals = UnityEngine.Object.Instantiate(Singleton<ManagementWorldspaceCanvas>.Instance.TransitRouteVisualsPrefab.gameObject, GameObject.Find("_Temp").transform).GetComponent<TransitLineVisuals>();
		}
		visuals.gameObject.SetActive(active);
		if (active)
		{
			Update();
		}
	}

	private void Update()
	{
		ValidateEntities();
		if (!(visuals == null) && visuals.gameObject.activeSelf)
		{
			if (Source == null || Destination == null)
			{
				visuals.gameObject.SetActive(value: false);
				return;
			}
			Vector3.Distance(Source.LinkOrigin.position, Destination.LinkOrigin.position);
			visuals.SetSourcePosition(Source.LinkOrigin.position);
			visuals.SetDestinationPosition(Destination.LinkOrigin.position);
		}
	}

	public virtual void SetSource(ITransitEntity source)
	{
		Source = source;
		if (onSourceChange != null)
		{
			onSourceChange(Source);
		}
	}

	public bool AreEntitiesNonNull()
	{
		ValidateEntities();
		if (Source != null)
		{
			return Destination != null;
		}
		return false;
	}

	public virtual void SetDestination(ITransitEntity destination)
	{
		Destination = destination;
		if (onDestinationChange != null)
		{
			onDestinationChange(Destination);
		}
	}

	private void ValidateEntities()
	{
		if (Source != null && Source.IsDestroyed)
		{
			SetSource(null);
		}
		if (Destination != null && Destination.IsDestroyed)
		{
			SetDestination(null);
		}
	}
}
