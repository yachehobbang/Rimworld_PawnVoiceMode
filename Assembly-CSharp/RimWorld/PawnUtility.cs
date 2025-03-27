using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public static class PawnUtility
{
	private static List<Pawn> tmpPawns = new List<Pawn>();

	private static List<string> tmpPawnKindsStr = new List<string>();

	private static HashSet<PawnKindDef> tmpAddedPawnKinds = new HashSet<PawnKindDef>();

	private static List<PawnKindDef> tmpPawnKinds = new List<PawnKindDef>();

	private static List<Thing> tmpThings = new List<Thing>();

	public static Faction GetFactionLeaderFaction(Pawn pawn)
	{
		List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
		for (int i = 0; i < allFactionsListForReading.Count; i++)
		{
			if (allFactionsListForReading[i].leader == pawn)
			{
				return allFactionsListForReading[i];
			}
		}
		return null;
	}

	public static bool IsFactionLeader(Pawn pawn)
	{
		return GetFactionLeaderFaction(pawn) != null;
	}

	public static bool IsInteractionBlocked(this Pawn pawn, InteractionDef interaction, bool isInitiator, bool isRandom)
	{
		MentalStateDef mentalStateDef = pawn.MentalStateDef;
		if (mentalStateDef != null)
		{
			if (isRandom)
			{
				return mentalStateDef.blockRandomInteraction;
			}
			if (interaction == null)
			{
				return false;
			}
			List<InteractionDef> list = (isInitiator ? mentalStateDef.blockInteractionInitiationExcept : mentalStateDef.blockInteractionRecipientExcept);
			if (list != null)
			{
				return !list.Contains(interaction);
			}
			return false;
		}
		List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			if (hediffs[i].def.blocksSocialInteraction)
			{
				return true;
			}
		}
		Lord lord = pawn.GetLord();
		if (lord != null && lord.BlocksSocialInteraction(pawn))
		{
			return true;
		}
		return false;
	}

	public static bool IsKidnappedPawn(Pawn pawn)
	{
		List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
		for (int i = 0; i < allFactionsListForReading.Count; i++)
		{
			if (allFactionsListForReading[i].kidnapped.KidnappedPawnsListForReading.Contains(pawn))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsTravelingInTransportPodWorldObject(Pawn pawn)
	{
		if (!pawn.IsWorldPawn() || !ThingOwnerUtility.AnyParentIs<ActiveDropPodInfo>(pawn))
		{
			return ThingOwnerUtility.AnyParentIs<TravelingTransportPods>(pawn);
		}
		return true;
	}

	public static bool ForSaleBySettlement(Pawn pawn)
	{
		return pawn.ParentHolder is Settlement_TraderTracker;
	}

	public static bool IsCarrying(this Pawn pawn)
	{
		if (!pawn.Destroyed && pawn.carryTracker != null)
		{
			return pawn.carryTracker.CarriedThing != null;
		}
		return false;
	}

	public static bool IsCarryingPawn(this Pawn pawn, Pawn carryPawn = null)
	{
		if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing is Pawn)
		{
			if (carryPawn != null)
			{
				return pawn.carryTracker.CarriedThing == carryPawn;
			}
			return true;
		}
		return false;
	}

	public static bool IsCarryingThing(this Pawn pawn, Thing carriedThing)
	{
		if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
		{
			return pawn.carryTracker.CarriedThing == carriedThing;
		}
		return false;
	}

	public static void TryDestroyStartingColonistFamily(Pawn pawn)
	{
		if (!pawn.relations.RelatedPawns.Any((Pawn x) => Find.GameInitData.startingAndOptionalPawns.Contains(x)))
		{
			DestroyStartingColonistFamily(pawn);
		}
	}

	public static void DestroyStartingColonistFamily(Pawn pawn)
	{
		foreach (Pawn item in pawn.relations.RelatedPawns.ToList())
		{
			if (!Find.GameInitData.startingAndOptionalPawns.Contains(item))
			{
				WorldPawnSituation situation = Find.WorldPawns.GetSituation(item);
				if (situation == WorldPawnSituation.Free || situation == WorldPawnSituation.Dead)
				{
					Find.WorldPawns.RemovePawn(item);
					Find.WorldPawns.PassToWorld(item, PawnDiscardDecideMode.Discard);
				}
			}
		}
	}

	public static bool EnemiesAreNearby(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1)
	{
		TraverseParms tp = (passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn));
		int count = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region r)
		{
			List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].HostileTo(pawn) && (maxDistance <= 0f || list[i].Position.InHorDistOf(pawn.Position, maxDistance)))
				{
					count++;
				}
			}
			return count >= maxCount;
		}, regionsToScan);
		return count >= maxCount;
	}

	public static bool WillSoonHaveBasicNeed(Pawn p, float thresholdOffset = 0.05f)
	{
		if (p.needs == null)
		{
			return false;
		}
		if (p.needs.rest != null && p.needs.rest.CurLevel < 0.28f + thresholdOffset)
		{
			return true;
		}
		if (p.needs.food != null && p.needs.food.CurLevelPercentage < p.needs.food.PercentageThreshHungry + thresholdOffset)
		{
			return true;
		}
		return false;
	}

	public static bool CanCasuallyInteractNow(this Pawn p, bool twoWayInteraction = false, bool canInteractWhileSleeping = false, bool canInteractWhileRoaming = false, bool canInteractWhileDrafted = false)
	{
		if (p.Drafted && !canInteractWhileDrafted)
		{
			return false;
		}
		if (p.IsPsychologicallyInvisible())
		{
			return false;
		}
		if (ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(p))
		{
			return false;
		}
		if (p.InAggroMentalState)
		{
			return false;
		}
		if (p.InMentalState && p.MentalStateDef == MentalStateDefOf.Roaming && !canInteractWhileRoaming)
		{
			return false;
		}
		if (!p.Awake() && !canInteractWhileSleeping)
		{
			return false;
		}
		if (p.IsFormingCaravan())
		{
			return false;
		}
		Job curJob = p.CurJob;
		if (curJob != null && twoWayInteraction && (!curJob.def.casualInterruptible || !curJob.playerForced))
		{
			return false;
		}
		return true;
	}

	public static IEnumerable<Pawn> SpawnedMasteredPawns(Pawn master)
	{
		if (Current.ProgramState != ProgramState.Playing || master.Faction == null || !master.RaceProps.Humanlike || !master.Spawned)
		{
			yield break;
		}
		List<Pawn> pawns = master.Map.mapPawns.SpawnedPawnsInFaction(master.Faction);
		for (int i = 0; i < pawns.Count; i++)
		{
			if (pawns[i].playerSettings != null && pawns[i].playerSettings.Master == master)
			{
				yield return pawns[i];
			}
		}
	}

	public static bool InValidState(Pawn p)
	{
		if (p.health == null)
		{
			return false;
		}
		if (!p.Dead && (p.stances == null || p.mindState == null || p.needs == null || p.ageTracker == null))
		{
			return false;
		}
		return true;
	}

	public static PawnPosture GetPosture(this Pawn p)
	{
		PawnPosture pawnPosture = (p.Dead ? PawnPosture.LayingOnGroundNormal : ((!ModsConfig.BiotechActive || !p.IsCharging()) ? ((!(p.ParentHolder is IThingHolderWithDrawnPawn thingHolderWithDrawnPawn)) ? (p.Downed ? ((p.jobs == null || !p.jobs.posture.Laying()) ? PawnPosture.LayingOnGroundNormal : p.jobs.posture) : ((p.jobs != null) ? p.jobs.posture : PawnPosture.Standing)) : thingHolderWithDrawnPawn.HeldPawnPosture) : PawnPosture.Standing));
		Pawn_MindState mindState = p.mindState;
		if (mindState != null && mindState.duty?.def?.forceFaceUpPosture == true && pawnPosture != 0)
		{
			pawnPosture |= PawnPosture.FaceUpMask;
		}
		return pawnPosture;
	}

	public static void ForceWait(Pawn pawn, int ticks, Thing faceTarget = null, bool maintainPosture = false, bool maintainSleep = false)
	{
		if (ticks <= 0)
		{
			Log.ErrorOnce("Forcing a wait for zero ticks", 47045639);
		}
		JobDef def = (maintainPosture ? JobDefOf.Wait_MaintainPosture : JobDefOf.Wait);
		if (pawn.IsSelfShutdown())
		{
			def = JobDefOf.SelfShutdown;
		}
		else if (pawn.InBed())
		{
			def = (pawn.Awake() ? JobDefOf.LayDownAwake : JobDefOf.LayDown);
		}
		else if (!pawn.health.capacities.CanBeAwake)
		{
			def = JobDefOf.Wait_Downed;
		}
		else if (maintainSleep && !pawn.Awake())
		{
			def = JobDefOf.Wait_Asleep;
		}
		Job job = JobMaker.MakeJob(def, faceTarget);
		if (maintainSleep && !pawn.Awake())
		{
			job.forceSleep = true;
			job.targetA = pawn.Position;
		}
		if (pawn.InBed())
		{
			job.targetA = pawn.CurrentBed();
		}
		job.expiryInterval = ticks;
		pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
	}

	public static void GainComfortFromCellIfPossible(this Pawn p, bool chairsOnly = false)
	{
		if (p.Spawned && Find.TickManager.TicksGame % 10 == 0)
		{
			Building edifice = p.Position.GetEdifice(p.Map);
			if (edifice != null && (!chairsOnly || (edifice.def.category == ThingCategory.Building && edifice.def.building.isSittable)))
			{
				GainComfortFromThingIfPossible(p, edifice);
			}
		}
	}

	public static void GainComfortFromThingIfPossible(Pawn p, Thing from)
	{
		if (Find.TickManager.TicksGame % 10 == 0)
		{
			float statValue = from.GetStatValue(StatDefOf.Comfort);
			if (statValue >= 0f && p.needs != null && p.needs.comfort != null)
			{
				p.needs.comfort.ComfortUsed(statValue);
			}
		}
	}

	public static float BodyResourceGrowthSpeed(Pawn pawn)
	{
		return pawn.needs?.food?.CurCategory.HungerMultiplier() ?? 1f;
	}

	public static bool FertileMateTarget(Pawn male, Pawn female)
	{
		if (female.gender != Gender.Female || female.Sterile())
		{
			return false;
		}
		CompEggLayer compEggLayer = female.TryGetComp<CompEggLayer>();
		if (compEggLayer != null)
		{
			return !compEggLayer.FullyFertilized;
		}
		return true;
	}

	public static void Mated(Pawn male, Pawn female)
	{
		if (!female.Sterile() && !male.Sterile())
		{
			CompEggLayer compEggLayer = female.TryGetComp<CompEggLayer>();
			if (compEggLayer != null)
			{
				compEggLayer.Fertilize(male);
			}
			else if (Rand.Value < 0.5f)
			{
				Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.Pregnant, female);
				hediff_Pregnant.SetParents(null, male, null);
				female.health.AddHediff(hediff_Pregnant, null, null);
			}
		}
	}

	public static bool PlayerForcedJobNowOrSoon(Pawn pawn)
	{
		if (pawn.jobs == null)
		{
			return false;
		}
		Job curJob = pawn.CurJob;
		if (curJob != null)
		{
			return curJob.playerForced;
		}
		if (pawn.jobs.jobQueue.Count > 0)
		{
			return pawn.jobs.jobQueue.Peek().job.playerForced;
		}
		return false;
	}

	public static bool TrySpawnHatchedOrBornPawn(Pawn pawn, Thing motherOrEgg, IntVec3? positionOverride = null)
	{
		if (motherOrEgg.SpawnedOrAnyParentSpawned)
		{
			return GenSpawn.Spawn(pawn, positionOverride ?? motherOrEgg.PositionHeld, motherOrEgg.MapHeld) != null;
		}
		if (motherOrEgg is Pawn pawn2)
		{
			if (pawn2.IsCaravanMember())
			{
				pawn2.GetCaravan().AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
				Find.WorldPawns.PassToWorld(pawn);
				return true;
			}
			if (pawn2.IsWorldPawn())
			{
				Find.WorldPawns.PassToWorld(pawn);
				return true;
			}
		}
		else if (motherOrEgg.ParentHolder != null && motherOrEgg.ParentHolder is Pawn_InventoryTracker pawn_InventoryTracker)
		{
			if (pawn_InventoryTracker.pawn.IsCaravanMember())
			{
				pawn_InventoryTracker.pawn.GetCaravan().AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
				Find.WorldPawns.PassToWorld(pawn);
				return true;
			}
			if (pawn_InventoryTracker.pawn.IsWorldPawn())
			{
				Find.WorldPawns.PassToWorld(pawn);
				return true;
			}
		}
		return false;
	}

	public static ByteGrid GetAvoidGrid(this Pawn p, bool onlyIfLordAllows = true)
	{
		if (!p.Spawned)
		{
			return null;
		}
		if (p.Faction == null)
		{
			return null;
		}
		if (!p.Faction.def.canUseAvoidGrid)
		{
			return null;
		}
		if (p.Faction == Faction.OfPlayer || !p.Faction.HostileTo(Faction.OfPlayer))
		{
			return null;
		}
		if (p.kindDef.canUseAvoidGrid)
		{
			return p.Map.avoidGrid.Grid;
		}
		if (onlyIfLordAllows)
		{
			Lord lord = p.GetLord();
			if (lord?.CurLordToil != null && lord.CurLordToil.useAvoidGrid)
			{
				return lord.Map.avoidGrid.Grid;
			}
			return null;
		}
		return p.Map.avoidGrid.Grid;
	}

	public static bool ShouldCollideWithPawns(Pawn p)
	{
		if (p.Downed || p.Dead)
		{
			return false;
		}
		if (p.IsShambler)
		{
			return true;
		}
		if (!p.mindState.anyCloseHostilesRecently)
		{
			return false;
		}
		if (p.IsPsychologicallyInvisible())
		{
			return false;
		}
		if (!p.kindDef.collidesWithPawns)
		{
			return false;
		}
		return true;
	}

	public static bool AnyPawnBlockingPathAt(IntVec3 c, Pawn forPawn, bool actAsIfHadCollideWithPawnsJob = false, bool collideOnlyWithStandingPawns = false, bool forPathFinder = false)
	{
		return PawnBlockingPathAt(c, forPawn, actAsIfHadCollideWithPawnsJob, collideOnlyWithStandingPawns, forPathFinder) != null;
	}

	public static Pawn PawnBlockingPathAt(IntVec3 c, Pawn forPawn, bool actAsIfHadCollideWithPawnsJob = false, bool collideOnlyWithStandingPawns = false, bool forPathFinder = false)
	{
		List<Thing> thingList = c.GetThingList(forPawn.Map);
		if (thingList.Count == 0)
		{
			return null;
		}
		bool flag = false;
		if (actAsIfHadCollideWithPawnsJob)
		{
			flag = true;
		}
		else
		{
			Job curJob = forPawn.CurJob;
			if (curJob != null && (curJob.collideWithPawns || curJob.def.collideWithPawns || forPawn.jobs.curDriver.collideWithPawns))
			{
				flag = true;
			}
			else if (forPawn.Drafted)
			{
				_ = forPawn.pather.Moving;
			}
		}
		for (int i = 0; i < thingList.Count; i++)
		{
			if (!(thingList[i] is Pawn pawn) || pawn == forPawn || pawn.Downed || (collideOnlyWithStandingPawns && (pawn.pather.MovingNow || (pawn.pather.Moving && pawn.pather.MovedRecently(60)))) || PawnsCanShareCellBecauseOfBodySize(pawn, forPawn) || pawn.IsPsychologicallyInvisible() || !pawn.kindDef.collidesWithPawns)
			{
				continue;
			}
			if (pawn.HostileTo(forPawn))
			{
				return pawn;
			}
			if ((!forPawn.IsShambler || MutantUtility.ShamblerShouldCollideWith(forPawn, pawn)) && flag && (forPathFinder || !forPawn.Drafted || !pawn.RaceProps.Animal))
			{
				Job curJob2 = pawn.CurJob;
				if (curJob2 != null && (curJob2.collideWithPawns || curJob2.def.collideWithPawns || pawn.jobs.curDriver.collideWithPawns))
				{
					return pawn;
				}
			}
		}
		return null;
	}

	private static bool PawnsCanShareCellBecauseOfBodySize(Pawn p1, Pawn p2)
	{
		if (p1.BodySize >= 1.5f || p2.BodySize >= 1.5f)
		{
			return false;
		}
		float num = p1.BodySize / p2.BodySize;
		if (num < 1f)
		{
			num = 1f / num;
		}
		return num > 3.57f;
	}

	public static bool KnownDangerAt(IntVec3 c, Map map, Pawn forPawn)
	{
		return c.GetEdifice(map)?.IsDangerousFor(forPawn) ?? false;
	}

	[Obsolete("Lord and job report display validation is now checked separately.")]
	public static bool ShouldDisplayActionInInspectString(Pawn p)
	{
		if (p.Faction != Faction.OfPlayer && p.HostFaction != Faction.OfPlayer)
		{
			return false;
		}
		if (p.InMentalState)
		{
			return false;
		}
		if (ModsConfig.AnomalyActive && p.IsMutant)
		{
			return false;
		}
		return true;
	}

	public static bool ShouldDisplayLordReport(Pawn pawn)
	{
		if (ShouldShowCultistLordReport(pawn))
		{
			return true;
		}
		return ShouldShowActionReportToPlayer(pawn);
	}

	public static bool ShouldDisplayJobReport(Pawn pawn)
	{
		if (ModsConfig.AnomalyActive && pawn.IsMutant)
		{
			return false;
		}
		if (pawn.CurJobDef != null && pawn.CurJobDef.alwaysShowReport)
		{
			return true;
		}
		if (ModsConfig.AnomalyActive && pawn.Faction == Faction.OfHoraxCult && pawn.CurJobDef == JobDefOf.HateChanting)
		{
			return true;
		}
		if (ShouldShowCultistLordReport(pawn))
		{
			return true;
		}
		return ShouldShowActionReportToPlayer(pawn);
	}

	private static bool ShouldShowActionReportToPlayer(Pawn p)
	{
		if (p.Faction != Faction.OfPlayer && p.HostFaction != Faction.OfPlayer)
		{
			return false;
		}
		if (p.InMentalState)
		{
			return false;
		}
		if (ModsConfig.AnomalyActive && p.IsMutant)
		{
			return false;
		}
		return true;
	}

	public static bool ShouldDisplayFactionInInspectString(Pawn p)
	{
		if (ModsConfig.AnomalyActive && p.IsMutant)
		{
			return false;
		}
		return true;
	}

	private static bool ShouldShowCultistLordReport(Pawn pawn)
	{
		if (ModsConfig.AnomalyActive && pawn.Faction == Faction.OfHoraxCult && pawn.mindState.duty?.def == DutyDefOf.Invoke)
		{
			return true;
		}
		return false;
	}

	public static bool ShouldSendNotificationAbout(Pawn p)
	{
		if (Current.ProgramState != ProgramState.Playing)
		{
			return false;
		}
		if (p == null)
		{
			return false;
		}
		if (PawnGenerator.IsBeingGenerated(p))
		{
			return false;
		}
		if (p.IsWorldPawn() && (!p.IsCaravanMember() || !p.GetCaravan().IsPlayerControlled) && !IsTravelingInTransportPodWorldObject(p) && !p.IsBorrowedByAnyFaction() && p.Corpse.DestroyedOrNull())
		{
			return false;
		}
		if (ModsConfig.AnomalyActive)
		{
			if (p.IsMutant)
			{
				return false;
			}
			if (p.Corpse is UnnaturalCorpse)
			{
				return false;
			}
		}
		if (p.Faction != Faction.OfPlayer)
		{
			if (p.HostFaction != Faction.OfPlayer)
			{
				return false;
			}
			if (p.RaceProps.Humanlike && p.guest.Released && !p.Downed && !p.InBed())
			{
				return false;
			}
			if (p.CurJob != null && p.CurJob.exitMapOnArrival && !PrisonBreakUtility.IsPrisonBreaking(p))
			{
				return false;
			}
			if (IsExitingMap(p))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldGetThoughtAbout(Pawn pawn, Pawn subject)
	{
		if (ModsConfig.AnomalyActive && (pawn.IsMutant || subject.IsMutant))
		{
			return false;
		}
		if (pawn.Faction != subject.Faction)
		{
			if (!subject.IsWorldPawn())
			{
				return !pawn.IsWorldPawn();
			}
			return false;
		}
		return true;
	}

	public static bool IsTeetotaler(this Pawn pawn)
	{
		if (!new HistoryEvent(HistoryEventDefOf.IngestedDrug, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
		{
			return true;
		}
		if (!new HistoryEvent(HistoryEventDefOf.IngestedRecreationalDrug, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
		{
			return true;
		}
		if (pawn.story != null)
		{
			return pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) < 0;
		}
		return false;
	}

	public static bool CanTakeDrug(this Pawn pawn, ThingDef drug)
	{
		CompProperties_Drug compProperties = drug.GetCompProperties<CompProperties_Drug>();
		if (compProperties == null)
		{
			return true;
		}
		if (CanTakeDrugForDependency(pawn, drug))
		{
			return true;
		}
		if (!compProperties.teetotalerCanConsume && pawn.IsTeetotaler())
		{
			return false;
		}
		if (ModsConfig.IdeologyActive)
		{
			if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf.IngestedDrug, pawn))
			{
				return false;
			}
			if (drug.IsNonMedicalDrug && !IdeoUtility.DoerWillingToDo(HistoryEventDefOf.IngestedRecreationalDrug, pawn))
			{
				return false;
			}
			if (drug.ingestible != null && drug.ingestible.drugCategory == DrugCategory.Hard && !IdeoUtility.DoerWillingToDo(HistoryEventDefOf.IngestedHardDrug, pawn))
			{
				return false;
			}
		}
		return true;
	}

	public static bool CanTakeDrugForDependency(Pawn pawn, ThingDef drug)
	{
		if (!ModsConfig.BiotechActive || pawn.genes == null)
		{
			return false;
		}
		CompProperties_Drug compProperties = drug.GetCompProperties<CompProperties_Drug>();
		if (compProperties == null)
		{
			return true;
		}
		foreach (Gene item in pawn.genes.GenesListForReading)
		{
			if (item is Gene_ChemicalDependency gene_ChemicalDependency && item.Active && gene_ChemicalDependency.def.chemical == compProperties.chemical)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetChemicalDependencyGene(Pawn pawn, out Gene_ChemicalDependency gene)
	{
		gene = null;
		if (!ModsConfig.BiotechActive || pawn.genes == null)
		{
			return false;
		}
		foreach (Gene item in pawn.genes.GenesListForReading)
		{
			if (item is Gene_ChemicalDependency gene_ChemicalDependency)
			{
				gene = gene_ChemicalDependency;
				return true;
			}
		}
		return false;
	}

	public static bool PawnWouldBeUnhappyTakingDrug(this Pawn pawn, ThingDef drug)
	{
		if (pawn.IsTeetotaler())
		{
			return drug.GetCompProperties<CompProperties_Drug>() != null;
		}
		return false;
	}

	public static bool IsProsthophobe(this Pawn pawn)
	{
		if (pawn.story != null)
		{
			return pawn.story.traits.HasTrait(TraitDefOf.BodyPurist);
		}
		return false;
	}

	public static bool IsPrisonerInPrisonCell(this Pawn pawn)
	{
		if (pawn.IsPrisoner && pawn.Spawned)
		{
			return pawn.Position.IsInPrisonCell(pawn.Map);
		}
		return false;
	}

	public static bool IsBeingArrested(Pawn pawn)
	{
		if (pawn.Map == null)
		{
			return false;
		}
		foreach (Pawn item in pawn.Map.mapPawns.AllPawnsSpawned)
		{
			if (item != pawn && item.CurJobDef == JobDefOf.Arrest && item.CurJob.AnyTargetIs(pawn))
			{
				return true;
			}
		}
		return false;
	}

	public static string PawnKindsToCommaList(IEnumerable<Pawn> pawns, bool useAnd = false)
	{
		tmpPawns.Clear();
		tmpPawns.AddRange(pawns);
		if (tmpPawns.Count >= 2)
		{
			tmpPawns.SortBy((Pawn x) => !x.RaceProps.Humanlike, (Pawn x) => x.GetKindLabelPlural());
		}
		tmpAddedPawnKinds.Clear();
		tmpPawnKindsStr.Clear();
		for (int i = 0; i < tmpPawns.Count; i++)
		{
			if (tmpAddedPawnKinds.Contains(tmpPawns[i].kindDef))
			{
				continue;
			}
			tmpAddedPawnKinds.Add(tmpPawns[i].kindDef);
			int num = 0;
			for (int j = 0; j < tmpPawns.Count; j++)
			{
				if (tmpPawns[j].kindDef == tmpPawns[i].kindDef)
				{
					num++;
				}
			}
			if (num == 1)
			{
				tmpPawnKindsStr.Add("1 " + tmpPawns[i].KindLabel);
			}
			else
			{
				tmpPawnKindsStr.Add(num + " " + tmpPawns[i].GetKindLabelPlural(num));
			}
		}
		tmpPawns.Clear();
		return tmpPawnKindsStr.ToCommaList(useAnd);
	}

	public static List<string> PawnKindsToList(IEnumerable<PawnKindDef> pawnKinds)
	{
		tmpPawnKinds.Clear();
		tmpPawnKinds.AddRange(pawnKinds);
		if (tmpPawnKinds.Count >= 2)
		{
			tmpPawnKinds.SortBy((PawnKindDef x) => !x.RaceProps.Humanlike, (PawnKindDef x) => GenLabel.BestKindLabel(x, Gender.None, plural: true));
		}
		tmpAddedPawnKinds.Clear();
		tmpPawnKindsStr.Clear();
		for (int i = 0; i < tmpPawnKinds.Count; i++)
		{
			if (tmpAddedPawnKinds.Contains(tmpPawnKinds[i]))
			{
				continue;
			}
			tmpAddedPawnKinds.Add(tmpPawnKinds[i]);
			int num = 0;
			for (int j = 0; j < tmpPawnKinds.Count; j++)
			{
				if (tmpPawnKinds[j] == tmpPawnKinds[i])
				{
					num++;
				}
			}
			if (num == 1)
			{
				tmpPawnKindsStr.Add("1 " + GenLabel.BestKindLabel(tmpPawnKinds[i], Gender.None));
			}
			else
			{
				tmpPawnKindsStr.Add(num + " " + GenLabel.BestKindLabel(tmpPawnKinds[i], Gender.None, plural: true, num));
			}
		}
		return tmpPawnKindsStr;
	}

	public static string PawnKindsToLineList(IEnumerable<PawnKindDef> pawnKinds, string prefix)
	{
		PawnKindsToList(pawnKinds);
		return tmpPawnKindsStr.ToLineList(prefix);
	}

	public static string PawnKindsToLineList(IEnumerable<PawnKindDef> pawnKinds, string prefix, Color color)
	{
		PawnKindsToList(pawnKinds);
		for (int i = 0; i < tmpPawnKindsStr.Count; i++)
		{
			tmpPawnKindsStr[i] = tmpPawnKindsStr[i].Colorize(color);
		}
		return tmpPawnKindsStr.ToLineList(prefix);
	}

	public static string PawnKindsToCommaList(IEnumerable<PawnKindDef> pawnKinds, bool useAnd = false)
	{
		PawnKindsToList(pawnKinds);
		return tmpPawnKindsStr.ToCommaList(useAnd);
	}

	public static LocomotionUrgency ResolveLocomotion(Pawn pawn, LocomotionUrgency secondPriority)
	{
		if (!pawn.Dead && pawn.mindState.duty != null && pawn.mindState.duty.locomotion != 0)
		{
			return pawn.mindState.duty.locomotion;
		}
		return secondPriority;
	}

	public static LocomotionUrgency ResolveLocomotion(Pawn pawn, LocomotionUrgency secondPriority, LocomotionUrgency thirdPriority)
	{
		LocomotionUrgency locomotionUrgency = ResolveLocomotion(pawn, secondPriority);
		if (locomotionUrgency != 0)
		{
			return locomotionUrgency;
		}
		return thirdPriority;
	}

	public static Danger ResolveMaxDanger(Pawn pawn, Danger secondPriority)
	{
		if (!pawn.Dead && pawn.mindState.duty != null && pawn.mindState.duty.maxDanger != 0)
		{
			return pawn.mindState.duty.maxDanger;
		}
		return secondPriority;
	}

	public static Danger ResolveMaxDanger(Pawn pawn, Danger secondPriority, Danger thirdPriority)
	{
		Danger danger = ResolveMaxDanger(pawn, secondPriority);
		if (danger != 0)
		{
			return danger;
		}
		return thirdPriority;
	}

	public static bool IsPermanentCombatant(this Pawn pawn)
	{
		if (pawn?.mindState == null)
		{
			return true;
		}
		if (pawn.IsNonMutantAnimal && pawn.Faction != null)
		{
			return false;
		}
		if (pawn.DevelopmentalStage.Juvenile())
		{
			return false;
		}
		return true;
	}

	public static bool IsCombatant(this Pawn pawn)
	{
		if (pawn.IsPermanentCombatant())
		{
			return true;
		}
		return pawn.mindState.CombatantRecently;
	}

	public static bool IsFighting(this Pawn pawn)
	{
		if (pawn.CurJob != null)
		{
			if (pawn.CurJob.def != JobDefOf.AttackMelee && pawn.CurJob.def != JobDefOf.AttackStatic && pawn.CurJob.def != JobDefOf.Wait_Combat && pawn.CurJob.def != JobDefOf.PredatorHunt)
			{
				return pawn.CurJob.def == JobDefOf.ManTurret;
			}
			return true;
		}
		return false;
	}

	public static bool IsAttacking(this Pawn pawn)
	{
		if (pawn.CurJobDef == JobDefOf.AttackMelee || pawn.CurJobDef == JobDefOf.AttackStatic)
		{
			return true;
		}
		if (pawn.CurJobDef == JobDefOf.Wait_Combat && pawn.stances.curStance is Stance_Busy stance_Busy && stance_Busy.focusTarg.IsValid)
		{
			return true;
		}
		return false;
	}

	public static Hediff_Psylink GetMainPsylinkSource(this Pawn pawn)
	{
		return (Hediff_Psylink)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier);
	}

	public static int GetPsylinkLevel(this Pawn pawn)
	{
		int num = 0;
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (hediff is Hediff_Psylink hediff_Psylink)
			{
				num += hediff_Psylink.level;
			}
		}
		return num;
	}

	public static int GetMaxPsylinkLevel(this Pawn pawn)
	{
		return (int)HediffDefOf.PsychicAmplifier.maxSeverity;
	}

	public static RoyalTitle GetMaxPsylinkLevelTitle(this Pawn pawn)
	{
		if (pawn.royalty == null)
		{
			return null;
		}
		int num = 0;
		RoyalTitle result = null;
		foreach (RoyalTitle item in pawn.royalty.AllTitlesInEffectForReading)
		{
			if (num < item.def.maxPsylinkLevel)
			{
				num = item.def.maxPsylinkLevel;
				result = item;
			}
		}
		return result;
	}

	public static int GetMaxPsylinkLevelByTitle(this Pawn pawn)
	{
		return pawn.GetMaxPsylinkLevelTitle()?.def.maxPsylinkLevel ?? 0;
	}

	public static void ChangePsylinkLevel(this Pawn pawn, int levelOffset, bool sendLetter = true)
	{
		Hediff_Psylink mainPsylinkSource = pawn.GetMainPsylinkSource();
		if (mainPsylinkSource == null)
		{
			mainPsylinkSource = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
			try
			{
				mainPsylinkSource.suppressPostAddLetter = !sendLetter;
				pawn.health.AddHediff(mainPsylinkSource, pawn.health.hediffSet.GetBrain(), null);
				return;
			}
			finally
			{
				mainPsylinkSource.suppressPostAddLetter = false;
			}
		}
		mainPsylinkSource.ChangeLevel(levelOffset, sendLetter);
	}

	public static void GiveAllStartingPlayerPawnsThought(ThoughtDef thought)
	{
		foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
		{
			if (startingAndOptionalPawn.needs.mood == null)
			{
				continue;
			}
			if (thought.IsSocial)
			{
				foreach (Pawn startingAndOptionalPawn2 in Find.GameInitData.startingAndOptionalPawns)
				{
					if (startingAndOptionalPawn2 != startingAndOptionalPawn)
					{
						startingAndOptionalPawn.needs.mood.thoughts.memories.TryGainMemory(thought, startingAndOptionalPawn2);
					}
				}
			}
			else
			{
				startingAndOptionalPawn.needs.mood.thoughts.memories.TryGainMemory(thought);
			}
		}
	}

	public static IntVec3 DutyLocation(this Pawn pawn)
	{
		Pawn_RopeTracker roping = pawn.roping;
		if (roping != null && roping.IsRopedToSpot)
		{
			return pawn.roping.RopedToSpot;
		}
		if (pawn.mindState.duty != null && pawn.mindState.duty.focus.IsValid)
		{
			return pawn.mindState.duty.focus.Cell;
		}
		return pawn.Position;
	}

	public static bool EverBeenColonistOrTameAnimal(Pawn pawn)
	{
		return pawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0;
	}

	public static bool EverBeenPrisoner(Pawn pawn)
	{
		return pawn.records.GetAsInt(RecordDefOf.TimeAsPrisoner) > 0;
	}

	public static bool EverBeenQuestLodger(Pawn pawn)
	{
		return pawn.records.GetAsInt(RecordDefOf.TimeAsQuestLodger) > 0;
	}

	public static void RecoverFromUnwalkablePositionOrKill(IntVec3 c, Map map)
	{
		if (!c.InBounds(map) || c.Walkable(map))
		{
			return;
		}
		tmpThings.Clear();
		tmpThings.AddRange(c.GetThingList(map));
		for (int i = 0; i < tmpThings.Count; i++)
		{
			if (!(tmpThings[i] is Pawn pawn))
			{
				continue;
			}
			if (CellFinder.TryFindBestPawnStandCell(pawn, out var cell))
			{
				pawn.Position = cell;
				pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
				continue;
			}
			DamageInfo damageInfo = new DamageInfo(DamageDefOf.Crush, 99999f, 999f, -1f, null, pawn.health.hediffSet.GetBrain(), null, DamageInfo.SourceCategory.Collapse);
			damageInfo.SetIgnoreInstantKillProtection(ignore: true);
			pawn.TakeDamage(damageInfo);
			if (!pawn.Dead)
			{
				pawn.Kill(damageInfo);
			}
		}
	}

	public static float GetManhunterOnDamageChance(Pawn pawn, float distance, Thing instigator)
	{
		float manhunterOnDamageChance = GetManhunterOnDamageChance(pawn.kindDef);
		manhunterOnDamageChance *= GenMath.LerpDoubleClamped(1f, 30f, 3f, 1f, distance);
		if (instigator != null)
		{
			manhunterOnDamageChance *= 1f - instigator.GetStatValue(StatDefOf.HuntingStealth);
			if (instigator is Pawn instigator2)
			{
				manhunterOnDamageChance *= GetManhunterChanceFactorForInstigator(instigator2);
			}
		}
		return manhunterOnDamageChance;
	}

	public static float GetManhunterOnDamageChance(Pawn pawn, Thing instigator = null)
	{
		if (instigator != null)
		{
			return GetManhunterOnDamageChance(pawn, pawn.Position.DistanceTo(instigator.Position), instigator);
		}
		return GetManhunterOnDamageChance(pawn.kindDef);
	}

	public static float GetManhunterChanceFactorForInstigator(Pawn instigator)
	{
		if (ModsConfig.AnomalyActive && instigator.Faction == Faction.OfEntities)
		{
			return 0f;
		}
		float num = 1f;
		if (ModsConfig.IdeologyActive && instigator?.Ideo != null)
		{
			RoleEffect roleEffect = instigator.Ideo.GetRole(instigator)?.def.roleEffects?.FirstOrDefault((RoleEffect eff) => eff is RoleEffect_HuntingRevengeChanceFactor);
			if (roleEffect != null)
			{
				num *= ((RoleEffect_HuntingRevengeChanceFactor)roleEffect).factor;
			}
		}
		return num;
	}

	public static float GetManhunterOnDamageChance(PawnKindDef kind)
	{
		return kind.RaceProps.manhunterOnDamageChance * Find.Storyteller.difficulty.manhunterChanceOnDamageFactor;
	}

	public static float GetManhunterOnDamageChance(RaceProperties race)
	{
		return race.manhunterOnDamageChance * Find.Storyteller.difficulty.manhunterChanceOnDamageFactor;
	}

	public static bool PlayerHasReproductivePair(PawnKindDef pawnKindDef)
	{
		if (!pawnKindDef.RaceProps.Animal)
		{
			return false;
		}
		List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction;
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.Count; i++)
		{
			Pawn pawn = allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction[i];
			if (pawn.kindDef == pawnKindDef && pawn.ageTracker.CurLifeStage.reproductive)
			{
				if (pawn.gender == Gender.Male)
				{
					flag = true;
				}
				else if (pawn.gender == Gender.Female)
				{
					flag2 = true;
				}
				if (flag && flag2)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static float PlayerAnimalBodySizePerCapita()
	{
		float num = 0f;
		int num2 = 0;
		List<Pawn> allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction;
		for (int i = 0; i < allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.Count; i++)
		{
			Pawn pawn = allMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction[i];
			if (pawn.IsFreeColonist && !pawn.IsQuestLodger())
			{
				num2++;
			}
			if (pawn.IsNonMutantAnimal)
			{
				num += pawn.BodySize;
			}
		}
		if (num2 <= 0)
		{
			return 0f;
		}
		return num / (float)num2;
	}

	private static List<Pawn> PawnsOfFactionOnMapOrInCaravan(Pawn pawn)
	{
		if (pawn.Spawned)
		{
			return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
		}
		return pawn.GetCaravan()?.PawnsListForReading;
	}

	public static float PlayerVeneratedAnimalBodySizePerCapitaOnMapOrCaravan(Pawn pawn)
	{
		if (pawn.Ideo == null || pawn.Faction == null)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = 0;
		List<Pawn> list = PawnsOfFactionOnMapOrInCaravan(pawn);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Faction == pawn.Faction && !pawn.IsQuestLodger())
			{
				if (list[i].IsNonMutantAnimal && pawn.Ideo.IsVeneratedAnimal(list[i]))
				{
					num += list[i].BodySize;
				}
				else if (list[i].RaceProps.Humanlike)
				{
					num2++;
				}
			}
		}
		return Mathf.Round(((num2 > 0) ? (num / (float)num2) : 0f) * 100f) / 100f;
	}

	public static Pawn FirstVeneratedAnimalOnMapOrCaravan(Pawn pawn)
	{
		if (pawn.Ideo == null || pawn.Faction == null)
		{
			return null;
		}
		List<Pawn> list = PawnsOfFactionOnMapOrInCaravan(pawn);
		for (int i = 0; i < list.Count; i++)
		{
			if (pawn.Faction == list[i].Faction && pawn.Ideo.IsVeneratedAnimal(list[i]))
			{
				return list[i];
			}
		}
		return null;
	}

	public static bool HasClothingNotRequiredByKind(Pawn p)
	{
		if (p.apparel == null)
		{
			return false;
		}
		List<Apparel> wornApparel = p.apparel.WornApparel;
		if (wornApparel.Count > 0 && p.kindDef.apparelRequired.NullOrEmpty())
		{
			return true;
		}
		for (int i = 0; i < wornApparel.Count; i++)
		{
			Apparel apparel = wornApparel[i];
			if (apparel.def.apparel.countsAsClothingForNudity && !p.kindDef.apparelRequired.Contains(apparel.def))
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<PawnKindDef> GetCombatPawnKindsForPoints(Func<PawnKindDef, bool> selector, float points, Func<PawnKindDef, float> selectionWeight = null)
	{
		IEnumerable<PawnKindDef> allKinds = DefDatabase<PawnKindDef>.AllDefsListForReading.Where(selector);
		selectionWeight = selectionWeight ?? ((Func<PawnKindDef, float>)((PawnKindDef pk) => 1f));
		PawnKindDef result;
		while (points > 0f && allKinds.Where((PawnKindDef def) => def.combatPower > 0f && def.combatPower <= points).TryRandomElementByWeight(selectionWeight, out result))
		{
			points -= result.combatPower;
			yield return result;
		}
	}

	public static int GetMaxAllowedToPickUp(Pawn pawn, ThingDef thingDef)
	{
		int maxAllowedToPickUp = GetMaxAllowedToPickUp(thingDef, pawn.Map);
		if (maxAllowedToPickUp <= 0)
		{
			return 0;
		}
		int num = pawn.inventory.Count((Thing t) => t.def.orderedTakeGroup == thingDef.orderedTakeGroup);
		return Math.Max(maxAllowedToPickUp - num, 0);
	}

	public static int GetMaxAllowedToPickUp(ThingDef thingDef, Map map = null)
	{
		if (map != null && !map.IsPlayerHome)
		{
			return int.MaxValue;
		}
		if (thingDef.orderedTakeGroup == null)
		{
			return 0;
		}
		return thingDef.orderedTakeGroup.max;
	}

	public static bool CanPickUp(Pawn pawn, ThingDef thingDef)
	{
		if (!pawn.Map.IsPlayerHome)
		{
			return true;
		}
		if (pawn.inventory != null && thingDef.orderedTakeGroup != null)
		{
			return thingDef.orderedTakeGroup.max > 0;
		}
		return false;
	}

	public static bool ShouldBeSlaughtered(this Pawn pawn)
	{
		if (!pawn.Spawned || !pawn.IsNonMutantAnimal)
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(pawn, DesignationDefOf.Slaughter) != null || pawn.Map.autoSlaughterManager.AnimalsToSlaughter.Contains(pawn))
		{
			return pawn.Map.designationManager.DesignationOn(pawn, DesignationDefOf.ReleaseAnimalToWild) == null;
		}
		return false;
	}

	public static bool CanBeBuried(this Thing t)
	{
		if (t is Corpse { MapHeld: not null } corpse)
		{
			return corpse.MapHeld.designationManager.DesignationOn(corpse, DesignationDefOf.ExtractSkull) == null;
		}
		return true;
	}

	public static bool PawnHadFuneral(Pawn pawn)
	{
		Precept_Ritual precept_Ritual = (Precept_Ritual)(pawn.ideo?.Ideo?.GetPrecept(PreceptDefOf.Funeral));
		if (precept_Ritual != null && !precept_Ritual.completedObligations.NullOrEmpty())
		{
			return precept_Ritual.completedObligations.Any((RitualObligation o) => o.FirstValidTarget.Thing == pawn);
		}
		return false;
	}

	public static bool IsBiologicallyOrArtificiallyBlind(Pawn pawn)
	{
		if (!IsBiologicallyBlind(pawn))
		{
			return IsArtificiallyBlind(pawn);
		}
		return true;
	}

	public static bool IsBiologicallyBlind(Pawn pawn)
	{
		return !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight);
	}

	public static bool IsArtificiallyBlind(Pawn p)
	{
		if (IsBiologicallyBlind(p))
		{
			return false;
		}
		if (p.apparel != null)
		{
			foreach (Apparel item in p.apparel.WornApparel)
			{
				if (item.def.apparel.blocksVision)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsWorkTypeDisabledByAge(this Pawn pawn, WorkTypeDef workType, out int minAgeRequired)
	{
		for (int i = 0; i < pawn.RaceProps.lifeStageWorkSettings.Count; i++)
		{
			LifeStageWorkSettings lifeStageWorkSettings = pawn.RaceProps.lifeStageWorkSettings[i];
			if (lifeStageWorkSettings.workType == workType && lifeStageWorkSettings.IsDisabled(pawn))
			{
				minAgeRequired = lifeStageWorkSettings.minAge;
				return true;
			}
		}
		minAgeRequired = 0;
		return false;
	}

	public static bool DutyActiveWhenDown(this Pawn pawn, bool onlyInBed = false)
	{
		if (onlyInBed && !pawn.InBed())
		{
			return false;
		}
		return pawn.GetLord()?.LordJob?.DutyActiveWhenDown(pawn) ?? false;
	}

	public static bool IsExitingMap(Pawn pawn)
	{
		Lord lord = pawn.GetLord();
		if (lord == null)
		{
			return false;
		}
		if (!(lord.LordJob is LordJob_ExitMapBest) && !(lord.LordJob is LordJob_ExitMapNear) && !(lord.LordJob is LordJob_ExitOnShuttle))
		{
			return lord.LordJob is LordJob_TravelAndExit;
		}
		return true;
	}

	public static void ForceEjectFromContainer(Pawn pawn)
	{
		Thing resultingThing;
		if (pawn.ParentHolder is Pawn_CarryTracker pawn_CarryTracker)
		{
			pawn_CarryTracker.TryDropCarriedThing(pawn_CarryTracker.pawn.Position, ThingPlaceMode.Near, out resultingThing);
			pawn_CarryTracker.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
		if (pawn.ParentHolder is Building_Enterable building_Enterable)
		{
			building_Enterable.innerContainer.TryDrop(pawn, building_Enterable.InteractionCell, building_Enterable.Map, ThingPlaceMode.Near, out resultingThing);
		}
		if (pawn.ParentHolder is Building_Casket building_Casket)
		{
			building_Casket.EjectContents();
		}
		if (pawn.ParentHolder is Building_HoldingPlatform building_HoldingPlatform)
		{
			building_HoldingPlatform.EjectContents();
		}
		if (pawn.ParentHolder is CompBiosculpterPod compBiosculpterPod)
		{
			compBiosculpterPod.EjectContents(interrupted: true, playSounds: true);
		}
	}
}
