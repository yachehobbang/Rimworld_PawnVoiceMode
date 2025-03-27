namespace PawnVoice
{
    class PawnVoice
    {
        public static void Test(VoiceTypeEnum value)
        {
            Mod.Log(value.ToString());

			Mod.Log(Mod.Instance.Content.RootDir);
		}
    }
}
