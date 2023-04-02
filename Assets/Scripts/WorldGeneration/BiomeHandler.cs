using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BiomeHandler
{
	// Main References
	public WorldGenerator generator;

	// Biome Information
	public static Dictionary<byte, Biome> dataset = new Dictionary<byte, Biome>();
	public static Dictionary<byte, string> codeToBiome = new Dictionary<byte, string>();
	public ushort[] biomeBlendingValue;
	private BiomeTable surfaceBiomeTable;
	private BiomeTable undergroundBiomeTable;
	private BiomeTable hellBiomeTable;
	private BiomeTable coreBiomeTable;

	private int currentBiome = 0;


	public BiomeHandler(){
		this.surfaceBiomeTable = new BiomeTable(ChunkDepthID.SURFACE);
		this.undergroundBiomeTable = new BiomeTable(ChunkDepthID.UNDERGROUND);
		this.hellBiomeTable = new BiomeTable(ChunkDepthID.HELL);
		this.coreBiomeTable = new BiomeTable(ChunkDepthID.CORE);

		Biome plains = new Biome("Plains", BiomeCode.PLAINS, BiomeType.LOW, ChunkDepthID.SURFACE,
			1, 
			new List<StructureGroupID>(){StructureGroupID.PLAINS_TREES, StructureGroupID.DIRT_PATCHES,
			 	StructureGroupID.GRAVEL_PATCHES, StructureGroupID.GRAVEL_PATCHES_SURFACE, StructureGroupID.SURFACE_ORES, StructureGroupID.BOULDERS_LOW_DENSITY}, AmbientGroup.PLAINS);

		Biome grassyHighlands = new Biome("Grassy Highlands", BiomeCode.GRASSY_HIGHLANDS, BiomeType.PEAK, ChunkDepthID.SURFACE,
			3,
			new List<StructureGroupID>(){StructureGroupID.GRASS_HIGHLANDS_TREES, StructureGroupID.DIRT_PATCHES,
			 	StructureGroupID.GRAVEL_PATCHES, StructureGroupID.GRAVEL_PATCHES_SURFACE, StructureGroupID.SURFACE_ORES, StructureGroupID.BOULDERS_MID_DENSITY}, AmbientGroup.PLAINS);

		Biome ocean = new Biome("Ocean", BiomeCode.OCEAN, BiomeType.OCEAN, ChunkDepthID.SURFACE,
			8,
			new List<StructureGroupID>(){StructureGroupID.SURFACE_ORES}, AmbientGroup.OCEAN);

		Biome forest = new Biome("Forest", BiomeCode.FOREST, BiomeType.MID, ChunkDepthID.SURFACE,
			1,
			new List<StructureGroupID>(){StructureGroupID.FOREST_TREES, StructureGroupID.SURFACE_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.FOREST);

		Biome desert = new Biome("Desert", BiomeCode.DESERT, BiomeType.LOW, ChunkDepthID.SURFACE,
			8,
			new List<StructureGroupID>(){StructureGroupID.DESERT_TREES, StructureGroupID.SURFACE_ORES,
				StructureGroupID.BOULDERS_LOW_DENSITY, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.DESERT);

		Biome snowPlains = new Biome("Snowy Plains", BiomeCode.SNOWY_PLAINS, BiomeType.LOW, ChunkDepthID.SURFACE,
			9,
			new List<StructureGroupID>(){StructureGroupID.ICE_PLAINS_TREES, StructureGroupID.DIRT_PATCHES,
			 	StructureGroupID.SURFACE_ORES, StructureGroupID.BOULDERS_LOW_DENSITY, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.SNOW);

		Biome snowyHighlands = new Biome("Snowy Highlands", BiomeCode.SNOWY_HIGHLANDS, BiomeType.PEAK, ChunkDepthID.SURFACE,
			9,
			new List<StructureGroupID>(){StructureGroupID.ICE_HIGHLANDS_TREES, StructureGroupID.DIRT_PATCHES,
			 	StructureGroupID.SURFACE_ORES, StructureGroupID.BOULDERS_MID_DENSITY, StructureGroupID.GRAVEL_PATCHES, StructureGroupID.GRAVEL_PATCHES_SURFACE}, AmbientGroup.SNOW);

		Biome iceOcean = new Biome("Ice Ocean", BiomeCode.ICE_OCEAN, BiomeType.OCEAN, ChunkDepthID.SURFACE,
			9,
			new List<StructureGroupID>(){StructureGroupID.SURFACE_ORES}, AmbientGroup.SNOW);

		Biome snowyForest = new Biome("Snow Forest", BiomeCode.SNOWY_FOREST, BiomeType.MID, ChunkDepthID.SURFACE,
			9,
			new List<StructureGroupID>(){StructureGroupID.ICE_FOREST_TREES, StructureGroupID.SURFACE_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.SNOW);

		Biome caverns = new Biome("Caverns", BiomeCode.CAVERNS, BiomeType.MID, ChunkDepthID.UNDERGROUND,
			3,
			new List<StructureGroupID>(){StructureGroupID.UNDERGROUND_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.CAVERNS);

		Biome basaltCaves = new Biome("Basalt Cave", BiomeCode.BASALT_CAVES, BiomeType.PEAK, ChunkDepthID.UNDERGROUND,
			(ushort)BlockID.BASALT,
			new List<StructureGroupID>(){StructureGroupID.UNDERGROUND_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.CAVERNS);

		Biome submergedCave = new Biome("Submerged Cave", BiomeCode.UNDERWATER_CAVES, BiomeType.OCEAN, ChunkDepthID.UNDERGROUND,
			(ushort)BlockID.STONE,
			new List<StructureGroupID>(){StructureGroupID.UNDERGROUND_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.CAVERNS);

		Biome iceCave = new Biome("Ice Cave", BiomeCode.ICE_CAVES, BiomeType.LOW, ChunkDepthID.UNDERGROUND,
			(ushort)BlockID.SNOW,
			new List<StructureGroupID>(){StructureGroupID.UNDERGROUND_ORES, StructureGroupID.GRAVEL_PATCHES}, AmbientGroup.ICE_CAVERNS);

		Biome hellPlains = new Biome("Hell Plains", BiomeCode.HELL_PLAINS, BiomeType.MID, ChunkDepthID.HELL,
			(ushort)BlockID.HELL_MARBLE,
			new List<StructureGroupID>(){StructureGroupID.SMALL_BONES, StructureGroupID.HELL_ORES}, AmbientGroup.HELL);

		Biome boneValley = new Biome("Bone Valley", BiomeCode.BONE_VALLEY, BiomeType.MID, ChunkDepthID.HELL,
			(ushort)BlockID.HELL_MARBLE,
			new List<StructureGroupID>(){StructureGroupID.GREATER_BONES, StructureGroupID.HELL_ORES}, AmbientGroup.HELL);

		Biome lavaOcean = new Biome("Lava Ocean", BiomeCode.LAVA_OCEAN, BiomeType.LOW, ChunkDepthID.HELL,
			(ushort)BlockID.LAVA,
			new List<StructureGroupID>(){StructureGroupID.HELL_ORES}, AmbientGroup.HELL);

		Biome deepCliff = new Biome("Deep Cliff", BiomeCode.DEEP_CLIFF, BiomeType.OCEAN, ChunkDepthID.HELL,
			(ushort)BlockID.ACASTER,
			new List<StructureGroupID>(){}, AmbientGroup.HELL);

		Biome hellHighlands= new Biome("Hell Highlands", BiomeCode.HELL_HIGHLANDS, BiomeType.PEAK, ChunkDepthID.HELL,
			(ushort)BlockID.HELL_MARBLE,
			new List<StructureGroupID>(){StructureGroupID.SMALL_BONES, StructureGroupID.HELL_ORES}, AmbientGroup.HELL);

		Biome volcanicHighlands = new Biome("Volcanic Highlands", BiomeCode.VOLCANIC_HIGHLANDS, BiomeType.PEAK, ChunkDepthID.HELL,
			(ushort)BlockID.BASALT,
			new List<StructureGroupID>(){StructureGroupID.SMALL_BONES, StructureGroupID.HELL_ORES}, AmbientGroup.HELL);

		Biome core = new Biome("Core", BiomeCode.CORE, BiomeType.PEAK, ChunkDepthID.CORE,
			(ushort)BlockID.MOONSTONE,
			new List<StructureGroupID>(){StructureGroupID.CORE_ORES}, AmbientGroup.CORE);

		AddBiome(plains);
		AddBiome(grassyHighlands);
		AddBiome(ocean);
		AddBiome(forest);
		AddBiome(desert);
		AddBiome(snowPlains);
		AddBiome(snowyHighlands);
		AddBiome(iceOcean);
		AddBiome(snowyForest);
		AddBiome(caverns);
		AddBiome(basaltCaves);
		AddBiome(submergedCave);
		AddBiome(iceCave);
		AddBiome(hellPlains);
		AddBiome(boneValley);
		AddBiome(lavaOcean);
		AddBiome(deepCliff);
		AddBiome(hellHighlands);
		AddBiome(volcanicHighlands);
		AddBiome(core);


		this.biomeBlendingValue = new ushort[this.currentBiome];

		for(byte i=0; i < this.currentBiome; i++)
			this.biomeBlendingValue[i] = dataset[i].blendingBlock; 
	}

	// Sets the WorldGenerator obj reference
	public void SetWorldGenerator(WorldGenerator wgen){
		this.generator = wgen;
		this.generator.SetBiomeBlending(this.biomeBlendingValue);
	}

	// Gets biome byte code from name
	public static byte BiomeToByte(string biomeName){
		foreach(Biome b in dataset.Values){
			if(b.name == biomeName)
				return b.biomeCode;
		}
		return 0;
	}

	// Gets biome name from byte code
	public static string ByteToBiome(byte code){
		return codeToBiome[code];
	}


	// Initializes biome in Biome Handler at start of runtime
	private void AddBiome(Biome b){
		dataset.Add(b.biomeCode, b);
		codeToBiome.Add(b.biomeCode, b.name);

		currentBiome++;
	}

	// Clears everything in BiomeHandler
	public void Clear(){
		BiomeHandler.dataset.Clear();
		BiomeHandler.codeToBiome.Clear();
	}

	// Returns the list of possible Structures in a biome
	public static List<int> GetBiomeStructs(BiomeCode biome){
		return dataset[(byte)biome].structCodes;
	}

	// Returns the list of possible Amounts in a biome
	public static List<int> GetBiomeAmounts(BiomeCode biome){
		return dataset[(byte)biome].amountStructs;
	}

	// Returns the list of possible Percentages in a biome
	public static List<float> GetBiomePercentages(BiomeCode biome){
		return dataset[(byte)biome].percentageStructs;
	}

	// Returns the list of possible Depths in a biome
	public static List<int> GetBiomeDepth(BiomeCode biome){
		return dataset[(byte)biome].depthValues;
	}

	// Returns the list of possible HardSetDepths in a biome
	public static List<int> GetBiomeHSDepth(BiomeCode biome){
		return dataset[(byte)biome].hardSetDepth;
	}

	// Returns the list of possible Range in a biome
	public static List<bool> GetBiomeRange(BiomeCode biome){
		return dataset[(byte)biome].hasRange;
	}

	// Returns the list of possible Range in a biome
	public static List<int> GetBiomeMinHeight(BiomeCode biome){
		return dataset[(byte)biome].minHeight;
	}
	
	// Returns the AmbientGroup this biome is from
	public static AmbientGroup GetAmbientGroup(byte biome){
		if(!dataset.ContainsKey(biome))
			return AmbientGroup.PLAINS;
			
		return dataset[biome].ambient;
	}

	/*
	Main Function, assigns biome based on 
	*/
	public byte AssignBiome(float[] data, ChunkDepthID layer){
		float5 biomeInfo = new float5(data[0], data[1], data[2], data[3], data[4]);

		switch(layer){
			case ChunkDepthID.SURFACE:
				return (byte)this.surfaceBiomeTable.GetBiome(biomeInfo);
			case ChunkDepthID.UNDERGROUND:
				return (byte)this.undergroundBiomeTable.GetBiome(biomeInfo);
			case ChunkDepthID.HELL:
				return (byte)this.hellBiomeTable.GetBiome(biomeInfo);
			case ChunkDepthID.CORE:
				return (byte)this.coreBiomeTable.GetBiome(biomeInfo);
			default:
				return 0;
		}
		
	}
}


public struct Biome{
	public string name;
	public byte biomeCode;
	public byte biomeType;

	public ushort blendingBlock;

	public List<int> structCodes;
	public List<int> amountStructs;
	public List<float> percentageStructs;
	public List<int> depthValues;
	public List<int> hardSetDepth;
	public List<bool> hasRange;
	public List<int> minHeight;

	public AmbientGroup ambient;

	public Biome(string n, BiomeCode code, BiomeType type, ChunkDepthID layer, ushort blendingBlock, List<StructureGroupID> structureGroups, AmbientGroup agroup){
		this.name = n;
		this.biomeCode = (byte)code;
		this.biomeType = (byte)type;

		this.blendingBlock = blendingBlock;
		
		this.structCodes = new List<int>();
		this.amountStructs = new List<int>();
		this.percentageStructs = new List<float>();
		this.depthValues = new List<int>();
		this.hardSetDepth = new List<int>();
		this.hasRange = new List<bool>();
		this.minHeight = new List<int>();
		this.ambient = agroup;

		foreach(StructureGroupID id in structureGroups){
			StructureGroup.AddStructureGroup(id, this);
		}
	}
}

public struct float5{
	public int t;
	public int h;
	public int b;
	public int e;
	public int p;

	public float5(float x, float y, float z, float w, float k){
		this.t = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (x+1)/2f)));
		this.h = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (y+1)/2f)));
		this.b = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (z+1)/2f)));
		this.e = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (w+1)/2f)));
		this.p = LimitValue(Mathf.FloorToInt(Mathf.Lerp(0, BiomeTable.separatorSize, (k+1)/2f)));

		int LimitValue(int x){
			if(x > BiomeTable.separatorSize-1)
				return BiomeTable.separatorSize-1;
			return x;
		}
	}
}

public enum BiomeType : byte{
	OCEAN,
	LOW,
	MID,
	PEAK
}

public enum BiomeCode : byte{
	PLAINS,
	GRASSY_HIGHLANDS,
	OCEAN,
	FOREST,
	DESERT,
	SNOWY_PLAINS,
	SNOWY_HIGHLANDS,
	ICE_OCEAN,
	SNOWY_FOREST,
	CAVERNS,
	BASALT_CAVES,
	UNDERWATER_CAVES,
	ICE_CAVES,
	HELL_PLAINS,
	LAVA_OCEAN,
	HELL_HIGHLANDS,
	VOLCANIC_HIGHLANDS,
	BONE_VALLEY,
	DEEP_CLIFF,
	CORE
}