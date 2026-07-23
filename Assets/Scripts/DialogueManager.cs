using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   //角色高亮必须要用上
using TMPro;            //显示文字必须要用上


//可以在Inspector面板里配置的"名字-颜色"对应关系
//[System.Serializable]让C#的普通[类]能在Inspector里展开显示、可编辑
[System.Serializable]
public class SpeakerColor
{
    public string speakerName;          //说话人名字
    public Color color = Color.black;   //说话人对应的颜色
}


//  最简版对话系统，只做三件事：
//  1. 用 TextAsset 读取纯文本文件，一行一句对话
//  2. 按 "|" 把每行拆成：[说话人] 和 [内容]
//  3. 用一个协程把每一句话用打字机效果显示出来，点击后播放下一句
public class DialogueManager : MonoBehaviour
{
    [Header("对话文本文件.txt")]
    public TextAsset dialogueText;    //对话文本文件
    [Header("UI组件")]
    public TMP_Text contentText;      //显示对话内容的文本UI组件
    public GameObject dialoguePanel;  //对话面板UI组件
    public GameObject hintText;       //操作提示UI组件
    [Header("打字机效果: 数字越小打字越快")]
    public float typeSpeed = 0.03f; //打字机效果的速度
    [Header("参与对话的角色")]
    public GameObject[] speakerObjects; //参与对话的角色高亮显示
    [Range(0, 1)]
    public float dimAlpha = 0.4f; //不说话的角色透明度: 0是全透明，1是完全不变暗
    [Header("说话人颜色配置")]
    public SpeakerColor[] speakerColors; //在Inspector里逐个添加说话人和文本颜色
    [Header("分支选项按钮")]
    public GameObject choicePanel;       //装选项按钮的父物体，默认隐藏
    public Button[] choiceButtons;       //预先摆好的按钮，数量决定了一句台词最多能有几个选项
    //解析以后的对话数据保存在一个列表List里
    // DialogueLine 是一个自定义的类，里面有两个字段：说话人speaker和内容content
    //private List<DialogueLine> dialogueLines = new List<DialogueLine>();
    
    //有了编号以后 List 要改成 Dictionary
    private Dictionary<int, DialogueLine> dialogueLines = new Dictionary<int, DialogueLine>();

    private void Awake()
    { 
        //解析文本
        ParseText();
        //一开始先把对话框隐藏起来，等玩家触发对话时才显示
        dialoguePanel.SetActive(false);
        //选项按钮面板一开始也要隐藏，等真正出现分支的时候才显示
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
        //游戏一开始就显示操作提示，告诉玩家该怎么触发和进行对话
        if (hintText != null)
        {
            hintText.SetActive(true);
        }
    }


    //按 T 键开始进行测试跑对话
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartDialogue();
        }
    }


//把.txt文本文件里的内容解析成一个个的DialogueLine对象，存入dialogueLines词典里
//解析文本：每行格式变成 编号|说话人|内容|选项1文字->目标编号|选项2文字->目标编号...
//后面的选项部分是可选的，没有就是普通的顺序播放台词
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

            //parts[0]是编号，必须先转成数字才能用，转不了说明这一行编号写错了
            if (!int.TryParse(parts[0].Trim(), out int id))
            {
                Debug.LogError($"对话文件格式错误，编号必须是数字：{trimmedRow}");
                continue;
            }

            string speakerName = parts[1].Trim(); //说话人名字两端可能带空格，必须单独Trim一次
            string content = parts[2].Trim();     //说话内容两端也可能带空格，一起处理掉
            DialogueLine line = new DialogueLine(id, speakerName, content);
            
            //从第4段开始，每一段要么是"强制跳转"(>>编号)，要么是"真选项"(选项文字->编号)
            for(int i = 3; i < parts.Length; i++)
            {
                string extraPart = parts[i].Trim();
                if(string.IsNullOrEmpty(extraPart)) continue;
                if (extraPart.StartsWith(">>"))
                {
                    //强制跳转：这句话讲完不弹选项，直接跳到指定编号
                    string targetStr = extraPart.Substring(2).Trim(); //去掉开头的">>"
                    if(int.TryParse(targetStr, out int forcedId))
                    {
                        line.forcedNextId = forcedId;
                    }
                    else
                    {
                        Debug.LogError($"强制跳转格式错误，>>后面必须是数字：{extraPart}");
                    }
                    continue;
                }
            //剩下的情况按"真选项"处理：选项文字->目标编号
            string[] choiceSplit = extraPart.Split(new string[] { "->" }, System.StringSplitOptions.None);
            if (choiceSplit.Length < 2)
            {
                Debug.LogError($"选项格式错误，应该写成「选项文字->目标编号」或者「>>目标编号」：{extraPart}");
                continue;
            }

            string choiceText = choiceSplit[0].Trim();
            if (!int.TryParse(choiceSplit[1].Trim(), out int targetId))
            {
                Debug.LogError($"选项跳转目标编号必须是数字：{extraPart}");
                continue;
            }

            line.choices.Add(new DialogueChoice(choiceText, targetId));
            }
            //用编号当key存进词典，重复编号会直接覆盖前一条（方便调试时观察）
            dialogueLines[id] = line;
        }
    }


    //外部调用这个方法就能开始播放对话，比如按 T 键测试
    public void StartDialogue()
    {
        if(hintText != null)
        {
            hintText.SetActive(false); //对话开始后就把操作提示隐藏掉
        }
        dialoguePanel.SetActive(true);  //显示对话框
        StartCoroutine(PlayAllLines()); //播放对话
    }


    //核心协程：从编号0开始播放，遇到分支就等玩家选，没分支就跳到下一句(forcedNextId或者+1)
    private IEnumerator PlayAllLines()
    {
        int currentId = 0; //固定从编号0开始播放

        //只要词典里还能找到这个编号对应的台词，就继续播放
        while (dialogueLines.ContainsKey(currentId))
        {
            DialogueLine line = dialogueLines[currentId];

            //先高亮当前说话角色，把其他角色变暗
            HighlightSpeaker(line.speaker);
            //每句话先用打字机效果显示出来，yield return 会等这个协程跑完才往下走
            yield return StartCoroutine(TypeOneLine(line));

            if (line.choices.Count > 0)
            {
                //这句话带真选项：显示按钮，等玩家点一个
                int chosenTargetId = -1;
                yield return StartCoroutine(ShowChoicesRoutine(line.choices, targetId =>
                {
                    chosenTargetId = targetId;
                }));
                currentId = chosenTargetId; //跳到玩家选的那个分支目标
            }
            else
            {
                //没有真选项：显示完之后，等玩家点击鼠标左键再播放下一句
                yield return StartCoroutine(WaitForClick());
                //如果这句话指定了forcedNextId就跳去那里，没指定就按顺序+1
                currentId = line.forcedNextId != -1 ? line.forcedNextId : currentId + 1;
            }
        }
        //全部对话播完了，把对话框关掉，把所有角色恢复正常
        ResetHighlight();
        dialoguePanel.SetActive(false);
    }


    //显示分支选项按钮，等玩家点击后把选中的目标编号通过回调传出去
    private IEnumerator ShowChoicesRoutine(List<DialogueChoice> choices, System.Action<int> onChosen)
    {
        choicePanel.SetActive(true);
        int selectedIndex = -1; //所有按钮共享这一个"选中结果"

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choices[i].text;

                int captured = i; //闭包陷阱：必须用局部变量捕获循环变量i
                choiceButtons[i].onClick.RemoveAllListeners(); //防止上一次绑定的回调还留着，越点越乱
                choiceButtons[i].onClick.AddListener(() => { selectedIndex = captured; });
            }
            else
            {
                //选项数量不够按钮多，多出来的按钮先藏起来
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        yield return new WaitUntil(() => selectedIndex != -1); //每帧检查有没有按钮被点了

        choicePanel.SetActive(false);
        onChosen?.Invoke(choices[selectedIndex].targetId);
    }


    //把所有角色的透明度恢复成1，对话结束时调用
    private void ResetHighlight()
    {
        foreach(GameObject obj in speakerObjects){
            if (obj == null) continue;
            Image img = obj.GetComponent<Image>();
            if (img == null)  continue;
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }
    }


    //高亮当前说话角色，把其他角色变暗
    private void HighlightSpeaker(string speaker)
    {
        foreach (GameObject obj in speakerObjects)
        {
            if(obj == null) continue;
            Image img = obj.GetComponent<Image>();
            if (img == null)  continue;
            //obj.name 是 GameObject 自带的一个属性（Unity内置）
            //不用手动赋值，就是我们在 Hierarchy 面板里看到的那个物体名字
            //speaker 字段是单独放在 DialogueData.cs 里的
            bool isSpeaking = obj.name == speaker; //判断这个角色是不是当前文本中规定应该说话的角色
            Color c = img.color;  //先取出当前角色的颜色（里面包含透明度）
            c.a = isSpeaking ? 1f : dimAlpha; //如果是当前说话角色就不变暗，否则就变暗
            img.color = c; //把修改后的颜色再赋值回去，Game视图会立刻显现出来
        }
    }


    //打字机效过果的协程：把一行对话的内容一个字一个字显示出来
    private IEnumerator TypeOneLine(DialogueLine line)
    {
        contentText.color = GetColorForSpeaker(line.speaker);//先把整句话的颜色设置好
        contentText.text = ""; //先清空文本框
        //字符串本质是char数组，可以直接用foreach一个字一个字取出来
        foreach (char c in line.content)
        {
            contentText.text += c; //把这个字符加到文本UI组件里
            yield return new WaitForSeconds(typeSpeed); //等待一段时间再显示下一个字符
        }
    }


    private Color GetColorForSpeaker(string speaker)
    {
        foreach (SpeakerColor sc in speakerColors)
        {
            if(sc.speakerName == speaker)
            {
                return sc.color;   //找到匹配的名字，返回配置好的颜色
            }
        }
        return Color.black;       //默认文本颜色
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