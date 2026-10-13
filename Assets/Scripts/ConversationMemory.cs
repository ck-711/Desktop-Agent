using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class ConversationMemory : MonoBehaviour
{
    [Serializable]
    private sealed class MemoryEntry
    {
        public string role;
        public string content;
    }

    [Serializable]
    private sealed class MemoryData
    {
        public List<MemoryEntry> entries = new List<MemoryEntry>();
    }

    [Header("Memory Limits")]
    [SerializeField, Min(1)] private int maxTurns = 6;
    [SerializeField, Min(100)] private int maxCharacters = 1800;
    [SerializeField] private bool persistAcrossSessions = true;
    [SerializeField] private string fileName = "conversation_memory.json";

    private readonly List<MemoryEntry> entries = new List<MemoryEntry>();

    private string FilePath => Path.Combine(
        Application.persistentDataPath,
        fileName);

    private void Awake()
    {
        LoadMemory();
    }

    public string BuildPrompt()
    {
        StringBuilder builder = new StringBuilder();
        foreach (MemoryEntry entry in entries)
        {
            if (entry == null ||
                string.IsNullOrEmpty(entry.role) ||
                string.IsNullOrEmpty(entry.content))
            {
                continue;
            }

            builder.Append("<|im_start|>");
            builder.Append(entry.role);
            builder.Append('\n');
            builder.Append(entry.content);
            builder.Append("<|im_end|>\n");
        }

        return builder.ToString();
    }

    public void RecordUser(string content)
    {
        AddEntry("user", content);
    }

    public void RecordAssistant(string content)
    {
        AddEntry("assistant", content);
    }

    [ContextMenu("Clear Saved Memory")]
    public void ClearMemory()
    {
        entries.Clear();

        if (persistAcrossSessions && File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }

    private void AddEntry(string role, string content)
    {
        content = SanitizeContent(content);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        entries.Add(new MemoryEntry
        {
            role = role,
            content = content
        });
        Prune();
        SaveMemory();
    }

    private void LoadMemory()
    {
        if (!persistAcrossSessions || !File.Exists(FilePath))
        {
            return;
        }

        try
        {
            MemoryData data = JsonUtility.FromJson<MemoryData>(
                File.ReadAllText(FilePath));
            if (data?.entries == null)
            {
                return;
            }

            entries.Clear();
            entries.AddRange(data.entries);
            Prune();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Conversation memory could not be loaded: {exception.Message}",
                this);
            entries.Clear();
        }
    }

    private void SaveMemory()
    {
        if (!persistAcrossSessions)
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                FilePath,
                JsonUtility.ToJson(new MemoryData { entries = entries }));
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Conversation memory could not be saved: {exception.Message}",
                this);
        }
    }

    private void Prune()
    {
        int maxEntries = Mathf.Max(1, maxTurns) * 2;
        while (entries.Count > maxEntries)
        {
            entries.RemoveAt(0);
        }

        while (entries.Count > 1 && TotalCharacterCount() > maxCharacters)
        {
            entries.RemoveAt(0);
        }
    }

    private int TotalCharacterCount()
    {
        int count = 0;
        foreach (MemoryEntry entry in entries)
        {
            if (entry != null && entry.content != null)
            {
                count += entry.content.Length;
            }
        }

        return count;
    }

    private static string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return content
            .Replace("<|im_start|>", string.Empty)
            .Replace("<|im_end|>", string.Empty)
            .Replace("\0", string.Empty)
            .Trim();
    }
}
