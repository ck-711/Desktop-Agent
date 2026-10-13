using System.Collections.Generic;
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

    [SerializeField] private string systemPrompt = "����һ����ࡢ�Ѻõ� AI ���֡�" +
                                             "������û������������Ȼ��׼ȷ�Ļش�";

    private readonly StringBuilder streamBuffer = new StringBuilder();
    private readonly StringBuilder speechBuffer = new StringBuilder();
    private bool insideThink;
    private bool isGenerating;

    private const string ThinkStart = "<think>";
    private const string ThinkEnd = "</think>";

    private void Start()
    {
        inputField.onSubmit.AddListener(OnInputSubmit);
        qwen3.OnText += OnTextReceived;
    }

private async void OnInputSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input) ||
            isGenerating ||
            (qwen3TTS != null && qwen3TTS.IsBusy))
        {
            return;
        }

        isGenerating = true;
        outputText.text += "\n我：" + input + "\nAI：";
        inputField.text = "";

        streamBuffer.Clear();
        speechBuffer.Clear();
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

        try
        {
            await qwen3.GenerateAsync(prompt);

            if (!insideThink && streamBuffer.Length > 0)
            {
                AppendVisibleText(streamBuffer.ToString());
            }

            FlushSpeechBuffer();
        }
        finally
        {
            streamBuffer.Clear();
            isGenerating = false;
            inputField.ActivateInputField();
        }
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
                // ˼��״̬��Ѱ�ҽ�����ǩ
                int endIndex = buffer.IndexOf(
                    ThinkEnd, StringComparison.Ordinal);

                if (endIndex >= 0)
                {
                    streamBuffer.Remove(0, endIndex + ThinkEnd.Length);
                    insideThink = false;
                    continue;
                }

                // ����������ڿ� token ��ǩ��β���ַ�
                int keep = ThinkEnd.Length - 1;

                if (streamBuffer.Length > keep)
                    streamBuffer.Remove(0, streamBuffer.Length - keep);

                return;
            }
            else
            {
                // ��ͨ�ش�״̬��Ѱ��˼����ʼ��ǩ
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

                // �ݲ�����������ڿ� token ��ǩ��β���ַ�
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
        speechBuffer.Append(text);
        QueueCompletedSentences();
    }

private void QueueCompletedSentences()
    {
        while (speechBuffer.Length > 0)
        {
            int sentenceEnd = speechBuffer.ToString().IndexOfAny(
                new[] { '。', '！', '？', '.', '!', '?', '；', ';', '\n' });
            if (sentenceEnd < 0)
                return;

            string sentence = speechBuffer.ToString(0, sentenceEnd + 1).Trim();
            speechBuffer.Remove(0, sentenceEnd + 1);
            QueueSpeech(sentence);
        }
    }

    private void FlushSpeechBuffer()
    {
        string sentence = speechBuffer.ToString().Trim();
        speechBuffer.Clear();
        QueueSpeech(sentence);
    }

    private void QueueSpeech(string sentence)
    {
        if (string.IsNullOrEmpty(sentence) || qwen3TTS == null)
            return;

        qwen3TTS.EnqueueSpeech(sentence);
    }


    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnInputSubmit);

        if (qwen3 != null)
            qwen3.OnText -= OnTextReceived;
    }
}
