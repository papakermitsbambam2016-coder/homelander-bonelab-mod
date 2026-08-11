using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

internal sealed class HomelanderAI
{
    private readonly HeatVisionSystem heatVision;
    private readonly VoiceSystem voice;
    private readonly AnimationController animation;

    private GameObject? npc;
    private float attackCooldown;
    private float tauntCooldown;
    private float flightHeight;
    private bool heatVisionActive;

    public bool IsAlive { get; private set; } = true;

    public HomelanderAI(
        HeatVisionSystem heatVision,
        VoiceSystem voice,
        AnimationController animation)
    {
        this.heatVision = heatVision;
        this.voice = voice;
        this.animation = animation;
    }

    public void Attach(GameObject go)
    {
        npc = go;

        IsAlive = true;
        attackCooldown = 0f;
        tauntCooldown = 0f;
        flightHeight = 0f;
        heatVisionActive = false;

        animation.Find(go);
        heatVision.Ensure(go);

        animation.Play("Idle");

        voice.Play("intro", go.transform.position);
    }

    public void Update()
    {
        if (npc == null)
            return;

        if (!IsAlive)
            return;

        if (!ModConfig.EnableAI.Value)
            return;

        Camera? camera = Camera.main;

        if (camera == null)
            return;

        Transform target = camera.transform;

        float deltaTime = Time.deltaTime;

        attackCooldown -= deltaTime;
        tauntCooldown -= deltaTime;

        Vector3 npcPosition = npc.transform.position;
        Vector3 targetPosition = target.position;

        float distance =
            Vector3.Distance(
                npcPosition,
                targetPosition);

        if (ModConfig.EnableFlight.Value &&
            flightHeight > 0f)
        {
            Vector3 desired =
                targetPosition +
                Vector3.up * flightHeight -
                target.forward * 4f;

            npc.transform.position =
                Vector3.Lerp(
                    npc.transform.position,
                    desired,
                    deltaTime * 1.5f);

            animation.Play("Fly");
        }
        else if (distance >
                 ModConfig.FollowDistance.Value)
        {
            Vector3 flatTarget =
                new Vector3(
                    targetPosition.x,
                    npcPosition.y,
                    targetPosition.z);

            Vector3 direction =
                flatTarget -
                npcPosition;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();

                npc.transform.position +=
                    direction *
                    ModConfig.MoveSpeed.Value *
                    deltaTime;

                Quaternion look =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up);

                npc.transform.rotation =
                    Quaternion.Slerp(
                        npc.transform.rotation,
                        look,
                        deltaTime * 8f);
            }

            animation.Play("Walk");
        }
        else
        {
            animation.Play("Idle");
        }

        if (distance <= ModConfig.AttackDistance.Value &&
            attackCooldown <= 0f)
        {
            Attack(target);
        }

        if (distance < 12f &&
            tauntCooldown <= 0f)
        {
            voice.Play(
                "taunt",
                npc.transform.position);

            tauntCooldown = 12f;
        }

        heatVision.Update(
            npc.transform,
            target,
            heatVisionActive);
    }

    public void Attack(Transform target)
    {
        if (npc == null)
            return;

        attackCooldown = 2f;

        animation.Play("Attack");

        voice.Play(
            "attack",
            npc.transform.position);

        /*
         * We intentionally don't reference Rigidbody or ForceMode here.
         *
         * Those are Unity PhysicsModule types and aren't currently
         * available to the GitHub compiler.
         *
         * Instead, we find Rigidbody at runtime using reflection.
         */

        try
        {
            Type? rigidbodyType =
                FindUnityType(
                    "UnityEngine.Rigidbody");

            if (rigidbodyType == null)
            {
                MelonLogger.Warning(
                    "Rigidbody type was not available.");
                return;
            }

            Vector3 origin =
                npc.transform.position +
                Vector3.up * 1.2f;

            Vector3 direction =
                (target.position - origin).normalized;

            UnityEngine.Object[] objects =
                UnityEngine.Object.FindObjectsOfType(
                    rigidbodyType);

            foreach (UnityEngine.Object obj in objects)
            {
                if (obj == null)
                    continue;

                Component? component =
                    obj as Component;

                if (component == null)
                    continue;

                float distance =
                    Vector3.Distance(
                        component.transform.position,
                        npc.transform.position);

                if (distance > 3f)
                    continue;

                ApplyImpulse(
                    component,
                    direction);
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning(
                $"Super-strength physics failed: {ex.Message}");
        }
    }

    private static void ApplyImpulse(
        Component rigidbody,
        Vector3 direction)
    {
        try
        {
            MethodInfo[] methods =
                rigidbody
                    .GetType()
                    .GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public);

            foreach (MethodInfo method in methods)
            {
                if (method.Name != "AddForce")
                    continue;

                ParameterInfo[] parameters =
                    method.GetParameters();

                if (parameters.Length != 2)
                    continue;

                if (parameters[0].ParameterType !=
                    typeof(Vector3))
                    continue;

                Type forceModeType =
                    parameters[1].ParameterType;

                object? impulse =
                    Enum.Parse(
                        forceModeType,
                        "Impulse");

                Vector3 force =
                    (direction +
                     Vector3.up * 0.35f) *
                    12f;

                method.Invoke(
                    rigidbody,
                    new object[]
                    {
                        force,
                        impulse
                    });

                return;
            }
        }
        catch
        {
            // Physics is optional for this first test.
        }
    }

    public void ToggleFlight()
    {
        if (npc == null)
            return;

        if (!ModConfig.EnableFlight.Value)
            return;

        if (flightHeight > 0f)
            flightHeight = 0f;
        else
            flightHeight = 3.5f;

        heatVisionActive = false;

        voice.Play(
            "taunt",
            npc.transform.position);
    }

    public void ToggleHeatVision()
    {
        if (npc == null)
            return;

        if (!ModConfig.EnableHeatVision.Value)
            return;

        heatVisionActive =
            !heatVisionActive;

        if (heatVisionActive)
        {
            animation.Play("Attack");

            voice.Play(
                "heatvision",
                npc.transform.position);
        }
        else
        {
            animation.Play("Idle");
        }
    }

    public void Kill()
    {
        if (npc == null)
            return;

        IsAlive = false;

        heatVisionActive = false;

        heatVision.SetVisible(false);

        animation.Play("Death");

        voice.Play(
            "death",
            npc.transform.position);
    }

    public void Dispose()
    {
        heatVision.Dispose();

        npc = null;
    }

    private static Type? FindUnityType(
        string typeName)
    {
        try
        {
            foreach (Assembly assembly
                     in AppDomain.CurrentDomain
                         .GetAssemblies())
            {
                Type? type =
                    assembly.GetType(
                        typeName,
                        false);

                if (type != null)
                    return type;
            }
        }
        catch
        {
        }

        return null;
    }
}
