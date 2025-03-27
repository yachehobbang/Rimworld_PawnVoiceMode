using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class CompProperties_Facility : CompProperties
{
	[Unsaved(false)]
	public List<ThingDef> linkableBuildings;

	public List<StatModifier> statOffsets;

	public int maxSimultaneous = 1;

	public bool mustBePlacedAdjacent;

	public bool mustBePlacedAdjacentCardinalToBedHead;

	public bool mustBePlacedAdjacentCardinalToAndFacingBedHead;

	public bool mustBePlacedFacingThingLinear;

	public bool canLinkToMedBedsOnly;

	public float maxDistance = 8f;

	public float minDistance;

	public bool showMaxSimultaneous = true;

	public bool requiresLOS = true;

	public CompProperties_Facility()
	{
		compClass = typeof(CompFacility);
	}

	public override void ResolveReferences(ThingDef parentDef)
	{
		linkableBuildings = new List<ThingDef>();
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			CompProperties_AffectedByFacilities compProperties = allDefsListForReading[i].GetCompProperties<CompProperties_AffectedByFacilities>();
			if (compProperties == null || compProperties.linkableFacilities == null)
			{
				continue;
			}
			for (int j = 0; j < compProperties.linkableFacilities.Count; j++)
			{
				if (compProperties.linkableFacilities[j] == parentDef)
				{
					linkableBuildings.Add(allDefsListForReading[i]);
					break;
				}
			}
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
		{
			yield return item;
		}
		if (!ModsConfig.AnomalyActive || statOffsets == null)
		{
			yield break;
		}
		foreach (StatModifier statOffset in statOffsets)
		{
			if (statOffset.stat == StatDefOf.ContainmentStrength)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.Containment, "StatsReport_ContainmentStrengthOffset".Translate(), statOffset.value.ToString("F1"), "StatsReport_ContainmentStrengthOffset_Desc".Translate(), 500);
			}
		}
	}
}
