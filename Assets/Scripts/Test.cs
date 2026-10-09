using System.IO;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Qwen3ASR asr;

    void Start()
    {
        asr = new Qwen3ASR();

        string model = Path.Combine(
            Application.streamingAssetsPath, "models",
            "qwen3-asr-0.6b-q8_0.gguf");

        string wav = Path.Combine(
            Application.streamingAssetsPath,
            "audio.wav");

        asr.LoadModel(model);
        Debug.Log(asr.TranscribeWav(wav));
    }

    void OnDestroy()
    {
        asr?.Dispose();
    }
}
