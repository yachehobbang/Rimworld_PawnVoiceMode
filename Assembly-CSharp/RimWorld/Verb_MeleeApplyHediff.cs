using Verse;

namespace RimWorld;

public class Verb_MeleeApplyHediff : Verb_MeleeAttack
{
	protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
	{
		DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
		if (tool == null)
		{
			Log.ErrorOnce("Attempted to apply melee hediff without a tool", 38381735);
			return damageResult;
		}
		if (!(target.Thing is Pawn pawn))
		{
			Log.ErrorOnce("Attempted to apply melee hediff without pawn target", 78330053);
			return damageResult;
		}
		foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, verbProps.bodypartTagTarget))
		{
			damageResult.AddHediff(pawn.health.AddHediff(tool.hediff, notMissingPart, null));
			damageResult.AddPart(pawn, notMissingPart);
			damageResult.wounded = true;
		}
		return damageResult;
	}

	public override bool IsUsableOn(Thing target)
	{
		return target is Pawn;
	}
}
