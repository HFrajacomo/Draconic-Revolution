using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

public class VoxelLoader : BaseLoader {
	private static readonly string BLOCK_LIST_RESPATH = "Textures/Voxels/Blocks/BLOCK_LIST";
	private static readonly string OBJECT_LIST_RESPATH = "Textures/Voxels/Objects/OBJECT_LIST";
	private static readonly string BLOCK_RESPATH = "Textures/Voxels/Blocks/";
	private static readonly string OBJECT_RESPATH = "Textures/Voxels/Objects/";

	private static readonly int TEXTURE_SIZE = 32;

	// Entries read from main list
	private static List<string> blockEntries = new List<string>();
	private static List<string> objectEntries = new List<string>();

	// Texture Information
	private static HashSet<string> foundBlockTextureEntries = new HashSet<string>();
	private static HashSet<string> foundObjectTextureEntries = new HashSet<string>();

	// Atlas Information
	private static Dictionary<ShaderIndex, List<string>> atlasTextureNames = new Dictionary<ShaderIndex, List<string>>();
	private static Dictionary<string, ushort> codenameToBlockID = new Dictionary<string, ushort>();
	private static Dictionary<string, int> codenameToTexID = new Dictionary<string, int>();

	// Atlas
	private static Dictionary<ShaderIndex, Texture2D> textureAtlas = new Dictionary<ShaderIndex, Texture2D>();
	
	// Atlas Sizes
	private static int2[] atlasSize;

	// Block Book
	private static Blocks[] blockBook;
	private static BlocklikeObject[] objectBook;

	private static bool isClient;

	// Counters
	private static ushort amountOfBlocks = 0;
	private static ushort amountOfObjects = 0;
	private static ushort currentBlockID = 0;
	private static ushort currentObjectID = 0;

	// Objects Reference
	private static GameObject prefabObjects;


	public VoxelLoader(bool client, GameObject prefabRoot){
		isClient = client;
		prefabObjects = prefabRoot;
	}


	public override bool Load(){
		SetPrefabPersistance();
		InitAtlases();
		ParseBlockList();
		ParseObjectList();
		LoadVoxels();
		CreateTextureAtlases();
		RunPostDeserializationRoutine();
		InitBlockEncyclopediaECS();

		return true;
	}

	public static Blocks GetBlock(ushort code){return blockBook[code];}
	public static BlocklikeObject GetObject(ushort code){return objectBook[ushort.MaxValue - code];}

	public static ushort GetBlockID(string name){return codenameToBlockID[name];}
	public static int GetTextureID(string name){return codenameToTexID[name];}

	public static void SetAtlasTextures(ChunkRenderer rend){
		Material[] materials = rend.GetComponent<MeshRenderer>().sharedMaterials;

		materials[(int)ShaderIndex.OPAQUE].SetTexture("_TextureAtlas", textureAtlas[ShaderIndex.OPAQUE]);
		materials[(int)ShaderIndex.LEAVES].SetTexture("_TextureAtlas", textureAtlas[ShaderIndex.LEAVES]);
		materials[(int)ShaderIndex.ASSETS].SetTexture("_TextureAtlas", textureAtlas[ShaderIndex.ASSETS]);

		rend.GetComponent<MeshRenderer>().sharedMaterials = materials;
	}

	public static ushort GetAmountOfBlocks(){return amountOfBlocks;}
	public static ushort GetAmountOfObjects(){return amountOfObjects;}

	private void InitBlockEncyclopediaECS(){
    	// Loads all blocks
        for(int i=0; i < blockBook.Length; i++){
            BlockEncyclopediaECS.blockHP[i] = blockBook[i].maxHP;
            BlockEncyclopediaECS.blockSolid[i] = blockBook[i].solid;
            BlockEncyclopediaECS.blockTransparent[i] = blockBook[i].transparent;
            BlockEncyclopediaECS.blockSeamless[i] = blockBook[i].seamless;
            BlockEncyclopediaECS.blockLoad[i] = blockBook[i].hasLoadEvent;
            BlockEncyclopediaECS.blockInvisible[i] = blockBook[i].invisible;
            BlockEncyclopediaECS.blockMaterial[i] = blockBook[i].shaderIndex;
            BlockEncyclopediaECS.blockTiles[i] = new int3(blockBook[i].GetTextureTop(), blockBook[i].GetTextureBottom(), blockBook[i].GetTextureSide());
            BlockEncyclopediaECS.blockWashable[i] = blockBook[i].washable;
            BlockEncyclopediaECS.blockAffectLight[i] = blockBook[i].affectLight;
            BlockEncyclopediaECS.blockLuminosity[i] = blockBook[i].luminosity;
            BlockEncyclopediaECS.blockDrawRegardless[i] = blockBook[i].drawRegardless;
        }

        // Loads all object meshes
        for(int i=0; i < objectBook.Length; i++){
            BlockEncyclopediaECS.objectHP[i] = objectBook[i].maxHP;
            BlockEncyclopediaECS.objectSolid[i] = objectBook[i].solid;
            BlockEncyclopediaECS.objectTransparent[i] = objectBook[i].transparent;
            BlockEncyclopediaECS.objectSeamless[i] = objectBook[i].seamless;
            BlockEncyclopediaECS.objectLoad[i] = objectBook[i].hasLoadEvent;
            BlockEncyclopediaECS.objectInvisible[i] = objectBook[i].invisible;
            BlockEncyclopediaECS.objectMaterial[i] = objectBook[i].shaderIndex;
            BlockEncyclopediaECS.objectScaling[i] = objectBook[i].scaling;
            BlockEncyclopediaECS.hitboxScaling[i] = objectBook[i].hitboxScaling;
            BlockEncyclopediaECS.objectNeedRotation[i] = objectBook[i].needsRotation;
            BlockEncyclopediaECS.objectWashable[i] = objectBook[i].washable;
            BlockEncyclopediaECS.objectAffectLight[i] = objectBook[i].affectLight;
            BlockEncyclopediaECS.objectLuminosity[i] = objectBook[i].luminosity;
        }

        for(int i=0; i < atlasSize.Length; i++){
        	BlockEncyclopediaECS.atlasSize[i] = atlasSize[i];
        }
	}

    public static void Destroy(){
        BlockEncyclopediaECS.Destroy();
        Compression.Destroy();
    }


    // Gets customBreak value from block
    public static bool CheckCustomBreak(ushort blockCode){
      if(blockCode <= ushort.MaxValue/2)
        return blockBook[blockCode].customBreak;
      else
        return objectBook[ushort.MaxValue - blockCode].customBreak;
    }

    // Gets customPlace value from block
    public static bool CheckCustomPlace(ushort blockCode){
      if(blockCode <= ushort.MaxValue/2)
        return blockBook[blockCode].customPlace;
      else
        return objectBook[ushort.MaxValue - blockCode].customPlace;
    }

    // Gets solid value from block
    public static bool CheckSolid(ushort? code){
        if(code == null)
            return false;

        if(code <= ushort.MaxValue/2)
            return blockBook[(ushort)code].solid;
        else
            return objectBook[ushort.MaxValue - (ushort)code].solid;
    }

    // Gets transparency value from block
    public static bool CheckTransparent(ushort? code){
        if(code == null)
            return false;

        if(code <= ushort.MaxValue/2)
            return blockBook[(ushort)code].transparent;
        else
            return objectBook[ushort.MaxValue - (ushort)code].transparent;
    }

    // Gets washable value from block
    public static bool CheckWashable(ushort code){
        if(code <= ushort.MaxValue/2)
            return blockBook[code].washable;
        else
            return objectBook[ushort.MaxValue - code].washable;
    }

    // Gets washable value from block
    public static bool CheckLiquid(ushort? code){
        if(code == null)
            return false;
            
        if(code <= ushort.MaxValue/2)
            return blockBook[(ushort)code].liquid;
        else
            return objectBook[ushort.MaxValue - (ushort)code].liquid;
    }

    // Gets name of given code
    public static string CheckName(ushort code){
        if(code <= ushort.MaxValue/2)
            return blockBook[code].name;
        else
            return objectBook[ushort.MaxValue - code].name;
    }

    // Gets affectLight from given code
    public static bool CheckAffectLight(ushort code){
        if(code <= ushort.MaxValue/2)
            return blockBook[code].affectLight;
        else
            return objectBook[ushort.MaxValue - code].affectLight;
    }

    // Get the damage received by a given block/object
    public static int GetDamageReceived(ushort block, ushort blockDamage){
        if(block <= ushort.MaxValue/2)
            return blockBook[block].CalculateDamage(blockDamage);
        else
            return objectBook[ushort.MaxValue - block].CalculateDamage(blockDamage);
    }


	private void InitAtlases(){
		int numOfShaders = 0;

		foreach(ShaderIndex atlas in (ShaderIndex[])Enum.GetValues(typeof(ShaderIndex))){
			atlasTextureNames.Add(atlas, new List<string>());
			numOfShaders++;
		}

		atlasSize = new int2[numOfShaders];
	}

	private void ParseBlockList(){
		TextAsset textAsset = Resources.Load<TextAsset>(BLOCK_LIST_RESPATH);

		if(textAsset == null){
			Debug.Log("Couldn't Locate the BLOCK_LIST while loading the TextureLoader");
			Application.Quit();
		}


		foreach(string line in textAsset.text.Replace("\r", "").Split("\n")){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;
			if(line[0] == ' ')
				continue;

			blockEntries.Add(line);
			amountOfBlocks++;
		}

		if(amountOfBlocks >= ushort.MaxValue/2){
			Debug.Log($"This amount of blocks are not supported. Max amount is {ushort.MaxValue/2}, but found {amountOfBlocks}");
			Application.Quit();
		}
	}

	private void ParseObjectList(){
		TextAsset textAsset = Resources.Load<TextAsset>(OBJECT_LIST_RESPATH);

		if(textAsset == null){
			Debug.Log("Couldn't Locate the OBJECT_LIST while loading the TextureLoader");
			Application.Quit();
		}


		foreach(string line in textAsset.text.Replace("\r", "").Split("\n")){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;
			if(line[0] == ' ')
				continue;

			objectEntries.Add(line);
			amountOfObjects++;
		}

		if(amountOfObjects >= ushort.MaxValue/2){
			Debug.Log($"This amount of objects are not supported. Max amount is {ushort.MaxValue/2}, but found {amountOfObjects}");
			Application.Quit();
		}
	}

	private void LoadVoxels(){
		TextAsset textAsset;
		Blocks serializedBlock;
		BlocklikeObject serializedBLO;
		ShaderIndex shader;
		
		List<Blocks> blockList = new List<Blocks>();
		List<BlocklikeObject> objectList = new List<BlocklikeObject>();

		// Iterate through blocks
		foreach(string block in blockEntries){
			textAsset = Resources.Load<TextAsset>(BLOCK_RESPATH + block);

			if(textAsset != null){
				serializedBlock = VoxelDeserializer.DeserializeBlock(textAsset.text);
				shader = serializedBlock.shaderIndex;

				// If texture is not in atlas set yet
				if(!foundBlockTextureEntries.Contains(serializedBlock.tileTop)){
					if(serializedBlock.tileTop != "" && serializedBlock.tileTop != null){
						atlasTextureNames[shader].Add(serializedBlock.tileTop);
						foundBlockTextureEntries.Add(serializedBlock.tileTop);
					}
				}
				if(!foundBlockTextureEntries.Contains(serializedBlock.tileSide)){
					if(serializedBlock.tileSide != "" && serializedBlock.tileSide != null){
						atlasTextureNames[shader].Add(serializedBlock.tileSide);
						foundBlockTextureEntries.Add(serializedBlock.tileSide);
					}
				}
				if(!foundBlockTextureEntries.Contains(serializedBlock.tileBottom)){
					if(serializedBlock.tileBottom != "" && serializedBlock.tileBottom != null){
						atlasTextureNames[shader].Add(serializedBlock.tileBottom);
						foundBlockTextureEntries.Add(serializedBlock.tileBottom);
					}
				}

				blockList.Add(serializedBlock);
				AssignBlockID(block, true);
			}
		}

		// Iterate through Objects
		foreach(string obj in objectEntries){
			textAsset = Resources.Load<TextAsset>(OBJECT_RESPATH + obj);

			if(textAsset != null){
				serializedBLO = VoxelDeserializer.DeserializeObject(textAsset.text);
				shader = serializedBLO.shaderIndex;

				// If texture is not in atlas set yet
				if(!foundObjectTextureEntries.Contains(serializedBLO.GetTextureName())){
					if(serializedBLO.GetTextureName() != "" && serializedBLO.GetTextureName() != null){
						atlasTextureNames[shader].Add(serializedBLO.GetTextureName());
						foundBlockTextureEntries.Add(serializedBLO.GetTextureName());
					}
				}

				objectList.Add(serializedBLO);
				AssignBlockID(obj, false);
			}
		}

		blockBook = blockList.ToArray();
		objectBook = objectList.ToArray();
	}

	private void AssignBlockID(string codename, bool isBlock){
		if(isBlock){
			codenameToBlockID.Add(codename, currentBlockID);
			currentBlockID++;
		}
		else{
			codenameToBlockID.Add(codename, (ushort)(ushort.MaxValue - currentObjectID));
			currentObjectID++;
		}
	}

	private void RunPostDeserializationRoutine(){
		List<Vector2> objUV = new List<Vector2>();
		int2 atSize;
		int texCode;
		float u, v;

		foreach(Blocks b in blockBook){
			b.SetupAfterSerialize(isClient);
			b.SetupTextureIDs();
		}
		foreach(BlocklikeObject b in objectBook){
			texCode = codenameToTexID[b.codename];
			b.SetupAfterSerialize(isClient);

			b.GetMesh().GetUVs(0, objUV);
			atSize = atlasSize[(int)b.shaderIndex];
			texCode = codenameToTexID[b.codename]; 
			

			for(int i=0; i < objUV.Count; i++){
				u = Mathf.Lerp((texCode%atSize.x)*(1f/atSize.x), ((texCode%atSize.x)*(1f/atSize.x))+(1f/atSize.x), objUV[i].x);
				v = Mathf.Lerp((int)(texCode/atSize.x)*(1f/atSize.y), ((int)(texCode/atSize.x)*(1f/atSize.y))+(1f/atSize.y), objUV[i].y);

				objUV[i] = new Vector2(u, v);
			}

			b.GetMesh().SetUVs(0, objUV);
			objUV.Clear();
		}
	}

	private void CreateTextureAtlases(){
		List<Texture2D> textures = new List<Texture2D>();
		Texture2D tex;
		ushort currentTexID = 0;

		foreach(ShaderIndex shader in (ShaderIndex[])Enum.GetValues(typeof(ShaderIndex))){
			foreach(string textureName in atlasTextureNames[shader]){
				if(shader == ShaderIndex.ASSETS || shader == ShaderIndex.ASSETS_SOLID){
					tex = Resources.Load<Texture2D>($"{OBJECT_RESPATH}{textureName}");
				}
				else{
					tex = Resources.Load<Texture2D>($"{BLOCK_RESPATH}{textureName}");
				}


				if(tex == null){
					Debug.Log($"PROBLEM WHEN TRYING TO LOAD TEXTURE: {textureName} in Shader {shader}");
					Application.Quit();
				}

				tex.name = textureName;
				textures.Add(tex);
				codenameToTexID.Add(textureName, currentTexID);
				currentTexID++;
			}
		
			// Get Atlas Sizes
			int a, b;
			int xOffset, yOffset;
			int count = 0;
			Color[] pixels;

			TweakAtlasValues(textures.Count, out a, out b);
			atlasSize[(int)shader] = new int2(a, b);

			textureAtlas.Add(shader, new Texture2D(a*TEXTURE_SIZE, b*TEXTURE_SIZE));
			textureAtlas[shader].filterMode = FilterMode.Point;

			// Build Atlas
			foreach(Texture2D tex2d in textures){
				RenderTexture rendTex = RenderTexture.GetTemporary(TEXTURE_SIZE, TEXTURE_SIZE, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
				Graphics.Blit(tex2d, rendTex);
			    
			    RenderTexture previous = RenderTexture.active;
			    RenderTexture.active = rendTex;

				Texture2D final = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE);
			    final.ReadPixels(new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), 0, 0);
			    final.Apply();
			    RenderTexture.active = previous;
			    RenderTexture.ReleaseTemporary(rendTex);

				pixels = final.GetPixels();

            	xOffset = (count % a) * TEXTURE_SIZE;
            	yOffset = (int)(count / a) * TEXTURE_SIZE;
            	textureAtlas[shader].SetPixels(xOffset, yOffset, TEXTURE_SIZE, TEXTURE_SIZE, pixels);

            	textureAtlas[shader].Apply();

				count++;
			}

			currentTexID = 0;
			textures.Clear();
		}
	}

	private void TweakAtlasValues(int n, out int a, out int b){
		a = GetClosestSquare(n);
		b = a;

		int mainDiff = (a*b) - n;
		int maxAcceptableDiff = GetMaxDiffVal(n);

		int bestA = a;
		int bestB = b;
		int bestDiff = mainDiff;
		int diff;


		if(CheckValidVal(a) && CheckValidVal(b) && mainDiff <= maxAcceptableDiff)
			return;

		while(a > 2 || b <= Mathf.CeilToInt(n/2)+1){
			if(!CheckValidVal(a)){
				a--;
				continue;
			}
			if(!CheckValidVal(b)){
				b++;
				continue;
			}

			diff = (a*b) - n;

			if(diff == 0)
				return;
			if(diff > 0 && diff <= maxAcceptableDiff)
				return;
			if(diff > 0 && diff <= bestDiff){
				bestDiff = diff;
				bestA = a;
				bestB = b;
			}

			if(diff > 0){
				a--;
				continue;
			}
			else{
				b++;
				continue;
			}
		}

		if(CheckValidVal(n)){
			a = 1;
			b = n;
		}

		a = bestA;
		b = bestB;
	}


	private int GetClosestSquare(int num){
		return Mathf.CeilToInt(Mathf.Sqrt(num));
	}

	private int GetMaxDiffVal(int n){
		if(n <= 12)
			return Mathf.CeilToInt(n*0.35f);
		else if(n <= 100)
			return Mathf.CeilToInt(n*0.18f);
		else
			return Mathf.CeilToInt(n*0.08f);
	}

	private bool CheckValidVal(int val){
		if(val == 1)
			return true;
		if(val%3 == 0 || val%7 == 0 || val%9 == 0 || val%11 == 0 || val%13 == 0 || val%17 == 0 || val%19 == 0 || val%23 == 0 || (val%2 == 1 && val%5 != 0))
			return false;
		return true;
	}

    private void SetPrefabPersistance(){
    	Object.DontDestroyOnLoad(prefabObjects); 
    }

}