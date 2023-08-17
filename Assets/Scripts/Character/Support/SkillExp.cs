using System;

public class SkillExp{
	private static int[] levelCap;
	private byte level;
	private int currentExp;

	public SkillExp(){
		PopulateRequiredExp();

		this.level = 1;
		this.currentExp = 0;
	}

	public SkillExp(byte level, int currentExp){
		PopulateRequiredExp();

		this.level = level;
		this.currentExp = currentExp;
	}

	// Calculates the EXP progression to the next levels
	private void PopulateRequiredExp(){
		if(levelCap != null)
			return;

		int initial = 80;
		int levelAdd = 20;
		float multiplier = .08607f;

		levelCap = new int[100];
		levelCap[0] = 0;
		levelCap[1] = initial;

		for(int i=2; i < 100; i++){
			levelCap[i] = (int)(levelCap[i-1] + levelCap[i-1]*multiplier + levelAdd);
		}
	}

	public byte GetLevel(){return this.level;}
	public int GetCurrentExp(){return this.currentExp;}
	public int GetFinalExp(){return SkillExp.levelCap[this.level];}
	public string GetProgress(){return (this.currentExp/SkillExp.levelCap[this.level]).ToString("{#.##}");}
}