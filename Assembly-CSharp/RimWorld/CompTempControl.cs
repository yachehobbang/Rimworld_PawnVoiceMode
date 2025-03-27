using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class CompTempControl : ThingComp
{
	[Unsaved(false)]
	public bool operatingAtHighPower;

	public float targetTemperature = -99999f;

	protected const float DefaultTargetTemperature = 21f;

	public virtual float TargetTemperature
	{
		get
		{
			return targetTemperature;
		}
		set
		{
			targetTemperature = value;
		}
	}

	public CompProperties_TempControl Props => (CompProperties_TempControl)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (TargetTemperature < -2000f)
		{
			TargetTemperature = Props.defaultTargetTemperature;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref targetTemperature, "targetTemperature", 0f);
	}

	protected float RoundedToCurrentTempModeOffset_NewTemp(float celsiusTemp)
	{
		return GenTemperature.ConvertTemperatureOffset(Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode)), Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
	}

	private float RoundedToCurrentTempModeOffset(float celsiusTemp)
	{
		return RoundedToCurrentTempModeOffset_NewTemp(celsiusTemp);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		float offset = RoundedToCurrentTempModeOffset_NewTemp(-10f);
		Command_Action command_Action = new Command_Action();
		command_Action.action = delegate
		{
			InterfaceChangeTargetTemperature_NewTemp(offset);
		};
		command_Action.defaultLabel = offset.ToStringTemperatureOffset("F0");
		command_Action.defaultDesc = "CommandLowerTempDesc".Translate();
		command_Action.hotKey = KeyBindingDefOf.Misc5;
		command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
		yield return command_Action;
		float offset2 = RoundedToCurrentTempModeOffset_NewTemp(-1f);
		Command_Action command_Action2 = new Command_Action();
		command_Action2.action = delegate
		{
			InterfaceChangeTargetTemperature_NewTemp(offset2);
		};
		command_Action2.defaultLabel = offset2.ToStringTemperatureOffset("F0");
		command_Action2.defaultDesc = "CommandLowerTempDesc".Translate();
		command_Action2.hotKey = KeyBindingDefOf.Misc4;
		command_Action2.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
		yield return command_Action2;
		Command_Action command_Action3 = new Command_Action();
		command_Action3.action = delegate
		{
			TargetTemperature = 21f;
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
			ThrowCurrentTemperatureText_NewTemp();
		};
		command_Action3.defaultLabel = "CommandResetTemp".Translate();
		command_Action3.defaultDesc = "CommandResetTempDesc".Translate();
		command_Action3.hotKey = KeyBindingDefOf.Misc1;
		command_Action3.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset");
		yield return command_Action3;
		float offset3 = RoundedToCurrentTempModeOffset_NewTemp(1f);
		Command_Action command_Action4 = new Command_Action();
		command_Action4.action = delegate
		{
			InterfaceChangeTargetTemperature_NewTemp(offset3);
		};
		command_Action4.defaultLabel = "+" + offset3.ToStringTemperatureOffset("F0");
		command_Action4.defaultDesc = "CommandRaiseTempDesc".Translate();
		command_Action4.hotKey = KeyBindingDefOf.Misc2;
		command_Action4.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
		yield return command_Action4;
		float offset4 = RoundedToCurrentTempModeOffset_NewTemp(10f);
		Command_Action command_Action5 = new Command_Action();
		command_Action5.action = delegate
		{
			InterfaceChangeTargetTemperature_NewTemp(offset4);
		};
		command_Action5.defaultLabel = "+" + offset4.ToStringTemperatureOffset("F0");
		command_Action5.defaultDesc = "CommandRaiseTempDesc".Translate();
		command_Action5.hotKey = KeyBindingDefOf.Misc3;
		command_Action5.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
		yield return command_Action5;
	}

	protected void InterfaceChangeTargetTemperature_NewTemp(float offset)
	{
		SoundDefOf.DragSlider.PlayOneShotOnCamera();
		TargetTemperature += offset;
		TargetTemperature = Mathf.Clamp(TargetTemperature, -273.15f, 1000f);
		ThrowCurrentTemperatureText_NewTemp();
	}

	private void InterfaceChangeTargetTemperature(float offset)
	{
		InterfaceChangeTargetTemperature_NewTemp(offset);
	}

	protected void ThrowCurrentTemperatureText_NewTemp()
	{
		MoteMaker.ThrowText(parent.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), parent.Map, TargetTemperature.ToStringTemperature("F0"), Color.white);
	}

	private void ThrowCurrentTemperatureText()
	{
		ThrowCurrentTemperatureText_NewTemp();
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append((Props.inspectString ?? ((string)"TargetTemperature".Translate())) + ": ");
		stringBuilder.AppendLine(TargetTemperature.ToStringTemperature("F0"));
		stringBuilder.Append("PowerConsumptionMode".Translate() + ": ");
		if (operatingAtHighPower)
		{
			stringBuilder.Append("PowerConsumptionHigh".Translate().CapitalizeFirst());
		}
		else
		{
			stringBuilder.Append("PowerConsumptionLow".Translate().CapitalizeFirst());
		}
		return stringBuilder.ToString();
	}
}
