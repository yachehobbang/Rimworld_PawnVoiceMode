using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class IngestionOutcomeDoer_OffsetNeed : IngestionOutcomeDoer
{
	public NeedDef need;

	public float offset;

	public ChemicalDef toleranceChemical;

	public bool perIngested;

	protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
	{
		if (pawn.needs == null)
		{
			return;
		}
		Need need = pawn.needs.TryGetNeed(this.need);
		if (need != null)
		{
			float effect = offset * (float)ingestedCount;
			AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize_NewTemp(pawn, toleranceChemical, ref effect, applyGeneToleranceFactor: false);
			if (perIngested)
			{
				effect *= (float)ingested.stackCount;
			}
			need.CurLevel += effect;
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
	{
		string text = ((offset >= 0f) ? "+" : string.Empty);
		yield return new StatDrawEntry(StatCategoryDefOf.Drug, need.LabelCap, text + offset.ToStringPercent(), need.description, need.listPriority);
	}
}
