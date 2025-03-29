using HarmonyLib;
using Verse;

namespace PawnVoice
{
	public sealed class Mod : Verse.Mod
	{
		public const string Id = "PawnVoice";
		public const string Name = "Pawn Voice";
		public const string Version = "1.0";
		public static Mod Instance;

		public Mod(ModContentPack content)
		  : base(content)
		{
			Mod.Instance = this;
			var harmony = new Harmony("PawnVoice");
			harmony.PatchAll();

			Mod.Log("Initialized");
		}

		public static void Log(string message) => Verse.Log.Message(PrefixMessage(message));

		public static void Warning(string message) => Verse.Log.Warning(PrefixMessage(message));

		public static void Error(string message) => Verse.Log.Error(PrefixMessage(message));

		private static string PrefixMessage(string message) => $"[{Name} v{Version}] " + message;
	}
}
