using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class Qwen3ASR : MonoBehaviour
{
    private const string Dll = "qwen3-asr";
    private IntPtr context;

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qwen3asr_create();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qwen3asr_destroy(IntPtr context);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qwen3asr_load_model(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string modelPath);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qwen3asr_transcribe_wav(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string audioPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string language,
        int maxTokens,
        int threads);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qwen3asr_get_text(IntPtr context);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qwen3asr_get_error(IntPtr context);

    public Qwen3ASR()
    {
        context = qwen3asr_create();
        if (context == IntPtr.Zero)
            throw new Exception("无法创建 Qwen3 ASR 实例");
    }

    public void LoadModel(string modelPath)
    {
        if (qwen3asr_load_model(context, modelPath) == 0)
            throw new Exception(GetError());
    }

    public string TranscribeWav(string wavPath, string language = "")
    {
        if (qwen3asr_transcribe_wav(context, wavPath, language, 256, 4) == 0)
            throw new Exception(GetError());

        return Marshal.PtrToStringUTF8(qwen3asr_get_text(context));
    }

    private string GetError() =>
        Marshal.PtrToStringUTF8(qwen3asr_get_error(context)) ?? "未知错误";

    public void Dispose()
    {
        if (context != IntPtr.Zero)
        {
            qwen3asr_destroy(context);
            context = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}