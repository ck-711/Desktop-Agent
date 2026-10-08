using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class Qwen3
    : MonoBehaviour
{
    [DllImport("qwen3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int qwen3_test(
        string input,
        StringBuilder output,
        int outputSize
    );

    private void Start()
    {
        StringBuilder output = new StringBuilder(1024);

        int result = qwen3_test(
            "hello",
            output,
            output.Capacity
        );

        Debug.Log($"qwen3_test result = {result}");
        Debug.Log($"qwen3_test output = {output}");
    }
}