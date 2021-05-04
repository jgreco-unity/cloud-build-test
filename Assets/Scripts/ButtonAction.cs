using UnityEngine;
using UnityEngine.UI;

public class ButtonAction : MonoBehaviour
{
    public static int count = 0;
  
    public void Press()
    {
        count++;
        Debug.Log($"Button pressed {count} times");
        var buttonText = GameObject.Find("ButtonCount").GetComponent<Text>();
        buttonText.text = count.ToString();
    }
}