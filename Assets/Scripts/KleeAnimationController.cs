using System;
using UnityEngine;

public sealed class KleeAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Range(0.01f, 1f)] private float transitionDuration = 0.15f;

    private const int BaseLayer = 0;
    private const int AdditiveLayer = 1;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    public bool SetAnimationTag(string tag)
    {
        if (animator == null ||
            !TryGetState(tag, out string stateName, out int layer))
        {
            return false;
        }

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            layer,
            0f);
        return true;
    }

    private static bool TryGetState(
        string tag,
        out string stateName,
        out int layer)
    {
        switch (tag?.Trim())
        {
            case string value when string.Equals(value, "angry_hands_on_waist", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_angry_01_hands_on_waist";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "brave_hand_on_chest", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_brave_01_hand_on_chest";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "calm_hands_on_back", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_calm_01_hands_on_back";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "concern_right_hand_front", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_concern_01_right_hand_front";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "energetic_right_fist_up", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_energetic_01_right_fist_up";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "energetic_right_hand_piece", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_energetic_02_right_hand_piece";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "pitiable_right_hand_on_back_head", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_pitiable_01_right_hand_on_back_head";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "surprise_hands_open_front", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Idle_surprise_01_hands_open_front";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "walking", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Other_walking_01";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "waving_arm", StringComparison.OrdinalIgnoreCase):
                stateName = "Base Layer.AGIA_Other_waving_arm_01";
                layer = BaseLayer;
                return true;
            case string value when string.Equals(value, "look_away", StringComparison.OrdinalIgnoreCase):
                stateName = "Additive Layer.AGIA_Layer_look_away_01";
                layer = AdditiveLayer;
                return true;
            case string value when string.Equals(value, "nodding_once", StringComparison.OrdinalIgnoreCase):
                stateName = "Additive Layer.AGIA_Layer_nodding_once_01";
                layer = AdditiveLayer;
                return true;
            case string value when string.Equals(value, "swinging_body", StringComparison.OrdinalIgnoreCase):
                stateName = "Additive Layer.AGIA_Layer_swinging_body_01";
                layer = AdditiveLayer;
                return true;
            default:
                stateName = null;
                layer = -1;
                return false;
        }
    }
}
