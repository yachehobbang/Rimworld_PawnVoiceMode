using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_PlantSow : JobDriver
{
	private float sowWorkDone;

	private Plant Plant => (Plant)job.GetTarget(TargetIndex.A).Thing;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref sowWorkDone, "sowWorkDone", 0f);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch).FailOn(() => PlantUtility.AdjacentSowBlocker(job.plantDefToSow, base.TargetA.Cell, base.Map) != null).FailOn(() => !job.plantDefToSow.CanNowPlantAt(base.TargetLocA, base.Map))
			.FailOn((Func<bool>)delegate
			{
				List<Thing> thingList = base.TargetA.Cell.GetThingList(base.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].def == job.plantDefToSow)
					{
						return true;
					}
				}
				return false;
			});
		Toil sowToil = ToilMaker.MakeToil("MakeNewToils");
		sowToil.initAction = delegate
		{
			base.TargetThingA = GenSpawn.Spawn(job.plantDefToSow, base.TargetLocA, base.Map);
			pawn.Reserve(base.TargetThingA, sowToil.actor.CurJob);
			Plant plant = Plant;
			plant.Growth = 0f;
			plant.sown = true;
		};
		sowToil.tickAction = delegate
		{
			Pawn actor = sowToil.actor;
			actor.skills?.Learn(SkillDefOf.Plants, 0.085f);
			Plant plant2 = Plant;
			if (plant2.LifeStage != 0)
			{
				Log.Error(string.Concat(this, " getting sowing work while not in Sowing life stage."));
			}
			sowWorkDone += actor.GetStatValue(StatDefOf.PlantWorkSpeed);
			if (sowWorkDone >= plant2.def.plant.sowWork)
			{
				plant2.Growth = 0.0001f;
				base.Map.mapDrawer.MapMeshDirty(plant2.Position, MapMeshFlagDefOf.Things);
				actor.records.Increment(RecordDefOf.PlantsSown);
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SowedPlant, actor.Named(HistoryEventArgsNames.Doer)));
				if (plant2.def.plant.humanFoodPlant)
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SowedHumanFoodPlant, actor.Named(HistoryEventArgsNames.Doer)));
				}
				ReadyForNextToil();
			}
		};
		sowToil.defaultCompleteMode = ToilCompleteMode.Never;
		sowToil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		sowToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		sowToil.WithEffect(EffecterDefOf.Sow, TargetIndex.A, null);
		sowToil.WithProgressBar(TargetIndex.A, () => sowWorkDone / Plant.def.plant.sowWork, interpolateBetweenActorAndTarget: true);
		sowToil.PlaySustainerOrSound(() => SoundDefOf.Interact_Sow);
		sowToil.AddFinishAction(delegate
		{
			if (base.TargetThingA != null)
			{
				Plant plant3 = (Plant)sowToil.actor.CurJob.GetTarget(TargetIndex.A).Thing;
				if (sowWorkDone < plant3.def.plant.sowWork && !base.TargetThingA.Destroyed)
				{
					base.TargetThingA.Destroy();
				}
			}
		});
		sowToil.activeSkill = () => SkillDefOf.Plants;
		yield return sowToil;
	}
}
