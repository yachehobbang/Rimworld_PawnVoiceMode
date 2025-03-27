using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class IncidentWorker_MakeGameCondition : IncidentWorker
{
	public virtual GameConditionDef GetGameConditionDef(IncidentParms parms)
	{
		return def.gameCondition;
	}

	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (parms.target is Map map)
		{
			foreach (GameCondition activeCondition in map.gameConditionManager.ActiveConditions)
			{
				if (activeCondition.def.preventIncidents)
				{
					return false;
				}
			}
		}
		GameConditionManager gameConditionManager = parms.target.GameConditionManager;
		if (gameConditionManager == null)
		{
			Log.ErrorOnce($"Couldn't find condition manager for incident target {parms.target}", 70849667);
			return false;
		}
		GameConditionDef gameConditionDef = GetGameConditionDef(parms);
		if (gameConditionDef == null)
		{
			return false;
		}
		if (gameConditionManager.ConditionIsActive(gameConditionDef))
		{
			return false;
		}
		List<GameCondition> activeConditions = gameConditionManager.ActiveConditions;
		for (int i = 0; i < activeConditions.Count; i++)
		{
			if (!gameConditionDef.CanCoexistWith(activeConditions[i].def))
			{
				return false;
			}
		}
		return true;
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		GameConditionManager gameConditionManager = parms.target.GameConditionManager;
		GameConditionDef gameConditionDef = GetGameConditionDef(parms);
		int duration = Mathf.RoundToInt(def.durationDays.RandomInRange * 60000f);
		GameCondition gameCondition = GameConditionMaker.MakeCondition(gameConditionDef, duration);
		gameConditionManager.RegisterCondition(gameCondition);
		if (!def.letterLabel.NullOrEmpty() && !gameCondition.def.letterText.NullOrEmpty() && (!(parms.target is Map map) || !gameCondition.HiddenByOtherCondition(map)))
		{
			parms.letterHyperlinkThingDefs = gameCondition.def.letterHyperlinks;
			SendStandardLetter(def.letterLabel, gameCondition.LetterText, def.letterDef, parms, LookTargets.Invalid);
		}
		return true;
	}
}
