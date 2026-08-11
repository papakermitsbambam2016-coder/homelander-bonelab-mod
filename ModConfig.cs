using MelonLoader;

namespace HomelanderNPC;

internal static class ModConfig
{
    public static MelonPreferences_Category Category = null!;

    public static MelonPreferences_Entry<bool> AutoSpawn = null!;
    public static MelonPreferences_Entry<bool> EnableAI = null!;
    public static MelonPreferences_Entry<bool> EnableFlight = null!;
    public static MelonPreferences_Entry<bool> EnableHeatVision = null!;
    public static MelonPreferences_Entry<bool> EnableVoice = null!;
    public static MelonPreferences_Entry<float> FollowDistance = null!;
    public static MelonPreferences_Entry<float> AttackDistance = null!;
    public static MelonPreferences_Entry<float> MoveSpeed = null!;
    public static MelonPreferences_Entry<float> Health = null!;

    public static void Create()
    {
        Category = MelonPreferences.CreateCategory("HomelanderNPC");

        AutoSpawn = Category.CreateEntry("AutoSpawn", true, "Spawn the NPC after a scene loads.");
        EnableAI = Category.CreateEntry("EnableAI", true, "Enable player-following AI.");
        EnableFlight = Category.CreateEntry("EnableFlight", true, "Allow the NPC to hover/fly.");
        EnableHeatVision = Category.CreateEntry("EnableHeatVision", true, "Allow the heat-vision beam.");
        EnableVoice = Category.CreateEntry("EnableVoice", true, "Play optional WAV voice lines.");
        FollowDistance = Category.CreateEntry("FollowDistance", 5.0f, "Distance at which the NPC stops walking.");
        AttackDistance = Category.CreateEntry("AttackDistance", 2.0f, "Distance at which the NPC attacks.");
        MoveSpeed = Category.CreateEntry("MoveSpeed", 2.4f, "Ground movement speed.");
        Health = Category.CreateEntry("Health", 500f, "Test health value.");
    }
}
