using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorld;

public class MinifiedTree : MinifiedThing
{
	private int ticksTillDeath;

	public const int InitialTicksTillDeath = 420000;

	public const float DyingYieldPercentage = 0.5f;

	public int TicksTillDeath => ticksTillDeath;

	public Plant InnerTree => (Plant)base.InnerThing;

	public override Graphic Graphic
	{
		get
		{
			if (cachedGraphic == null)
			{
				cachedGraphic = base.InnerThing.Graphic.ExtractInnerGraphicFor(base.InnerThing, null);
				Vector2 minifiedDrawSize = GetMinifiedDrawSize(base.InnerThing.def.size.ToVector2(), 1.1f);
				Vector2 newDrawSize = new Vector2(minifiedDrawSize.x / (float)base.InnerThing.def.size.x * cachedGraphic.drawSize.x, minifiedDrawSize.y / (float)base.InnerThing.def.size.z * cachedGraphic.drawSize.y);
				cachedGraphic = cachedGraphic.GetCopy(newDrawSize, ShaderTypeDefOf.Cutout.Shader);
			}
			return cachedGraphic;
		}
	}

	protected override Graphic LoadCrateFrontGraphic()
	{
		return GraphicDatabase.Get<Graphic_Single>("Things/Item/Minified/BurlapBag", ShaderDatabase.Cutout, GetMinifiedDrawSize(base.InnerThing.def.size.ToVector2(), 1.1f) * 1.16f, Color.white);
	}

	private void RecordTreeDeath(LookTargets lookTargets)
	{
		if (base.InnerThing.def.plant.treeLoversCareIfChopped)
		{
			foreach (Ideo allIdeo in Faction.OfPlayer.ideos.AllIdeos)
			{
				if (allIdeo.WarnPlayerOnDesignateChopTree)
				{
					Messages.Message("MessageMinifiedTreeDied".Translate(), lookTargets, MessageTypeDefOf.NegativeEvent);
					break;
				}
			}
		}
		Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.MinifiedTreeDied, new SignalArgs(base.InnerThing.Named(HistoryEventArgsNames.Victim))));
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if (base.InnerThing == null)
		{
			base.Destroy(mode);
			return;
		}
		Caravan anyParent = ThingOwnerUtility.GetAnyParent<Caravan>(this);
		ActiveDropPodInfo activeDropPodInfo = base.ParentHolder as ActiveDropPodInfo;
		RecordTreeDeath((anyParent == null) ? new LookTargets(base.PositionHeld, base.MapHeld) : ((LookTargets)anyParent));
		ThingDef harvestedThingDef = base.InnerThing.def.plant.harvestedThingDef;
		int num = (int)((float)((Plant)base.InnerThing).YieldNow() * 0.5f);
		List<Thing> list = new List<Thing>();
		while (num > 0)
		{
			int num2 = Mathf.Min(num, harvestedThingDef.stackLimit);
			Thing thing = ThingMaker.MakeThing(harvestedThingDef);
			thing.stackCount = num2;
			list.Add(thing);
			num -= num2;
		}
		IntVec3 center = ((anyParent == null) ? base.PositionHeld : IntVec3.Invalid);
		Map map = ((anyParent == null) ? base.MapHeld : null);
		if (base.ParentHolder != null)
		{
			base.ParentHolder.GetDirectlyHeldThings().Remove(this);
		}
		base.Destroy(mode);
		if (anyParent != null)
		{
			foreach (Thing item in list)
			{
				anyParent.AddPawnOrItem(item, addCarriedPawnToWorldPawnsIfAny: true);
			}
			return;
		}
		if (activeDropPodInfo != null)
		{
			foreach (Thing item2 in list)
			{
				activeDropPodInfo.innerContainer.TryAdd(item2);
			}
			return;
		}
		if (map == null)
		{
			return;
		}
		foreach (Thing item3 in list)
		{
			GenPlace.TryPlaceThing(item3, center, map, ThingPlaceMode.Near);
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		ticksTillDeath = 420000;
	}

	public override void Tick()
	{
		base.Tick();
		ticksTillDeath--;
		if (ticksTillDeath <= 0)
		{
			Destroy();
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "DEV: Destroy";
			command_Action.action = delegate
			{
				Destroy();
			};
			yield return command_Action;
			Command_Action command_Action2 = new Command_Action();
			command_Action2.defaultLabel = "DEV: Die in 1 hour";
			command_Action2.action = delegate
			{
				ticksTillDeath = 2500;
			};
			yield return command_Action2;
			Command_Action command_Action3 = new Command_Action();
			command_Action3.defaultLabel = "DEV: Die in 1 day";
			command_Action3.action = delegate
			{
				ticksTillDeath = 60000;
			};
			yield return command_Action3;
		}
	}

	public override void Notify_MyMapRemoved()
	{
		base.Notify_MyMapRemoved();
		RecordTreeDeath(null);
	}

	public override string GetInspectString()
	{
		return "MinifiedTreeWillDieIn".Translate(ticksTillDeath.ToStringTicksToPeriod().Named("time"));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksTillDeath, "ticksTillDeath", 0);
	}
}
