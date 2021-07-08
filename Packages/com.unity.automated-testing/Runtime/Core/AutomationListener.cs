using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unity.AutomatedQA.Listeners.KeyInputHandler;

namespace Unity.AutomatedQA.Listeners
{
	public class AutomationListener : MonoBehaviour
	{
		void Start()
		{
			if (TryGetComponent(out InputField input))
			{
				input.onValueChanged.AddListener(delegate
				{
					if (!IsInputSet(currentInput))
					{
						currentInput = (input, Time.time, 0f);
					}
					else
					{
						currentInput = (input, currentInput.StartTime, Time.time);
					}
				});
			}
		}
	}
}