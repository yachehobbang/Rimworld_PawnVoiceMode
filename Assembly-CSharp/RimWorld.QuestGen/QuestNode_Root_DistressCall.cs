using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_DistressCall : QuestNode
{
	private const string SitePartTag = "DistressCall";

	private const int MaxDistanceFromColony = 9;

	private const int MinDistanceFromColony = 3;

	private const float MinPoints = 100f;

	private const int TimeoutTicks = 900000;

	private const float EmpireSitePointsThreshold = 2000f;

	private const float AmbushChance = 0.75f;

	private static readonly IntRange AmbushDelayTicks = new IntRange(2400, 4800);

	private static readonly SimpleCurve AmbushPointsCurve = new SimpleCurve
	{
		new CurvePoint(100f, 100f),
		new CurvePoint(1000f, 400f),
		new CurvePoint(5000f, 1000f)
	};

	protected override bool TestRunInt(Slate slate)
	{
		if (!Find.Storyteller.difficulty.allowViolentQuests)
		{
			return false;
		}
		QuestGenUtility.TestRunAdjustPointsForDistantFight(slate);
		float num = slate.Get("points", 0f);
		if (num < 100f)
		{
			return false;
		}
		Faction faction;
		if (TryFindSiteTile(out var _))
		{
			return TryFindFaction(num, out faction);
		}
		return false;
	}

	protected override void RunInt()
	{
		Quest quest = QuestGen.quest;
		Slate slate = QuestGen.slate;
		float num = slate.Get("points", 0f);
		if (num < 100f)
		{
			num = 100f;
		}
		TryFindSiteTile(out var tile);
		TryFindFaction(num, out var faction);
		slate.Set("faction", faction);
		IEnumerable<SitePartDef> source = DefDatabase<SitePartDef>.AllDefs.Where((SitePartDef def) => def.tags != null && def.tags.Contains("DistressCall") && typeof(SitePartWorker_DistressCall).IsAssignableFrom(def.workerClass));
		Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[1]
		{
			new SitePartDefWithParams(source.RandomElementByWeight((SitePartDef sp) => sp.selectionWeight), new SitePartParams
			{
				threatPoints = num
			})
		}, tile, faction);
		quest.SpawnWorldObject(site);
		slate.Set("site", site);
		string inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
		string text = QuestGenUtility.HardcodedSignalWithQuestID("site.AllEnemiesDefeated");
		string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
		string signalAmbush = QuestGenUtility.HardcodedSignalWithQuestID("ambush");
		quest.Letter(LetterDefOf.NeutralEvent, null, null, label: "DistressSignalLabel".Translate(), text: "DistressSignalText".Translate(site.Faction.Named("FACTION")).Resolve(), lookTargets: Gen.YieldSingle(site), relatedFaction: site.Faction);
		QuestPart_Choice questPart_Choice = quest.RewardChoice();
		QuestPart_Choice.Choice item = new QuestPart_Choice.Choice
		{
			rewards = { (Reward)new Reward_CampLoot() }
		};
		questPart_Choice.choices.Add(item);
		if (Rand.Chance(0.75f))
		{
			quest.Delay(AmbushDelayTicks.RandomInRange, delegate
			{
				quest.SignalPass(null, null, signalAmbush);
			}, inSignalEnable);
			quest.AddPart(new QuestPart_DistressCallAmbush(signalAmbush, site, AmbushPointsCurve.Evaluate(num)));
		}
		quest.WorldObjectTimeout(site, 900000);
		quest.Delay(900000, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		});
		Quest quest3 = quest;
		string inSignal3 = text;
		quest3.Notify_PlayerRaidedSomeone(null, site, inSignal3);
		quest.End(QuestEndOutcome.Success, 0, null, inSignal);
	}

	private bool TryFindFaction(float points, out Faction faction)
	{
		return Find.FactionManager.AllFactionsListForReading.Where((Faction f) => FactionUsable(f, points)).TryRandomElement(out faction);
	}

	private bool FactionUsable(Faction f, float points)
	{
		if (ModsConfig.RoyaltyActive && points < 2000f && f == Faction.OfEmpire)
		{
			return false;
		}
		if (f.def.humanlikeFaction && !f.def.pawnGroupMakers.NullOrEmpty())
		{
			return !f.def.permanentEnemy;
		}
		return false;
	}

	private bool TryFindSiteTile(out int tile)
	{
		return TileFinder.TryFindNewSiteTile(out tile, 3, 9);
	}
}
