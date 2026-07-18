//只是一个普通的类，用来保存分割出来的"一句话"
public class DialogueLine{
    public string speaker;   //谁在说话
    public string content;   //说的内容
    public DialogueLine(string speaker, string content)
    {
        this.speaker = speaker;
        this.content = content;
    }
}