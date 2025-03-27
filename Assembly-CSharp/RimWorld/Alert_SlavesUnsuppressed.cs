using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Alert_SlavesUnsuppressed : Alert
{
	private List<Pawn> targetsResult = new List<Pawn>();

	public List<Pawn> Targets
	{
		get
		{
			targetsResult.Clear();
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				foreach (Pawn freeColonist in maps[i].mapPawns.FreeColonists)
				{
					if (!freeColonist.Suspended && freeColonist.IsSlave)
					{
						Need_Suppression need_Suppression = freeColonist.needs.TryGetNeed<Need_Suppression>();
						if (need_Suppression != null && need_Suppression.IsHigh)
						{
							targetsResult.Add(freeColonist);
						}
					}
				}
			}
			return targetsResult;
		}
	}

	public Alert_SlavesUnsuppressed()
	{
		defaultLabel = "SlavesUnsuppressedLabel".Translate();
		defaultExplanation = "SlavesUnsuppressedDesc".Translate();
		defaultPriority = AlertPriority.High;
		requireIdeology = true;
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(Targets);
	}

	public override TaggedString GetExplanation()
	{
		return "SlavesUnsuppressedDesc".Translate(Targets[0]);
	}
}
