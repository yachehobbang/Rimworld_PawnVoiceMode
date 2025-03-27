using System.Collections.Generic;
using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientRechargeRoom : SketchResolver
{
	private static IEnumerable<ThingDef> CentralThings
	{
		get
		{
			yield return ThingDefOf.AncientBasicRecharger;
			yield return ThingDefOf.AncientStandardRecharger;
		}
	}

	protected override bool CanResolveInt(ResolveParams parms)
	{
		if (parms.rect.HasValue)
		{
			return parms.sketch != null;
		}
		return false;
	}

	protected override void ResolveInt(ResolveParams parms)
	{
		if (!ModLister.CheckBiotech("Ancient recharger room"))
		{
			return;
		}
		ResolveParams parms2 = parms;
		parms2.cornerThing = ThingDefOf.AncientLamp;
		parms2.requireFloor = true;
		SketchResolverDefOf.AddCornerThings.Resolve(parms2);
		foreach (ThingDef centralThing in CentralThings)
		{
			ResolveParams parms3 = parms;
			parms3.thingCentral = centralThing;
			parms3.requireFloor = true;
			SketchResolverDefOf.AddThingsCentral.Resolve(parms3);
		}
		ResolveParams parms4 = parms;
		parms4.wallEdgeThing = ThingDefOf.Table1x2c;
		parms4.allowWood = false;
		SketchResolverDefOf.AddWallEdgeThings.Resolve(parms4);
	}
}
