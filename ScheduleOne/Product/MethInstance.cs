using System;
using ScheduleOne.Audio;
using ScheduleOne.AvatarFramework;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs;
using ScheduleOne.Packaging;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product.Packaging;
using UnityEngine;

namespace ScheduleOne.Product;

[Serializable]
public class MethInstance : ProductItemInstance
{
	public MethInstance()
	{
	}

	public MethInstance(ItemDefinition definition, int quantity, EQuality quality, PackagingDefinition packaging = null)
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
		return new MethInstance(base.Definition, quantity, Quality, base.AppliedPackaging);
	}

	public override void SetupPackagingVisuals(FilledPackagingVisuals visuals)
	{
		base.SetupPackagingVisuals(visuals);
		if (visuals == null)
		{
			Console.LogError("MethInstance: visuals is null!");
			return;
		}
		MethDefinition methDefinition = base.Definition as MethDefinition;
		if (methDefinition == null)
		{
			Console.LogError("MethInstance: definition is null! Type: " + base.Definition);
			return;
		}
		MeshRenderer[] crystalMeshes = visuals.methVisuals.CrystalMeshes;
		for (int i = 0; i < crystalMeshes.Length; i++)
		{
			crystalMeshes[i].material = methDefinition.CrystalMaterial;
		}
		visuals.methVisuals.Container.gameObject.SetActive(value: true);
	}

	public override ItemData GetItemData()
	{
		return new MethData(base.Definition.ID, Quantity, Quality.ToString(), PackagingID);
	}

	public override void ApplyEffectsToNPC(NPC npc)
	{
		Console.Log("Applying meth effects to NPC: " + npc.fullName);
		npc.Avatar.EmotionManager.AddEmotionOverride("Meth", Name);
		npc.Avatar.Eyes.OverrideEyeballTint(new Color32(165, 112, 86, byte.MaxValue));
		npc.Avatar.Eyes.OverrideEyeLids(new Eye.EyeLidConfiguration
		{
			bottomLidOpen = 0.5f,
			topLidOpen = 0.1f
		});
		npc.Avatar.Eyes.SetPupilDilation(0.1f, writeDefault: false);
		npc.Avatar.Eyes.ForceBlink();
		npc.OverrideAggression(1f);
		base.ApplyEffectsToNPC(npc);
	}

	public override void ClearEffectsFromNPC(NPC npc)
	{
		npc.Avatar.EmotionManager.RemoveEmotionOverride(Name);
		npc.Avatar.Eyes.ResetEyeballTint();
		npc.Avatar.Eyes.ResetEyeLids();
		npc.Avatar.Eyes.ResetPupilDilation();
		npc.Avatar.Eyes.ForceBlink();
		npc.ResetAggression();
		base.ClearEffectsFromNPC(npc);
	}

	public override void ApplyEffectsToPlayer(Player player)
	{
		player.Avatar.EmotionManager.AddEmotionOverride("Meth", Name);
		player.Avatar.Eyes.OverrideEyeballTint(new Color32(165, 112, 86, byte.MaxValue));
		player.Avatar.Eyes.SetPupilDilation(0.1f, writeDefault: false);
		player.Avatar.Eyes.ForceBlink();
		if (player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.MethVisuals = true;
			Singleton<PostProcessingManager>.Instance.ColorFilterController.AddOverride((definition as MethDefinition).TintColor, 1, "Meth");
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
		if (Player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.MethVisuals = false;
			Singleton<PostProcessingManager>.Instance.ColorFilterController.RemoveOverride("Meth");
			Singleton<MusicPlayer>.Instance.SetMusicDistorted(distorted: false);
			Singleton<AudioManager>.Instance.SetDistorted(distorted: false);
		}
		base.ClearEffectsFromPlayer(Player);
	}
}
