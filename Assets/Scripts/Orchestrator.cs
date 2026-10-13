using System;
using System.Text;
using TMPro;
using UnityEngine;

public class Orchestrator : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Qwen3 qwen3;
    [SerializeField] private Qwen3TTS qwen3TTS;
    [SerializeField] private TMP_Text outputText;

    [SerializeField] private bool think;

    [SerializeField] private string systemPrompt = "你是一个简洁、友好的 AI 助手。" +
                                             "请根据用户的问题给出自然、准确的回答。";

    private readonly StringBuilder streamBuffer = new StringBuilder();
    private readonly StringBuilder responseBuffer = new StringBuilder();
    private bool insideThink;

    private const string ThinkStart = "<think>";
    private const string ThinkEnd = "</think>";

    private void Start()
    {
        inputField.onSubmit.AddListener(OnInputSubmit);
        qwen3.OnText += OnTextReceived;
    }

    private async void OnInputSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        outputText.text += "\n我：" + input + "\nAI：";
        inputField.text = "";

        // 重置上一次生成的过滤状态
        streamBuffer.Clear();
        responseBuffer.Clear();
        insideThink = false;

        string prompt =
            "<|im_start|>system\n" +
            systemPrompt +
            "<|im_end|>\n" +

            "<|im_start|>user\n" +
            (!think ? "/no_think\n" : "/think\n") +
            input +
            "<|im_end|>\n" +

            "<|im_start|>assistant\n";

        await qwen3.GenerateAsync(prompt);

        // 生成结束，输出剩余的普通文本
        if (!insideThink && streamBuffer.Length > 0)
        {
            AppendVisibleText(streamBuffer.ToString());
            streamBuffer.Clear();
        }

        if (qwen3TTS != null && responseBuffer.Length > 0)
        {
            await qwen3TTS.SpeakAsync(responseBuffer.ToString().Trim());
        }

        streamBuffer.Clear();
        inputField.ActivateInputField();
    }

    private void OnTextReceived(string text)
    {
        streamBuffer.Append(text);
        ProcessStream();
    }

    private void ProcessStream()
    {
        while (streamBuffer.Length > 0)
        {
            string buffer = streamBuffer.ToString();

            if (insideThink)
            {
                // 思考状态：寻找结束标签
                int endIndex = buffer.IndexOf(
                    ThinkEnd, StringComparison.Ordinal);

                if (endIndex >= 0)
                {
                    streamBuffer.Remove(0, endIndex + ThinkEnd.Length);
                    insideThink = false;
                    continue;
                }

                // 保留可能属于跨 token 标签的尾部字符
                int keep = ThinkEnd.Length - 1;

                if (streamBuffer.Length > keep)
                    streamBuffer.Remove(0, streamBuffer.Length - keep);

                return;
            }
            else
            {
                // 普通回答状态：寻找思考开始标签
                int startIndex = buffer.IndexOf(
                    ThinkStart, StringComparison.Ordinal);

                if (startIndex >= 0)
                {
                    if (startIndex > 0)
                    {
                        AppendVisibleText(buffer.Substring(0, startIndex));
                    }

                    streamBuffer.Remove(
                        0, startIndex + ThinkStart.Length);

                    insideThink = true;
                    continue;
                }

                // 暂不输出可能属于跨 token 标签的尾部字符
                int safeLength = streamBuffer.Length
                                 - (ThinkStart.Length - 1);

                if (safeLength > 0)
                {
                    AppendVisibleText(buffer.Substring(0, safeLength));
                    streamBuffer.Remove(0, safeLength);
                }

                return;
            }
        }
    }

    private void AppendVisibleText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        outputText.text += text;
        responseBuffer.Append(text);
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnInputSubmit);

        if (qwen3 != null)
            qwen3.OnText -= OnTextReceived;
    }
}
