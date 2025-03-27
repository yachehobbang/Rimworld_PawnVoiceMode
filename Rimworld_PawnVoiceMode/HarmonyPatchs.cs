using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PawnVoice
{
	[HarmonyPatch(typeof(LessonAutoActivator), nameof(LessonAutoActivator.TeachOpportunity), new[] { typeof(ConceptDef), typeof(OpportunityType) })]
	public static class LessonAutoActivator_TeachOpportunity
	{
		public static void Postfix(ConceptDef conc, OpportunityType opp)
		{
			// 유저가 Draft를 직접 켜는 시점을 잡고 싶은데 잡을 시점이 없어서 어울리지 않는 함수지만 이걸로 잡는다
			if (conc == ConceptDefOf.QueueOrders && opp == OpportunityType.GoodToKnow)
			{
				PawnVoice.Test(VoiceTypeEnum.Draft);
			}
		}
	}

	[HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoCell), new[] { typeof(TargetIndex), typeof(PathEndMode) })]
	public static class Toils_Goto_GotoCell1
	{
		public static void Postfix(TargetIndex ind, PathEndMode peMode)
		{
			// 여러마리 이동
			// MultiPawnGotoController.IssueGotoJobs


			// 한마리 이동
			//FloatMenuMakerMap.GotoLocationOption
			Mod.Log("Toils_Goto_GotoCell1");
		}
	}

	[HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoCell), new[] { typeof(IntVec3), typeof(PathEndMode) })]
	public static class Toils_Goto_GotoCell2
	{
		public static void Postfix(IntVec3 cell, PathEndMode peMode)
		{
			Mod.Log("Toils_Goto_GotoCell2");
		}
	}
}