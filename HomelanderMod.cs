using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

public sealed class HomelanderMod : MelonMod
{
    private GameObject? npc;
    private HomelanderAI? ai;
    private VoiceSystem? voice;
    private HeatVisionSystem? heatVision;
    private AnimationController? animation;
    private bool sceneReady;
    private float spawnDelay = 2f;

    private string AssetRoot =>
        Path.Combine(MelonUtils.UserDataDirectory, "HomelanderNPC");

    public override void OnInitializeMelon()
    {
        ModConfig.Create();

        Directory.CreateDirectory(AssetRoot);
        Directory.CreateDirectory(Path.Combine(AssetRoot, "Audio"));

        voice = new VoiceSystem();
        voice.Initialize();

        heatVision = new HeatVisionSystem();
        animation = new AnimationController();
        ai = new HomelanderAI(heatVision, voice, animation);

        MelonLogger.Msg("======================================");
        MelonLogger.Msg(" Homelander NPC 0.1.0");
        MelonLogger.Msg(" Quest/PC test build");
        MelonLogger.Msg("======================================");
        MelonLogger.Msg($"Asset root: {AssetRoot}");
        MelonLogger.Msg("F6 spawn/despawn | F7 flight | F8 heat vision | F9 taunt | F10 reset");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        sceneReady = true;
        spawnDelay = 2f;

        MelonLogger.Msg($"Scene loaded: {sceneName}");

        if (ModConfig.AutoSpawn.Value)
            MelonLogger.Msg("AutoSpawn enabled; NPC will spawn shortly.");
    }

    public override void OnUpdate()
    {
        if (!sceneReady)
            return;

        if (spawnDelay > 0f)
        {
            spawnDelay -= Time.deltaTime;

            if (spawnDelay <= 0f && ModConfig.AutoSpawn.Value && npc == null)
                Spawn();

            return;
        }

        HandleKeyboardTesting();
        ai?.Update();
    }

    private void HandleKeyboardTesting()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.F6))
                ToggleSpawn();

            if (Input.GetKeyDown(KeyCode.F7))
                ai?.ToggleFlight();

            if (Input.GetKeyDown(KeyCode.F8))
                ai?.ToggleHeatVision();

            if (Input.GetKeyDown(KeyCode.F9) && npc != null)
                voice?.Play("taunt", npc.transform.position);

            if (Input.GetKeyDown(KeyCode.F10))
            {
                Despawn();
                Spawn();
            }
        }
        catch
        {
            // Input APIs can differ between PC and Quest. Never let testing input crash the mod.
        }
    }

    public void ToggleSpawn()
    {
        if (npc == null)
            Spawn();
        else
            Despawn();
    }

    public void Spawn()
    {
        if (npc != null)
            return;

        var camera = Camera.main;
        if (camera == null)
        {
            MelonLogger.Warning("Cannot spawn: Camera.main is not ready yet.");
            return;
        }

        Vector3 position = camera.transform.position + camera.transform.forward * 5f;
        position.y = camera.transform.position.y;

        npc = LoadCustomPrefab(position);

        if (npc == null)
            npc = CreateFallbackNPC(position);

        if (npc == null)
        {
            MelonLogger.Error("NPC creation failed.");
            return;
        }

        npc.name = "HomelanderNPC_Runtime";
        ai?.Attach(npc);

        MelonLogger.Msg("Homelander NPC spawned.");
    }

    private GameObject? LoadCustomPrefab(Vector3 position)
    {
        string[] candidates =
        {
            Path.Combine(AssetRoot, "Homelander.bundle"),
            Path.Combine(MelonUtils.BaseDirectory, "Mods", "Homelander.bundle")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                MelonLogger.Msg($"Loading asset bundle: {path}");

                var bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    MelonLogger.Warning("AssetBundle.LoadFromFile returned null.");
                    continue;
                }

                var assets = bundle.LoadAllAssets<GameObject>();
                if (assets == null || assets.Length == 0)
                {
                    MelonLogger.Warning("Bundle contains no GameObject assets.");
                    bundle.Unload(false);
                    continue;
                }

                var instance = UnityEngine.Object.Instantiate(assets[0]);
                instance.transform.position = position;

                bundle.Unload(false);

                MelonLogger.Msg($"Custom prefab loaded: {assets[0].name}");
                return instance;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Bundle load failed: {ex}");
            }
        }

        MelonLogger.Msg("No custom bundle found; using built-in test NPC.");
        return null;
    }

    private static GameObject CreateFallbackNPC(Vector3 position)
    {
        try
        {
            var root = new GameObject("HomelanderNPC_Fallback");
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "HomelanderBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.65f, 1.0f, 0.65f);

            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
                bodyRenderer.material.color = new Color(0.05f, 0.12f, 0.8f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "HomelanderHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = Vector3.up * 1.35f;
            head.transform.localScale = Vector3.one * 0.52f;

            var headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
                headRenderer.material.color = new Color(0.95f, 0.72f, 0.58f);

            var eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.transform.SetParent(root.transform, false);
            eyeL.transform.localPosition = new Vector3(-0.18f, 1.42f, 0.44f);
            eyeL.transform.localScale = new Vector3(0.07f, 0.04f, 0.025f);

            var eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.transform.SetParent(root.transform, false);
            eyeR.transform.localPosition = new Vector3(0.18f, 1.42f, 0.44f);
            eyeR.transform.localScale = new Vector3(0.07f, 0.04f, 0.025f);

            var cape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cape.name = "Cape";
            cape.transform.SetParent(root.transform, false);
            cape.transform.localPosition = new Vector3(0f, 0.75f, -0.42f);
            cape.transform.localScale = new Vector3(0.9f, 1.5f, 0.08f);

            var capeRenderer = cape.GetComponent<Renderer>();
            if (capeRenderer != null)
                capeRenderer.material.color = Color.red;

            return root;
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Fallback NPC creation failed: {ex}");
            return null;
        }
    }

    public void Despawn()
    {
        if (npc == null)
            return;

        ai?.Dispose();
        UnityEngine.Object.Destroy(npc);
        npc = null;

        MelonLogger.Msg("Homelander NPC despawned.");
    }

    public override void OnApplicationQuit()
    {
        Despawn();
        voice?.Dispose();
        sceneReady = false;
    }
}
