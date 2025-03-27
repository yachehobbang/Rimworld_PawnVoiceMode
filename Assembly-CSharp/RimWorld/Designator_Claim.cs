using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Designator_Claim : Designator
{
	private static StringBuilder _tmpBuilder = new StringBuilder();

	public override int DraggableDimensions => 2;

	public Designator_Claim()
	{
		defaultLabel = "DesignatorClaim".Translate();
		defaultDesc = "DesignatorClaimDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/Claim");
		soundDragSustain = SoundDefOf.Designate_DragStandard;
		soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
		useMouseIcon = true;
		soundSucceeded = SoundDefOf.Designate_Claim;
		hotKey = KeyBindingDefOf.Misc4;
		showReverseDesignatorDisabledReason = true;
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (!c.InBounds(base.Map))
		{
			return false;
		}
		if (c.Fogged(base.Map))
		{
			return false;
		}
		if (!(from t in c.GetThingList(base.Map)
			where CanDesignateThing(t).Accepted
			select t).Any())
		{
			return "MessageMustDesignateClaimable".Translate();
		}
		return true;
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (CanDesignateThing(thingList[i]).Accepted)
			{
				DesignateThing(thingList[i]);
			}
		}
	}

	public override AcceptanceReport CanDesignateThing(Thing t)
	{
		_tmpBuilder.Clear();
		if (t.Faction != Faction.OfPlayer && t.ClaimableBy(Faction.OfPlayer, _tmpBuilder))
		{
			return true;
		}
		return _tmpBuilder.ToString();
	}

	public override void DesignateThing(Thing t)
	{
		t.SetFaction(Faction.OfPlayer);
		foreach (IntVec3 item in t.OccupiedRect())
		{
			FleckMaker.ThrowMetaPuffs(new TargetInfo(item, base.Map));
		}
	}
}
