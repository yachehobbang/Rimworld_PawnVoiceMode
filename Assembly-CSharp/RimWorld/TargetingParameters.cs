using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class TargetingParameters
{
	public bool canTargetLocations;

	public bool canTargetSelf;

	public bool canTargetPawns = true;

	public bool canTargetFires;

	public bool canTargetBuildings = true;

	public bool canTargetItems;

	public bool canTargetAnimals = true;

	public bool canTargetHumans = true;

	public bool canTargetMechs = true;

	public bool canTargetPlants;

	public bool canTargetMutants = true;

	public List<Faction> onlyTargetFactions;

	public Predicate<TargetInfo> validator;

	public bool onlyTargetFlammables;

	public Thing targetSpecificThing;

	public bool mustBeSelectable;

	public bool neverTargetDoors;

	public bool neverTargetIncapacitated;

	public bool neverTargetHostileFaction;

	public bool onlyTargetSameIdeo;

	public bool onlyTargetThingsAffectingRegions;

	public bool onlyTargetDamagedThings;

	public bool mapObjectTargetsMustBeAutoAttackable = true;

	public bool onlyTargetIncapacitatedPawns;

	public bool onlyTargetColonistsOrPrisoners;

	public bool onlyTargetColonistsOrPrisonersOrSlaves;

	public bool onlyTargetColonistsOrPrisonersOrSlavesAllowMinorMentalBreaks;

	public bool onlyTargetControlledPawns;

	public bool onlyTargetColonists;

	public bool onlyTargetPrisonersOfColony;

	public bool onlyTargetPsychicSensitive;

	public bool onlyTargetAnimaTrees;

	public bool canTargetBloodfeeders = true;

	public bool onlyRepairableMechs;

	public ThingCategory thingCategory;

	public bool onlyTargetDoors;

	public bool canTargetCorpses;

	public bool onlyTargetCorpses;

	public int mapBoundsContractedBy;

	public bool CanTarget(TargetInfo targ, ITargetingSource source = null)
	{
		if (validator != null && !validator(targ))
		{
			return false;
		}
		if (targ.Thing == null)
		{
			return canTargetLocations;
		}
		if (neverTargetDoors && targ.Thing.def.IsDoor)
		{
			return false;
		}
		if (onlyTargetDamagedThings && targ.Thing.HitPoints == targ.Thing.MaxHitPoints)
		{
			return false;
		}
		if (onlyTargetFlammables && !targ.Thing.FlammableNow)
		{
			return false;
		}
		if (mustBeSelectable && !ThingSelectionUtility.SelectableByMapClick(targ.Thing))
		{
			return false;
		}
		if (onlyTargetColonistsOrPrisoners && targ.Thing.def.category != ThingCategory.Pawn)
		{
			return false;
		}
		if (onlyTargetColonistsOrPrisonersOrSlaves && targ.Thing.def.category != ThingCategory.Pawn)
		{
			return false;
		}
		if (onlyTargetDoors && !targ.Thing.def.IsDoor)
		{
			return false;
		}
		Corpse corpse = (targ.Thing as Corpse) ?? (targ.Thing as Pawn)?.Corpse;
		if (canTargetCorpses && corpse != null)
		{
			if (!canTargetMechs && corpse.InnerPawn.RaceProps.IsMechanoid)
			{
				return false;
			}
			if (!canTargetAnimals && corpse.InnerPawn.RaceProps.Animal)
			{
				return false;
			}
			if (!canTargetHumans && corpse.InnerPawn.RaceProps.Humanlike)
			{
				return false;
			}
			if (!canTargetMutants && corpse.InnerPawn.IsMutant)
			{
				return false;
			}
			return true;
		}
		if (onlyTargetCorpses)
		{
			return false;
		}
		if (targetSpecificThing != null && targ.Thing == targetSpecificThing)
		{
			return true;
		}
		if (canTargetFires && targ.Thing.def == ThingDefOf.Fire)
		{
			return true;
		}
		if (canTargetPawns && targ.Thing.def.category == ThingCategory.Pawn)
		{
			Pawn pawn = (Pawn)targ.Thing;
			if (pawn.Downed)
			{
				if (neverTargetIncapacitated)
				{
					return false;
				}
			}
			else if (onlyTargetIncapacitatedPawns)
			{
				return false;
			}
			if (onlyTargetFactions != null && !onlyTargetFactions.Contains(targ.Thing.Faction))
			{
				return false;
			}
			if (pawn.NonHumanlikeOrWildMan())
			{
				if (pawn.Faction != null && pawn.RaceProps.IsMechanoid)
				{
					if (!canTargetMechs)
					{
						return false;
					}
					if (onlyRepairableMechs && !MechRepairUtility.CanRepair(pawn))
					{
						return false;
					}
				}
				else if (!canTargetAnimals)
				{
					return false;
				}
			}
			if (!pawn.NonHumanlikeOrWildMan() && !canTargetHumans)
			{
				return false;
			}
			if (!canTargetMutants && pawn.IsMutant)
			{
				return false;
			}
			if (onlyTargetControlledPawns && !pawn.IsColonistPlayerControlled)
			{
				return false;
			}
			if (onlyTargetColonists && (!pawn.IsColonist || pawn.HostFaction != null))
			{
				return false;
			}
			if (onlyTargetPrisonersOfColony && !pawn.IsPrisonerOfColony)
			{
				return false;
			}
			if (onlyTargetColonistsOrPrisoners && !pawn.IsColonistPlayerControlled && !pawn.IsPrisonerOfColony)
			{
				return false;
			}
			if (onlyTargetColonistsOrPrisonersOrSlaves && !pawn.IsColonistPlayerControlled && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
			{
				return false;
			}
			if (onlyTargetColonistsOrPrisonersOrSlavesAllowMinorMentalBreaks)
			{
				if (!pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony && (!pawn.IsColonist || (pawn.HostFaction != null && !pawn.IsSlave)))
				{
					return false;
				}
				MentalStateDef mentalStateDef = pawn.MentalStateDef;
				if (mentalStateDef != null && mentalStateDef.IsAggro)
				{
					return false;
				}
			}
			if (onlyTargetPsychicSensitive && pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= 0f)
			{
				return false;
			}
			if (neverTargetHostileFaction && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
			{
				Faction homeFaction = pawn.HomeFaction;
				if (homeFaction != null && homeFaction.HostileTo(Faction.OfPlayer))
				{
					return false;
				}
			}
			if (onlyTargetSameIdeo)
			{
				if (source == null)
				{
					Log.Error("Source passed in is null but targeting parameters have onlyTargetSameIdeo set.");
				}
				else if (source is Verb { CasterPawn: not null } verb)
				{
					Ideo ideo = ((targ.Thing is Pawn pawn2) ? pawn2.Ideo : null);
					if (verb.CasterPawn.Ideo != ideo)
					{
						return false;
					}
				}
				else
				{
					Log.Error("Source passed in is incompatible type but targeting parameters have onlyTargetSameIdeo set.");
				}
			}
			if (!canTargetBloodfeeders && ModsConfig.BiotechActive && pawn.IsBloodfeeder())
			{
				return false;
			}
			return true;
		}
		if (canTargetBuildings && targ.Thing.def.category == ThingCategory.Building)
		{
			if (mapObjectTargetsMustBeAutoAttackable && !targ.Thing.def.building.isTargetable)
			{
				return false;
			}
			if (onlyTargetThingsAffectingRegions && !targ.Thing.def.AffectsRegions)
			{
				return false;
			}
			if (onlyTargetFactions != null && !onlyTargetFactions.Contains(targ.Thing.Faction))
			{
				return false;
			}
			return true;
		}
		if (canTargetPlants && targ.Thing.def.category == ThingCategory.Plant)
		{
			if (ModsConfig.RoyaltyActive && onlyTargetAnimaTrees && targ.Thing.def != ThingDefOf.Plant_TreeAnima)
			{
				return false;
			}
			return true;
		}
		if (canTargetItems)
		{
			if (mapObjectTargetsMustBeAutoAttackable && !targ.Thing.def.isAutoAttackableMapObject)
			{
				return false;
			}
			if (thingCategory == ThingCategory.None || thingCategory == targ.Thing.def.category)
			{
				return true;
			}
		}
		return false;
	}

	public static TargetingParameters ForSelf(Pawn p)
	{
		return new TargetingParameters
		{
			targetSpecificThing = p,
			canTargetPawns = false,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false
		};
	}

	public static TargetingParameters ForArrest(Pawn arrester)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate(TargetInfo targ)
			{
				if (!targ.HasThing)
				{
					return false;
				}
				if (!(targ.Thing is Pawn pawn) || pawn == arrester || !pawn.CanBeArrestedBy(arrester))
				{
					return false;
				}
				return (!pawn.Downed || !pawn.guilt.IsGuilty) ? true : false;
			}
		};
	}

	public static TargetingParameters ForAttackHostile()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = true,
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = true,
			validator = delegate(TargetInfo targ)
			{
				if (!targ.HasThing)
				{
					return false;
				}
				if (targ.Thing.def.IsNonDeconstructibleAttackableBuilding)
				{
					return true;
				}
				BuildingProperties building = targ.Thing.def.building;
				if (building != null && building.quickTargetable)
				{
					return true;
				}
				if (targ.Thing.HostileTo(Faction.OfPlayer))
				{
					return true;
				}
				return (targ.Thing is Pawn p && p.NonHumanlikeOrWildMan()) ? true : false;
			}
		};
	}

	public static TargetingParameters ForAttackAny()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = true,
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = true
		};
	}

	public static TargetingParameters ForRescue(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			onlyTargetIncapacitatedPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false
		};
	}

	public static TargetingParameters ForStrip(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo targ) => targ.HasThing && StrippableUtility.CanBeStrippedByColony(targ.Thing)
		};
	}

	public static TargetingParameters ForCarry(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetMechs = true,
			canTargetBuildings = false,
			onlyTargetIncapacitatedPawns = true
		};
	}

	public static TargetingParameters ForDraftedCarryBed(Pawn sleeper, Pawn carrier, GuestStatus? guestStatus = null)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			canTargetItems = false,
			canTargetBuildings = true,
			validator = (TargetInfo target) => target.HasThing && RestUtility.IsValidBedFor(target.Thing, sleeper, carrier, checkSocialProperness: false, allowMedBedEvenIfSetToNoCare: true, ignoreOtherReservations: true, guestStatus)
		};
	}

	public static TargetingParameters ForDraftedCarryTransporter(Pawn carriedPawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			canTargetItems = true,
			canTargetBuildings = true,
			validator = (TargetInfo target) => target.HasThing && target.Thing.TryGetComp<CompTransporter>() != null
		};
	}

	public static TargetingParameters ForDraftedCarryCryptosleepCasket(Pawn carrier)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			canTargetItems = true,
			canTargetBuildings = true,
			validator = (TargetInfo target) => target.HasThing && target.Thing.def.IsCryptosleepCasket
		};
	}

	public static TargetingParameters ForTrade()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo x) => x.Thing is ITrader trader && trader.CanTradeNow
		};
	}

	public static TargetingParameters ForDropPodsDestination()
	{
		return new TargetingParameters
		{
			canTargetLocations = true,
			canTargetSelf = false,
			canTargetPawns = false,
			canTargetFires = false,
			canTargetBuildings = false,
			canTargetItems = false,
			validator = (TargetInfo x) => DropCellFinder.IsGoodDropSpot(x.Cell, x.Map, allowFogged: false, canRoofPunch: true)
		};
	}

	public static TargetingParameters ForQuestPawnsWhoWillJoinColony(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo x) => x.Thing is Pawn { Dead: false } pawn && pawn.mindState.WillJoinColonyIfRescued
		};
	}

	public static TargetingParameters ForOpen(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			canTargetBuildings = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo x) => x.Thing is IOpenable openable && openable.CanOpen
		};
	}

	public static TargetingParameters ForShuttle(Pawn hauler)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = delegate(TargetInfo targ)
			{
				if (!targ.HasThing)
				{
					return false;
				}
				if (!(targ.Thing is Pawn { Dead: false } pawn) || pawn == hauler)
				{
					return false;
				}
				if (pawn.Downed)
				{
					return true;
				}
				return pawn.IsPrisonerOfColony ? pawn.guest.PrisonerIsSecure : pawn.AnimalOrWildMan();
			}
		};
	}

	public static TargetingParameters ForBuilding(ThingDef def = null)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			canTargetItems = false,
			canTargetBuildings = true,
			validator = delegate(TargetInfo targ)
			{
				if (!targ.HasThing)
				{
					return false;
				}
				return def == null || targ.Thing.def == def;
			}
		};
	}

	public static TargetingParameters ForTend(Pawn doctor)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			canTargetMechs = false,
			validator = delegate(TargetInfo targ)
			{
				if (!targ.HasThing)
				{
					return false;
				}
				if (!(targ.Thing is Pawn pawn))
				{
					return false;
				}
				if (pawn.Downed)
				{
					return true;
				}
				if (pawn.HostileTo(doctor.Faction))
				{
					return false;
				}
				if (pawn.IsColonist || pawn.IsQuestLodger() || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || (pawn.Faction == Faction.OfPlayer && pawn.IsNonMutantAnimal))
				{
					return true;
				}
				return (pawn.IsColonyMutant && pawn.mutant.Def.entitledToMedicalCare) ? true : false;
			}
		};
	}

	public static TargetingParameters ForRepair(Pawn repairer)
	{
		return new TargetingParameters
		{
			canTargetPawns = false,
			validator = (TargetInfo targ) => targ.HasThing && RepairUtility.PawnCanRepairEver(repairer, targ.Thing)
		};
	}

	public static TargetingParameters ForCarryToBiosculpterPod(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			onlyTargetColonistsOrPrisoners = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo t) => (!t.HasThing || !(t.Thing is Pawn { IsPrisoner: not false } pawn) || !PrisonBreakUtility.IsPrisonBreaking(pawn)) ? true : false
		};
	}

	public static TargetingParameters ForXenogermAbsorption(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			mapObjectTargetsMustBeAutoAttackable = false,
			validator = (TargetInfo targ) => GeneUtility.CanAbsorbXenogerm(targ.Thing as Pawn)
		};
	}

	public static TargetingParameters ForCarryDeathresterToBed(Pawn p)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			onlyTargetColonistsOrPrisoners = true,
			canTargetMechs = false,
			canTargetAnimals = false,
			canTargetBuildings = false,
			validator = (TargetInfo targ) => targ.Thing is Pawn pawn && pawn.Deathresting
		};
	}

	public static TargetingParameters ForBabyCare(Pawn _)
	{
		return new TargetingParameters
		{
			canTargetLocations = false,
			canTargetSelf = false,
			canTargetPawns = true,
			canTargetFires = false,
			canTargetBuildings = false,
			canTargetItems = false,
			canTargetAnimals = false,
			canTargetHumans = true,
			canTargetMechs = false,
			canTargetPlants = false
		};
	}

	public static TargetingParameters ForRomance(Pawn _)
	{
		return new TargetingParameters
		{
			canTargetLocations = false,
			canTargetSelf = false,
			canTargetPawns = true,
			canTargetFires = false,
			canTargetBuildings = false,
			canTargetItems = false,
			canTargetAnimals = false,
			canTargetHumans = true,
			canTargetMechs = false,
			canTargetPlants = false,
			onlyTargetColonists = true
		};
	}

	public static TargetingParameters ForBloodfeeding(Pawn pawn)
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBloodfeeders = false,
			canTargetBuildings = false,
			canTargetMechs = false,
			onlyTargetPrisonersOfColony = true,
			validator = delegate(TargetInfo targ)
			{
				if (!(targ.Thing is Pawn pawn2))
				{
					return false;
				}
				return (pawn2.IsPrisonerOfColony && pawn2.guest.PrisonerIsSecure && !pawn2.guest.IsInteractionDisabled(PrisonerInteractionModeDefOf.Bloodfeed)) ? true : false;
			}
		};
	}

	public static TargetingParameters ForColonist()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			onlyTargetColonists = true
		};
	}

	public static TargetingParameters ForPawns()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false
		};
	}

	public static TargetingParameters ForMech()
	{
		return new TargetingParameters
		{
			canTargetPawns = true,
			canTargetBuildings = false,
			canTargetHumans = false,
			canTargetMechs = true,
			canTargetAnimals = false,
			onlyTargetColonists = false
		};
	}

	public static TargetingParameters ForCell()
	{
		return new TargetingParameters
		{
			canTargetLocations = true,
			canTargetBuildings = false,
			canTargetHumans = false,
			canTargetMechs = false,
			canTargetAnimals = false,
			onlyTargetColonists = false
		};
	}

	public static TargetingParameters ForEntityCapture()
	{
		return new TargetingParameters
		{
			canTargetLocations = false,
			canTargetBuildings = false,
			canTargetHumans = true,
			canTargetMechs = false,
			canTargetAnimals = true,
			onlyTargetColonists = false,
			canTargetItems = true,
			validator = delegate(TargetInfo t)
			{
				CompHoldingPlatformTarget compHoldingPlatformTarget = t.Thing?.TryGetComp<CompHoldingPlatformTarget>();
				return (compHoldingPlatformTarget != null && compHoldingPlatformTarget.StudiedAtHoldingPlatform && compHoldingPlatformTarget.CanBeCaptured) ? true : false;
			},
			mapObjectTargetsMustBeAutoAttackable = false
		};
	}

	public static TargetingParameters ForHeldEntity()
	{
		return new TargetingParameters
		{
			canTargetLocations = false,
			canTargetBuildings = true,
			canTargetHumans = false,
			canTargetMechs = false,
			canTargetAnimals = false,
			onlyTargetColonists = false,
			canTargetItems = false,
			validator = (TargetInfo t) => t.Thing is Building_HoldingPlatform { HeldPawn: not null },
			mapObjectTargetsMustBeAutoAttackable = false
		};
	}

	public static TargetingParameters ForDecipher()
	{
		return new TargetingParameters
		{
			canTargetLocations = false,
			canTargetBuildings = true,
			canTargetHumans = false,
			canTargetMechs = false,
			canTargetAnimals = false,
			onlyTargetColonists = false,
			canTargetItems = false,
			mapObjectTargetsMustBeAutoAttackable = false
		};
	}

	public static TargetingParameters ForPsychicRitual_Invocation()
	{
		return new TargetingParameters
		{
			canTargetLocations = true,
			canTargetBuildings = false,
			canTargetHumans = false,
			canTargetMechs = false,
			canTargetAnimals = false,
			onlyTargetColonists = false,
			canTargetSelf = false
		};
	}
}
