using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class Qwen3 : MonoBehaviour
{
    private const string DllName = "qwen3";

    public string modelPath;
    public uint contextSize = 4096;
    public int threadCount = 4;
    [Tooltip("0 uses CPU only; a positive value offloads that many layers; a negative value offloads all layers to the GPU.")]
    public int gpuLayerCount = -1;

    private IntPtr handle = IntPtr.Zero;
    private volatile bool cancelRequested;
    private Task generationTask;

    private readonly ConcurrentQueue<string> outputQueue = new();

    public event Action<string> OnText;
    public event Action<Qwen3Status> OnFinished;

    public enum Qwen3Status
    {
        Ok = 0,
        Cancelled = 1,
        InvalidArgument = -1,
        TokenizeFailed = -2,
        ContextTooSmall = -3,
        DecodeFailed = -4,
        PieceFailed = -5,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TokenCallback(
        IntPtr text,
        UIntPtr textSize,
        IntPtr userData);

    private static readonly TokenCallback tokenCallback = OnNativeToken;

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qwen3_create(
        byte[] modelPath,
        uint contextSize,
        int threadCount,
        int gpuLayerCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern Qwen3Status qwen3_generate_stream(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prompt,
        UIntPtr promptSize,
        int maxTokens,
        TokenCallback callback,
        IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qwen3_destroy(IntPtr handle);
    private async void Start()
    {
        if (!await LoadAsync())
        {
            Debug.LogError($"Qwen3 failed to load model: {modelPath}");
        }
    }
    public async Task<bool> LoadAsync()
    {
        modelPath = Path.Combine(
            Application.streamingAssetsPath,
            "models",
            "Qwen3-1.7B-Q4_K_M.gguf");
        if (handle != IntPtr.Zero)
        {
            return true;
        }

        byte[] path = Utf8NullTerminated(modelPath);

        handle = await Task.Run(() =>
            qwen3_create(path, contextSize, threadCount, gpuLayerCount));

        return handle != IntPtr.Zero;
    }

    public async Task<Qwen3Status> GenerateAsync(string prompt)
    {
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Call LoadAsync first.");
        }

        if (generationTask != null && !generationTask.IsCompleted)
        {
            throw new InvalidOperationException("Generation is already running.");
        }

        cancelRequested = false;
        GCHandle selfHandle = GCHandle.Alloc(this);

        Qwen3Status result = Qwen3Status.InvalidArgument;

        generationTask = Task.Run(() =>
        {
            try
            {
                result = qwen3_generate_stream(
                    handle,
                    prompt,
                    (UIntPtr)Encoding.UTF8.GetByteCount(prompt),
                    512,
                    tokenCallback,
                    GCHandle.ToIntPtr(selfHandle));
            }
            finally
            {
                selfHandle.Free();
            }
        });

        await generationTask;
        DrainOutputQueue();
        OnFinished?.Invoke(result);
        return result;
    }

    public void Cancel()
    {
        cancelRequested = true;
    }

    private static int OnNativeToken(
        IntPtr text,
        UIntPtr textSize,
        IntPtr userData)
    {
        var self = (Qwen3)GCHandle.FromIntPtr(userData).Target;
        if (self.cancelRequested || text == IntPtr.Zero)
        {
            return 1;
        }

        ulong size = textSize.ToUInt64();
        if (size == 0 || size > int.MaxValue)
        {
            return 1;
        }

        byte[] bytes = new byte[(int)size];
        Marshal.Copy(text, bytes, 0, bytes.Length);
        self.outputQueue.Enqueue(Encoding.UTF8.GetString(bytes));
        return 0;
    }

    private void Update()
    {
        DrainOutputQueue();
    }

    private void DrainOutputQueue()
    {
        while (outputQueue.TryDequeue(out string text))
        {
            OnText?.Invoke(text);
        }
    }

    private async void OnDestroy()
    {
        cancelRequested = true;

        if (generationTask != null)
        {
            await generationTask;
        }

        if (handle != IntPtr.Zero)
        {
            qwen3_destroy(handle);
            handle = IntPtr.Zero;
        }

    }

    private static byte[] Utf8NullTerminated(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        Array.Resize(ref bytes, bytes.Length + 1);
        return bytes;
    }

}
