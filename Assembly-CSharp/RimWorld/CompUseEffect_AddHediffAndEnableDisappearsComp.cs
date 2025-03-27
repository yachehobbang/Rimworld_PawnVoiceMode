using Verse;

namespace RimWorld;

public class CompUseEffect_AddHediffAndEnableDisappearsComp : CompUseEffect_AddHediff
{
	public override void DoEffect(Pawn user)
	{
		user.health.AddHediff(base.Props.hediffDef, null, null).TryGetComp<HediffComp_DisappearsDisableable>().disabled = false;
	}
}
