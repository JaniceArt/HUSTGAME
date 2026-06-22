using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("=== GIAO DIỆN HỘI THOẠI ===")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI nextIndicatorText; // Chữ ▼ nhấp nháy

    [Header("=== GIAO DIỆN LỰA CHỌN ===")]
    public GameObject choicesContainer; // Nhóm chứa các lựa chọn
    public List<TextMeshProUGUI> choiceTexts; // Danh sách 2 Text lựa chọn (đã bao gồm dấu ▶)

    [Header("=== HIỆU ỨNG ===")]
    public float typingSpeed = 0.03f;
    public float bounceSpeed = 5f; // Tốc độ nảy lên xuống
    public float bounceHeight = 10f; // Độ cao nảy lên xuống

    [Header("=== ÂM THANH ===")]
    public AudioClip typingSound; // Âm thanh gõ chữ (nói chuyện)
    private AudioSource audioSource;

    private List<DialogNode> currentNodes;
    private int currentNodeIndex = 0;
    private Action<int> onDialogComplete; // Callback trả về lựa chọn (nếu có), -1 nếu không có lựa chọn

    private bool isTyping = false;
    private bool isWaitingForInput = false;
    private bool isChoosing = false;
    private bool isShowingReply = false; // Trạng thái đang hiện câu trả lời của Lựa Chọn
    private int selectedChoiceIndex = 0;
    private int finalizedChoiceResult = -1; // Lưu lại lựa chọn cuối cùng để truyền vào callback

    private Vector2 indicatorStartPos;

    private Coroutine typingCoroutine;
    private Coroutine bounceCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (choicesContainer != null) choicesContainer.SetActive(false);
        if (nextIndicatorText != null)
        {
            nextIndicatorText.gameObject.SetActive(false);
            indicatorStartPos = nextIndicatorText.rectTransform.anchoredPosition; // Lưu vị trí gốc của dấu tam giác
        }
    }

    public bool IsDialogActive => dialogPanel.activeInHierarchy;

    public void StartDialogSequence(List<DialogNode> nodes, Action<int> onComplete)
    {
        if (nodes == null || nodes.Count == 0)
        {
            onComplete?.Invoke(-1);
            return;
        }

        // Khóa di chuyển của người chơi khi đang mở bảng thoại
        FirstPersonController.CanMove = false;

        currentNodes = nodes;
        onDialogComplete = onComplete;
        currentNodeIndex = 0;
        finalizedChoiceResult = -1;
        isChoosing = false;
        isShowingReply = false;

        dialogPanel.SetActive(true);
        choicesContainer.SetActive(false);
        if (nextIndicatorText != null) nextIndicatorText.rectTransform.anchoredPosition = indicatorStartPos;
        
        DisplayNextNode();
    }

    void Update()
    {
        if (!IsDialogActive || Keyboard.current == null) return;

        if (isChoosing)
        {
            HandleChoiceInput();
        }
        else if (isWaitingForInput && (Keyboard.current.spaceKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame))
        {
            if (isShowingReply)
            {
                // Vừa đọc xong câu trả lời của đáp án, thì kết thúc Node này và đi tiếp
                isShowingReply = false;
                DisplayNextNode();
            }
            else
            {
                // Kiểm tra xem câu hiện tại có chứa lựa chọn không?
                DialogNode currentNode = currentNodes[currentNodeIndex - 1];
                if (currentNode.hasChoices && currentNode.choices != null && currentNode.choices.Count > 0)
                {
                    ShowChoices(currentNode);
                }
                else
                {
                    DisplayNextNode();
                }
            }
        }
    }

    void DisplayNextNode()
    {
        if (currentNodeIndex < currentNodes.Count)
        {
            DialogNode node = currentNodes[currentNodeIndex];
            currentNodeIndex++;
            
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            
            nextIndicatorText.gameObject.SetActive(false);
            isWaitingForInput = false;
            
            typingCoroutine = StartCoroutine(TypeSentence(node.sentence));
        }
        else
        {
            // Kết thúc kịch bản
            EndDialog(finalizedChoiceResult);
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogText.text = "";
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogText.text += letter;

            // Phát âm thanh gõ chữ (nếu chưa đang phát)
            if (typingSound != null && letter != ' ' && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(typingSound, 0.5f);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        // Tắt âm thanh ngay khi chạy hết chữ
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isTyping = false;
        isWaitingForInput = true;
        bounceCoroutine = StartCoroutine(BounceIndicator());
    }

    IEnumerator BounceIndicator()
    {
        nextIndicatorText.gameObject.SetActive(true);
        while (true)
        {
            float newY = indicatorStartPos.y + Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            nextIndicatorText.rectTransform.anchoredPosition = new Vector2(indicatorStartPos.x, newY);
            yield return null;
        }
    }

    void ShowChoices(DialogNode node)
    {
        isWaitingForInput = false;
        isChoosing = true;
        selectedChoiceIndex = 0;
        choicesContainer.SetActive(true);
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        nextIndicatorText.gameObject.SetActive(false);

        UpdateChoiceUI(node);
    }

    void HandleChoiceInput()
    {
        DialogNode currentNode = currentNodes[currentNodeIndex - 1];

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            selectedChoiceIndex--;
            if (selectedChoiceIndex < 0) selectedChoiceIndex = currentNode.choices.Count - 1;
            UpdateChoiceUI(currentNode);
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            selectedChoiceIndex++;
            if (selectedChoiceIndex >= currentNode.choices.Count) selectedChoiceIndex = 0;
            UpdateChoiceUI(currentNode);
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            finalizedChoiceResult = selectedChoiceIndex;
            DialogChoice chosen = currentNode.choices[finalizedChoiceResult];
            
            choicesContainer.SetActive(false);
            isChoosing = false;

            if (!string.IsNullOrEmpty(chosen.replyText))
            {
                // Có câu trả lời -> gõ chữ câu trả lời ra
                isShowingReply = true;
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeSentence(chosen.replyText));
            }
            else
            {
                // Không có câu trả lời -> đi tiếp câu thoại hoặc kết thúc
                DisplayNextNode();
            }
        }
    }

    void UpdateChoiceUI(DialogNode node)
    {
        for (int i = 0; i < choiceTexts.Count && i < node.choices.Count; i++)
        {
            if (i == selectedChoiceIndex)
                choiceTexts[i].text = $"<color=yellow>> {node.choices[i].choiceText}</color>";
            else
                choiceTexts[i].text = $"  <color=#CCCCCC>{node.choices[i].choiceText}</color>";
        }
    }

    void EndDialog(int choiceResult)
    {
        dialogPanel.SetActive(false);
        choicesContainer.SetActive(false);
        isChoosing = false;
        isWaitingForInput = false;

        // Cho phép người chơi di chuyển lại sau khi đóng bảng thoại (nếu không đang soi tài liệu)
        if (DocumentManager.Instance == null || !DocumentManager.Instance.IsViewingDocument)
        {
            FirstPersonController.CanMove = true;
        }

        onDialogComplete?.Invoke(choiceResult);
    }
}
