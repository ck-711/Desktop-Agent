using System;
using System.Collections.Generic;
using UniVRM10;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class FaceController : MonoBehaviour
{
    [SerializeField] private Vrm10Instance vrm;
    [SerializeField, Range(0.01f, 1f)] private float transitionDuration = 0.12f;

    private readonly Dictionary<ExpressionPreset, float> currentWeights =
        new Dictionary<ExpressionPreset, float>();
    private readonly Dictionary<ExpressionPreset, float> targetWeights =
        new Dictionary<ExpressionPreset, float>();

    private void Awake()
    {
        if (vrm == null)
        {
            vrm = GetComponentInChildren<Vrm10Instance>(true);
        }

        foreach (ExpressionPreset preset in FacePresets())
        {
            currentWeights[preset] = 0f;
            targetWeights[preset] = 0f;
        }
    }

    private void LateUpdate()
    {
        if (vrm == null || vrm.Vrm == null)
        {
            return;
        }

        float speed = Time.deltaTime / Mathf.Max(transitionDuration, 0.01f);
        foreach (ExpressionPreset preset in FacePresets())
        {
            float weight = Mathf.MoveTowards(
                currentWeights[preset], targetWeights[preset], speed);
            currentWeights[preset] = weight;
            vrm.Runtime.Expression.SetWeight(new ExpressionKey(preset), weight);
        }
    }

    public bool SetExpressionTag(string tag)
    {
        if (!TryGetPreset(tag, out ExpressionPreset selectedPreset))
        {
            return false;
        }

        foreach (ExpressionPreset preset in FacePresets())
        {
            targetWeights[preset] = preset == selectedPreset ? 1f : 0f;
        }

        return true;
    }

    private static bool TryGetPreset(string tag, out ExpressionPreset preset)
    {
        switch (tag?.Trim())
        {
            case string value when string.Equals(value, "Joy", StringComparison.OrdinalIgnoreCase):
                preset = ExpressionPreset.happy;
                return true;
            case string value when string.Equals(value, "Angry", StringComparison.OrdinalIgnoreCase):
                preset = ExpressionPreset.angry;
                return true;
            case string value when string.Equals(value, "Sorrow", StringComparison.OrdinalIgnoreCase):
                preset = ExpressionPreset.sad;
                return true;
            case string value when string.Equals(value, "Fun", StringComparison.OrdinalIgnoreCase):
                preset = ExpressionPreset.relaxed;
                return true;
            case string value when string.Equals(value, "Surprised", StringComparison.OrdinalIgnoreCase):
                preset = ExpressionPreset.surprised;
                return true;
            default:
                preset = default;
                return false;
        }
    }

    private static IEnumerable<ExpressionPreset> FacePresets()
    {
        yield return ExpressionPreset.happy;
        yield return ExpressionPreset.angry;
        yield return ExpressionPreset.sad;
        yield return ExpressionPreset.relaxed;
        yield return ExpressionPreset.surprised;
    }
}
