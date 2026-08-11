using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace HomelanderNPC;

internal sealed class VoiceSystem
{
    private readonly Dictionary<string, AudioClip> clips = new();
    private AudioSource? source;
    private GameObject? audioObject;

    public void Initialize()
    {
        try
        {
            var root = Path.Combine(MelonUtils.UserDataDirectory, "HomelanderNPC", "Audio");
            Directory.CreateDirectory(root);

            audioObject = new GameObject("HomelanderNPC_Voice");
            source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.playOnAwake = false;

            foreach (var name in new[] { "intro", "taunt", "attack", "hurt", "death", "heatvision" })
            {
                var path = Path.Combine(root, name + ".wav");
                if (!File.Exists(path))
                    continue;

                try
                {
                    var clip = LoadWav(path);
                    if (clip != null)
                        clips[name] = clip;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Voice load failed for {path}: {ex.Message}");
                }
            }

            MelonLogger.Msg($"Voice system ready. Loaded {clips.Count} clip(s).");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Voice system initialization failed: {ex}");
        }
    }

    public void Play(string name, Vector3 position)
    {
        if (!ModConfig.EnableVoice.Value || source == null)
            return;

        if (!clips.TryGetValue(name, out var clip))
            return;

        try
        {
            source.transform.position = position;
            source.PlayOneShot(clip, 1f);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"Voice playback failed: {ex.Message}");
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

        clips.Clear();
    }

    // Minimal PCM WAV loader: 8/16-bit PCM, mono or stereo.
    private static AudioClip? LoadWav(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 44)
            return null;

        int channels = BitConverter.ToInt16(data, 22);
        int sampleRate = BitConverter.ToInt32(data, 24);
        int bitsPerSample = BitConverter.ToInt16(data, 34);

        if (BitConverter.ToInt32(data, 0) != 0x46464952 ||
            BitConverter.ToInt32(data, 8) != 0x45564157)
            return null;

        int dataOffset = -1;
        int dataSize = 0;

        int pos = 12;
        while (pos + 8 <= data.Length)
        {
            int id = BitConverter.ToInt32(data, pos);
            int size = BitConverter.ToInt32(data, pos + 4);
            pos += 8;

            if (id == 0x61746164) // "data"
            {
                dataOffset = pos;
                dataSize = Math.Min(size, data.Length - pos);
                break;
            }

            pos += size;
            if ((size & 1) != 0) pos++;
        }

        if (dataOffset < 0 || dataSize <= 0 || channels <= 0 || sampleRate <= 0)
            return null;

        if (bitsPerSample != 8 && bitsPerSample != 16)
            return null;

        int sampleCount = dataSize / (bitsPerSample / 8) / channels;
        var samples = new float[sampleCount * channels];

        if (bitsPerSample == 16)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                short s = BitConverter.ToInt16(data, dataOffset + i * 2);
                samples[i] = s / 32768f;
            }
        }
        else
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (data[dataOffset + i] - 128) / 128f;
        }

        var clip = AudioClip.Create(
            Path.GetFileNameWithoutExtension(path),
            sampleCount,
            channels,
            sampleRate,
            false);

        clip.SetData(samples, 0);
        return clip;
    }
}
