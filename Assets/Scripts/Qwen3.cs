using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class Qwen3 : MonoBehaviour
{
    [DllImport(
        "qwen3",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr qwen3_create(
        string modelPath,
        int nCtx,
        int nThreads
    );

    [DllImport(
        "qwen3",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int qwen3_generate(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prompt,
        StringBuilder output,
        int outputSize,
        int maxTokens
    );

    [DllImport(
        "qwen3",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void qwen3_destroy(
        IntPtr handle
    );

    private IntPtr handle = IntPtr.Zero;

    private void Start()
    {
        string modelPath =
            Application.streamingAssetsPath +
            "/models/Qwen3-1.7B-Q4_K_M.gguf";

        Debug.Log("Qwen3 model path: " + modelPath);

        handle = qwen3_create(
            modelPath,
            4096,
            8
        );

        if (handle == IntPtr.Zero)
        {
            Debug.LogError("Qwen3 初始化失败");
            return;
        }

        Debug.Log("Qwen3 初始化成功");

        TestGenerate();
    }

    private void TestGenerate()
    {
        StringBuilder output = new StringBuilder(8192);

        int ret = qwen3_generate(
            handle,
            "你好，请简单介绍一下自己。",
            output,
            output.Capacity,
            256
        );

        Debug.Log($"Qwen3 ret = {ret}");
        Debug.Log($"Qwen3 output = {output}");
    }

    private void OnDestroy()
    {
        if (handle != IntPtr.Zero)
        {
            qwen3_destroy(handle);
            handle = IntPtr.Zero;
        }
    }
}
