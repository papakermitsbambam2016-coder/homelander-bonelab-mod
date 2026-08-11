using System;
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
    private float flightHeight = 0f;
    private bool heatVisionActive;

    public bool IsAlive { get; private set; } = true;

    public HomelanderAI(HeatVisionSystem heatVision, VoiceSystem voice, AnimationController animation)
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
        if (npc == null || !IsAlive || !ModConfig.EnableAI.Value)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        var target = camera.transform;
        float dt = Time.deltaTime;

        attackCooldown -= dt;
        tauntCooldown -= dt;

        Vector3 npcPos = npc.transform.position;
        Vector3 targetPos = target.position;

        float distance = Vector3.Distance(npcPos, targetPos);

        if (ModConfig.EnableFlight.Value && flightHeight > 0f)
        {
            Vector3 desired = targetPos + Vector3.up * flightHeight - target.forward * 4f;
            npc.transform.position = Vector3.Lerp(npc.transform.position, desired, dt * 1.5f);
            animation.Play("Fly");
        }
        else if (distance > ModConfig.FollowDistance.Value)
        {
            Vector3 flatTarget = new Vector3(targetPos.x, npcPos.y, targetPos.z);
            Vector3 direction = flatTarget - npcPos;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                npc.transform.position += direction * ModConfig.MoveSpeed.Value * dt;

                var look = Quaternion.LookRotation(direction, Vector3.up);
                npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, look, dt * 8f);
            }

            animation.Play("Walk");
        }
        else
        {
            animation.Play("Idle");
        }

        if (distance <= ModConfig.AttackDistance.Value && attackCooldown <= 0f)
            Attack(target);

        if (distance < 12f && tauntCooldown <= 0f)
        {
            voice.Play("taunt", npc.transform.position);
            tauntCooldown = 12f;
        }

        heatVision.Update(npc.transform, target, heatVisionActive);
    }

    public void Attack(Transform target)
    {
        if (npc == null)
            return;

        attackCooldown = 2.0f;
        animation.Play("Attack");
        voice.Play("attack", npc.transform.position);

        // A safe visual/gameplay test: apply a forward impulse to nearby rigidbodies.
        // This does not directly depend on BONELAB's internal damage classes.
        try
        {
            Vector3 origin = npc.transform.position + Vector3.up * 1.2f;
            Vector3 dir = (target.position - origin).normalized;

            foreach (var body in UnityEngine.Object.FindObjectsOfType<Rigidbody>())
            {
                if (body == null) continue;

                float d = Vector3.Distance(body.position, npc.transform.position);
                if (d > 3.0f) continue;

                body.AddForce((dir + Vector3.up * 0.35f) * 12f, ForceMode.Impulse);
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Attack physics failed: {ex.Message}");
        }
    }

    public void ToggleFlight()
    {
        if (npc == null || !ModConfig.EnableFlight.Value)
            return;

        flightHeight = flightHeight > 0f ? 0f : 3.5f;
        heatVisionActive = false;
        voice.Play("taunt", npc.transform.position);
    }

    public void ToggleHeatVision()
    {
        if (npc == null || !ModConfig.EnableHeatVision.Value)
            return;

        heatVisionActive = !heatVisionActive;
        if (heatVisionActive)
        {
            animation.Play("Attack");
            voice.Play("heatvision", npc.transform.position);
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
        voice.Play("death", npc.transform.position);
    }

    public void Dispose()
    {
        heatVision.Dispose();
        npc = null;
    }
}
