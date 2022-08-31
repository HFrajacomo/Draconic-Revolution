
# [SOP] Adding New Audio

## Overview

In order to add new audio to Draconic Revolution, follow the steps

1. Create or find .ogg files to be used in-game
2. Figure out their usecase
3. Put the files in the appropriate folder
4. Register the new sound(s) in AudioLibrary

Now, let's cover each step independently.


## Creating files

Getting new files into the project isn't a mindless effort. In order to maintain quality and functionality, a few steps must be followed and a few rules must be kept in mind.

### Rules
1. Files must be .ogg
2. The sound/music must be reviewed and approved by the Audio Director person
3. Music must be unique or properly licensed
4. Sounds must be unique, properly licensed or an altered enough version of another sound


## Assign sound usecase

Usecase is defined as the spatial and sound type of an Audio. Every new Sound must fit in one of the Usecases already defined in **AudioUsecase.cs**. The current AudioUsecases are:

|Usecase|Usage|
|--|--|
|MUSIC_CLIP|Also known as 2DMusic. Audios with this usecase are meant to be music that isn't affected by spatialization techniques|
|MUSIC_3D| Also known as 3DMusic. Audios with this usecase are meant to be assigned to AudioSources located around the world and will be affected by spatialization|
|SFX_CLIP| Audios that are not affect by spatialization and are triggered in a one-shot fashion|
|SFX_3D| Audios that are assigned to AudioSources around the world and are triggered in one-shot fashion|
|VOICE_CLIP| Voice lines that are triggered without spatialization. Voice-overs are good example of that|
|VOICE_3D| Voice lines that are assigned to AudioSources in the world. NPC dialog is an example| 


## Appropriate folder
This one is pretty much a no-brainer, but let's go through it anyways.

The files are organized in the Audio folder contained inside the StreamingAssets folder. Inside, there are folders for each usecase. Simply put the file inside its usecase folder and you are good to go!


## Registering Sounds in AudioLibrary

**AudioLibrary** is a static class meant to be the source of all audio information. 

### Register an AudioName
No matter if you're trying to register Sound or DynamicMusic, every class should have a unique AudioName to use as reference. It's a good practice to use the suffix ***"_GROUP"*** for DynamicMusic namings.

To create an AudioName, just create a new enum item in **AudioName.cs**

### Register a Sound
In order to register new sounds, just add them to the **AudioLibrary.sounds** dictionary.

### Register a DynamicMusic
DynamicMusic have a slightly different way of being registered. First, register all 3 sounds of the DynamicMusic group to the *sounds* dictionary. After that, register the DynamicMusic itself to the **dynamicMusic** dictionary.


# Conclusion
Everything is registered and you should be able to call your sounds in-game by calling: 
> AudioManager.Play(AudioName);