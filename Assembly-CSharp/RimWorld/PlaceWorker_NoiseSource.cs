using UnityEngine;
using Verse;

namespace RimWorld;

public class PlaceWorker_NoiseSource : PlaceWorker
{
	public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
	{
		CompProperties_NoiseSource compProperties = def.GetCompProperties<CompProperties_NoiseSource>();
		if (compProperties != null)
		{
			GenDraw.DrawRadiusRing(center, compProperties.radius, Color.white);
		}
	}
}
