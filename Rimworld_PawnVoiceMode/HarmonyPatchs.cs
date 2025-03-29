using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Verse.AI;
using System.Text;
using Verse;
using static RimWorld.ColonistBar;

namespace PawnVoice;

[HarmonyPatch(typeof(LessonAutoActivator), nameof(LessonAutoActivator.TeachOpportunity), [typeof(ConceptDef), typeof(OpportunityType)])]
public static class LessonAutoActivator_TeachOpportunity
{
	public static void Postfix(ConceptDef conc, OpportunityType opp)
	{
		// 유저가 Draft를 직접 켜는 시점을 잡고 싶은데 잡을 시점이 없어서 어울리지 않는 함수지만 이걸로 잡는다
		if (conc == ConceptDefOf.QueueOrders && opp == OpportunityType.GoodToKnow)
		{
			//PawnVoice.TriggerEvent(VoiceEventEnum.Draft);
		}
	}
}

public static class Pawn_DraftController_GetGizmos_Patch
{
	private static readonly FieldInfo pawnField = AccessTools.Field(typeof(Pawn_DraftController), nameof(Pawn_DraftController.pawn));

	public static void Patch(Harmony harmony)
	{
		var baseType = typeof(Pawn_DraftController);
		var methodBody = PatchProcessor.ReadMethodBody(AccessTools.Method(baseType, "GetGizmos"));

		string innerTypeName = null;
		foreach (var (opCode, obj) in methodBody)
		{
			if (opCode == OpCodes.Stfld)
			{
				var fieldInfo = obj as FieldInfo;
				if (fieldInfo == null)
				{
					continue;
				}
				if (fieldInfo.DeclaringType.Name.StartsWith("<GetGizmos>"))
				{
					innerTypeName = fieldInfo.DeclaringType.Name;
				}
			}
		}

		if (innerTypeName == null)
		{
			Log.Warning("[Patch] 상태머신 타입을 찾을 수 없습니다");
			return;
		}

		var innerType = baseType.GetNestedType(innerTypeName, BindingFlags.NonPublic);

		if (innerType == null)
		{
			Log.Warning($"[Patch] 상태머신 타입을 찾을 수 없습니다: {innerTypeName}");
			return;
		}

		var moveNextMethod = innerType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);

		var moveNextBody = PatchProcessor.ReadMethodBody(moveNextMethod);
		string toggleActionDelegateName = null;
		// 처음 나오는 toggleAction의 delegate가 Draft를 켜고 끄는 토글 함수로 가정한다. 이 순서까지 바꾸는 모드가 있으면 이 코드는 작동하지 않는다
		foreach (var (opCode, obj) in moveNextBody)
		{
			if (opCode == OpCodes.Stfld)
			{
				if (obj is FieldInfo fieldInfo && fieldInfo.Name == "toggleAction")
				{
					break;
				}
			}
			else if (opCode == OpCodes.Ldftn)
			{
				if (obj is MethodBase member && member.Name.StartsWith("<GetGizmos>"))
				{
					toggleActionDelegateName = member.Name;
				}
			}
		}

		if (toggleActionDelegateName == null)
		{
			Log.Warning("[Patch] toggleAction 대리자를 찾을 수 없습니다");
			return;
		}

		var method = baseType.GetMethod(toggleActionDelegateName, BindingFlags.Instance | BindingFlags.NonPublic);

		if (method == null)
		{
			Log.Warning($"toggleAction 대상 메서드를 찾을 수 없습니다: {toggleActionDelegateName}");
			return;
		}

		harmony.Patch(method,
			transpiler: new HarmonyMethod(typeof(Pawn_DraftController_GetGizmos_Patch), nameof(GizmoTranspiler)));
	}

	static IEnumerable<CodeInstruction> GizmoTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = new List<CodeInstruction>(instructions);
		var targetMethod = AccessTools.Method(typeof(LessonAutoActivator), nameof(LessonAutoActivator.TeachOpportunity), [typeof(ConceptDef), typeof(OpportunityType)]);
		var injectMethod = AccessTools.Method(typeof(PawnVoiceEventTrigger), nameof(PawnVoiceEventTrigger.TriggerEvent));

		for (int i = 0; i < codes.Count; i++)
		{
			yield return codes[i];

			if (codes[i].Calls(targetMethod))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, pawnField);
				yield return new CodeInstruction(OpCodes.Ldc_I4, (int)VoiceEventEnum.Draft);
				yield return new CodeInstruction(OpCodes.Call, injectMethod);
			}
		}
	}
}

[HarmonyPatch(typeof(Toils_Goto), nameof(Toils_Goto.GotoCell), [typeof(TargetIndex), typeof(PathEndMode)])]
public static class Toils_Goto_GotoCell1
{
	public static void Postfix(TargetIndex ind, PathEndMode peMode)
	{
		// 여러마리 이동
		// MultiPawnGotoController.IssueGotoJobs


		// 한마리 이동
		//FloatMenuMakerMap.GotoLocationOption
	}
}