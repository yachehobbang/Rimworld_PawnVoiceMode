using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimWorld;

public abstract class RaidStrategyWorker
{
	public RaidStrategyDef def;

	public float SelectionWeightForFaction(Map map, Faction faction, float basePoints)
	{
		if (faction != null && def.selectionWeightCurvesPerFaction != null)
		{
			List<FactionCurve> selectionWeightCurvesPerFaction = def.selectionWeightCurvesPerFaction;
			for (int i = 0; i < selectionWeightCurvesPerFaction.Count; i++)
			{
				if (selectionWeightCurvesPerFaction[i].faction == faction.def)
				{
					return selectionWeightCurvesPerFaction[i].Evaluate(basePoints);
				}
			}
		}
		return SelectionWeight(map, basePoints);
	}

	public virtual float SelectionWeight(Map map, float basePoints)
	{
		return def.selectionWeightPerPointsCurve.Evaluate(basePoints);
	}

	protected abstract LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed);

	public virtual void MakeLords(IncidentParms parms, List<Pawn> pawns)
	{
		Map map = (Map)parms.target;
		List<List<Pawn>> list = IncidentParmsUtility.SplitIntoGroups(pawns, parms.pawnGroups);
		int @int = Rand.Int;
		for (int i = 0; i < list.Count; i++)
		{
			List<Pawn> list2 = list[i];
			Lord lord = LordMaker.MakeNewLord(parms.faction, MakeLordJob(parms, map, list2, @int), map, list2);
			lord.inSignalLeave = parms.inSignalEnd;
			QuestUtility.AddQuestTag(lord, parms.questTag);
			if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
			{
				Log.Message("Market value threshold to start stealing (raiders=" + lord.ownedPawns.Count + "): " + StealAIUtility.StartStealingMarketValueThreshold(lord) + " (colony wealth=" + map.wealthWatcher.WealthTotal + ")");
			}
		}
	}

	public virtual bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
	{
		if (parms.faction != null && parms.faction.def.disallowedRaidStrategies.NotNullAndContains(def))
		{
			return false;
		}
		if (SelectionWeightForFaction(parms.target as Map, parms.faction, parms.points) <= 0f)
		{
			return false;
		}
		if (parms.points < MinimumPoints(parms.faction, groupKind))
		{
			return false;
		}
		return true;
	}

	public virtual float MinimumPoints(Faction faction, PawnGroupKindDef groupKind)
	{
		return faction.def.MinPointsToGeneratePawnGroup(groupKind);
	}

	public virtual float MinMaxAllowedPawnGenOptionCost(Faction faction, PawnGroupKindDef groupKind)
	{
		return 0f;
	}

	public virtual bool CanUsePawnGenOption(float pointsTotal, PawnGenOption g, List<PawnGenOptionWithXenotype> chosenGroups, Faction faction = null)
	{
		if (faction != null && faction.def.humanlikeFaction && g.kind.RaceProps.Animal && chosenGroups != null && !chosenGroups.Any((PawnGenOptionWithXenotype x) => x.Option.kind.RaceProps.Humanlike))
		{
			return false;
		}
		return true;
	}

	public virtual bool CanUsePawn(float pointsTotal, Pawn p, List<Pawn> otherPawns)
	{
		return true;
	}

	public virtual void TryGenerateThreats(IncidentParms parms)
	{
	}

	public virtual List<Pawn> SpawnThreats(IncidentParms parms)
	{
		if (parms.pawnKind != null)
		{
			List<Pawn> list = new List<Pawn>();
			for (int i = 0; i < parms.pawnCount; i++)
			{
				PawnGenerationRequest request = new PawnGenerationRequest(parms.pawnKind, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: def.pawnsCanBringFood, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, extraPawnForExtraRelationChance: null, relationWithExtraPawnChanceFactor: 1f, validatorPreGear: null, validatorPostGear: null, forcedTraits: null, prohibitedTraits: null, minChanceToRedressWorldPawn: null, fixedBiologicalAge: null, fixedChronologicalAge: null, fixedGender: null, fixedLastName: null, fixedBirthName: null, fixedTitle: null, fixedIdeo: null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, forcedXenogenes: null, forcedEndogenes: null, forcedXenotype: null, forcedCustomXenotype: null, allowedXenotypes: null, forceBaselinerChance: 0f, developmentalStages: DevelopmentalStage.Adult, pawnKindDefGetter: null, excludeBiologicalAgeRange: null, biologicalAgeRange: null);
				request.BiocodeApparelChance = 1f;
				Pawn pawn = PawnGenerator.GeneratePawn(request);
				if (pawn != null)
				{
					list.Add(pawn);
				}
			}
			if (list.Any())
			{
				parms.raidArrivalMode.Worker.Arrive(list, parms);
				return list;
			}
		}
		return null;
	}
}
