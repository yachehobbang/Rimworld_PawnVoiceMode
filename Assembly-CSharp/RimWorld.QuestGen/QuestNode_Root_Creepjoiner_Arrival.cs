using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_Creepjoiner_Arrival : QuestNode
{
	private const int TimeoutTicks = 60000;

	protected override void RunInt()
	{
		Quest quest = QuestGen.quest;
		Slate slate = QuestGen.slate;
		Pawn pawn = SpawnPawn(quest.points);
		Pawn_CreepJoinerTracker creepjoiner = pawn.creepjoiner;
		slate.Set("pawn", pawn);
		SendLetter(pawn);
		string text = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
		string text2 = QuestGenUtility.HardcodedSignalWithQuestID("Reject");
		string text3 = QuestGenUtility.HardcodedSignalWithQuestID("Capture");
		string text4 = QuestGenUtility.HardcodedSignalWithQuestID("SpokeTo");
		string text5 = QuestGenUtility.HardcodedSignalWithQuestID("Timeout");
		string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Killed");
		string text6 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.TookDamageFromPlayer");
		string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.LeftMap");
		string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Recruited");
		creepjoiner.quest = quest;
		creepjoiner.spokeToSignal = text4;
		creepjoiner.timeoutAt = GenTicks.TicksAbs + 60000;
		QuestPart_CreepJoinerOutcomes part = new QuestPart_CreepJoinerOutcomes
		{
			pawn = pawn,
			timeout = 60000,
			signalAccept = text,
			signalReject = text2,
			signalCapture = text3,
			signalAttacked = text6,
			signalShow = text4,
			signalTimeout = text5,
			signalListenMode = QuestPart.SignalListenMode.Always
		};
		quest.AddPart(part);
		quest.Signal(text, delegate
		{
			quest.SetFaction(Gen.YieldSingle(pawn), Faction.OfPlayer);
			QuestGen_End.End(quest, QuestEndOutcome.Success);
		});
		quest.Signal(text2, delegate
		{
			quest.GiveDiedOrDownedThoughts(pawn, PawnDiedOrDownedThoughtsKind.DeniedJoining);
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		});
		quest.Signal(text3, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		});
		quest.Signal(text6, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		});
		quest.Signal(inSignal, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		});
		quest.Signal(inSignal2, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		});
		quest.Signal(inSignal3, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		});
		quest.Delay(60000, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		}, null, null, text5);
	}

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	private Pawn SpawnPawn(float combatPoints = 0f)
	{
		Slate slate = QuestGen.slate;
		if (slate.TryGet<CreepJoinerFormKindDef>("form", out var var) && slate.TryGet<CreepJoinerBenefitDef>("benefit", out var var2) && slate.TryGet<CreepJoinerDownsideDef>("downside", out var var3) && slate.TryGet<CreepJoinerAggressiveDef>("aggressive", out var var4) && slate.TryGet<CreepJoinerRejectionDef>("rejection", out var var5))
		{
			return CreepJoinerUtility.GenerateAndSpawn(var, var2, var3, var4, var5, QuestGen_Get.GetMap(mustBeInfestable: false, null));
		}
		return CreepJoinerUtility.GenerateAndSpawn(QuestGen_Get.GetMap(mustBeInfestable: false, null), combatPoints);
	}

	private void SendLetter(Pawn pawn)
	{
		string text = pawn.GetKindLabelSingular().CapitalizeFirst();
		TaggedString taggedString = pawn.creepjoiner.form.letterLabel.Formatted(pawn.Named("PAWN"));
		ChoiceLetter let = LetterMaker.MakeLetter(text: taggedString + ("\n\n" + "LetterCreeperAppearedAppended".Translate(pawn.Named("PAWN")).CapitalizeFirst()), label: text, def: LetterDefOf.NeutralEvent, lookTargets: pawn);
		Find.LetterStack.ReceiveLetter(let);
	}
}
