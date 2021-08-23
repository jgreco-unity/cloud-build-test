using System;
using System.Collections;
using System.Collections.Generic;
#if AQA_USE_TMP
using TMPro;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.AutomatedQA.Listeners
{
	public class GameListenerHandler : MonoBehaviour
	{
		private List<(KeyCode Key, float PressStartTime)> KeyPresses = new List<(KeyCode Key, float PressStartTime)>();
		private List<KeyCode> RelevantKeys = new List<KeyCode>();
		private static bool waitAFrame { get; set; }
		private static bool forceRefresh { get; set; } = true;
		private static float lastRunTime { get; set; }
		private static readonly float UPDATE_COOLDOWN = 5f;

		internal static List<GameObject> AllActiveAndInteractableGameObjects { get; set; } = new List<GameObject>();
		internal static (KeyCode Key, float StartTime, float LastInputTime) currentKeyPress { get; set; }
		internal static AutomationInput currentInput { get; set; }
		internal enum ActableTypes { Clickable, Draggable, Input, KeyDown, Scroll, Screenshot, TextForAssert, Wait }

		internal static List<AutomationListener> ActiveListeners
		{
			get
			{
				_activeListeners.RemoveAll(al => al == null || !al.isActiveAndEnabled);
				return _activeListeners;
			}
			private set
			{
				_activeListeners = value;
			}
		}
		private static List<AutomationListener> _activeListeners = new List<AutomationListener>();

		private void Start()
		{
			RelevantKeys = new List<KeyCode>((KeyCode[])Enum.GetValues(typeof(KeyCode)));
			// Touch inputs are handled in RecordingInputModule, so we will ignore them here.
			RelevantKeys.RemoveAll(x => x.ToString().StartsWith("Mouse") || x.ToString().StartsWith("Joystick"));
			StartCoroutine(Runner());
			SceneManager.sceneLoaded += Refresh;
		}

		void Update()
		{
			// Handle keypresses NOT in the context of an input field.
			//HandleOldInputSystemActions();
			//HandleNewInputSystemActions();

			/// Handle keypresses in the context of an input field.
			if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				StartCoroutine(DelayedRefresh());
			}
			waitAFrame = false;
		}

		private void HandleOldInputSystemActions()
		{
			if (!waitAFrame && !IsInputSet(currentInput))
			{
				foreach (KeyCode key in RelevantKeys)
				{
					if (Input.GetKeyDown(key))
					{
						HandleKeyAction(key, true);
					}
					else if (!Input.GetKey(key) && KeyPresses.FindAll(x => x.Key == key).Any())
					{
						HandleKeyAction(key, false);
					}
				}
			}
		}

		private void HandleNewInputSystemActions()
		{
			// TODO: Setup.
		}

		/// <summary>
		/// Records a key press action as TouchData.
		/// </summary>
		/// <param name="key">Key that was pressed.</param>
		/// <param name="isKeyDown">Is the key pressed down or was it released.</param>
		private void HandleKeyAction(KeyCode key, bool isKeyDown)
		{
			(KeyCode Key, float PressStartTime) keyAction = (key, Time.time);
			if (isKeyDown)
			{
				KeyPresses.Add(keyAction);
			}
			else
			{
				KeyPresses.RemoveAll(x => x.Key == key);
			}

			TouchData td = new TouchData
			{
				eventType = isKeyDown ? TouchData.type.keydown : TouchData.type.keyup,
				timeDelta = isKeyDown ? Time.time - Instance.GetLastEventTime() : Time.time - keyAction.PressStartTime,
				inputDuration = currentInput.LastInputTime - currentInput.StartTime,
				positional = true,
				scene = SceneManager.GetActiveScene().name,
				keyCode = key.ToString()

			};

			if (isKeyDown) Instance.AddFullTouchData(td);
		}

		internal static void FinalizeAnyTextInputInProgress()
		{
			if (IsInputSet(currentInput))
			{
				FinalizeTextInput();
				currentInput = new AutomationInput();
				waitAFrame = true;
			}
		}

		private static void FinalizeTextInput()
		{
			GameObject inputGo = null;
			string text = string.Empty;
			if (currentInput.InputField != null)
			{
				inputGo = currentInput.InputField.gameObject;
				text = currentInput.InputField.text;
			}
#if AQA_USE_TMP
			else if (currentInput.TmpInput != null)
			{
				inputGo = currentInput.TmpInput.gameObject;
				text = currentInput.TmpInput.text;
			}
#endif
			TouchData td = new TouchData
			{
				eventType = TouchData.type.input,
				objectName = inputGo.name,
				objectHierarchy = string.Join("/", Instance.GetHierarchy(inputGo)),
				querySelector = ElementQuery.ConstructQuerySelectorString(inputGo),
				timeDelta = currentInput.StartTime - Instance.GetLastEventTime(),
				inputDuration = currentInput.LastInputTime - currentInput.StartTime,
				positional = true,
				scene = SceneManager.GetActiveScene().name,
				inputText = text
			};
			Instance.AddFullTouchData(td);
		}

		private IEnumerator Runner()
		{
			while (true)
			{
				if (forceRefresh || Time.time - lastRunTime > UPDATE_COOLDOWN)
				{
					forceRefresh = false;
					ActiveListeners.RemoveAll(al => al == null);
					AllActiveAndInteractableGameObjects.RemoveAll(al => al == null);
					AllActiveAndInteractableGameObjects = AllActiveAndInteractableGameObjects.GetUniqueObjectsBetween(ElementQuery.Instance.GetAllActiveGameObjects());

					for (int x = 0; x < AllActiveAndInteractableGameObjects.Count; x++)
					{
						if (AllActiveAndInteractableGameObjects[x] == null)
						{
							continue;
						}

						AutomationListener al = AllActiveAndInteractableGameObjects[x].GetComponent<AutomationListener>();
						if (al == null)
						{
							List<MonoBehaviour> components = AllActiveAndInteractableGameObjects[x].GetComponents<MonoBehaviour>().ToList();
							for (int co = 0; co < components.Count; co++)
							{
								// Handle GameObjects with missing component references.
								if (components[co] == null || components[co].GetType() == null)
									continue;

								string scriptName = components[co].GetType().Name;
#region Clickables
								if (scriptName == "Collider" || typeof(Collider).IsAssignableFrom(components[co].GetType()))
								{
									if (AllActiveAndInteractableGameObjects[x].GetComponent<Collider>().isTrigger)
									{
										AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
										continue;
									}
								}
								if (scriptName == "Dropdown" || typeof(Dropdown).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Button" || typeof(Button).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Toggle" || typeof(Toggle).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Scrollbar" || typeof(Scrollbar).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Draggable);
									continue;
								}
								if (scriptName == "Slider" || typeof(Slider).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Draggable);
									continue;
								}
								if (scriptName == "Selectable" || typeof(Selectable).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
#endregion

#region Inputs
								if (scriptName == "InputField" || components[co].GetType().IsAssignableFrom(typeof(InputField)))
								{

									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Input);
									continue;

								}
#if AQA_USE_TMP
								if (scriptName == "TMP_InputField" || components[co].GetType().IsAssignableFrom(typeof(TMP_InputField)))
								{

									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Input);
									continue;

								}
#endif
#endregion
							}
						}
						else if (!ActiveListeners.Contains(al))
						{
							ActiveListeners.Add(al);
						}
					}
					lastRunTime = Time.time;
				}
				yield return null;
			}
		}

		public static void Refresh()
		{
			forceRefresh = true;
		}

		public void Refresh(Scene scene, LoadSceneMode mod)
		{
			forceRefresh = true;
		}

		public IEnumerator DelayedRefresh()
		{
			yield return new WaitForSeconds(0.25f);
			Refresh();
			yield break;
		}

		internal void AddListener(GameObject obj, ActableTypes type)
		{
			if (obj.GetComponent<AutomationListener>() == null)
			{
				AutomationListener listener = obj.AddComponent<AutomationListener>();
				ActiveListeners.Add(listener);
			}
		}

		public static bool IsInputSet(AutomationInput input)
		{
			if (input != default(AutomationInput))
			{
				if (input.InputField != null && input.InputField.text.Length > 0)
				{
					return true;
				}
#if AQA_USE_TMP
				if (input.TmpInput != null && input.TmpInput.text.Length > 0)
				{
					return true;
				}
#endif
			}
			return false;
		}

		public class AutomationInput
		{
			public AutomationInput() { }
			public AutomationInput(InputField inputField, float startTime, float lastInputTime)
			{
				InputField = inputField;
				StartTime = startTime;
				LastInputTime = lastInputTime;
			}
#if AQA_USE_TMP
			public AutomationInput(TMP_InputField tmpInput, float startTime, float lastInputTime)
			{
				TmpInput = tmpInput;
				StartTime = startTime;
				LastInputTime = lastInputTime;
			}
			public TMP_InputField TmpInput { get; set; }
#endif
			public InputField InputField { get; set; }
			public float StartTime { get; set; }
			public float LastInputTime { get; set; }
		}
	}
}