using Verse;

namespace RimWorld;

public class RoleEffect_NoRangedWeapons : RoleEffect
{
	public override bool IsBad => true;

	public RoleEffect_NoRangedWeapons()
	{
		labelKey = "RoleEffectWontUseRangedWeapons";
	}

	public override bool CanEquip(Pawn pawn, Thing thing)
	{
		return !thing.def.IsRangedWeapon;
	}
}
