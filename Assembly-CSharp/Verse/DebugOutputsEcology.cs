using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;

namespace Verse;

public static class DebugOutputsEcology
{
	[DebugOutput]
	public static void Plants()
	{
		DebugTables.MakeTablesDialog(from d in DefDatabase<ThingDef>.AllDefs
			where d.category == ThingCategory.Plant
			orderby d.plant.fertilitySensitivity
			select d, new TableDataGetter<ThingDef>("defName", (ThingDef d) => d.defName), new TableDataGetter<ThingDef>("grow days", (ThingDef d) => d.plant.growDays.ToString("F2")), new TableDataGetter<ThingDef>("nutrition", (ThingDef d) => Nutrition(d).ToString("F2")), new TableDataGetter<ThingDef>("nutrition\n/day", (ThingDef d) => (Nutrition(d) / d.plant.growDays).ToString("F4")), new TableDataGetter<ThingDef>("fertility\nmin", (ThingDef d) => d.plant.fertilityMin.ToString("F2")), new TableDataGetter<ThingDef>("fertility\nsensitivity", (ThingDef d) => d.plant.fertilitySensitivity.ToString("F2")), new TableDataGetter<ThingDef>("harvest\nnutrition", (ThingDef d) => HarvestNutrition(d).ToString("F2")), new TableDataGetter<ThingDef>("nutrition\n/ harvest nutrition", (ThingDef d) => (!(HarvestNutrition(d) <= 0f)) ? (Nutrition(d) / HarvestNutrition(d)).ToString("F2") : ""), new TableDataGetter<ThingDef>("tree", (ThingDef d) => (!d.plant.IsTree) ? "" : "yes"), new TableDataGetter<ThingDef>("farm animal edible", (ThingDef d) => (!MapPlantGrowthRateCalculator.IsEdibleByPastureAnimals(d)) ? "" : "yes"), new TableDataGetter<ThingDef>("minifiable", (ThingDef d) => d.Minifiable), new TableDataGetter<ThingDef>("treelovers\ncare", (ThingDef d) => d.plant.treeLoversCareIfChopped));
		static float HarvestNutrition(ThingDef d)
		{
			if (d.plant.harvestedThingDef == null)
			{
				return 0f;
			}
			return d.plant.harvestYield * d.plant.harvestedThingDef.GetStatValueAbstract(StatDefOf.Nutrition);
		}
		static float Nutrition(ThingDef d)
		{
			if (d.ingestible == null)
			{
				return 0f;
			}
			return d.GetStatValueAbstract(StatDefOf.Nutrition);
		}
	}

	[DebugOutput(true)]
	public static void PlantCurrentProportions()
	{
		PlantUtility.LogPlantProportions();
	}

	[DebugOutput]
	public static void Biomes()
	{
		DebugTables.MakeTablesDialog(DefDatabase<BiomeDef>.AllDefs.OrderByDescending((BiomeDef d) => d.plantDensity), new TableDataGetter<BiomeDef>("defName", (BiomeDef d) => d.defName), new TableDataGetter<BiomeDef>("animalDensity", (BiomeDef d) => d.animalDensity.ToString("F2")), new TableDataGetter<BiomeDef>("plantDensity", (BiomeDef d) => d.plantDensity.ToString("F2")), new TableDataGetter<BiomeDef>("tree density", (BiomeDef d) => d.TreeDensity.ToStringPercent()), new TableDataGetter<BiomeDef>("tree sightings\nper hour", (BiomeDef d) => d.TreeSightingsPerHourFromCaravan), new TableDataGetter<BiomeDef>("diseaseMtbDays", (BiomeDef d) => d.diseaseMtbDays.ToString("F0")), new TableDataGetter<BiomeDef>("movementDifficulty", (BiomeDef d) => (!d.impassable) ? d.movementDifficulty.ToString("F1") : "-"), new TableDataGetter<BiomeDef>("forageability", (BiomeDef d) => d.forageability.ToStringPercent()), new TableDataGetter<BiomeDef>("forageFood", (BiomeDef d) => (d.foragedFood == null) ? "" : d.foragedFood.label), new TableDataGetter<BiomeDef>("forageable plants", (BiomeDef d) => (from pd in d.AllWildPlants
			where pd.plant.harvestedThingDef != null && pd.plant.harvestedThingDef.IsNutritionGivingIngestible
			select pd.defName).ToCommaList()), new TableDataGetter<BiomeDef>("wildPlantRegrowDays", (BiomeDef d) => d.wildPlantRegrowDays.ToString("F0")), new TableDataGetter<BiomeDef>("wildPlantsCareAboutLocalFertility", (BiomeDef d) => d.wildPlantsCareAboutLocalFertility.ToStringCheckBlank()));
	}

	[DebugOutput]
	public static void BiomeAnimalsSpawnChances()
	{
		BiomeAnimalsInternal(delegate(PawnKindDef k, BiomeDef b)
		{
			float num = b.CommonalityOfAnimal(k);
			return (num == 0f) ? "" : (num / DefDatabase<PawnKindDef>.AllDefs.Sum((PawnKindDef ki) => b.CommonalityOfAnimal(ki))).ToStringPercent("F1");
		});
	}

	[DebugOutput]
	public static void BiomeAnimalsTypicalCounts()
	{
		BiomeAnimalsInternal((PawnKindDef k, BiomeDef b) => ExpectedAnimalCount(k, b).ToStringEmptyZero("F2"));
	}

	private static float ExpectedAnimalCount(PawnKindDef k, BiomeDef b)
	{
		float num = b.CommonalityOfAnimal(k);
		if (num == 0f)
		{
			return 0f;
		}
		float num2 = DefDatabase<PawnKindDef>.AllDefs.Sum((PawnKindDef ki) => b.CommonalityOfAnimal(ki));
		float num3 = num / num2;
		float num4 = 10000f / b.animalDensity;
		float num5 = 62500f / num4;
		float totalCommonality = DefDatabase<PawnKindDef>.AllDefs.Sum((PawnKindDef ki) => b.CommonalityOfAnimal(ki));
		float num6 = DefDatabase<PawnKindDef>.AllDefs.Sum((PawnKindDef ki) => k.ecoSystemWeight * (b.CommonalityOfAnimal(ki) / totalCommonality));
		return num5 / num6 * num3;
	}

	private static void BiomeAnimalsInternal(Func<PawnKindDef, BiomeDef, string> densityInBiomeOutputter)
	{
		List<TableDataGetter<PawnKindDef>> list = (from b in DefDatabase<BiomeDef>.AllDefs
			where b.implemented && b.canBuildBase
			orderby b.animalDensity
			select new TableDataGetter<PawnKindDef>(b.defName, (PawnKindDef k) => densityInBiomeOutputter(k, b))).ToList();
		list.Insert(0, new TableDataGetter<PawnKindDef>("animal", (PawnKindDef k) => k.defName + (k.race.race.predator ? " (P)" : "")));
		DebugTables.MakeTablesDialog(from d in DefDatabase<PawnKindDef>.AllDefs
			where d.race != null && d.RaceProps.Animal
			orderby d.defName
			select d, list.ToArray());
	}

	[DebugOutput]
	public static void BiomePlantsExpectedCount()
	{
		Func<ThingDef, BiomeDef, string> expectedCountInBiomeOutputter = (ThingDef p, BiomeDef b) => (b.CommonalityOfPlant(p) * b.plantDensity * 4000f).ToString("F0");
		List<TableDataGetter<ThingDef>> list = (from b in DefDatabase<BiomeDef>.AllDefs
			where b.implemented && b.canBuildBase
			orderby b.plantDensity
			select new TableDataGetter<ThingDef>(b.defName + " (" + b.plantDensity.ToString("F2") + ")", (ThingDef k) => expectedCountInBiomeOutputter(k, b))).ToList();
		list.Insert(0, new TableDataGetter<ThingDef>("plant", (ThingDef k) => k.defName));
		DebugTables.MakeTablesDialog(from d in DefDatabase<ThingDef>.AllDefs
			where d.category == ThingCategory.Plant
			orderby d.defName
			select d, list.ToArray());
	}

	[DebugOutput]
	public static void AnimalWildCountsOnMap()
	{
		Map map = Find.CurrentMap;
		DebugTables.MakeTablesDialog(from k in DefDatabase<PawnKindDef>.AllDefs
			where k.race != null && k.RaceProps.Animal && ExpectedAnimalCount(k, map.Biome) > 0f
			orderby ExpectedAnimalCount(k, map.Biome) descending
			select k, new TableDataGetter<PawnKindDef>("animal", (PawnKindDef k) => k.defName), new TableDataGetter<PawnKindDef>("expected count on map (inaccurate)", (PawnKindDef k) => ExpectedAnimalCount(k, map.Biome).ToString("F2")), new TableDataGetter<PawnKindDef>("actual count on map", (PawnKindDef k) => map.mapPawns.AllPawnsSpawned.Where((Pawn p) => p.kindDef == k).Count().ToString()));
	}

	[DebugOutput]
	public static void PlantCountsOnMap()
	{
		Map map = Find.CurrentMap;
		DebugTables.MakeTablesDialog(from p in DefDatabase<ThingDef>.AllDefs
			where p.category == ThingCategory.Plant && map.Biome.CommonalityOfPlant(p) > 0f
			orderby map.Biome.CommonalityOfPlant(p) descending
			select p, new TableDataGetter<ThingDef>("plant", (ThingDef p) => p.defName), new TableDataGetter<ThingDef>("biome-defined commonality", (ThingDef p) => map.Biome.CommonalityOfPlant(p).ToString("F2")), new TableDataGetter<ThingDef>("expected count (rough)", (ThingDef p) => (map.Biome.CommonalityOfPlant(p) * map.Biome.plantDensity * 4000f).ToString("F0")), new TableDataGetter<ThingDef>("actual count on map", (ThingDef p) => map.AllCells.Where((IntVec3 c) => c.GetPlant(map) != null && c.GetPlant(map).def == p).Count().ToString()));
	}

	[DebugOutput]
	public static void BiomeRanching()
	{
		List<TerrainDef> list = new List<TerrainDef>();
		list.Add(TerrainDefOf.Soil);
		list.Add(TerrainDefOf.SoilRich);
		list.Add(TerrainDefOf.Sand);
		List<TableDataGetter<BiomeDef>> list2 = new List<TableDataGetter<BiomeDef>>();
		list2.Add(new TableDataGetter<BiomeDef>("biome", (BiomeDef b) => b.defName));
		foreach (Quadrum quadrum in QuadrumUtility.Quadrums)
		{
			foreach (TerrainDef terrain in list)
			{
				list2.Add(new TableDataGetter<BiomeDef>(quadrum.LabelShort() + "\nnutrition\ndaily\n/10x10\n" + terrain.defName, (BiomeDef b) => (!b.terrainsByFertility.Any((TerrainThreshold tbf) => tbf.terrain == terrain)) ? "" : (GetAverageNutritionPerDay(quadrum, b, terrain) * 10f * 10f).ToString("F3")));
			}
		}
		foreach (Quadrum quadrum2 in QuadrumUtility.Quadrums)
		{
			foreach (TerrainDef terrain2 in list)
			{
				list2.Add(new TableDataGetter<BiomeDef>(quadrum2.LabelShort() + "\ncows\n/10x10\n" + terrain2.defName, (BiomeDef b) => (!b.terrainsByFertility.Any((TerrainThreshold tbf) => tbf.terrain == terrain2)) ? "" : (CowsFeedPerDay(quadrum2, b, terrain2) * 10f * 10f).ToString("F3")));
			}
		}
		DebugTables.MakeTablesDialog(DefDatabase<BiomeDef>.AllDefs.Where((BiomeDef b) => b.canBuildBase && b.terrainsByFertility.Any((TerrainThreshold x) => x.terrain.fertility > 0f)), list2.ToArray());
		static float CowsFeedPerDay(Quadrum q, BiomeDef b, TerrainDef t)
		{
			return GetAverageNutritionPerDay(q, b, t) / SimplifiedPastureNutritionSimulator.NutritionConsumedPerDay(ThingDefOf.Cow);
		}
		static float GetAverageNutritionPerDay(Quadrum q, BiomeDef b, TerrainDef t)
		{
			int num = -1;
			for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
			{
				if (Find.WorldGrid.tiles[i].biome == b)
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				Log.Error("Could not find tile on map to sample for biome: " + b.label);
				return 0f;
			}
			MapPlantGrowthRateCalculator mapPlantGrowthRateCalculator = new MapPlantGrowthRateCalculator();
			mapPlantGrowthRateCalculator.BuildFor(num);
			MapPastureNutritionCalculator mapPastureNutritionCalculator = new MapPastureNutritionCalculator();
			mapPastureNutritionCalculator.Reset(num, Find.WorldGrid[num].biome, 0.64f, mapPlantGrowthRateCalculator);
			return mapPastureNutritionCalculator.GetAverageNutritionPerDay(q, t);
		}
	}
}
