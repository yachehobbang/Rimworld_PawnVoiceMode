using RimWorld;

namespace Verse.AI;

public static class Toils_Rope
{
	private const int RopeWorkDuration = 30;

	public static Toil RopePawn(TargetIndex ropeeInd)
	{
		Toil toil = ToilMaker.MakeToil("RopePawn");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			if (actor.jobs.curJob.GetTarget(ropeeInd).Thing is Pawn pawn)
			{
				toil.actor.roping.RopePawn(pawn);
				pawn.caller?.DoCall();
				PawnUtility.ForceWait(pawn, 30, actor);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = 30;
		toil.FailOnDespawnedOrNull(ropeeInd);
		toil.PlaySustainerOrSound(() => SoundDefOf.Roping);
		return toil;
	}

	public static Toil UnropeFromSpot(TargetIndex ropeeInd)
	{
		Toil toil = ToilMaker.MakeToil("UnropeFromSpot");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			if (actor.jobs.curJob.GetTarget(ropeeInd).Thing is Pawn pawn && pawn.roping.IsRopedToSpot)
			{
				pawn.roping.UnropeFromSpot();
				pawn.caller?.DoCall();
				PawnUtility.ForceWait(pawn, 30, actor);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = 30;
		toil.FailOnDespawnedOrNull(ropeeInd);
		return toil;
	}

	public static Toil GotoRopeAttachmentInteractionCell(TargetIndex ropeeIndex)
	{
		Toil toil = ToilMaker.MakeToil("GotoRopeAttachmentInteractionCell");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Pawn ropee = actor.CurJob.GetTarget(ropeeIndex).Thing as Pawn;
			IntVec3 intVec = AnimalPenUtility.RopeAttachmentInteractionCell(actor, ropee);
			if (!intVec.IsValid)
			{
				actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
			}
			if (actor.Position == intVec)
			{
				actor.jobs.curDriver.ReadyForNextToil();
			}
			else
			{
				actor.pather.StartPath(intVec, PathEndMode.OnCell);
			}
		};
		toil.tickAction = delegate
		{
			Pawn actor2 = toil.actor;
			Pawn ropee2 = actor2.CurJob.GetTarget(ropeeIndex).Thing as Pawn;
			if (actor2.pather.Moving && !AnimalPenUtility.IsGoodRopeAttachmentInteractionCell(actor2, ropee2, actor2.pather.Destination.Cell))
			{
				IntVec3 intVec2 = AnimalPenUtility.RopeAttachmentInteractionCell(actor2, ropee2);
				if (intVec2.IsValid)
				{
					actor2.pather.StartPath(intVec2, PathEndMode.OnCell);
				}
				else
				{
					actor2.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
				}
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
		toil.FailOnDespawnedOrNull(ropeeIndex);
		return toil;
	}
}
