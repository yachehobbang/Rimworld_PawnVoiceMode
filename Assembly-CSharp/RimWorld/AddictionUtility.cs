using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public static class AddictionUtility
{
	public static bool HasChemicalDependency(Pawn pawn, Thing drug)
	{
		if (!ModsConfig.BiotechActive)
		{
			return false;
		}
		ChemicalDef chemicalDef = drug.TryGetComp<CompDrug>()?.Props?.chemical;
		if (chemicalDef == null)
		{
			return false;
		}
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (hediff is Hediff_ChemicalDependency hediff_ChemicalDependency && hediff_ChemicalDependency.chemical == chemicalDef)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsAddicted(Pawn pawn, Thing drug)
	{
		return FindAddictionHediff(pawn, drug) != null;
	}

	public static bool IsAddicted(Pawn pawn, ChemicalDef chemical)
	{
		return FindAddictionHediff(pawn, chemical) != null;
	}

	public static Hediff_Addiction FindAddictionHediff(Pawn pawn, Thing drug)
	{
		if (!drug.def.IsDrug)
		{
			return null;
		}
		CompDrug compDrug = drug.TryGetComp<CompDrug>();
		if (!compDrug.Props.Addictive)
		{
			return null;
		}
		return FindAddictionHediff(pawn, compDrug.Props.chemical);
	}

	public static Hediff_Addiction FindAddictionHediff(Pawn pawn, ChemicalDef chemical)
	{
		return (Hediff_Addiction)pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == chemical.addictionHediff);
	}

	public static Hediff FindToleranceHediff(Pawn pawn, ChemicalDef chemical)
	{
		if (chemical.toleranceHediff == null)
		{
			return null;
		}
		return pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == chemical.toleranceHediff);
	}

	[Obsolete]
	public static void ModifyChemicalEffectForToleranceAndBodySize(Pawn pawn, ChemicalDef chemicalDef, ref float effect, bool applyGeneToleranceFactor)
	{
		ModifyChemicalEffectForToleranceAndBodySize_NewTemp(pawn, chemicalDef, ref effect, applyGeneToleranceFactor);
	}

	public static void ModifyChemicalEffectForToleranceAndBodySize_NewTemp(Pawn pawn, ChemicalDef chemicalDef, ref float effect, bool applyGeneToleranceFactor, bool divideByBodySize = true)
	{
		if (chemicalDef != null)
		{
			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			for (int i = 0; i < hediffs.Count; i++)
			{
				hediffs[i].ModifyChemicalEffect(chemicalDef, ref effect);
			}
		}
		if (applyGeneToleranceFactor && ModsConfig.BiotechActive && pawn.genes != null)
		{
			foreach (Gene item in pawn.genes.GenesListForReading)
			{
				if (item.Active && item.def.chemical == chemicalDef)
				{
					effect *= item.def.toleranceBuildupFactor;
				}
			}
		}
		if (divideByBodySize)
		{
			effect /= pawn.BodySize;
		}
	}

	public static void CheckDrugAddictionTeachOpportunity(Pawn pawn)
	{
		if (pawn.RaceProps.IsFlesh && pawn.Spawned && (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer) && AddictedToAnything(pawn))
		{
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.DrugAddiction, pawn, OpportunityType.Important);
		}
	}

	public static bool AddictedToAnything(Pawn pawn)
	{
		List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			if (hediffs[i] is Hediff_Addiction)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CanBingeOnNow(Pawn pawn, ChemicalDef chemical, DrugCategory drugCategory)
	{
		if (!chemical.canBinge)
		{
			return false;
		}
		if (!pawn.Spawned)
		{
			return false;
		}
		List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Drug);
		for (int i = 0; i < list.Count; i++)
		{
			if (!list[i].Position.Fogged(list[i].Map) && (drugCategory == DrugCategory.Any || list[i].def.ingestible.drugCategory == drugCategory) && list[i].TryGetComp<CompDrug>().Props.chemical == chemical && (list[i].Position.Roofed(list[i].Map) || list[i].Position.InHorDistOf(pawn.Position, 45f)) && pawn.CanReach(list[i], PathEndMode.ClosestTouch, Danger.Deadly))
			{
				return true;
			}
		}
		return false;
	}
}
