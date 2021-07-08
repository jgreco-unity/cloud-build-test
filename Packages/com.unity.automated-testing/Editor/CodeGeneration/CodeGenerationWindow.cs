using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.RecordedPlayback.Editor
{
	public class CodeGenerationWindow : EditorWindow
	{
		private static readonly string WINDOW_FILE_NAME = "code-generation";
		private static string classBasedOnCurrentEditorColorTheme;
		private static string resourcePath = "Packages/com.unity.automated-testing/Editor/CodeGeneration/";
		private static CodeGenerationWindow wnd;
		private static List<string> allRecordingFiles;
		private static List<(string recording, Toggle toggle)> toggles;
		private static List<(string recording, Toggle toggle)> overrideToggles;
		private static List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits;
		private static readonly string TOGGLE_ON_CHAR = "✓";
		private static readonly string TOGGLE_OFF_CHAR = "✗";
		private static bool editedContentShouldBeFullTest = true;
		private static VisualElement root;
		private static VisualElement mainButtonRow;
		private static Label successLabel;
		private static Label errorLabel;

		[MenuItem("Automated QA/Test Generation...", priority = AutomatedQAEditorSettings.MenuItems.CodeGeneration)]
		public static void ShowWindow()
		{
			wnd = GetWindow<CodeGenerationWindow>();
			wnd.Show();
			wnd.Init();
		}

		private void Init()
		{
			initialized = false;
			if (!Directory.Exists(AutomatedQARuntimeSettings.RecordingDataPath))
			{
				Directory.CreateDirectory(AutomatedQARuntimeSettings.RecordingDataPath);
			}
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
			wnd.titleContent = new GUIContent("Test Generator");
		}

		bool initialized = false;
		public void OnGUI()
		{
			if (wnd == null)
				ShowWindow();
			if (!initialized)
			{
				SetUpView();
				initialized = true;
			}
		}

		void SetUpView(bool isSave = false, bool isError = false)
		{
			overrideToggles = new List<(string recording, Toggle toggle)>();
			toggles = new List<(string recording, Toggle toggle)>();
			classBasedOnCurrentEditorColorTheme = EditorGUIUtility.isProSkin ? "editor-is-dark-theme" : "editor-is-light-theme";

			root = rootVisualElement;
			root.Clear();
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
			visualTree.CloneTree(root);
			root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));

			// Add primary Generate Full Tests and Generate Simple Tests buttons.
			mainButtonRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
			};
			mainButtonRow.AddToClassList("center-align");
			Label label = new Label() { text = "Generate From Recordings:" };
			label.AddToClassList("header");
			root.Add(label);

			Button fullTestButton = new Button() { text = "Full Tests", style = { flexGrow = 1, height = 20 } };
			fullTestButton.clickable.clicked += () => { GenerateTests(false); };
			fullTestButton.tooltip = "Generates a test with step-by-step code allowing for easy test customization.";
			fullTestButton.AddToClassList(classBasedOnCurrentEditorColorTheme);
			fullTestButton.AddToClassList("button");
			mainButtonRow.Add(fullTestButton);

			// If any step files exist in the list of items to overwrite, do not show the "Simple Tests" option.
			if (!filesWithEdits.FindAll(f => f.isStepFile).Any())
			{
				Button simpleTestsButton = new Button() { text = "Simple Tests", style = { flexGrow = 1, height = 20 } };
				simpleTestsButton.clickable.clicked += () => { GenerateTests(true); };
				simpleTestsButton.AddToClassList(classBasedOnCurrentEditorColorTheme);
				simpleTestsButton.tooltip = "Generates a test that simply points to a recording file. Offers minimal opportunity for customization within test code.";
				simpleTestsButton.AddToClassList("button");
				if (!isSave && !isError)
					mainButtonRow.AddToClassList("label-space-padding");
				mainButtonRow.Add(simpleTestsButton);
			}
			root.Add(mainButtonRow);

			// Add message labels for a successful code generation, or for encountered errors.
			if (isSave)
			{
				successLabel = new Label() { text = "Success!" };
				successLabel.AddToClassList("success-label");
				root.Add(successLabel);
				RemoveMessageLabel();
			}
			else if (isError)
			{
				errorLabel = new Label() { text = "Please select test(s) first." };
				errorLabel.AddToClassList("error-label");
				root.Add(errorLabel);
				RemoveMessageLabel();
			}
			VisualElement toggleAllRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f
				}
			};
			toggleAllRow.AddToClassList("toggle-row-warning");
			Label toggleAllLabel = new Label();
			toggleAllLabel.text = "Toggle All";
			toggleAllLabel.AddToClassList("recording");
			toggleAllLabel.AddToClassList("toggle-all-label");
			toggleAllRow.Add(toggleAllLabel);
			Button toggleAll = new Button();
			toggleAll.text = TOGGLE_ON_CHAR;
			toggleAll.clickable.clicked += () => {
				bool isToggleOn = false;
				if (toggleAll.text == TOGGLE_ON_CHAR)
				{
					isToggleOn = true;
					toggleAll.text = TOGGLE_OFF_CHAR;
				}
				else
				{
					toggleAll.text = TOGGLE_ON_CHAR;
				}
				foreach ((string recording, Toggle toggle) toggle in toggles)
				{
					toggle.toggle.value = isToggleOn;
				}
				foreach ((string recording, Toggle toggle) toggle in overrideToggles)
				{
					toggle.toggle.value = isToggleOn;
				}
			};
			toggleAllRow.Add(toggleAll);
			root.Add(toggleAllRow);

			// Show only files with edits that need to be overwritten or excluded.
			if (filesWithEdits.Any())
			{
				VisualElement regionBox = new VisualElement();
				regionBox.AddToClassList("box-region");
				regionBox.AddToClassList(classBasedOnCurrentEditorColorTheme);
				Label editedWarningLabel = new Label();
				editedWarningLabel.text = "Several of these recordings have existing tests generated for them, and these existing tests have user-edited content. Are you sure you want to overwrite these changes?";
				editedWarningLabel.AddToClassList("warning");
				regionBox.Add(editedWarningLabel);

				VisualElement confirmationButtonRow = new VisualElement()
				{
					style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
				};
				Button overwriteCheckedTests = new Button();
				overwriteCheckedTests.text = "Overwrite Checked";
				overwriteCheckedTests.AddToClassList("button-confirm");
				overwriteCheckedTests.AddToClassList("button-overwrite");
				overwriteCheckedTests.clickable.clicked += () => { GenerateTests(!editedContentShouldBeFullTest); };
				confirmationButtonRow.Add(overwriteCheckedTests);
				Button clear = new Button();
				clear.text = "Clear All";
				clear.AddToClassList("button-confirm");
				clear.clickable.clicked += () => { ClearAll(); };
				confirmationButtonRow.Add(clear);
				regionBox.Add(confirmationButtonRow);
				foreach ((string recordingFileName, string cSharpScriptFileName, bool isStepFile) edited in filesWithEdits)
				{
					VisualElement recordingRowConfirm = new VisualElement()
					{
						style =
					{
						flexDirection = FlexDirection.Row,
						flexShrink = 0f,
					}
					};
					recordingRowConfirm.AddToClassList("warning-recordings");
					Toggle toggle = new Toggle();
					overrideToggles.Add((edited.recordingFileName, toggle));
					recordingRowConfirm.Add(toggle);
					Label toggleLabel = new Label();
					toggleLabel.text = $"{edited.cSharpScriptFileName}{(edited.isStepFile ? " (Step File)" : string.Empty)}";
					recordingRowConfirm.Add(toggleLabel);
					regionBox.Add(recordingRowConfirm);
					root.Add(regionBox);
				}
			}
			// List all recordings that can be transformed into compiled code tests.
			else
			{
				// Ignore any segments that are part of a composite recording (but increment child count for parent segment), then display all non-segment recording files.
				allRecordingFiles = Directory.GetFiles(AutomatedQARuntimeSettings.RecordingDataPath).ToList();
				List<(string fileName, InputModuleRecordingData data)> allRecordings = new List<(string fileName, InputModuleRecordingData data)>();
				foreach (string file in Directory.GetFiles(AutomatedQARuntimeSettings.RecordingDataPath))
				{
					if (new FileInfo(file).Extension != ".json")
						continue;
					var json = File.ReadAllText(Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.RecordingFolderName, file));
					allRecordings.Add((new FileInfo(file).Name, JsonUtility.FromJson<InputModuleRecordingData>(json)));
				}
				Dictionary<string, int> finalRecordings = new Dictionary<string, int>();
				for (int x = 0; x < allRecordings.Count; x++)
				{
					List<(string fileName, InputModuleRecordingData data)> parents = allRecordings.FindAll(a => a.data.recordings.FindAll(r => r.filename == allRecordings[x].fileName).Any());
					for (int i = 0; i < parents.Count; i++)
					{
						if (!finalRecordings.ContainsKey(parents[i].fileName))
							finalRecordings.Add(parents[i].fileName, 0);
						finalRecordings[parents[i].fileName]++;
					}
					if (!parents.Any() && !finalRecordings.ContainsKey(allRecordings[x].fileName))
					{
						finalRecordings.Add(allRecordings[x].fileName, 0);
					}
				}
				foreach (KeyValuePair<string, int> recording in finalRecordings)
				{
					VisualElement recordingRow = new VisualElement()
					{
						style =
					{
						flexDirection = FlexDirection.Row,
						flexShrink = 0f,
					}
					};
					recordingRow.AddToClassList("toggle-row-main");
					string recordingNameWithoutFileType = recording.Key.Replace(".json", string.Empty);
					Toggle toggle = new Toggle();
					toggle.AddToClassList("recording");
					toggles.Add((recordingNameWithoutFileType, toggle));
					recordingRow.Add(toggle);
					Label toggleLabel = new Label();
					toggleLabel.AddToClassList("recording");
					toggleLabel.text = recordingNameWithoutFileType;
					recordingRow.Add(toggleLabel);
					if (recording.Value > 0)
					{
						Label childCountLabel = new Label();
						childCountLabel.AddToClassList("recording-segments-found");
						childCountLabel.text = $"({recording.Value})";
						childCountLabel.tooltip = "The number of segments that make up this composite recording.";
						recordingRow.Add(childCountLabel);
					}
					root.Add(recordingRow);
				}
			}
		}

		public void GenerateTests(bool replaceWithSimpleTests)
		{
			if (AreNoRecordingCheckboxesSelected())
			{
				SetUpView(false, true);
				return;
			}

			if (overrideToggles.Any())
			{
				// Get a list of all step files that should be overwritten. Pass it in to the GenerateTest method, and if that test file generates these step files, allow overwrite. 
				List<string> stepFilesToOverwrite = new List<string>();
				foreach ((string recordingFileName, string cSharpScriptFileName, bool isStepFile) file in filesWithEdits.FindAll(f => f.isStepFile && overrideToggles.FindAll(ot => ot.recording == f.recordingFileName && ot.toggle.value).Any()))
				{
					stepFilesToOverwrite.Add(file.recordingFileName);
					if (!replaceWithSimpleTests)
					{
						CodeGenerator.GenerateTest(string.Empty, true, false, file.recordingFileName); // Handle step files.
					}
				}

				foreach ((string recording, Toggle toggle) file in overrideToggles)
				{
					// Ignore step files in the invocation of GenerateTest. Instead, pass them in as an argument to each invocation.
					if (stepFilesToOverwrite.Contains(file.recording))
						continue;

					string originalRecordingFileName = string.Empty;
					foreach (string originalFilePath in allRecordingFiles)
					{
						string originalFile = new FileInfo(originalFilePath).Name;
						if (originalFile == file.recording)
						{
							originalRecordingFileName = originalFile;
						}
					}

					// If the associated overwrite toggle was checked, overwrite the edited file.
					if (file.toggle.value)
					{
						CodeGenerator.GenerateTest(originalRecordingFileName, true, replaceWithSimpleTests);
					}
				}
			}

			editedContentShouldBeFullTest = !replaceWithSimpleTests;
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
			overrideToggles = new List<(string recording, Toggle toggle)>();

			// Test that the old file content is not identical to the newly-generated content, which indicates that a user edited the file directly, or edited the recording.
			foreach ((string recording, Toggle toggle) file in toggles)
			{
				if (!file.toggle.value)
					continue;

				// Test that the old file content is different from a freshly-generated test.
				filesWithEdits.AddRange(CodeGenerator.GenerateTest(file.recording, false, replaceWithSimpleTests));
			}
			bool isSuccess = false;
			// Do not recompile if we have any edited files needing confirmation. If we do, the editor window will refresh and displayed tests will be removed.
			if (!filesWithEdits.Any())
			{
				isSuccess = true;
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			SetUpView(isSuccess);
		}

		public void ClearAll()
		{
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
			overrideToggles = new List<(string recording, Toggle toggle)>();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			SetUpView();
		}

		async static void RemoveMessageLabel()
		{
			await Task.Delay(2500);
			if (successLabel != null)
				root.Remove(successLabel);
			if (errorLabel != null)
				root.Remove(errorLabel);
			mainButtonRow.AddToClassList("label-space-padding");
			errorLabel = successLabel = null;
		}

		/// <summary>
		/// Determines if any checkboxes are selected on the main or overwrite views. If none are selected, no work can be done by code generation logic.
		/// </summary>
		/// <returns></returns>
		private bool AreNoRecordingCheckboxesSelected()
		{
			return !overrideToggles.Any() && !toggles.FindAll(x => x.toggle.value).Any() || !toggles.Any() && !overrideToggles.FindAll(x => x.toggle.value).Any();
		}
	}
}