using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TextInputAutomatorConfig : AutomatorConfig<TextInputAutomator>
{
    public string inputFieldName = "InputField";
    public string text = "";
    public float interval = 0.1f;
}

public class TextInputAutomator : Automator<TextInputAutomatorConfig>
{
    private int charIndex = 0;
    private InputField inputField;

    public override void BeginAutomation()
    {
        base.BeginAutomation();

        var inputFieldGo = GameObject.Find(config.inputFieldName);
        inputField = inputFieldGo ? inputFieldGo.GetComponent<InputField>() : null;
        if (inputField == null)
        {
            Debug.LogError($"InputField '{config.inputFieldName}' not found");
            EndAutomation();
            return;
        }

        StartCoroutine(TypeChars());
    }

    private IEnumerator TypeChars()
    {
        while (charIndex < config.text.Length)
        {
            yield return new WaitForSeconds(config.interval);
            inputField.text += config.text[charIndex];
            charIndex++;
        }
     
        EndAutomation();
    }


}
