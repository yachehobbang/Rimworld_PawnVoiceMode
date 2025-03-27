using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class TrainableUtility
{
	private static List<TrainableDef> defsInListOrder = new List<TrainableDef>();

	public const int MinTrainInterval = 15000;

	private static readonly SimpleCurve DecayIntervalDaysFromWildnessCurve = new SimpleCurve
	{
		new CurvePoint(0f, 12f),
		new CurvePoint(1f, 6f)
	};

	public static List<TrainableDef> TrainableDefsInListOrder => defsInListOrder;

	public static void Reset()
	{
		defsInListOrder.Clear();
		defsInListOrder.AddRange(DefDatabase<TrainableDef>.AllDefsListForReading.OrderByDescending((TrainableDef td) => td.listPriority));
		bool flag;
		do
		{
			flag = false;
			for (int i = 0; i < defsInListOrder.Count; i++)
			{
				TrainableDef trainableDef = defsInListOrder[i];
				if (trainableDef.prerequisites != null)
				{
					for (int j = 0; j < trainableDef.prerequisites.Count; j++)
					{
						if (trainableDef.indent <= trainableDef.prerequisites[j].indent)
						{
							trainableDef.indent = trainableDef.prerequisites[j].indent + 1;
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		while (flag);
	}

	public static string MasterString(Pawn pawn)
	{
		if (pawn.playerSettings.Master == null)
		{
			return "(" + "NoneLower".TranslateSimple() + ")";
		}
		return RelationsUtility.LabelWithBondInfo(pawn.playerSettings.Master, pawn);
	}

	public static int MinimumHandlingSkill(Pawn p)
	{
		return Mathf.RoundToInt(p.GetStatValue(StatDefOf.MinimumHandlingSkill));
	}

	public static void MasterSelectButton(Rect rect, Pawn pawn, bool paintable)
	{
		Widgets.Dropdown(rect, pawn, MasterSelectButton_GetMaster, MasterSelectButton_GenerateMenu, MasterString(pawn).Truncate(rect.width), null, MasterString(pawn), null, null, paintable);
	}

	private static Pawn MasterSelectButton_GetMaster(Pawn pet)
	{
		return pet.playerSettings.Master;
	}

	private static IEnumerable<Widgets.DropdownMenuElement<Pawn>> MasterSelectButton_GenerateMenu(Pawn p)
	{
		yield return new Widgets.DropdownMenuElement<Pawn>
		{
			option = new FloatMenuOption("(" + "NoneLower".Translate() + ")", delegate
			{
				p.playerSettings.Master = null;
			}),
			payload = null
		};
		foreach (Pawn col in PawnsFinder.AllMaps_FreeColonistsSpawned)
		{
			string text = RelationsUtility.LabelWithBondInfo(col, p);
			Action action = null;
			if (CanBeMaster(col, p))
			{
				action = delegate
				{
					p.playerSettings.Master = col;
				};
			}
			else
			{
				int level = col.skills.GetSkill(SkillDefOf.Animals).Level;
				int num = MinimumHandlingSkill(p);
				if (level < num)
				{
					action = null;
					text += " (" + "SkillTooLow".Translate(SkillDefOf.Animals.LabelCap, level, num) + ")";
				}
			}
			yield return new Widgets.DropdownMenuElement<Pawn>
			{
				option = new FloatMenuOption(text, action),
				payload = col
			};
		}
	}

	public static bool CanBeMaster(Pawn master, Pawn animal, bool checkSpawned = true)
	{
		if ((checkSpawned && !master.Spawned) || master.IsPrisoner)
		{
			return false;
		}
		if (ModsConfig.IdeologyActive && animal.RaceProps.animalType == AnimalType.Dryad && animal.connections != null)
		{
			foreach (Thing connectedThing in animal.connections.ConnectedThings)
			{
				CompTreeConnection compTreeConnection = connectedThing.TryGetComp<CompTreeConnection>();
				if (compTreeConnection != null && compTreeConnection.ConnectedPawn == master)
				{
					return true;
				}
			}
			return false;
		}
		if (master.relations.DirectRelationExists(PawnRelationDefOf.Bond, animal))
		{
			return true;
		}
		int level = master.skills.GetSkill(SkillDefOf.Animals).Level;
		int num = MinimumHandlingSkill(animal);
		return level >= num;
	}

	public static string GetIconTooltipText(Pawn pawn)
	{
		string text = "";
		if (pawn.playerSettings != null && pawn.playerSettings.Master != null)
		{
			text += string.Format("{0}: {1}\n", "Master".Translate(), pawn.playerSettings.Master.LabelShort);
		}
		IEnumerable<Pawn> allColonistBondsFor = GetAllColonistBondsFor(pawn);
		if (allColonistBondsFor.Any())
		{
			text += string.Format("{0}: {1}\n", "BondedTo".Translate(), allColonistBondsFor.Select((Pawn bond) => bond.LabelShort).ToCommaList(useAnd: true));
		}
		return text.TrimEndNewlines();
	}

	public static IEnumerable<Pawn> GetAllColonistBondsFor(Pawn pet)
	{
		return from bond in pet.relations.DirectRelations
			where bond.def == PawnRelationDefOf.Bond && bond.otherPawn != null && bond.otherPawn.IsColonist
			select bond.otherPawn;
	}

	public static int DegradationPeriodTicks(ThingDef def)
	{
		return Mathf.RoundToInt(DecayIntervalDaysFromWildnessCurve.Evaluate(def.race.wildness) * 60000f);
	}

	public static bool TamenessCanDecay(ThingDef def)
	{
		if (def.race.FenceBlocked)
		{
			return false;
		}
		return def.race.wildness > 0.101f;
	}

	public static bool TrainedTooRecently(Pawn animal)
	{
		return Find.TickManager.TicksGame < animal.mindState.lastAssignedInteractTime + 15000;
	}

	public static string GetWildnessExplanation(ThingDef def)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("WildnessExplanation".Translate());
		stringBuilder.AppendLine();
		if (def.race != null && !def.race.Humanlike)
		{
			stringBuilder.AppendLine(string.Format("{0}: {1}", "TrainingDecayInterval".Translate(), DegradationPeriodTicks(def).ToStringTicksToDays()));
			stringBuilder.AppendLine();
		}
		if (!TamenessCanDecay(def))
		{
			string key = (def.race.FenceBlocked ? "TamenessWillNotDecayFenceBlocked" : "TamenessWillNotDecay");
			stringBuilder.AppendLine(key.Translate());
		}
		return stringBuilder.ToString();
	}

	public static bool IsReleasedToAttack(this Pawn pawn)
	{
		if (pawn?.playerSettings?.Master?.playerSettings == null || pawn.training == null)
		{
			return false;
		}
		if (pawn.playerSettings.Master.playerSettings.animalsReleased)
		{
			return pawn.training.HasLearned(TrainableDefOf.Release);
		}
		return false;
	}
}
