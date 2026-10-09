using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
    private static extern void qwen3_destroy(
        IntPtr handle
    );
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TokenCallback(
        IntPtr text,
        int textSize,
        IntPtr userData
    );



    [DllImport("qwen3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int qwen3_generate_stream(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prompt,
        int maxTokens,
        TokenCallback callback,
        IntPtr userData
    );

    private readonly ConcurrentQueue<string> textQueue = new();
    private readonly StringBuilder response = new();

    private TokenCallback callback;
    private bool generating;

    // 替换为你原有的模型句柄
    private IntPtr handle;

    public Text outputText;


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
    }

    private void Update()
    {
        bool changed = false;

        while (textQueue.TryDequeue(out string chunk))
        {
            response.Append(chunk);
            changed = true;
        }

        if (changed && outputText != null)
        {
            outputText.text = response.ToString();
        }
    }
    public async void GenerateStream(string prompt)
    {
        if (generating || handle == IntPtr.Zero)
            return;

        generating = true;
        response.Clear();

        while (textQueue.TryDequeue(out _)) { }

        if (outputText != null)
            outputText.text = "";

        // 保持委托存活，直到原生函数返回
        callback = OnToken;

        try
        {
            int result = await Task.Run(() =>
                qwen3_generate_stream(
                    handle,
                    prompt,
                    512,
                    callback,
                    IntPtr.Zero
                )
            );

            if (result != 0)
            {
                Debug.LogError($"Qwen3 流式生成失败，错误码：{result}");
            }
        }
        finally
        {
            generating = false;
        }
    }

    private void OnToken(
    IntPtr text,
    int textSize,
    IntPtr userData
)
    {
        if (text == IntPtr.Zero || textSize <= 0)
            return;

        byte[] bytes = new byte[textSize];
        Marshal.Copy(text, bytes, 0, textSize);

        string chunk = Encoding.UTF8.GetString(bytes);

        textQueue.Enqueue(chunk);
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
