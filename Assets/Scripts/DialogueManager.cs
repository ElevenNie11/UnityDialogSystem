using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   

public class DialogueManager : MonoBehaviour
{
    [Header("UI组件")]
    public Text textContent;
    [Header("文本文件")]
    public TextAsset textFile;
    public int index;     //序号
    //定义一个列表 list 把文本自动分成一句一句的存入列表
    List<string> textList = new List<string>();
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
