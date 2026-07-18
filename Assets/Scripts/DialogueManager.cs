using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//  最简版对话系统，只做三件事：
//  1. 用 TextAsset 读取纯文本文件，一行一句对话
// 2. 按 "|" 把每行拆成：[说话人] 和 [内容]
//  3. 用一个协程把每一句话用打字机效果显示出来，点击后播放下一句
public class DialogueManager : MonoBehaviour
{
    [Header("对话文本文件.txt")]
    public TextAsset dialogueText;    //对话文本文件
    [Header("UI组件")]
    public TMP_Text contentText;      //显示对话内容的文本UI组件
    public GameObject dialoguePanel;  //对话面板UI组件
    [Header("打字机效果: 数字越小打字越快")]
    public float typeSpeed = 0.03f; //打字机效果的速度
    //解析以后的对话数据保存在一个列表List里
    private List<DialogueLine> dialogueLines = new List<DialogueLine>();
    
    private void Awake()
    {
        ParseText();
        //一开始先把对话框隐藏起来，等玩家触发对话时才显示
        dialoguePanel.SetActive(false);
    }

    //按 T 键开始进行测试跑对话
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartDialogue();
        }
    }

    //把.txt文本文件里的内容解析成一个个的DialogueLine对象，存入dialogueLines列表里
    //按行拆开，每行再按"|"拆开，前面是说话人，后面是内容
    private void ParseText()
    {
        //按行拆开
        string[] rows = dialogueText.text.Split('\n');
        //遍历每一行
        foreach (string row in rows)
        {
            string trimmedRow = row.Trim(); //去掉首尾空格
            if (string.IsNullOrEmpty(trimmedRow))  continue; //如果这一行是空的 -> 跳过这一行  
            string[] parts = trimmedRow.Split('|'); //按"|"拆开: 把 "村长|你好" 拆成 ["村长", "你好"] 
            dialogueLines.Add(new DialogueLine(parts[0], parts[1])); //把拆开的说话人和内容传给DialogueLine类，生成一个对象，存入列表
        }
    }
    //外部调用这个方法就能开始播放对话，比如按 T 键测试
    public void StartDialogue()
    {
        dialoguePanel.SetActive(true); //显示对话框
        StartCoroutine(PlayAllLines()); //播放对话
    }
    //核心协程：把 dialogueData.lines 里的每一句话依次播放出来
    private IEnumerator PlayAllLines()
    {
        //用 foreach 依次取出每一句对话
        foreach (DialogueLine line in dialogueLines)
        {
            //每句话先用打字机效果显示出来，yield return 会等这个协程跑完才往下走
            yield return StartCoroutine(TypeOneLine(line));
            //显示完之后，等玩家点击鼠标左键再播放下一句
            yield return StartCoroutine(WaitForClick());
        }
        //全部对话播完了，把对话框关掉
        dialoguePanel.SetActive(false);
    }

    //打字机效过果的协程：把一行对话的内容一个字一个字显示出来
    private IEnumerator TypeOneLine(DialogueLine line)
    {
        contentText.text = ""; //先清空文本框
        //字符串本质是char数组，可以直接用foreach一个字一个字取出来
        foreach (char c in line.content)
        {
            contentText.text += c; //把这个字符加到文本UI组件里
            yield return new WaitForSeconds(typeSpeed); //等待一段时间再显示下一个字符
        }
    }

    //检查按键
    private IEnumerator WaitForClick()
    {
        bool clicked = false; //玩家是否点击了鼠标左键
        while (!clicked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
            }
            yield return null; //什么都不做，只是等下一帧再检查一次
        }
    }
}
