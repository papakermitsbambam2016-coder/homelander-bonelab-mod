using System;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

internal sealed class HeatVisionSystem
{
    private LineRenderer? left;
    private LineRenderer? right;

    public void Ensure(GameObject npc)
    {
        if (left != null && right != null)
            return;

        try
        {
            left = CreateBeam(npc.transform, "Homelander_HeatVision_L");
            right = CreateBeam(npc.transform, "Homelander_HeatVision_R");
            SetVisible(false);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Heat vision setup failed: {ex.Message}");
        }
    }

    private static LineRenderer CreateBeam(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.035f;
        line.endWidth = 0.01f;
        line.useWorldSpace = true;

        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = Color.red;
        line.material = material;

        return line;
    }

    public void Update(Transform npc, Transform target, bool active)
    {
        if (left == null || right == null)
            return;

        if (!active)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        Vector3 origin = npc.position + Vector3.up * 1.55f;
        Vector3 forward = (target.position - origin).normalized;
        Vector3 side = npc.right * 0.08f;

        left.SetPosition(0, origin + side);
        left.SetPosition(1, origin + side + forward * 30f);

        right.SetPosition(0, origin - side);
        right.SetPosition(1, origin - side + forward * 30f);
    }

    public void SetVisible(bool visible)
    {
        if (left != null) left.enabled = visible;
        if (right != null) right.enabled = visible;
    }

    public void Dispose()
    {
        if (left != null) UnityEngine.Object.Destroy(left.gameObject);
        if (right != null) UnityEngine.Object.Destroy(right.gameObject);
        left = null;
        right = null;
    }
}
