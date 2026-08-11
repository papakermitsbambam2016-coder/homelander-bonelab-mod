using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

internal sealed class VoiceSystem
{
    private readonly Dictionary<string, object> clips = new();

    private GameObject? audioObject;
    private Component? audioSource;

    public void Initialize()
    {
        try
        {
            string root = Path.Combine(
                MelonUtils.UserDataDirectory,
                "HomelanderNPC",
                "Audio");

            Directory.CreateDirectory(root);

            // Find AudioSource without referencing UnityEngine.AudioModule
            // at compile time.
            Type? audioSourceType =
                Type.GetType("UnityEngine.AudioSource, UnityEngine.AudioModule");

            if (audioSourceType == null)
            {
                MelonLogger.Warning(
                    "UnityEngine.AudioModule was not found. " +
                    "Voice system will be disabled.");
                return;
            }

            audioObject = new GameObject("HomelanderNPC_Voice");

            audioSource = audioObject.AddComponent(audioSourceType);

            SetProperty(audioSource, "spatialBlend", 1f);
            SetProperty(audioSource, "playOnAwake", false);

            foreach (string name in new[]
                     {
                         "intro",
                         "taunt",
                         "attack",
                         "hurt",
                         "death",
                         "heatvision"
                     })
            {
                string path = Path.Combine(root, name + ".wav");

                if (!File.Exists(path))
                    continue;

                object? clip = LoadWav(path);

                if (clip != null)
                    clips[name] = clip;
            }

            MelonLogger.Msg(
                $"Voice system initialized. Loaded {clips.Count} voice clip(s).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning(
                $"Voice system initialization failed: {ex.Message}");
        }
    }

    public void Play(string name, Vector3 position)
    {
        if (!ModConfig.EnableVoice.Value)
            return;

        if (audioSource == null)
            return;

        if (!clips.TryGetValue(name, out object? clip))
            return;

        try
        {
            audioSource.transform.position = position;

            MethodInfo? method =
                audioSource.GetType().GetMethod(
                    "PlayOneShot",
                    new[] { clip.GetType() });

            if (method != null)
            {
                method.Invoke(
                    audioSource,
                    new[] { clip });
            }
            else
            {
                MelonLogger.Warning(
                    "Could not find AudioSource.PlayOneShot.");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning(
                $"Could not play voice '{name}': {ex.Message}");
        }
    }

    private static void SetProperty(
        object? instance,
        string property,
        object value)
    {
        if (instance == null)
            return;

        try
        {
            PropertyInfo? info =
                instance.GetType().GetProperty(property);

            if (info != null && info.CanWrite)
                info.SetValue(instance, value);
        }
        catch
        {
        }
    }

    private static object? LoadWav(string path)
    {
        try
        {
            byte[] data = File.ReadAllBytes(path);

            if (data.Length < 44)
                return null;

            int channels =
                BitConverter.ToInt16(data, 22);

            int sampleRate =
                BitConverter.ToInt32(data, 24);

            int bitsPerSample =
                BitConverter.ToInt16(data, 34);

            if (channels <= 0 ||
                sampleRate <= 0 ||
                (bitsPerSample != 8 &&
                 bitsPerSample != 16))
            {
                return null;
            }

            int dataOffset = -1;
            int dataSize = 0;

            int position = 12;

            while (position + 8 <= data.Length)
            {
                string chunk =
                    System.Text.Encoding.ASCII.GetString(
                        data,
                        position,
                        4);

                int size =
                    BitConverter.ToInt32(
                        data,
                        position + 4);

                position += 8;

                if (chunk == "data")
                {
                    dataOffset = position;

                    dataSize =
                        Math.Min(
                            size,
                            data.Length - position);

                    break;
                }

                position += size;

                if ((size & 1) != 0)
                    position++;
            }

            if (dataOffset < 0 ||
                dataSize <= 0)
                return null;

            int bytesPerSample =
                bitsPerSample / 8;

            int sampleCount =
                dataSize /
                bytesPerSample /
                channels;

            float[] samples =
                new float[
                    sampleCount * channels];

            if (bitsPerSample == 16)
            {
                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    short value =
                        BitConverter.ToInt16(
                            data,
                            dataOffset + i * 2);

                    samples[i] =
                        value / 32768f;
                }
            }
            else
            {
                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    samples[i] =
                        (data[dataOffset + i] - 128) /
                        128f;
                }
            }

            // Find AudioClip without referencing AudioModule
            // at compile time.
            Type? audioClipType =
                Type.GetType(
                    "UnityEngine.AudioClip, UnityEngine.AudioModule");

            if (audioClipType == null)
            {
                MelonLogger.Warning(
                    "UnityEngine.AudioModule is unavailable.");
                return null;
            }

            MethodInfo? create =
                audioClipType.GetMethod(
                    "Create",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(int),
                        typeof(int),
                        typeof(bool)
                    },
                    null);

            if (create == null)
            {
                MelonLogger.Warning(
                    "Could not find AudioClip.Create.");
                return null;
            }

            object? clip =
                create.Invoke(
                    null,
                    new object[]
                    {
                        Path.GetFileNameWithoutExtension(path),
                        sampleCount,
                        channels,
                        sampleRate,
                        false
                    });

            if (clip == null)
                return null;

            MethodInfo? setData =
                audioClipType.GetMethod(
                    "SetData",
                    new[]
                    {
                        typeof(float[]),
                        typeof(int)
                    });

            if (setData == null)
                return null;

            setData.Invoke(
                clip,
                new object[]
                {
                    samples,
                    0
                });

            return clip;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning(
                $"WAV loading failed: {ex.Message}");

            return null;
        }
    }

    public void Dispose()
    {
        try
        {
            if (audioObject != null)
                UnityEngine.Object.Destroy(audioObject);
        }
        catch
        {
        }

        audioObject = null;
        audioSource = null;

        clips.Clear();
    }
}
