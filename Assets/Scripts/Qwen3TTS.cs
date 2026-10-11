using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class Qwen3TTS : MonoBehaviour
{
    // =========================================================
    // C++ 参数结构体
    // 对应 qwen3tts_c_api.cpp 中的 Qwen3TtsParams
    // =========================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct Qwen3TtsParams
    {
        public int max_audio_tokens;
        public float temperature;
        public float top_p;
        public int top_k;
        public int n_threads;
        public float repetition_penalty;
        public int language_id;
    }

    // =========================================================
    // C++ 音频结构体
    //
    // 对应：
    //
    // struct Qwen3TtsAudio {
    //     const float * samples;
    //     int32_t n_samples;
    //     int32_t sample_rate;
    // };
    // =========================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct Qwen3TtsAudio
    {
        public IntPtr samples;
        public int n_samples;
        public int sample_rate;
    }

    // =========================================================
    // C++ DLL
    // =========================================================

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_default_params",
        ExactSpelling = true)]
    private static extern void qwen3_tts_default_params(
        out Qwen3TtsParams parameters);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_create",
        ExactSpelling = true)]
    private static extern IntPtr qwen3_tts_create(
        string model_dir,
        int n_threads);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_is_loaded",
        ExactSpelling = true)]
    private static extern int qwen3_tts_is_loaded(
        IntPtr tts);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_synthesize",
        ExactSpelling = true)]
    private static extern IntPtr qwen3_tts_synthesize(
        IntPtr tts,
        string text,
        ref Qwen3TtsParams parameters);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_free_audio",
        ExactSpelling = true)]
    private static extern void qwen3_tts_free_audio(
        IntPtr audio);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_destroy",
        ExactSpelling = true)]
    private static extern void qwen3_tts_destroy(
        IntPtr tts);

    [DllImport(
        "qwen3tts",
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "qwen3_tts_get_error",
        ExactSpelling = true)]
    private static extern IntPtr qwen3_tts_get_error(
        IntPtr tts);


    // =========================================================
    // Unity
    // =========================================================

    [Header("Qwen3-TTS")]
    [SerializeField]
    private string modelPath = "";

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private int nThreads = 4;

    [SerializeField]
    [Tooltip("Uses the CUDA backend for all Qwen3-TTS model components. Falls back to CPU if CUDA is unavailable.")]
    private bool useGpu = true;

    [SerializeField]
    [Min(0)]
    [Tooltip("Zero-based CUDA device index used when GPU acceleration is enabled.")]
    private int gpuDeviceIndex = 0;


    // C++ Qwen3Tts*
    private IntPtr ttsHandle = IntPtr.Zero;

    private Qwen3TtsParams parameters;


    // =========================================================
    // 初始化
    // =========================================================

    private void Start()
    {
        modelPath = Path.Combine(
    Application.streamingAssetsPath,
    "models"
);

        Initialize();

        Speak("你好，欢迎使用 Qwen3-TTS。");
    }


    public bool Initialize()
    {
        if (ttsHandle != IntPtr.Zero)
        {
            Debug.LogWarning("Qwen3-TTS already initialized.");
            return true;
        }

        // -----------------------------------------------------
        // 1. 检查模型路径
        // -----------------------------------------------------

        if (string.IsNullOrEmpty(modelPath))
        {
            Debug.LogError("Qwen3-TTS model path is empty.");
            return false;
        }

        if (!Directory.Exists(modelPath))
        {
            Debug.LogError(
                $"Qwen3-TTS model directory does not exist:\n{modelPath}");

            return false;
        }

        // -----------------------------------------------------
        // 2. 获取默认参数
        // -----------------------------------------------------

        ConfigureBackend();

        qwen3_tts_default_params(out parameters);

        // 可以根据需要修改
        parameters.n_threads = nThreads;

        // -----------------------------------------------------
        // 3. 创建 Qwen3-TTS
        // -----------------------------------------------------

        Debug.Log(
            $"Loading Qwen3-TTS model:\n{modelPath}");

        ttsHandle = qwen3_tts_create(
            modelPath,
            nThreads);

        // -----------------------------------------------------
        // 4. 检查是否加载成功
        // -----------------------------------------------------

        if (ttsHandle == IntPtr.Zero)
        {
            Debug.LogError(
                "Failed to create Qwen3-TTS.");

            return false;
        }

        int loaded = qwen3_tts_is_loaded(ttsHandle);

        if (loaded == 0)
        {
            Debug.LogError(
                "Qwen3-TTS model is not loaded.");

            qwen3_tts_destroy(ttsHandle);
            ttsHandle = IntPtr.Zero;

            return false;
        }

        Debug.Log("Qwen3-TTS initialized successfully.");

        return true;
    }


    // =========================================================
    // 文本 → 语音
    // =========================================================

    private void ConfigureBackend()
    {
        Environment.SetEnvironmentVariable(
            "QWEN3_TTS_BACKEND",
            useGpu ? "cuda" : "cpu");
        Environment.SetEnvironmentVariable(
            "QWEN3_TTS_DEVICE",
            useGpu
                ? gpuDeviceIndex.ToString(CultureInfo.InvariantCulture)
                : null);
    }



    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Text is empty.");
            return;
        }

        if (ttsHandle == IntPtr.Zero)
        {
            Debug.LogError(
                "Qwen3-TTS is not initialized.");

            return;
        }

        Debug.Log($"Qwen3-TTS: {text}");

        // -----------------------------------------------------
        // 调用 C++
        // -----------------------------------------------------

        IntPtr audioPtr = qwen3_tts_synthesize(
            ttsHandle,
            text,
            ref parameters);

        // -----------------------------------------------------
        // 判断是否成功
        // -----------------------------------------------------

        if (audioPtr == IntPtr.Zero)
        {
            PrintError();
            return;
        }

        try
        {
            // -------------------------------------------------
            // 读取 C++ Qwen3TtsAudio
            // -------------------------------------------------

            Qwen3TtsAudio audio =
                Marshal.PtrToStructure<Qwen3TtsAudio>(
                    audioPtr);

            if (audio.samples == IntPtr.Zero ||
                audio.n_samples <= 0)
            {
                Debug.LogError(
                    "Qwen3-TTS returned empty audio.");

                return;
            }

            Debug.Log(
                $"Audio samples: {audio.n_samples}");

            Debug.Log(
                $"Sample rate: {audio.sample_rate}");


            // -------------------------------------------------
            // C++ float* → C# float[]
            // -------------------------------------------------

            float[] pcm =
                new float[audio.n_samples];

            Marshal.Copy(
                audio.samples,
                pcm,
                0,
                audio.n_samples);


            // -------------------------------------------------
            // 创建 Unity AudioClip
            // -------------------------------------------------

            AudioClip clip =
                AudioClip.Create(
                    "Qwen3TTS",
                    pcm.Length,
                    1,
                    audio.sample_rate,
                    false);


            // -------------------------------------------------
            // 设置 PCM 数据
            // -------------------------------------------------

            clip.SetData(
                pcm,
                0);


            // -------------------------------------------------
            // 播放
            // -------------------------------------------------

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning(
                    "AudioSource is not assigned.");
            }
        }
        finally
        {
            // -------------------------------------------------
            // 非常重要！
            //
            // C++ 中使用 new 分配的音频内存
            // 必须交给 C++ 自己释放
            // -------------------------------------------------

            qwen3_tts_free_audio(audioPtr);
        }
    }


    // =========================================================
    // 错误信息
    // =========================================================

    private void PrintError()
    {
        if (ttsHandle == IntPtr.Zero)
        {
            Debug.LogError(
                "Qwen3-TTS handle is null.");

            return;
        }

        IntPtr errorPtr =
            qwen3_tts_get_error(ttsHandle);

        if (errorPtr == IntPtr.Zero)
        {
            Debug.LogError(
                "Qwen3-TTS synthesis failed.");

            return;
        }

        string error =
            Marshal.PtrToStringAnsi(errorPtr);

        Debug.LogError(
            $"Qwen3-TTS error: {error}");
    }


    // =========================================================
    // 销毁
    // =========================================================

    private void OnDestroy()
    {
        if (ttsHandle != IntPtr.Zero)
        {
            qwen3_tts_destroy(ttsHandle);

            ttsHandle = IntPtr.Zero;

            Debug.Log(
                "Qwen3-TTS destroyed.");
        }
    }
}