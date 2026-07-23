//只是一个普通的类，用来保存分割出来的"一句话"
using System.Collections.Generic;

//一个分支选项
public class DialogueChoice{
    public string text;    //选项上的文字
    public int targetId;   //准备跳转到哪一句台词
    public DialogueChoice(string text, int targetId)
    {
        this.text = text;
        this.targetId = targetId;
    }
}

public class DialogueLine{
    public int id;            //这句话的编号
    public string speaker;   //谁在说话
    public string content;   //说的内容
    public List<DialogueChoice> choices = new List<DialogueChoice>();  //用来承装真正需要玩家点的分支选项
    public int forcedNextId = -1;  //强制指定"讲完这句该跳到哪个编号"，-1表示没指定，就用currentId+1
    
    public DialogueLine(int id, string speaker, string content)
    {
        this.id = id;
        this.speaker = speaker;
        this.content = content;
    }
}