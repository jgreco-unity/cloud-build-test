using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.AutomatedQA.Listeners
{
	public class KeyInputHandler : MonoBehaviour
	{
		private List<(KeyCode Key, float PressStartTime)> KeyPresses = new List<(KeyCode Key, float PressStartTime)>();
		private List<KeyCode> RelevantKeys = new List<KeyCode>();
		private static bool waitAFrame { get; set; }
		private static bool forceRefresh { get; set; }
		private static float lastRunTime { get; set; }
		private static readonly float UPDATE_COOLDOWN = 5f;
		private static List<GameObject> all = new List<GameObject>();

		internal static (KeyCode Key, float StartTime, float LastInputTime) currentKeyPress { get; set; }
		internal static (InputField Input, float StartTime, float LastInputTime) currentInput { get; set; }
		internal enum ActableTypes { Clickable, Draggable, Input, KeyDown, Scroll, Screenshot, TextForAssert, Wait }

		internal static List<AutomationListener> ActiveListeners
		{
			get
			{
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
		}

		void Update()
		{
			// Handle keypresses NOT in the context of an input field.
			// TODO: When Input wrapper class is activated, finish key press support. HandleOldInputSystemActions();
			// TODO: When Input wrapper class is activated, finish key press support. HandleNewInputSystemActions();

			/// Handle keypresses in the context of an input field.
			if ((Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) 
				|| Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.KeypadEnter))
				 && IsInputSet(currentInput))
			{
				// TODO: when key presses are added to record and playback, add FinalizeAnyTextInputInProgress() call here, as these indicate a removal of focus from the current InputField.
				// This will be called AFTER mouse pointer event triggers, meaning a click will be recorded before this call. FinalizeAnyTextInputInProgress();
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
				positional = false,
				scene = SceneManager.GetActiveScene().name,
				keyCode = key.ToString()

			};
			Instance.AddTouchData(td);
		}

		/// <summary>
		/// Finishes active text input action, and saves as TouchData to recording.
		/// </summary>
		internal static void FinalizeAnyTextInputInProgress()
		{
			if (IsInputSet(currentInput))
			{
				FinalizeTextInput();
				currentInput = default((InputField, float, float));
				waitAFrame = true;
			}
		}

		private static void FinalizeTextInput()
		{
			TouchData td = new TouchData
			{
				eventType = TouchData.type.input,
				objectName = currentInput.Input.name,
				objectHierarchy = string.Join("/", Instance.GetHierarchy(currentInput.Input.gameObject)),
				timeDelta = currentInput.StartTime - Instance.GetLastEventTime(),
				inputDuration = Math.Abs(currentInput.LastInputTime - currentInput.StartTime),
				positional = false,
				scene = SceneManager.GetActiveScene().name,
				inputText = currentInput.Input.text
			};
			Instance.AddTouchData(td);
		}

		private IEnumerator Runner()
		{
			while (true)
			{
				if (forceRefresh || Time.time - lastRunTime > UPDATE_COOLDOWN)
				{
					forceRefresh = false;
					all = all.GetUniqueObjectsBetween(Instance.GetActiveGameObjects());

					for (int x = 0; x < all.Count; x++)
					{
						if (all[x] == null)
						{
							continue;
						}

						AutomationListener al = all[x].GetComponent<AutomationListener>();
						if (al == null)
						{
							List<MonoBehaviour> components = all[x].GetComponents<MonoBehaviour>().ToList();
							for (int co = 0; co < components.Count; co++)
							{
								string scriptName = components[co].GetType().Name;

								// TODO: When we want to add support for trigger-based colliders.
								/*#region Clickables
								if (scriptName == "Collider" || components[co].GetType().IsAssignableFrom(typeof(Collider)))
								{

									if (all[x].GetComponent<Collider>().isTrigger)
									{

										AddListener(all[x], ActableTypes.Clickable);
										continue;

									}

								}
								#endregion*/

								#region Inputs
								if (scriptName == "InputField" || components[co].GetType().IsAssignableFrom(typeof(InputField)))
								{

									AddListener(all[x], ActableTypes.Input);
									continue;

								}
								#endregion
							}
						}
					}
					lastRunTime = Time.time;
				}
				yield return new WaitForEndOfFrame();
			}
		}

		public static void Refresh()
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

		public static bool IsInputSet((InputField, float, float) input)
		{
			return input != default((InputField, float, float)) && input.Item1.text.Length > 0;
		}
	}
}