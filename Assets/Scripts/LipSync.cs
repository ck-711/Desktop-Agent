using System;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;
using uLipSync;

[DefaultExecutionOrder(10000)]
public sealed class LipSync
    : MonoBehaviour
{
    [Header("Audio and VRM")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Vrm10Instance vrm;
    [SerializeField] private uLipSync.Profile profile;

    [Header("Profile phoneme names")]
    [SerializeField] private string phonemeA = "A";
    [SerializeField] private string phonemeI = "I";
    [SerializeField] private string phonemeU = "U";
    [SerializeField] private string phonemeE = "E";
    [SerializeField] private string phonemeO = "O";

    [Header("Tuning")]
    [SerializeField, Range(0f, 2f)] private float mouthGain = 1f;
    [SerializeField, Range(0.001f, 0.3f)] private float smoothing = 0.05f;

    private uLipSync.uLipSync analyzer;
    private readonly Dictionary<ExpressionPreset, float> targetWeights =
        new Dictionary<ExpressionPreset, float>();
    private readonly Dictionary<ExpressionPreset, float> currentWeights =
        new Dictionary<ExpressionPreset, float>();
    private bool warnedMissingProfile;

    private void Awake()
    {
        ResolveReferences();
        SetupAnalyzer();
        InitializeWeights();
    }

    private void OnDisable()
    {
        if (analyzer != null)
        {
            analyzer.onLipSyncUpdate.RemoveListener(OnLipSyncUpdate);
        }

        ResetMouth();
    }

    private void LateUpdate()
    {
        if (vrm == null || vrm.Vrm == null)
        {
            return;
        }

        foreach (ExpressionPreset preset in MouthPresets())
        {
            float current = currentWeights[preset];
            float target = targetWeights[preset];
            current = Mathf.MoveTowards(
                current,
                target,
                Time.deltaTime / Mathf.Max(smoothing, 0.001f));
            currentWeights[preset] = current;
            vrm.Runtime.Expression.SetWeight(
                new ExpressionKey(preset),
                current);
        }
    }

    private void ResolveReferences()
    {
        if (audioSource == null)
        {
            Qwen3TTS tts = GetComponent<Qwen3TTS>();
            audioSource = tts != null
                ? tts.PlaybackAudioSource
                : GetComponent<AudioSource>();
        }

        if (vrm == null)
        {
            vrm = GetComponentInChildren<Vrm10Instance>(true);
        }
    }

    private void SetupAnalyzer()
    {
        if (audioSource == null)
        {
            Debug.LogError(
                "Qwen3TTSLipSync: AudioSource is not assigned. Assign the same AudioSource used by Qwen3TTS.",
                this);
            return;
        }

        if (vrm == null)
        {
            Debug.LogError(
                "Qwen3TTSLipSync: Vrm10Instance was not found below this GameObject.",
                this);
            return;
        }

        if (profile == null)
        {
            if (!warnedMissingProfile)
            {
                warnedMissingProfile = true;
                Debug.LogWarning(
                    "Qwen3TTSLipSync: uLipSync Profile is not assigned. Create/calibrate a Profile and assign it to enable A/I/U/E/O lip sync.",
                    this);
            }
            return;
        }

        analyzer = audioSource.gameObject.GetComponent<uLipSync.uLipSync>();
        if (analyzer == null)
        {
            analyzer = audioSource.gameObject.AddComponent<uLipSync.uLipSync>();
        }

        analyzer.profile = profile;
        analyzer.onLipSyncUpdate.RemoveListener(OnLipSyncUpdate);
        analyzer.onLipSyncUpdate.AddListener(OnLipSyncUpdate);
    }

    private void InitializeWeights()
    {
        foreach (ExpressionPreset preset in MouthPresets())
        {
            targetWeights[preset] = 0f;
            currentWeights[preset] = 0f;
        }
    }

    private void OnLipSyncUpdate(LipSyncInfo info)
    {
        float volume = Mathf.Clamp01(info.volume) * mouthGain;
        targetWeights[ExpressionPreset.aa] = FindRatio(info.phonemeRatios, phonemeA) * volume;
        targetWeights[ExpressionPreset.ih] = FindRatio(info.phonemeRatios, phonemeI) * volume;
        targetWeights[ExpressionPreset.ou] = FindRatio(info.phonemeRatios, phonemeU) * volume;
        targetWeights[ExpressionPreset.ee] = FindRatio(info.phonemeRatios, phonemeE) * volume;
        targetWeights[ExpressionPreset.oh] = FindRatio(info.phonemeRatios, phonemeO) * volume;
    }

    private static float FindRatio(
        Dictionary<string, float> ratios,
        string phoneme)
    {
        if (ratios == null || string.IsNullOrWhiteSpace(phoneme))
        {
            return 0f;
        }

        if (ratios.TryGetValue(phoneme, out float value))
        {
            return Mathf.Clamp01(value);
        }

        foreach (KeyValuePair<string, float> pair in ratios)
        {
            if (string.Equals(pair.Key, phoneme, StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp01(pair.Value);
            }
        }

        return 0f;
    }

    private void ResetMouth()
    {
        foreach (ExpressionPreset preset in MouthPresets())
        {
            targetWeights[preset] = 0f;
            currentWeights[preset] = 0f;

            if (vrm != null && vrm.Vrm != null)
            {
                vrm.Runtime.Expression.SetWeight(
                    new ExpressionKey(preset),
                    0f);
            }
        }
    }

    private static IEnumerable<ExpressionPreset> MouthPresets()
    {
        yield return ExpressionPreset.aa;
        yield return ExpressionPreset.ih;
        yield return ExpressionPreset.ou;
        yield return ExpressionPreset.ee;
        yield return ExpressionPreset.oh;
    }
}
