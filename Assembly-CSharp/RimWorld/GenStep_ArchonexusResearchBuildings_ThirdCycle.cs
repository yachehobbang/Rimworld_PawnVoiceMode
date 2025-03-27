using RimWorld.BaseGen;
using Verse;

namespace RimWorld;

public class GenStep_ArchonexusResearchBuildings_ThirdCycle : GenStep_ArchonexusResearchBuildings
{
	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
	{
		ResolveParams resolveParams = default(ResolveParams);
		resolveParams.rect = CellRect.CenteredOn(loc, Size.x, Size.z);
		resolveParams.minorBuildingCount = 8;
		resolveParams.minorBuildingRadialDistance = 12;
		resolveParams.centralBuilding = ThingDefOf.GrandArchotechStructure;
		RimWorld.BaseGen.BaseGen.globalSettings.map = map;
		RimWorld.BaseGen.BaseGen.symbolStack.Push("archonexusResearchBuildings", resolveParams);
		RimWorld.BaseGen.BaseGen.Generate();
	}
}
