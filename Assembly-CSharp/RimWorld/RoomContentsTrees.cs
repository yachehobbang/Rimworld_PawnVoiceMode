using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RoomContentsTrees : RoomContentsWorker
{
	private static readonly IntRange DirtAmountRange = new IntRange(18, 24);

	private static readonly IntRange TressRange = new IntRange(2, 4);

	private static readonly FloatRange TreeGrowthRange = new FloatRange(0.7f, 1f);

	private static readonly List<ThingDef> treeKinds = new List<ThingDef>();

	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (treeKinds.Empty())
		{
			treeKinds.AddRange(new ThingDef[4]
			{
				ThingDefOf.Plant_TreeOak,
				ThingDefOf.Plant_TreePoplar,
				ThingDefOf.Plant_TreeBirch,
				ThingDefOf.Plant_TreePine
			});
		}
		ThingDef thingDef = treeKinds.RandomElement();
		if (!room.TryGetRandomCellInRoom(map, out var cell, 2))
		{
			return;
		}
		int randomInRange = DirtAmountRange.RandomInRange;
		int randomInRange2 = TressRange.RandomInRange;
		int num = 0;
		List<IntVec3> list = GridShapeMaker.IrregularLump(cell, map, randomInRange, (IntVec3 x) => room.Contains(x));
		for (int i = 0; i < list.Count; i++)
		{
			IntVec3 intVec = list[i];
			map.terrainGrid.SetTerrain(intVec, TerrainDefOf.Soil);
			if (Rand.DynamicChance(num, randomInRange2, list.Count - i))
			{
				num++;
				Plant plant = (Plant)GenSpawn.Spawn(thingDef, intVec, map);
				plant.Growth = TreeGrowthRange.RandomInRange;
				if (!thingDef.plant.dieIfLeafless)
				{
					plant.MakeLeafless(Plant.LeaflessCause.Cold);
				}
			}
		}
	}
}
