using System;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs;
using ScheduleOne.Packaging;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product.Packaging;
using UnityEngine;

namespace ScheduleOne.Product;

[Serializable]
public class CocaineInstance : ProductItemInstance
{
	public CocaineInstance()
	{
	}

	public CocaineInstance(ItemDefinition definition, int quantity, EQuality quality, PackagingDefinition packaging = null)
		: base(definition, quantity, quality, packaging)
	{
	}

	public override ItemInstance GetCopy(int overrideQuantity = -1)
	{
		int quantity = Quantity;
		if (overrideQuantity != -1)
		{
			quantity = overrideQuantity;
		}
		return new CocaineInstance(base.Definition, quantity, Quality, base.AppliedPackaging);
	}

	public override void SetupPackagingVisuals(FilledPackagingVisuals visuals)
	{
		base.SetupPackagingVisuals(visuals);
		if (visuals == null)
		{
			Console.LogError("CocaineInstance: visuals is null!");
			return;
		}
		CocaineDefinition cocaineDefinition = base.Definition as CocaineDefinition;
		if (cocaineDefinition == null)
		{
			Console.LogError("CocaineInstance: definition is null! Type: " + base.Definition);
			return;
		}
		MeshRenderer[] rockMeshes = visuals.cocaineVisuals.RockMeshes;
		for (int i = 0; i < rockMeshes.Length; i++)
		{
			rockMeshes[i].material = cocaineDefinition.RockMaterial;
		}
		visuals.cocaineVisuals.Container.gameObject.SetActive(value: true);
	}

	public override ItemData GetItemData()
	{
		return new CocaineData(base.Definition.ID, Quantity, Quality.ToString(), PackagingID);
	}

	public override void ApplyEffectsToNPC(NPC npc)
	{
		npc.Avatar.EmotionManager.AddEmotionOverride("Cocaine", Name);
		npc.Avatar.Eyes.OverrideEyeballTint(new Color32(200, 240, byte.MaxValue, byte.MaxValue));
		npc.Avatar.Eyes.SetPupilDilation(1f, writeDefault: false);
		npc.Avatar.Eyes.ForceBlink();
		npc.Movement.MoveSpeedMultiplier = 1.25f;
		npc.Avatar.LookController.LookLerpSpeed = 10f;
		base.ApplyEffectsToNPC(npc);
	}

	public override void ClearEffectsFromNPC(NPC npc)
	{
		npc.Avatar.EmotionManager.RemoveEmotionOverride(Name);
		npc.Avatar.Eyes.ResetEyeballTint();
		npc.Avatar.Eyes.ResetEyeLids();
		npc.Avatar.Eyes.ResetPupilDilation();
		npc.Avatar.Eyes.ForceBlink();
		npc.Movement.MoveSpeedMultiplier = 1f;
		npc.Avatar.LookController.LookLerpSpeed = 3f;
		base.ClearEffectsFromNPC(npc);
	}

	public override void ApplyEffectsToPlayer(Player player)
	{
		player.Avatar.EmotionManager.AddEmotionOverride("Cocaine", Name);
		player.Avatar.Eyes.OverrideEyeballTint(new Color32(200, 240, byte.MaxValue, byte.MaxValue));
		player.Avatar.Eyes.SetPupilDilation(1f, writeDefault: false);
		player.Avatar.Eyes.ForceBlink();
		player.Avatar.LookController.LookLerpSpeed = 10f;
		if (player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.CocaineVisuals = true;
			PlayerSingleton<PlayerCamera>.Instance.FoVChangeSmoother.AddOverride(10f, 6, "Cocaine");
			Singleton<MusicPlayer>.Instance.SetMusicDistorted(distorted: true);
			Singleton<AudioManager>.Instance.SetDistorted(distorted: true);
		}
		base.ApplyEffectsToPlayer(player);
	}

	public override void ClearEffectsFromPlayer(Player Player)
	{
		Player.Avatar.EmotionManager.RemoveEmotionOverride(Name);
		Player.Avatar.Eyes.ResetEyeballTint();
		Player.Avatar.Eyes.ResetEyeLids();
		Player.Avatar.Eyes.ResetPupilDilation();
		Player.Avatar.Eyes.ForceBlink();
		Player.Avatar.LookController.LookLerpSpeed = 3f;
		if (Player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.CocaineVisuals = false;
			PlayerSingleton<PlayerCamera>.Instance.FoVChangeSmoother.RemoveOverride("Cocaine");
			Singleton<MusicPlayer>.Instance.SetMusicDistorted(distorted: false);
			Singleton<AudioManager>.Instance.SetDistorted(distorted: false);
		}
		base.ClearEffectsFromPlayer(Player);
	}
}
