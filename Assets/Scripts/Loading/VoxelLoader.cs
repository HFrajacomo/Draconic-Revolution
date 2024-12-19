using System;
using System.Globalization;
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
	private static readonly string BLOCK_NORMAL_INTENSITY = "Textures/Voxels/Blocks/NORMAL_INTENSITY";
	private static readonly string OBJECT_NORMAL_INTENSITY = "Textures/Voxels/Objects/NORMAL_INTENSITY";
	private static readonly string PBR_SUFFIX = "-PBR";

	private static readonly CultureInfo parsingCulture = CultureInfo.InvariantCulture;

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
	private static Dictionary<string, float> texnameToNormalIntensity = new Dictionary<string, float>();

	// Final Array
	private static Dictionary<ShaderIndex, Texture2DArray> textureArray = new Dictionary<ShaderIndex, Texture2DArray>();
	private static Dictionary<ShaderIndex, Texture2DArray> normalArray = new Dictionary<ShaderIndex, Texture2DArray>();
	private static Dictionary<ShaderIndex, Texture2DArray> pbrArray = new Dictionary<ShaderIndex, Texture2DArray>();

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
		LoadTextureNormalIntensity();
		LoadVoxels();

		if(isClient)
			CreateTextureAtlases();

		InitBlockEncyclopediaECS();

		return true;
	}

	public static Blocks GetBlock(ushort code){return blockBook[code];}
	public static BlocklikeObject GetObject(ushort code){return objectBook[ushort.MaxValue - code];}

	public static string GetName(ushort code){
		if(code <= ushort.MaxValue/2)
			return GetBlock(code).name;
		return GetObject(code).name;
	}
	public static ushort GetBlockID(string name){return codenameToBlockID[name];}
	public static int GetTextureID(string name){return codenameToTexID[name];}

	public static void SetAtlasTextures(ChunkRenderer rend){
		Material[] materials = rend.GetComponent<MeshRenderer>().sharedMaterials;

		materials[(int)ShaderIndex.OPAQUE].SetTexture("_TextureArray", textureArray[ShaderIndex.OPAQUE]);
		materials[(int)ShaderIndex.OPAQUE].SetTexture("_NormalArray", normalArray[ShaderIndex.OPAQUE]);
		materials[(int)ShaderIndex.OPAQUE].SetTexture("_PBRArray", pbrArray[ShaderIndex.OPAQUE]);
		materials[(int)ShaderIndex.LEAVES].SetTexture("_TextureArray", textureArray[ShaderIndex.LEAVES]);
		materials[(int)ShaderIndex.LEAVES].SetTexture("_PBRArray", pbrArray[ShaderIndex.LEAVES]);
		materials[(int)ShaderIndex.ASSETS].SetTexture("_TextureArray", textureArray[ShaderIndex.ASSETS]);
		materials[(int)ShaderIndex.ASSETS].SetTexture("_PBRArray", pbrArray[ShaderIndex.ASSETS]);
		materials[(int)ShaderIndex.ASSETS_SOLID].SetTexture("_TextureArray", textureArray[ShaderIndex.ASSETS_SOLID]);
		materials[(int)ShaderIndex.ASSETS_SOLID].SetTexture("_PBRArray", pbrArray[ShaderIndex.ASSETS_SOLID]);

		rend.GetComponent<MeshRenderer>().sharedMaterials = materials;
	}

	public static ushort GetAmountOfBlocks(){return amountOfBlocks;}
	public static ushort GetAmountOfObjects(){return amountOfObjects;}

	public static void InitBlockEncyclopediaECS(){
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
	}

    public static void Destroy(){
        BlockEncyclopediaECS.Destroy();
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
		foreach(ShaderIndex atlas in (ShaderIndex[])Enum.GetValues(typeof(ShaderIndex))){
			atlasTextureNames.Add(atlas, new List<string>());
		}
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

	public void RunPostDeserializationRoutine(){
		foreach(Blocks b in blockBook){
			b.SetupAfterSerialize(isClient);

			if(isClient)
				b.SetupTextureIDs();
		}
		foreach(BlocklikeObject b in objectBook){
			if(isClient)
				b.SetupTextureIDs();
			
			b.SetupAfterSerialize(isClient);
		}
	}

	private void LoadTextureNormalIntensity(){
		string[] splitText;
		float normalIntensity;
		TextAsset textAsset = Resources.Load<TextAsset>(BLOCK_NORMAL_INTENSITY);

		if(textAsset != null){
			foreach(string line in textAsset.text.Split("\n")){
				splitText = line.Split("\t");

				if(splitText.Length < 2)
					continue;

				if(float.TryParse(splitText[1], NumberStyles.Float | NumberStyles.AllowThousands, parsingCulture, out normalIntensity)){
					texnameToNormalIntensity.Add(splitText[0], normalIntensity);
				}
				else{
					Debug.Log($"PROBLEM READING NORMAL INTENSITY FROM LINE: {line}");
				}
			}
		}

		textAsset = Resources.Load<TextAsset>(OBJECT_NORMAL_INTENSITY);

		if(textAsset != null){
			foreach(string line in textAsset.text.Split("\n")){
				splitText = line.Split("\t");

				if(float.TryParse(splitText[1], NumberStyles.Float | NumberStyles.AllowThousands, parsingCulture, out normalIntensity)){
					texnameToNormalIntensity.Add(splitText[0], normalIntensity);
				}
				else{
					Debug.Log($"PROBLEM READING NORMAL INTENSITY FROM LINE: {line}");
				}
			}
		}
	}

	private void CreateTextureAtlases(){
		List<Texture2D> textures = new List<Texture2D>();
		List<Texture2D> pbrTextures = new List<Texture2D>();
		Texture2D tex;
		Texture2D normal = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE);
		Texture2D pbr = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE);
		ushort currentTexID = 0;

		foreach(ShaderIndex shader in (ShaderIndex[])Enum.GetValues(typeof(ShaderIndex))){
			foreach(string textureName in atlasTextureNames[shader]){
				if(shader == ShaderIndex.ASSETS || shader == ShaderIndex.ASSETS_SOLID){
					tex = Resources.Load<Texture2D>($"{OBJECT_RESPATH}{textureName}");
					pbr = Resources.Load<Texture2D>($"{OBJECT_RESPATH}{textureName}{PBR_SUFFIX}");
				}
				else{
					tex = Resources.Load<Texture2D>($"{BLOCK_RESPATH}{textureName}");
					pbr = Resources.Load<Texture2D>($"{BLOCK_RESPATH}{textureName}{PBR_SUFFIX}");
				}


				if(tex == null || pbr == null){
					Debug.Log($"PROBLEM WHEN TRYING TO LOAD TEXTURE: {textureName} in Shader {shader}");
					Application.Quit();
				}

				tex.name = textureName;
				pbr.name = textureName;

				textures.Add(tex);
				pbrTextures.Add(pbr);
				codenameToTexID.Add(textureName, currentTexID);
				currentTexID++;
			}
		
			// Get Atlas Sizes
			int count = 0;

			textureArray.Add(shader, new Texture2DArray(TEXTURE_SIZE, TEXTURE_SIZE, Mathf.Max(1, textures.Count), TextureFormat.RGBA32, true));
			textureArray[shader].filterMode = FilterMode.Point;
			normalArray.Add(shader, new Texture2DArray(TEXTURE_SIZE, TEXTURE_SIZE, Mathf.Max(1, textures.Count), TextureFormat.RGBA32, true));
			normalArray[shader].filterMode = FilterMode.Point;
			pbrArray.Add(shader, new Texture2DArray(TEXTURE_SIZE, TEXTURE_SIZE, Mathf.Max(1, textures.Count), TextureFormat.RGBA32, true));
			pbrArray[shader].filterMode = FilterMode.Point;

			// Build Atlas and Normals
			foreach(Texture2D tex2d in textures){
				if(texnameToNormalIntensity.ContainsKey(tex2d.name)){
					AddImageToAtlas(textureArray[shader], normalArray[shader], tex2d, texnameToNormalIntensity[tex2d.name], TEXTURE_SIZE, count);
				}
				else{
					AddImageToAtlas(textureArray[shader], normalArray[shader], tex2d, 1f, TEXTURE_SIZE,count);	
				}

				count++;
			}

			count = 0;

			foreach(Texture2D tex2d in pbrTextures){
				AddImageToPBR(pbrArray[shader], tex2d, TEXTURE_SIZE, count);
				count++;
			}

			currentTexID = 0;
			textures.Clear();
			pbrTextures.Clear();

			textureArray[shader].Apply();
			normalArray[shader].Apply();
			pbrArray[shader].Apply();
		}
	}

	private void AddImageToAtlas(Texture2DArray main, Texture2DArray normalMain, Texture2D tex2d, float normalIntensity, int TEXTURE_SIZE, int count){
		RenderTexture rendTex = RenderTexture.GetTemporary(TEXTURE_SIZE, TEXTURE_SIZE, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
		Graphics.Blit(tex2d, rendTex);
	    RenderTexture previous = RenderTexture.active;
	    RenderTexture.active = rendTex;

		Texture2D final = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false, false);
		final.wrapMode = TextureWrapMode.Clamp;
	    final.ReadPixels(new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), 0, 0);
	    final.Apply();
	    RenderTexture.active = previous;
	    RenderTexture.ReleaseTemporary(rendTex);

		Texture2D finalNormal = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false, false);

		main.SetPixels(final.GetPixels(), count);
		CalculateNormalMap(final, final.GetPixels(), finalNormal, normalIntensity);
		normalMain.SetPixels(finalNormal.GetPixels(), count);
	}

	private void AddImageToPBR(Texture2DArray main, Texture2D tex2d, int TEXTURE_SIZE, int count){
		RenderTexture rendTex = RenderTexture.GetTemporary(TEXTURE_SIZE, TEXTURE_SIZE, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
		Graphics.Blit(tex2d, rendTex);
	    RenderTexture previous = RenderTexture.active;
	    RenderTexture.active = rendTex;

		Texture2D final = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false, false);
		final.wrapMode = TextureWrapMode.Clamp;
	    final.ReadPixels(new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), 0, 0);
	    final.Apply();
	    RenderTexture.active = previous;
	    RenderTexture.ReleaseTemporary(rendTex);

		main.SetPixels(final.GetPixels(), count);
	}

    private void CalculateNormalMap(Texture2D source, Color[] pixels, Texture2D dest, float strength){
        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                float nx = SampleHeight(source, x + 1, y) - SampleHeight(source, x - 1, y);
                float ny = SampleHeight(source, x, y + 1) - SampleHeight(source, x, y - 1);
                float nz = 1.0f / strength;

                Vector3 normal = new Vector3(nx, ny, nz).normalized * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);

                pixels[y * source.width + x] = new Color(normal.x, normal.y, normal.z, 1.0f);
            }
        }

        dest.SetPixels(pixels);
        dest.Apply();
    }

    private float SampleHeight(Texture2D tex, int x, int y){
	    x = Mathf.Clamp(x, 0, tex.width - 1);
	    y = Mathf.Clamp(y, 0, tex.height - 1);

	    Color pixel = tex.GetPixels(x, y, 1, 1)[0]; // Get a 1x1 pixel block

	    return pixel.grayscale;
    }

    private void SetPrefabPersistance(){
    	Object.DontDestroyOnLoad(prefabObjects); 
    }

}