# Asset bundle setup

The C# project intentionally does not pretend to be a Unity project. A Unity asset bundle must be built with Unity.

## Prefab requirements

Create one prefab named `Homelander`.

Recommended hierarchy:

Homelander
- Body
- Head
- LeftEye
- RightEye
- Cape
- Armature
- colliders

Add an Animator with states named:

- Idle
- Walk
- Run
- Attack
- Fly
- Hit
- Death

Build the bundle for Android/Quest, not Windows. Quest/LemonLoader documentation specifically notes that AssetBundles need to be compiled for Android when used on the Quest version. citeturn0search0

Name the output:

`Homelander.bundle`

Then place it at:

`MelonLoader/UserData/HomelanderNPC/Homelander.bundle`
