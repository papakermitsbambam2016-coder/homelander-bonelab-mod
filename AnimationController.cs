using System;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

internal sealed class AnimationController
{
    private Component? animator;
    private string current = "";

    public void Find(GameObject npc)
    {
        try
        {
            // Avoid a hard AnimationModule dependency. Animator is found by type name.
            foreach (var component in npc.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name == "Animator")
                {
                    animator = component;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Animator scan failed: {ex.Message}");
        }
    }

    public void Play(string state)
    {
        if (animator == null || string.Equals(current, state, StringComparison.OrdinalIgnoreCase))
            return;

        current = state;

        try
        {
            var method = animator.GetType().GetMethod("Play", new[] { typeof(string) });
            method?.Invoke(animator, new object[] { state });
        }
        catch
        {
            // State may not exist in the supplied asset. That's okay.
        }
    }
}
