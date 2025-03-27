using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class SitePartWorker_WorkSite_Hunting : SitePartWorker_WorkSite
{
	public override IEnumerable<PreceptDef> DisallowedPrecepts => DefDatabase<PreceptDef>.AllDefs.Where((PreceptDef p) => p.disallowHuntingCamps);

	public override PawnGroupKindDef WorkerGroupKind => PawnGroupKindDefOf.Hunters;

	public override bool CanSpawnOn(int tile)
	{
		if (Find.WorldGrid[tile].biome.animalDensity > BiomeDefOf.Desert.animalDensity)
		{
			return base.CanSpawnOn(tile);
		}
		return false;
	}

	public override IEnumerable<CampLootThingStruct> LootThings(int tile)
	{
		IEnumerable<ThingDef> enumerable = Find.WorldGrid[tile].biome.AllWildAnimals.Select((PawnKindDef a) => a.RaceProps.leatherDef);
		int num = enumerable.Count((ThingDef x) => x != null);
		if (num <= 0)
		{
			yield break;
		}
		float leatherWeight = 1f / (float)num;
		foreach (ThingDef item in enumerable)
		{
			if (item != null)
			{
				yield return new CampLootThingStruct
				{
					thing = item,
					thing2 = ThingDefOf.Pemmican,
					weight = leatherWeight
				};
			}
		}
	}
}
