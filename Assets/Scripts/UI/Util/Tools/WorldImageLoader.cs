using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class WorldImageLoader {
	// Sprite handlers
	private static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
	private static readonly Sprite defaultImage;

	// Constants
	private static readonly string WORLD_IMAGE_NAME = "/world_screenshot.png";
	private static readonly string PATH_TO_DEFAULT_IMAGE = "UI/default_world_image";

	// Image loading
	private static byte[] BUFFER;
	private static Texture2D cachedTexture;


	static WorldImageLoader(){
		defaultImage = Resources.Load<Sprite>(PATH_TO_DEFAULT_IMAGE);
	}

	public static Sprite GetWorldImage(string world){
		Debug.Log("World Name: " + world);
		return sprites[world];
	}

	public static void LoadSprites(){
		sprites.Clear();

		List<string> directories = EnvironmentVariablesCentral.ListWorldFolders();
		string[] splitted;
		string worldName;

		foreach(string dir in directories){
			if(dir.Contains("/"))
				splitted = dir.Split("/");
			else
				splitted = dir.Split("\\");

			worldName = splitted[splitted.Length-1];

			if(File.Exists(dir + WORLD_IMAGE_NAME)){
				cachedTexture = LoadTexture(dir + WORLD_IMAGE_NAME);

		        Sprite sprite = Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), Vector2.zero);
		        sprites.Add(worldName, sprite);
			}
			else{
				sprites.Add(worldName, defaultImage);
			}
		}
	}

    private static Texture2D LoadTexture(string path)
    {
        BUFFER = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(BUFFER))
        {
            return texture;
        }
        return null;
    }
}