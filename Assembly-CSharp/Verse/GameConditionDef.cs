using System;
using System.Collections.Generic;
using RimWorld;

namespace Verse;

public class GameConditionDef : Def
{
	public Type conditionClass = typeof(GameCondition);

	private List<GameConditionDef> exclusiveConditions;

	[MustTranslate]
	public string startMessage;

	[MustTranslate]
	public string endMessage;

	[MustTranslate]
	public string letterText;

	public List<ThingDef> letterHyperlinks;

	public LetterDef letterDef;

	public bool canBePermanent;

	public bool allowUnderground = true;

	[MustTranslate]
	public string descriptionFuture;

	[NoTranslate]
	public string jumpToSourceKey = "ClickToJumpToSource";

	public List<GameConditionDef> silencedByConditions;

	public bool natural = true;

	public bool preventNeutralVisitors;

	public bool preventIncidents;

	public bool displayOnUI = true;

	public bool pennedAnimalsSeekShelter;

	public PsychicDroneLevel defaultDroneLevel = PsychicDroneLevel.BadMedium;

	public bool preventRain;

	public WeatherDef weatherDef;

	public float temperatureOffset = -10f;

	public float minNearbyPollution;

	public SimpleCurve mtbOverNearbyPollutionCurve;

	public bool CanCoexistWith(GameConditionDef other)
	{
		if (this == other)
		{
			return false;
		}
		if (ModsConfig.AnomalyActive && this != GameConditionDefOf.UnnaturalDarkness && other == GameConditionDefOf.UnnaturalDarkness)
		{
			return false;
		}
		if (exclusiveConditions != null)
		{
			return !exclusiveConditions.Contains(other);
		}
		return true;
	}

	public static GameConditionDef Named(string defName)
	{
		return DefDatabase<GameConditionDef>.GetNamed(defName);
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (string item in base.ConfigErrors())
		{
			yield return item;
		}
		if (conditionClass == null)
		{
			yield return "conditionClass is null";
		}
	}
}
