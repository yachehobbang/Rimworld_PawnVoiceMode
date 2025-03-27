using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public abstract class AbilityComp
{
	public Ability parent;

	public AbilityCompProperties props;

	public virtual bool CanCast => true;

	public virtual void Initialize(AbilityCompProperties props)
	{
		this.props = props;
	}

	public override string ToString()
	{
		return string.Concat(GetType().Name, "(parent=", parent, " at=", (parent != null) ? parent.pawn.Position : IntVec3.Invalid, ")");
	}

	public virtual bool GizmoDisabled(out string reason)
	{
		reason = null;
		return false;
	}

	public virtual float PsyfocusCostForTarget(LocalTargetInfo target)
	{
		return 0f;
	}

	public virtual void CompTick()
	{
	}

	public virtual IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		return Enumerable.Empty<Gizmo>();
	}

	public virtual string CompInspectStringExtra()
	{
		return null;
	}

	public virtual void PostExposeData()
	{
	}
}
