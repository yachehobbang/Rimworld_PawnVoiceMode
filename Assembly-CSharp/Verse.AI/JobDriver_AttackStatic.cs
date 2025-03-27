using System.Collections.Generic;
using RimWorld;

namespace Verse.AI;

public class JobDriver_AttackStatic : JobDriver
{
	private const int AutotargetRadius = 4;

	private bool startedIncapacitated;

	private int numAttacksMade;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated", defaultValue: false);
		Scribe_Values.Look(ref numAttacksMade, "numAttacksMade", 0);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFinishAction(delegate
		{
			if (pawn.IsPlayerControlled && pawn.Drafted && !base.job.playerInterruptedForced)
			{
				Thing targetThingA = base.TargetThingA;
				if (targetThingA != null && targetThingA.def.autoTargetNearbyIdenticalThings)
				{
					Verb verb = pawn.TryGetAttackVerb(base.TargetA.Thing, !pawn.IsColonist);
					foreach (IntVec3 item in GenRadial.RadialCellsAround(base.TargetThingA.Position, 4f, useCenter: false).InRandomOrder())
					{
						if (item.InBounds(base.Map))
						{
							foreach (Thing thing in item.GetThingList(base.Map))
							{
								if (thing.def == base.TargetThingA.def && verb != null && verb.CanHitTargetFrom(pawn.Position, thing) && pawn.jobs.jobQueue.Count == 0)
								{
									Job job = base.job.Clone();
									job.targetA = thing;
									job.endIfCantShootTargetFromCurPos = true;
									pawn.jobs.jobQueue.EnqueueFirst(job, null);
									return;
								}
							}
						}
					}
				}
			}
		});
		yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
		Toil init = ToilMaker.MakeToil("MakeNewToils");
		init.initAction = delegate
		{
			if (base.TargetThingA is Pawn pawn)
			{
				startedIncapacitated = pawn.Downed;
			}
			base.pawn.pather.StopDead();
		};
		init.tickAction = delegate
		{
			if (!base.TargetA.IsValid)
			{
				EndJobWith(JobCondition.Succeeded);
			}
			else
			{
				if (base.TargetA.HasThing)
				{
					Pawn pawn2 = base.TargetA.Thing as Pawn;
					if (base.TargetA.Thing.Destroyed || (pawn2 != null && !startedIncapacitated && pawn2.Downed) || (pawn2 != null && pawn2.IsPsychologicallyInvisible()))
					{
						EndJobWith(JobCondition.Succeeded);
						return;
					}
				}
				if (numAttacksMade >= job.maxNumStaticAttacks && !pawn.stances.FullBodyBusy)
				{
					EndJobWith(JobCondition.Succeeded);
				}
				else if (pawn.TryStartAttack(base.TargetA))
				{
					numAttacksMade++;
				}
				else if (!pawn.stances.FullBodyBusy)
				{
					Verb verb2 = pawn.TryGetAttackVerb(base.TargetA.Thing, !pawn.IsColonist);
					if (job.endIfCantShootTargetFromCurPos && (verb2 == null || !verb2.CanHitTargetFrom(pawn.Position, base.TargetA)))
					{
						EndJobWith(JobCondition.Incompletable);
					}
					else if (job.endIfCantShootInMelee)
					{
						if (verb2 == null)
						{
							EndJobWith(JobCondition.Incompletable);
						}
						else
						{
							float num = verb2.verbProps.EffectiveMinRange(base.TargetA, pawn);
							if ((float)pawn.Position.DistanceToSquared(base.TargetA.Cell) < num * num && pawn.Position.AdjacentTo8WayOrInside(base.TargetA.Cell))
							{
								EndJobWith(JobCondition.Incompletable);
							}
						}
					}
				}
			}
		};
		init.defaultCompleteMode = ToilCompleteMode.Never;
		init.activeSkill = () => Toils_Combat.GetActiveSkillForToil(init);
		yield return init;
	}
}
