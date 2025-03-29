namespace PawnVoice;

class PawnVoiceEventTrigger
{
	public static void TriggerEvent(Verse.Pawn pawn, VoiceEventEnum eventType)
	{
		Mod.Log($"TriggerEvent {pawn} , {eventType}");
	}
}
