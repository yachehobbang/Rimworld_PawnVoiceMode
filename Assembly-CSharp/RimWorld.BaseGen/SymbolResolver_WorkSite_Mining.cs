using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld.BaseGen;

public class SymbolResolver_WorkSite_Mining : SymbolResolver_WorkSite
{
	private struct TunnelCandidate : IEquatable<TunnelCandidate>
	{
		public IntVec3 start;

		public IntVec3 end;

		public float pathCost;

		public CellRect Rect => new CellRect(Mathf.Min(start.x, end.x), Mathf.Min(start.z, end.z), Mathf.Abs(start.x - end.x), Mathf.Abs(start.z - end.z));

		public bool Equals(TunnelCandidate other)
		{
			if (!(start == other.start) || !(end == other.end))
			{
				if (end == other.start)
				{
					return start == other.end;
				}
				return false;
			}
			return true;
		}
	}

	private const int MinPointsDeepDrilling = 500;

	private const int MinTunnelLength = 4;

	private const int MaxTunnelExitDist = 30;

	private const float RockTunnelCountMin = 5f;

	private const float RockTunnelCountMax = 15f;

	private const float MinDistBetweenTunnels = 6f;

	private static readonly SimpleCurve DeepDrillCountCurve = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(500f, 1f),
		new CurvePoint(750f, 2f),
		new CurvePoint(1000f, 3f)
	};

	public override void Resolve(ResolveParams rp)
	{
		if (!MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var var))
		{
			var = new List<CellRect>();
			MapGenerator.SetVar("UsedRects", var);
		}
		List<CellRect> myUsedRects = new List<CellRect>(var);
		myUsedRects.Add(rp.rect);
		CellRect rect = rp.rect;
		IntVec3 centerCell = rect.CenterCell;
		TechLevel techLevel = ThingDefOf.GroundPenetratingScanner.researchPrerequisites.Max((ResearchProjectDef r) => r.techLevel);
		if (rp.workSitePoints >= 500f && (int)rp.faction.def.techLevel >= (int)techLevel)
		{
			CellRect rect2 = rect.ExpandedBy(5).ClipInsideMap(BaseGen.globalSettings.map);
			CellRect rect3 = rect2.ExpandedBy(10).ClipInsideMap(BaseGen.globalSettings.map);
			int num = Mathf.FloorToInt(DeepDrillCountCurve.Evaluate(rp.workSitePoints));
			for (int i = 0; i < num; i++)
			{
				BaseGen.symbolStack.Push("thing", new ResolveParams
				{
					rect = rect3,
					singleThingDef = ThingDefOf.DeepDrill
				});
			}
			BaseGen.symbolStack.Push("thing", new ResolveParams
			{
				rect = rect2,
				singleThingDef = ThingDefOf.GroundPenetratingScanner
			});
		}
		Map map = BaseGen.globalSettings.map;
		List<TunnelCandidate> tunnelCandidates = new List<TunnelCandidate>();
		foreach (IntVec3 start in GenRadial.RadialCellsAround(centerCell, 30f, useCenter: false))
		{
			if (!start.InBounds(map) || !HasRock(start) || IsInsideUsedRects(start) || tunnelCandidates.Any((TunnelCandidate tc) => tc.start.DistanceTo(start) < 6f || tc.end.DistanceTo(start) < 6f))
			{
				continue;
			}
			IntVec3 end = start;
			IntVec3 offset = centerCell - start;
			if (offset.x > 1)
			{
				offset.x = 1;
			}
			else if (offset.x < -1)
			{
				offset.x = -1;
			}
			if (offset.y > 1)
			{
				offset.y = 1;
			}
			else if (offset.y < -1)
			{
				offset.y = -1;
			}
			if (offset.z > 1)
			{
				offset.z = 1;
			}
			else if (offset.z < -1)
			{
				offset.z = -1;
			}
			Rot4 rot = Rot4.FromIntVec3(offset);
			int num2 = 0;
			while (HasRock(end) && !IsInsideUsedRects(end))
			{
				end += rot.FacingCell;
				if (!end.InBounds(map) || end.DistanceTo(map.Center) > 30f)
				{
					num2 = -1;
					end -= rot.FacingCell;
					break;
				}
				num2++;
			}
			if (num2 < 4)
			{
				continue;
			}
			PawnPath pawnPath = PawnPath.NotFound;
			if (map.reachability.CanReach(end, centerCell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)))
			{
				pawnPath = map.pathFinder.FindPath(end, centerCell, TraverseParms.For(TraverseMode.PassDoors));
			}
			PawnPath pawnPath2 = PawnPath.NotFound;
			if (map.reachability.CanReach(start, centerCell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)))
			{
				pawnPath2 = map.pathFinder.FindPath(start, centerCell, TraverseParms.For(TraverseMode.PassDoors));
			}
			if (pawnPath.Found || pawnPath2.Found)
			{
				float pathCost = ((pawnPath.Found && !pawnPath2.Found) ? pawnPath.TotalCost : ((!pawnPath2.Found || pawnPath.Found) ? Mathf.Min(pawnPath.TotalCost, pawnPath2.TotalCost) : pawnPath2.TotalCost));
				TunnelCandidate tunnelCandidate = default(TunnelCandidate);
				tunnelCandidate.start = start;
				tunnelCandidate.end = end;
				tunnelCandidate.pathCost = pathCost;
				TunnelCandidate item = tunnelCandidate;
				pawnPath.ReleaseToPool();
				pawnPath2.ReleaseToPool();
				CellRect cRect = item.Rect;
				if (!tunnelCandidates.Contains(item) && !tunnelCandidates.Any((TunnelCandidate tc) => tc.Rect.Overlaps(cRect) && !tc.Rect.AdjacentCells.Contains(start) && !tc.Rect.AdjacentCells.Contains(end)))
				{
					tunnelCandidates.Add(item);
				}
			}
		}
		List<IntVec3> excavatedCells = new List<IntVec3>();
		int num3 = Mathf.Min((int)Rand.Range(5f, 15f), tunnelCandidates.Count);
		int num4 = 0;
		SortCandidates();
		while (num4 < num3)
		{
			TunnelCandidate tunnelCandidate2 = tunnelCandidates[0];
			tunnelCandidates.RemoveAt(0);
			foreach (IntVec3 item2 in tunnelCandidate2.Rect.ExpandedBy(1))
			{
				if (!item2.InBounds(map))
				{
					continue;
				}
				List<Thing> thingList = item2.GetThingList(map);
				for (int num5 = thingList.Count - 1; num5 >= 0; num5--)
				{
					Thing thing = thingList[num5];
					if (thing.def.building != null && thing.def.building.isNaturalRock)
					{
						thing.Destroy();
					}
				}
				excavatedCells.Add(item2);
			}
			num4++;
			if (num4 == num3)
			{
				break;
			}
			SortCandidates();
		}
		base.Resolve(rp);
		bool HasRock(IntVec3 cell)
		{
			foreach (Thing thing2 in cell.GetThingList(map))
			{
				if (thing2.def.building != null && thing2.def.building.isNaturalRock)
				{
					return true;
				}
			}
			return false;
		}
		bool IsInsideUsedRects(IntVec3 cell)
		{
			foreach (CellRect item3 in myUsedRects)
			{
				if (item3.Contains(cell))
				{
					return true;
				}
			}
			return false;
		}
		void SortCandidates()
		{
			tunnelCandidates.SortBy(delegate(TunnelCandidate tc)
			{
				float num6 = float.PositiveInfinity;
				foreach (IntVec3 item4 in excavatedCells)
				{
					foreach (IntVec3 item5 in tc.Rect)
					{
						float num7 = item5.DistanceTo(item4);
						if (num7 < num6)
						{
							num6 = num7;
						}
					}
				}
				return (num6 == float.PositiveInfinity || num6 > 6f) ? tc.pathCost : float.PositiveInfinity;
			});
		}
	}
}
