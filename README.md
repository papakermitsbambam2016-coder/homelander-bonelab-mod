# HomelanderNPC

A BONELAB MelonLoader test mod designed for standalone Quest/LemonLoader as well as PC.

## What this build does

- Loads as a MelonLoader mod.
- Creates a configurable Homelander test NPC.
- If a custom asset bundle is present, it loads the first GameObject from the bundle.
- If no bundle is present, it creates a built-in superhero placeholder so the code can still be tested.
- Follows the player camera.
- Performs a close-range super-strength attack.
- Can launch the NPC into a hover/flying state.
- Creates a heat-vision beam using a LineRenderer.
- Looks for Animator states named `Idle`, `Walk`, `Run`, `Attack`, `Fly`, `Hit`, and `Death`.
- Loads optional WAV voice lines from UserData/HomelanderNPC/Audio.
- Uses MelonPreferences instead of requiring BoneLib.
- Writes detailed diagnostics to the MelonLoader log.

## Important limitation

The source package does NOT include a ripped Homelander model or audio from The Boys. Put your own legally obtained/created assets into the folders described below.

A real animated character requires a Unity-built asset bundle. The GitHub workflow builds the C# DLL, but it does not build a Unity asset bundle because Unity project assets are separate from a .NET project.

## Quest paths

The mod creates:

`MelonLoader/UserData/HomelanderNPC/`

Put your optional bundle at:

`MelonLoader/UserData/HomelanderNPC/Homelander.bundle`

Put voice files in:

`MelonLoader/UserData/HomelanderNPC/Audio/`

Supported voice filenames:

- intro.wav
- taunt.wav
- attack.wav
- hurt.wav
- death.wav
- heatvision.wav

The mod also checks the `Mods` directory for `Homelander.bundle` as a fallback.

## Default controls

For testing on a keyboard/PC:

- F6 = spawn/despawn
- F7 = toggle flight
- F8 = heat vision
- F9 = play taunt
- F10 = reset NPC

On Quest, use the config entries or adapt these calls to a BoneMenu bridge later.

## GitHub Actions

The workflow builds `HomelanderNPC.dll` and uploads it as an artifact named:

`HomelanderNPC-Quest`

You can download the ZIP from the GitHub Actions run without installing Visual Studio or a compiler.

## Asset-bundle contract

The bundle should contain one prefab/GameObject. The first GameObject found is instantiated.

For best results the prefab should contain:

- Animator
- humanoid/character rig
- colliders
- optional Rigidbody
- animation states named:
  Idle, Walk, Run, Attack, Fly, Hit, Death

The mod does not require the custom prefab to contain the mod's C# classes.

## Compatibility

This project deliberately avoids a hard BoneLib dependency. That makes the first test smaller and reduces the chance of a missing BoneLib assembly causing a Quest load failure.

BoneLib can be added later for a polished BoneMenu/spawn-crate implementation.
