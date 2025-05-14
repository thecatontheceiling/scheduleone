using System;
using System.Collections;
using System.Collections.Generic;
using ScheduleOne.Audio;
using ScheduleOne.Combat;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs;
using ScheduleOne.Noise;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Storage;
using ScheduleOne.Trash;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Equipping;

public class Equippable_RangedWeapon : Equippable_AvatarViewmodel
{
	public const float NPC_AIM_DETECTION_RANGE = 10f;

	public int MagazineSize = 7;

	[Header("Aim Settings")]
	public float AimDuration = 0.2f;

	public float AimFOVReduction = 10f;

	public float FOVChangeDuration = 0.3f;

	[Header("Firing")]
	public AudioSourceController FireSound;

	public AudioSourceController EmptySound;

	public float FireCooldown = 0.3f;

	public string[] FireAnimTriggers;

	public float AccuracyChangeDuration = 0.6f;

	[Header("Raycasting")]
	public float Range = 40f;

	public float RayRadius = 0.05f;

	[Header("Spread")]
	public float MinSpread = 5f;

	public float MaxSpread = 15f;

	[Header("Damage")]
	public float Damage = 60f;

	public float ImpactForce = 300f;

	[Header("Reloading")]
	public bool CanReload = true;

	public bool IncrementalReload;

	public StorableItemDefinition Magazine;

	public float ReloadStartTime = 1.5f;

	public float ReloadIndividalTime;

	public float ReloadEndTime;

	public string ReloadStartAnimTrigger = "MagazineReload";

	public string ReloadIndividualAnimTrigger = string.Empty;

	public string ReloadEndAnimTrigger = string.Empty;

	public TrashItem ReloadTrash;

	[Header("Cocking")]
	public bool MustBeCocked;

	public float CockTime = 0.5f;

	public string CockAnimTrigger = "MagazineReload";

	[Header("Effects")]
	public float TracerSpeed = 50f;

	public UnityEvent onFire;

	public UnityEvent onReloadStart;

	public UnityEvent onReloadIndividual;

	public UnityEvent onReloadEnd;

	public UnityEvent onCockStart;

	protected IntegerItemInstance weaponItem;

	private bool fovOverridden;

	private float aimVelocity;

	private Coroutine reloadRoutine;

	private bool shotQueued;

	private bool reloadQueued;

	private float timeSincePrimaryClick = 100f;

	public float Aim { get; private set; }

	public float Accuracy { get; private set; }

	public float TimeSinceFire { get; set; } = 1000f;

	public bool IsReloading { get; private set; }

	public bool IsCocked { get; private set; }

	public bool IsCocking { get; private set; }

	public int Ammo
	{
		get
		{
			if (weaponItem == null)
			{
				return 0;
			}
			return weaponItem.Value;
		}
	}

	private float aimFov => Singleton<Settings>.Instance.CameraFOV - AimFOVReduction;

	public override void Equip(ItemInstance item)
	{
		base.Equip(item);
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: false);
		Singleton<InputPromptsCanvas>.Instance.LoadModule("gun");
		weaponItem = item as IntegerItemInstance;
		InvokeRepeating("CheckAimingAtNPC", 0f, 0.5f);
	}

	public override void Unequip()
	{
		base.Unequip();
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: true);
		Singleton<InputPromptsCanvas>.Instance.UnloadModule();
		if (fovOverridden)
		{
			PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(FOVChangeDuration);
			PlayerSingleton<PlayerMovement>.Instance.RemoveSprintBlocker("Aiming");
			fovOverridden = false;
		}
		if (reloadRoutine != null)
		{
			StopCoroutine(reloadRoutine);
		}
	}

	protected override void Update()
	{
		base.Update();
		UpdateInput();
		UpdateAnim();
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: false);
		TimeSinceFire += Time.deltaTime;
	}

	private void UpdateInput()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		if ((GameInput.GetButton(GameInput.ButtonCode.SecondaryClick) || timeSincePrimaryClick < 0.5f || IsCocking) && CanAim())
		{
			Aim = Mathf.SmoothDamp(Aim, 1f, ref aimVelocity, AimDuration);
			Accuracy = Mathf.MoveTowards(Accuracy, 1f, Time.deltaTime / AccuracyChangeDuration);
			if (!fovOverridden)
			{
				PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(aimFov, FOVChangeDuration);
				PlayerSingleton<PlayerMovement>.Instance.AddSprintBlocker("Aiming");
				fovOverridden = true;
				Player.Local.SendEquippableMessage_Networked("Raise", UnityEngine.Random.Range(int.MinValue, int.MaxValue));
			}
		}
		else
		{
			if (TimeSinceFire > FireCooldown)
			{
				Aim = Mathf.SmoothDamp(Aim, 0f, ref aimVelocity, AimDuration);
			}
			Accuracy = Mathf.MoveTowards(Accuracy, 0f, Time.deltaTime / AccuracyChangeDuration * 2f);
			if (fovOverridden)
			{
				PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(FOVChangeDuration);
				PlayerSingleton<PlayerMovement>.Instance.RemoveSprintBlocker("Aiming");
				fovOverridden = false;
				Player.Local.SendEquippableMessage_Networked("Lower", UnityEngine.Random.Range(int.MinValue, int.MaxValue));
			}
		}
		float t = Mathf.Clamp01(PlayerSingleton<PlayerMovement>.Instance.Controller.velocity.magnitude / PlayerMovement.WalkSpeed);
		float num = Mathf.Lerp(1f, 0f, t);
		if (Accuracy > num)
		{
			Accuracy = Mathf.MoveTowards(Accuracy, num, Time.deltaTime / AccuracyChangeDuration * 2f);
		}
		if (Singleton<PauseMenu>.Instance.IsPaused)
		{
			return;
		}
		if (GameInput.GetButton(GameInput.ButtonCode.PrimaryClick))
		{
			timeSincePrimaryClick = 0f;
		}
		else
		{
			timeSincePrimaryClick += Time.deltaTime;
		}
		if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) || shotQueued)
		{
			if (CanFire(checkAmmo: false))
			{
				if (Ammo > 0)
				{
					if (!MustBeCocked || IsCocked)
					{
						Fire();
					}
					else
					{
						Cock();
					}
				}
				else if (EmptySound != null)
				{
					EmptySound.Play();
					shotQueued = false;
					if (IsReloadReady(ignoreTiming: false))
					{
						Reload();
					}
				}
			}
			else if (TimeSinceFire < FireCooldown || IsCocking)
			{
				shotQueued = true;
			}
		}
		if (reloadQueued || GameInput.GetButtonDown(GameInput.ButtonCode.Reload))
		{
			if (IsReloadReady(ignoreTiming: false))
			{
				Reload();
			}
			else if (GameInput.GetButtonDown(GameInput.ButtonCode.Reload) && IsReloadReady(ignoreTiming: true) && TimeSinceFire > FireCooldown * 0.5f)
			{
				Console.Log("Reload qeueued");
				reloadQueued = true;
			}
		}
	}

	private void UpdateAnim()
	{
		Singleton<ViewmodelAvatar>.Instance.Animator.SetFloat("Aim", Aim);
	}

	private bool CanAim()
	{
		return true;
	}

	public virtual void Fire()
	{
		IsCocked = false;
		shotQueued = false;
		TimeSinceFire = 0f;
		Vector3 data = PlayerSingleton<PlayerCamera>.Instance.transform.position + PlayerSingleton<PlayerCamera>.Instance.transform.forward * 50f;
		Player.Local.SendEquippableMessage_Networked_Vector("Shoot", UnityEngine.Random.Range(int.MinValue, int.MaxValue), data);
		Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(FireAnimTriggers[UnityEngine.Random.Range(0, FireAnimTriggers.Length)]);
		PlayerSingleton<PlayerCamera>.Instance.JoltCamera();
		FireSound.Play();
		weaponItem.ChangeValue(-1);
		float spread = GetSpread();
		Vector3 forward = PlayerSingleton<PlayerCamera>.Instance.transform.forward;
		forward = Quaternion.Euler(UnityEngine.Random.insideUnitCircle * spread) * forward;
		Vector3 position = PlayerSingleton<PlayerCamera>.Instance.transform.position;
		position += PlayerSingleton<PlayerCamera>.Instance.transform.forward * 0.4f;
		position += PlayerSingleton<PlayerCamera>.Instance.transform.right * 0.1f;
		position += PlayerSingleton<PlayerCamera>.Instance.transform.up * -0.03f;
		Singleton<FXManager>.Instance.CreateBulletTrail(position, forward, TracerSpeed, Range, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask);
		NoiseUtility.EmitNoise(base.transform.position, ENoiseType.Gunshot, 25f, Player.Local.gameObject);
		if (Player.Local.CurrentProperty == null)
		{
			Player.Local.VisualState.ApplyState("shooting", PlayerVisualState.EVisualState.DischargingWeapon, 4f);
		}
		RaycastHit[] array = Physics.SphereCastAll(position, RayRadius, forward, Range, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask);
		Array.Sort(array, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));
		RaycastHit[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			RaycastHit hit = array2[num];
			IDamageable componentInParent = hit.collider.GetComponentInParent<IDamageable>();
			if (componentInParent == null || componentInParent != Player.Local)
			{
				if (componentInParent != null)
				{
					Impact impact = new Impact(hit, hit.point, PlayerSingleton<PlayerCamera>.Instance.transform.forward, ImpactForce, Damage, EImpactType.Bullet, Player.Local, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
					componentInParent.SendImpact(impact);
					Singleton<FXManager>.Instance.CreateImpactFX(impact);
				}
				break;
			}
		}
		Accuracy = 0f;
		if (onFire != null)
		{
			onFire.Invoke();
		}
	}

	public virtual void Reload()
	{
		reloadQueued = false;
		IsReloading = true;
		Console.Log("Reloading...");
		reloadRoutine = StartCoroutine(ReloadRoutine());
		IEnumerator ReloadRoutine()
		{
			if (onReloadStart != null)
			{
				onReloadStart.Invoke();
			}
			Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(ReloadStartAnimTrigger);
			yield return new WaitForSeconds(ReloadStartTime);
			StorableItemInstance mag2;
			if (IncrementalReload)
			{
				StorableItemInstance mag;
				while (weaponItem.Value < MagazineSize && GetMagazine(out mag))
				{
					if (onReloadIndividual != null)
					{
						onReloadIndividual.Invoke();
					}
					Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(ReloadIndividualAnimTrigger);
					yield return new WaitForSeconds(ReloadIndividalTime);
					weaponItem.ChangeValue(1);
					IntegerItemInstance obj = mag as IntegerItemInstance;
					obj.ChangeValue(-1);
					NotifyIncrementalReload();
					if (obj.Value <= 0)
					{
						mag.ChangeQuantity(-1);
						if (ReloadTrash != null)
						{
							Vector3 posiiton = PlayerSingleton<PlayerCamera>.Instance.transform.position - PlayerSingleton<PlayerCamera>.Instance.transform.up * 0.4f;
							NetworkSingleton<TrashManager>.Instance.CreateTrashItem(ReloadTrash.ID, posiiton, UnityEngine.Random.rotation);
						}
					}
				}
				yield return new WaitForSeconds(0.05f);
				if (onReloadEnd != null)
				{
					onReloadEnd.Invoke();
				}
				Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(ReloadEndAnimTrigger);
				yield return new WaitForSeconds(ReloadEndTime);
			}
			else if (GetMagazine(out mag2))
			{
				IntegerItemInstance obj2 = mag2 as IntegerItemInstance;
				obj2.ChangeValue(-(MagazineSize - weaponItem.Value));
				if (obj2.Value <= 0)
				{
					mag2.ChangeQuantity(-1);
					if (ReloadTrash != null)
					{
						Vector3 posiiton2 = PlayerSingleton<PlayerCamera>.Instance.transform.position - PlayerSingleton<PlayerCamera>.Instance.transform.up * 0.4f;
						NetworkSingleton<TrashManager>.Instance.CreateTrashItem(ReloadTrash.ID, posiiton2, UnityEngine.Random.rotation);
					}
				}
				weaponItem.SetValue(MagazineSize);
			}
			Console.Log("Reloading done!");
			IsReloading = false;
			reloadRoutine = null;
		}
	}

	protected virtual void NotifyIncrementalReload()
	{
	}

	private bool IsReloadReady(bool ignoreTiming)
	{
		if (!CanReload)
		{
			return false;
		}
		if (IsReloading)
		{
			return false;
		}
		if (!GetMagazine(out var _))
		{
			return false;
		}
		if (weaponItem.Value >= MagazineSize)
		{
			return false;
		}
		if (TimeSinceFire < FireCooldown && !ignoreTiming)
		{
			return false;
		}
		if (!base.equipAnimDone && !ignoreTiming)
		{
			return false;
		}
		if (IsCocking)
		{
			return false;
		}
		return true;
	}

	protected virtual bool GetMagazine(out StorableItemInstance mag)
	{
		mag = null;
		for (int i = 0; i < PlayerSingleton<PlayerInventory>.Instance.hotbarSlots.Count; i++)
		{
			if (PlayerSingleton<PlayerInventory>.Instance.hotbarSlots[i].Quantity != 0 && PlayerSingleton<PlayerInventory>.Instance.hotbarSlots[i].ItemInstance.ID == Magazine.ID)
			{
				mag = PlayerSingleton<PlayerInventory>.Instance.hotbarSlots[i].ItemInstance as StorableItemInstance;
				return true;
			}
		}
		return false;
	}

	private bool CanFire(bool checkAmmo = true)
	{
		if (TimeSinceFire < FireCooldown)
		{
			return false;
		}
		if (Aim < 0.1f)
		{
			return false;
		}
		if (!base.equipAnimDone)
		{
			return false;
		}
		if (checkAmmo && Ammo <= 0)
		{
			return false;
		}
		if (IsReloading)
		{
			return false;
		}
		if (IsCocking)
		{
			return false;
		}
		return true;
	}

	private bool CanCock()
	{
		if (IsCocked)
		{
			return false;
		}
		if (IsCocking)
		{
			return false;
		}
		if (weaponItem.Value <= 0)
		{
			return false;
		}
		if (!base.equipAnimDone)
		{
			return false;
		}
		if (IsReloading)
		{
			return false;
		}
		if (TimeSinceFire < FireCooldown)
		{
			return false;
		}
		return true;
	}

	private void Cock()
	{
		Console.Log("Cocking");
		shotQueued = false;
		IsCocking = true;
		StartCoroutine(CockRoutine());
		IEnumerator CockRoutine()
		{
			if (onCockStart != null)
			{
				onCockStart.Invoke();
			}
			Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(CockAnimTrigger);
			yield return new WaitForSeconds(CockTime);
			IsCocked = true;
			IsCocking = false;
		}
	}

	private float GetSpread()
	{
		return Mathf.Lerp(MaxSpread, MinSpread, Accuracy);
	}

	private void CheckAimingAtNPC()
	{
		if (Aim < 0.5f)
		{
			return;
		}
		RaycastHit[] array = Physics.SphereCastAll(new Ray(PlayerSingleton<PlayerCamera>.Instance.transform.position, PlayerSingleton<PlayerCamera>.Instance.transform.forward), 0.5f, 10f, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask);
		List<NPC> list = new List<NPC>();
		RaycastHit[] array2 = array;
		foreach (RaycastHit raycastHit in array2)
		{
			NPC componentInParent = raycastHit.collider.GetComponentInParent<NPC>();
			if (componentInParent != null && !list.Contains(componentInParent))
			{
				list.Add(componentInParent);
				if (componentInParent.awareness.VisionCone.IsPlayerVisible(Player.Local))
				{
					componentInParent.responses.RespondToAimedAt(Player.Local);
				}
			}
		}
	}
}
