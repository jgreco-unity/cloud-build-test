using System;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.AutomatedQA.Listeners;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Unity.RecordedPlayback.Editor
{
	public class RecordedPlaybackWindow : EditorWindow
	{
		private static RecordedPlaybackWindow wnd;
		private static readonly string WINDOW_FILE_NAME = "recording-window";
		private static string resourcePath = "Packages/com.unity.automated-testing/Editor/Automators/RecordedPlayback/Windows/RecordedPlayback/";
		private ScrollView root;
		private VisualElement recordingContainer;
		private static string renameFile;
		private static Label stateLabel;
		private static bool WaitForModuleReady;
		private static bool firstRecordingListedIsBrandNew;
		private static List<string> recordingPaths;
		private static string searchFilterText;

		[SerializeField]
		private bool isRecording;

		[SerializeField]
		private bool startCrawl;

		[SerializeField]
		private bool renderStopButton;
		
		[MenuItem("Automated QA/Recorded Playback...", priority = AutomatedQAEditorSettings.MenuItems.RecordedPlayback)]
		public static void ShowWindow()
		{
			wnd = GetWindow<RecordedPlaybackWindow>();
			wnd.Show();
			wnd.Init();
		}

		private void Init()
		{
			recordingPaths = GetAllRecordingAssetPaths();
			recordingPaths.Sort();
			EditorApplication.playModeStateChanged -= StopRecording;
			EditorApplication.playModeStateChanged += StopRecording;
			initialized = false;
			wnd.titleContent = new GUIContent("Recorded Playback");
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
			else if (!WaitForModuleReady && renderStopButton && !RecordingInputModule.isWorkInProgress && ReportingManager.IsReportingFinished())
			{
				StopRecording(PlayModeStateChange.ExitingPlayMode);
			}

			if (startCrawl && RecordedPlaybackController.Instance != null)
			{
				startCrawl = false;
				RecordedPlaybackController.Instance.gameObject.AddComponent<GameListenerHandler>();
				GameCrawler gc = RecordedPlaybackController.Instance.gameObject.AddComponent<GameCrawler>();
				gc.Initialize();
			}
		}

        void SetUpView()
		{
			VisualElement baseRoot = rootVisualElement;
			rootVisualElement.Clear();

			root = new ScrollView();
			baseRoot.Add(root);

			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
			visualTree.CloneTree(baseRoot);

			baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));

			RenderElements();
			RenderRecordings();
		}

		void RenderElements()
		{
			VisualElement buttonsRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
					alignContent = Align.Center
				}
			};
			buttonsRow.AddToClassList("buttons-row");

			if (!renderStopButton && !RecordingInputModule.isWorkInProgress)
			{
				Button saveButton = new Button() { text = "Record", style = { flexGrow = 1, height = 30 } };
				saveButton.clickable.clicked += () => { StartRecording(); };
				saveButton.AddToClassList("button");
				buttonsRow.Add(saveButton);

				Button crawlButton = new Button() { text = "Crawl", style = { flexGrow = 1, height = 30 } };
				crawlButton.clickable.clicked += () => { StartCrawl(); };
				crawlButton.AddToClassList("button");
				buttonsRow.Add(crawlButton);
			}
			else
			{
				stateLabel = new Label();
				stateLabel.text = "●";
				stateLabel.AddToClassList("state-label");
				stateLabel.AddToClassList("red");
				buttonsRow.Add(stateLabel);
				Button stopButton = new Button() { text = "Stop", style = { flexGrow = 1, height = 30 } };
				stopButton.clickable.clicked += () => { StopRecording(); };
				stopButton.AddToClassList("button");
				buttonsRow.Add(stopButton);
			}

			if (ReportingManager.DoesReportExist(ReportingManager.ReportType.Html))
			{
				Button reportButton = new Button() { text = "☰ Show Report", style = { flexGrow = 1, height = 30 } };
				reportButton.clickable.clicked += () => { ShowHtmlReport(); };
				reportButton.AddToClassList("button");
				buttonsRow.Add(reportButton);
			}

			root.Add(buttonsRow);

			Label label = new Label();
			label.text = "- Recording asset path -";
			label.AddToClassList("center");
			root.Add(label);

			Label val = new Label()
			{
				style =
				{
					marginBottom = 10
				}
			}; 
			val.text = AutomatedQARuntimeSettings.RecordingFolderNameWithAssetPath;
			val.AddToClassList("center");
			root.Add(val);

			VisualElement refreshRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
					alignContent = Align.Center
				}
			};

			Label filterLabel = new Label();
			filterLabel.text = "Filter: ";
			filterLabel.AddToClassList("filter-label");
			refreshRow.Add(filterLabel);

			searchFilterText = string.Empty;
			TextField newName = new TextField();
			newName.AddToClassList("filter-field");
			newName.RegisterValueChangedCallback(x =>
			{
				searchFilterText = x.newValue;
				root.Remove(recordingContainer);
				RenderRecordings();
			});
			refreshRow.Add(newName);

			Button refreshListButton = new Button() { text = "↻", tooltip = "Refresh recordings list" };
			refreshListButton.clickable.clicked += () => 
			{
				newName.value = string.Empty;
				recordingPaths = GetAllRecordingAssetPaths();
				recordingPaths.Sort();
				SetUpView();
			};
			refreshListButton.AddToClassList("refresh-button");
			refreshRow.Add(refreshListButton);
			root.Add(refreshRow);
		}

		private void RenderRecordings()
		{
			if(!recordingPaths.Any())
				recordingPaths = GetAllRecordingAssetPaths();

			recordingContainer = new VisualElement();
			for (int i = 0; i < recordingPaths.Count; i++)
			{
				string recordingFilePath = recordingPaths[i];
				string filename = recordingFilePath.Substring($"{AutomatedQARuntimeSettings.RecordingFolderNameWithAssetPath}".Length + 1);

				if (!string.IsNullOrEmpty(searchFilterText))
				{
					if (!filename.Contains(searchFilterText))
						continue;
				}

				VisualElement row = new VisualElement();
				if (renameFile == recordingFilePath)
				{
					RecordingRenameView(recordingFilePath);
				}
				else
				{
					VisualElement recordingRow = new VisualElement();
					recordingRow.AddToClassList("item-row");
					if (i % 2 == 0)
					{
						recordingRow.AddToClassList("even");
					}
					if (firstRecordingListedIsBrandNew)
                    {
						firstRecordingListedIsBrandNew = false;
						recordingRow.AddToClassList("item-row-new");
					}

					Button playButton = new Button() { text = "▸" };
					playButton.clickable.clicked += () => {
						if (!RecordingInputModule.isWorkInProgress)
						{
							renderStopButton = true;
							PlayRecording(recordingFilePath);
							SetUpView();
							WaitForModuleToBeReady();
						}
					};
					playButton.tooltip = "Play recording";
					playButton.AddToClassList("small-button");
					playButton.AddToClassList("play-button");
					recordingRow.Add(playButton);

					Button renameButton = new Button() { text = "✎" };
					renameButton.clickable.clicked += () => {
						renameFile = recordingFilePath;
						SetUpView();
					};
					renameButton.tooltip = "Rename recording file";
					renameButton.AddToClassList("small-button");
					renameButton.AddToClassList("rename-button");
					recordingRow.Add(renameButton);

					Button findButton = new Button() { text = "↯" };
					findButton.clickable.clicked += () => {
						Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(recordingFilePath);
					};
					findButton.tooltip = "Find/Highlight recording file in project window.";
					findButton.AddToClassList("small-button");
					findButton.AddToClassList("find-button");
					recordingRow.Add(findButton);

					Label label = new Label();
					label.AddToClassList("recording-label");
					label.text = filename;
					recordingRow.Add(label);

					recordingContainer.Add(recordingRow);
				}
			}
			root.Add(recordingContainer);
		}

		private void RecordingRenameView(string recordingFilePath)
		{
			string fileName = recordingFilePath.Substring($"{AutomatedQARuntimeSettings.RecordingFolderNameWithAssetPath}".Length + 1);

			VisualElement renameContainer = new VisualElement();
			renameContainer.AddToClassList("item-block");

			TextField newName = new TextField();
			newName.value = fileName.Replace(".json", string.Empty);
			newName.AddToClassList("edit-field");

			// Schedule focus event to highlight input's text (Required for successful focus).
			rootVisualElement.schedule.Execute(() => {
				newName.ElementAt(0).Focus();
			});

			Button saveButton = new Button() { text = "✓" };
			saveButton.clickable.clicked += () => {
				var renamePath = Path.Combine("Assets", AutomatedQARuntimeSettings.RecordingFolderName, $"{newName.value}.json");
				AssetDatabase.MoveAsset(recordingFilePath, renamePath);
				recordingPaths = GetAllRecordingAssetPaths();
				recordingPaths.Sort();
				SetUpView();
			};
			saveButton.AddToClassList("small-button");
			renameContainer.Add(saveButton);

			Button cancelButton = new Button() { text = "X" };
			cancelButton.clickable.clicked += () => {
				renameFile = string.Empty;
				SetUpView();
			};
			cancelButton.AddToClassList("small-button");
			renameContainer.Add(cancelButton);

			renameContainer.Add(newName);
			root.Add(renameContainer);
		}

		void StartRecording()
		{
			WaitForModuleToBeReady();
			renderStopButton = isRecording = true;
			StartRecordedPlaybackFromEditor.StartRecording();
			SetUpView();
		}

		void StopRecording(PlayModeStateChange state = PlayModeStateChange.ExitingPlayMode)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				RecordingInputModule.Instance.EndRecording();
				if (isRecording || ReportingManager.IsCrawler)
				{
					recordingPaths = recordingPaths.AddAtAndReturnNewList(0, $"Assets/Recordings/{RecordedPlaybackEditorUtils.SaveCurrentRecordingDataAsProjectAsset()}");
					firstRecordingListedIsBrandNew = true;
				}
				GameCrawler.Stop = true;
				ReportingManager.IsCrawler = renderStopButton = isRecording = false;
				SetUpView();
			}
		}

		private void PlayRecording(string recordingFilePath)
		{
			StartRecordedPlaybackFromEditor.StartPlayback(recordingFilePath);
		}

		private List<string> GetAllRecordingAssetPaths()
		{
			if (!Directory.Exists($"{AutomatedQARuntimeSettings.RecordingFolderNameWithAssetPath}"))
			{
				return new List<string>();
			}

			var results = new List<string>(AssetDatabase.FindAssets("*", new[] { $"{AutomatedQARuntimeSettings.RecordingFolderNameWithAssetPath}" }));
			for (int i = 0; i < results.Count; i++)
			{
				results[i] = AssetDatabase.GUIDToAssetPath(results[i]);
			}

			results.Sort((a, b) => Convert.ToInt32((File.GetCreationTime(b) - File.GetCreationTime(a)).TotalSeconds));

			return results;
		}

		void StartCrawl()
		{
			WaitForModuleToBeReady();
			renderStopButton = true;
			StartRecordedPlaybackFromEditor.StartRecording();
			ReportingManager.InitializeReport();
			rootVisualElement.schedule.Execute(() => {
				startCrawl = true;
			}).ExecuteLater(2000);
			SetUpView();
		}

		void WaitForModuleToBeReady()
		{
			WaitForModuleReady = true;
			rootVisualElement.schedule.Execute(() => {
				WaitForModuleReady = false;
			}).ExecuteLater(500);
		}

		private void ShowHtmlReport()
		{
			ReportingManager.OpenReportFile(ReportingManager.ReportType.Html);
		}
	}
}