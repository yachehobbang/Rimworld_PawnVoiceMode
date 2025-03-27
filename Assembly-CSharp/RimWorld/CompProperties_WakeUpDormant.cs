using Verse;

namespace RimWorld;

public class CompProperties_WakeUpDormant : CompProperties
{
	public string wakeUpSignalTag = "CompCanBeDormant.WakeUp";

	public float wakeUpCheckRadius = -1f;

	public float wakeUpOnThingConstructedRadius = 3f;

	public bool wakeUpOnDamage = true;

	public bool onlyWakeUpSameFaction = true;

	public bool onlySendSignalOnce = true;

	public SoundDef wakeUpSound;

	public bool wakeUpIfAnyTargetClose;

	public TargetingParameters wakeUpTargetingParams = TargetingParameters.ForColonist();

	public bool wakeUpWithDelay;

	public string radiusCheckInspectPaneKey;

	public string activateMessageKey;

	public string activatePluralMessageKey;

	public MessageTypeDef activateMessageType;

	public CompProperties_WakeUpDormant()
	{
		compClass = typeof(CompWakeUpDormant);
	}
}
