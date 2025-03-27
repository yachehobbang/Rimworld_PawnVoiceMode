using UnityEngine;
using Verse;

namespace RimWorld;

public class GenStep_Monolith : GenStep_Scatterer
{
	private const int DebrisRadius = 4;

	private const int AsphaltSize = 30;

	private const int ClearRadius = 5;

	private static readonly IntRange BloodFilthRange = new IntRange(1, 4);

	private static readonly IntRange TwistedFleshFilthRange = new IntRange(3, 6);

	private static readonly IntRange CorpsesCountRange = new IntRange(1, 3);

	private static readonly IntRange CorpseAgeRangeDays = new IntRange(50, 200);

	public override int SeedPart => 345173948;

	protected override bool CanScatterAt(IntVec3 loc, Map map)
	{
		if (!base.CanScatterAt(loc, map))
		{
			return false;
		}
		CellRect cellRect = CellRect.CenteredOn(loc, 5);
		int newZ = cellRect.minZ - 1;
		for (int i = cellRect.minX; i <= cellRect.maxX; i++)
		{
			IntVec3 c = new IntVec3(i, 0, newZ);
			if (!c.InBounds(map) || !c.Walkable(map))
			{
				return false;
			}
		}
		return true;
	}

	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
	{
		GenerateMonolith(loc, map);
	}

	public static void GenerateMonolith(IntVec3 loc, Map map)
	{
		if (!Find.Anomaly.GenerateMonolith)
		{
			return;
		}
		GenSpawn.Spawn((Building_VoidMonolith)ThingMaker.MakeThing(ThingDefOf.VoidMonolith), loc, map);
		foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc, map, 30))
		{
			map.terrainGrid.SetTerrain(item, TerrainDefOf.BrokenAsphalt);
		}
		int randomInRange = BloodFilthRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			if (CellFinder.TryFindRandomCellNear(loc, map, 4, (IntVec3 c) => c.Standable(map), out var result))
			{
				FilthMaker.TryMakeFilth(result, map, ThingDefOf.Filth_Blood);
			}
		}
		int randomInRange2 = TwistedFleshFilthRange.RandomInRange;
		for (int j = 0; j < randomInRange2; j++)
		{
			if (CellFinder.TryFindRandomCellNear(loc, map, 4, (IntVec3 c) => c.Standable(map), out var result2))
			{
				FilthMaker.TryMakeFilth(result2, map, ThingDefOf.Filth_TwistedFlesh);
			}
		}
		int randomInRange3 = CorpsesCountRange.RandomInRange;
		for (int k = 0; k < randomInRange3; k++)
		{
			if (CellFinder.TryFindRandomCellNear(loc, map, 4, (IntVec3 c) => c.Standable(map), out var result3))
			{
				Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Drifter, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
				pawn.health.SetDead();
				pawn.apparel.DestroyAll();
				pawn.equipment.DestroyAllEquipment();
				Find.WorldPawns.PassToWorld(pawn);
				Corpse corpse = pawn.MakeCorpse(null, null);
				corpse.Age = Mathf.RoundToInt(CorpseAgeRangeDays.RandomInRange * 60000);
				corpse.GetComp<CompRottable>().RotProgress += corpse.Age;
				GenSpawn.Spawn(pawn.Corpse, result3, map);
			}
		}
	}
}
