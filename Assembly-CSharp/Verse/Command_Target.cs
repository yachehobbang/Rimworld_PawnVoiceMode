using System;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace Verse;

public class Command_Target : Command
{
	public Action<LocalTargetInfo> action;

	public TargetingParameters targetingParams;

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
		Find.DesignatorManager.Deselect();
		Find.Targeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target)
		{
			action(target);
		});
	}

	public override bool InheritInteractionsFrom(Gizmo other)
	{
		return false;
	}
}
