using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class Alert_NeedColonistBeds : Alert
{
	private static List<Pawn> tmpAssignedPawns = new List<Pawn>();

	public Alert_NeedColonistBeds()
	{
		defaultLabel = "NeedColonistBeds".Translate();
		defaultExplanation = "NeedColonistBedsDesc".Translate();
		defaultPriority = AlertPriority.High;
	}

	public override AlertReport GetReport()
	{
		if (GenDate.DaysPassed > 30)
		{
			return false;
		}
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			if (NeedColonistBeds(maps[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool NeedColonistBeds(Map map)
	{
		if (!map.IsPlayerHome)
		{
			return false;
		}
		AvailableColonistBeds(map, includeBabies: false, out var singleBeds, out var doubleBeds, out var _);
		if (singleBeds >= 0)
		{
			return doubleBeds < 0;
		}
		return true;
	}

	public static void AvailableColonistBeds(Map map, bool includeBabies, out int singleBeds, out int doubleBeds, out int cribs)
	{
		singleBeds = 0;
		doubleBeds = 0;
		cribs = 0;
		List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
		for (int i = 0; i < allBuildingsColonist.Count; i++)
		{
			if (allBuildingsColonist[i] is Building_Bed { ForColonists: not false, Medical: false } building_Bed && building_Bed.def.building.bed_humanlike)
			{
				if (building_Bed.ForHumanBabies)
				{
					cribs++;
				}
				else if (building_Bed.SleepingSlotsCount == 1)
				{
					singleBeds++;
				}
				else
				{
					doubleBeds++;
				}
			}
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		IEnumerable<Pawn> enumerable = map.mapPawns.FreeColonists.Where((Pawn p) => !p.IsSlave);
		tmpAssignedPawns.Clear();
		foreach (Pawn item in enumerable)
		{
			if ((!item.Spawned && !item.BrieflyDespawned()) || item.needs?.rest == null || tmpAssignedPawns.Contains(item) || item.DevelopmentalStage.Baby())
			{
				continue;
			}
			List<DirectPawnRelation> list = LovePartnerRelationUtility.ExistingLovePartners(item, allowDead: false);
			if (list.NullOrEmpty())
			{
				num2++;
				tmpAssignedPawns.Add(item);
				continue;
			}
			Pawn pawn = null;
			int num4 = int.MaxValue;
			for (int j = 0; j < list.Count; j++)
			{
				Pawn otherPawn = list[j].otherPawn;
				if (otherPawn != null && otherPawn.Spawned && otherPawn.Map == item.Map && otherPawn.Faction == Faction.OfPlayer && otherPawn.HostFaction == null && !tmpAssignedPawns.Contains(otherPawn) && otherPawn.needs?.rest != null)
				{
					int num5 = LovePartnerRelationUtility.ExistingLovePartnersCount(otherPawn, allowDead: false);
					if (pawn == null || num5 < num4)
					{
						pawn = otherPawn;
						num4 = num5;
					}
				}
			}
			if (pawn != null)
			{
				tmpAssignedPawns.Add(pawn);
				tmpAssignedPawns.Add(item);
				num3++;
			}
		}
		foreach (Pawn item2 in enumerable)
		{
			if (item2.needs?.rest != null && !tmpAssignedPawns.Contains(item2))
			{
				if (item2.DevelopmentalStage.Baby())
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		tmpAssignedPawns.Clear();
		for (int k = 0; k < num3; k++)
		{
			if (doubleBeds > 0)
			{
				doubleBeds--;
			}
			else
			{
				singleBeds -= 2;
			}
		}
		for (int l = 0; l < num2; l++)
		{
			if (doubleBeds > 0)
			{
				doubleBeds--;
			}
			else
			{
				singleBeds--;
			}
		}
		if (!includeBabies)
		{
			return;
		}
		for (int m = 0; m < num; m++)
		{
			if (doubleBeds > 0)
			{
				doubleBeds--;
			}
			else if (singleBeds > 0)
			{
				singleBeds--;
			}
			else
			{
				cribs--;
			}
		}
	}
}
