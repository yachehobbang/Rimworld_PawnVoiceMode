using RimWorld;
using Verse.AI.Group;

namespace Verse;

public class DeathActionWorker_Divide : DeathActionWorker
{
	public DeathActionProperties_Divide Props => (DeathActionProperties_Divide)props;

	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		if (!ModLister.CheckAnomaly("Pawn dividing"))
		{
			return;
		}
		Pawn innerPawn = corpse.InnerPawn;
		if (innerPawn == null)
		{
			return;
		}
		int dividePawnCount = Props.dividePawnCount;
		for (int i = 0; i < dividePawnCount; i++)
		{
			Pawn child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(Props.dividePawnKindOptions.RandomElement(), corpse.InnerPawn.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			SpawnPawn(child, innerPawn, corpse.PositionHeld, corpse.MapHeld, prevLord);
		}
		foreach (PawnKindDef item in Props.dividePawnKindAdditionalForced)
		{
			Pawn child2 = PawnGenerator.GeneratePawn(new PawnGenerationRequest(item, corpse.InnerPawn.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			SpawnPawn(child2, innerPawn, corpse.PositionHeld, corpse.MapHeld, prevLord);
		}
		FleshbeastUtility.MeatSplatter(Props.divideBloodFilthCountRange.RandomInRange, corpse.PositionHeld, corpse.MapHeld, FleshbeastUtility.ExplosionSizeFor(innerPawn));
		FilthMaker.TryMakeFilth(corpse.PositionHeld, corpse.MapHeld, ThingDefOf.Filth_TwistedFlesh);
		corpse.Destroy();
	}

	private void SpawnPawn(Pawn child, Pawn parent, IntVec3 position, Map map, Lord lord)
	{
		GenSpawn.Spawn(child, position, map, WipeMode.VanishOrMoveAside);
		lord?.AddPawn(child);
		CompInspectStringEmergence compInspectStringEmergence = child.TryGetComp<CompInspectStringEmergence>();
		if (compInspectStringEmergence != null)
		{
			compInspectStringEmergence.sourcePawn = parent;
		}
		FleshbeastUtility.SpawnPawnAsFlyer(child, map, position);
	}
}
