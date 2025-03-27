using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public static class ShaderPropertyIDs
{
	private static readonly string PlanetSunLightDirectionName = "_PlanetSunLightDirection";

	private static readonly string PlanetSunLightEnabledName = "_PlanetSunLightEnabled";

	private static readonly string PlanetRadiusName = "_PlanetRadius";

	private static readonly string MapSunLightDirectionName = "_CastVect";

	private static readonly string GlowRadiusName = "_GlowRadius";

	private static readonly string GameSecondsName = "_GameSeconds";

	private static readonly string ColorName = "_Color";

	private static readonly string ColorTwoName = "_ColorTwo";

	private static readonly string MaskTexName = "_MaskTex";

	private static readonly string SwayHeadName = "_SwayHead";

	private static readonly string ShockwaveSpanName = "_ShockwaveSpan";

	private static readonly string AgeSecsName = "_AgeSecs";

	private static readonly string RandomPerObjectName = "_RandomPerObject";

	private static readonly string RandomPerObjectOffsetRandomName = "_RandomPerObjectOffsetRandom";

	private static readonly string RotationName = "_Rotation";

	private static readonly string OverlayOpacityName = "_OverlayOpacity";

	private static readonly string OverlayColorName = "_OverlayColor";

	private static readonly string AgeSecsPausableName = "_AgeSecsPausable";

	private static readonly string MainTextureOffsetName = "_Main_TexOffset";

	private static readonly string MainTextureScaleName = "_Main_TexScale";

	private static readonly string MaskTextureOffsetName = "_Mask_TexOffset";

	private static readonly string MaskTextureScaleName = "_Mask_TexScale";

	private static readonly string ShockwaveColorName = "_ShockwaveColor";

	private static readonly string WaterCastVectSunName = "_WaterCastVectSun";

	private static readonly string WaterCastVectMoonName = "_WaterCastVectMoon";

	private static readonly string WaterOutputTexName = "_WaterOutputTex";

	private static readonly string WaterOffsetTexName = "_WaterOffsetTex";

	private static readonly string ShadowCompositeTexName = "_ShadowCompositeTex";

	private static readonly string FallIntensityName = "_FallIntensity";

	private static readonly string MainTex_STName = "_MainTex_ST";

	private static readonly string SquashNStretchName = "_SquashNStretch";

	private static readonly string WorkingName = "_Working";

	private static readonly string FadeTexName = "_FadeTex";

	private static readonly string FadeTexScrollSpeedName = "_FadeTexScrollSpeed";

	private static readonly string FadeTexScaleName = "_FadeTexScale";

	private static readonly string PollutedTexName = "_PollutedTex";

	private static readonly string ToxGasTexName = "_ToxGasTex";

	private static readonly string RotGasTexName = "_RotGasTex";

	private static readonly string DeadlifeDustTexName = "_DeadlifeDustTex";

	private static readonly string DestructionStartAgeName = "_DestructionStartAgeSecs";

	private static readonly string PawnCenterWorldName = "_pawnCenterWorld";

	private static readonly string PawnDrawSizeWorldName = "_pawnDrawSizeWorld";

	public static readonly int PlanetSunLightDirection = Shader.PropertyToID(PlanetSunLightDirectionName);

	public static readonly int PlanetSunLightEnabled = Shader.PropertyToID(PlanetSunLightEnabledName);

	public static readonly int PlanetRadius = Shader.PropertyToID(PlanetRadiusName);

	public static readonly int MapSunLightDirection = Shader.PropertyToID(MapSunLightDirectionName);

	public static readonly int GlowRadius = Shader.PropertyToID(GlowRadiusName);

	public static readonly int GameSeconds = Shader.PropertyToID(GameSecondsName);

	public static readonly int AgeSecs = Shader.PropertyToID(AgeSecsName);

	public static readonly int AgeSecsPausable = Shader.PropertyToID(AgeSecsPausableName);

	public static readonly int RandomPerObject = Shader.PropertyToID(RandomPerObjectName);

	public static readonly int RandomPerObjectOffsetRandom = Shader.PropertyToID(RandomPerObjectOffsetRandomName);

	public static readonly int Rotation = Shader.PropertyToID(RotationName);

	public static readonly int OverlayOpacity = Shader.PropertyToID(OverlayOpacityName);

	public static readonly int OverlayColor = Shader.PropertyToID(OverlayColorName);

	public static readonly int Color = Shader.PropertyToID(ColorName);

	public static readonly int ColorTwo = Shader.PropertyToID(ColorTwoName);

	public static readonly int MaskTex = Shader.PropertyToID(MaskTexName);

	public static readonly int SwayHead = Shader.PropertyToID(SwayHeadName);

	public static readonly int ShockwaveColor = Shader.PropertyToID(ShockwaveColorName);

	public static readonly int ShockwaveSpan = Shader.PropertyToID(ShockwaveSpanName);

	public static readonly int WaterCastVectSun = Shader.PropertyToID(WaterCastVectSunName);

	public static readonly int WaterCastVectMoon = Shader.PropertyToID(WaterCastVectMoonName);

	public static readonly int WaterOutputTex = Shader.PropertyToID(WaterOutputTexName);

	public static readonly int WaterOffsetTex = Shader.PropertyToID(WaterOffsetTexName);

	public static readonly int ShadowCompositeTex = Shader.PropertyToID(ShadowCompositeTexName);

	public static readonly int FallIntensity = Shader.PropertyToID(FallIntensityName);

	public static readonly int MainTextureOffset = Shader.PropertyToID(MainTextureOffsetName);

	public static readonly int MainTextureScale = Shader.PropertyToID(MainTextureScaleName);

	public static readonly int MaskTextureOffset = Shader.PropertyToID(MaskTextureOffsetName);

	public static readonly int MaskTextureScale = Shader.PropertyToID(MaskTextureScaleName);

	public static readonly int Tiling = Shader.PropertyToID(MainTex_STName);

	public static readonly int SquashNStretch = Shader.PropertyToID(SquashNStretchName);

	public static readonly int Working = Shader.PropertyToID(WorkingName);

	public static readonly int FadeTex = Shader.PropertyToID(FadeTexName);

	public static readonly int TexScrollSpeed = Shader.PropertyToID(FadeTexScrollSpeedName);

	public static readonly int TexScale = Shader.PropertyToID(FadeTexScaleName);

	public static readonly int PollutedTex = Shader.PropertyToID(PollutedTexName);

	public static readonly int ToxGasTex = Shader.PropertyToID(ToxGasTexName);

	public static readonly int RotGasTex = Shader.PropertyToID(RotGasTexName);

	public static readonly int DeadlifeDustTex = Shader.PropertyToID(DeadlifeDustTexName);

	public static readonly int VoidNodeDestructionStartAge = Shader.PropertyToID(DestructionStartAgeName);

	public static readonly int PawnCenterWorld = Shader.PropertyToID(PawnCenterWorldName);

	public static readonly int PawnDrawSizeWorld = Shader.PropertyToID(PawnDrawSizeWorldName);
}
