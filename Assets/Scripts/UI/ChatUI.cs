
using UnityEngine;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    [Header("UI")]
    public InputField inputField;
    public Text chatText;
    public Button sendButton;

    [Header("Qwen3")]
    public Qwen3 qwen3;

    private bool waiting;

    public void OnSendClicked()
    {
        if (waiting || qwen3 == null)
            return;

        string prompt = inputField.text.Trim();

        if (string.IsNullOrEmpty(prompt))
            return;

        waiting = true;
        sendButton.interactable = false;

        //chatText.text += "\n\n你："
        //    + prompt
        //    + "\n\nAI：";

        inputField.text = "";

        qwen3.GenerateStream(prompt);

        // 当前先采用简单版本：
        // 下一步再把生成完成事件接回 UI，
        // 自动恢复发送按钮。
        waiting = false;
        sendButton.interactable = true;
    }
}