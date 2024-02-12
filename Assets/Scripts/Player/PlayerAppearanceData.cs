using System;

public static class PlayerAppearanceData{
	private static CharacterAppearance appearance;
	private static bool isMale;
	private static bool preloadFlag = false; // Flag that tells the InfoClient socket pre-loader that the Redirector Scene was sent to Character Creation


	public static void SetAppearance(CharacterAppearance app){appearance = app;}
	public static void SetGender(bool gender){isMale = gender;}
	public static void SetPreloadFlag(bool b){preloadFlag = b;}
	
	public static CharacterAppearance GetAppearance(){return appearance;}
	public static bool GetGender(){return isMale;}
	public static bool GetPreloadFlag(){return preloadFlag;}
}