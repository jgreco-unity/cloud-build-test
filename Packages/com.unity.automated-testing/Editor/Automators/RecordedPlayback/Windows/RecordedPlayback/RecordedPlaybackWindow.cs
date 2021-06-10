using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Unity.RecordedPlayback.Editor
{
    public class RecordedPlaybackWindow: EditorWindow
    {
        public static class  StyleGuide
        {
            public static readonly RectOffset window_margin = new RectOffset(5,5,5,5);
            public static readonly float header_space = 20;
            public static readonly RectOffset big_button_padding = new RectOffset(15,15,8,8);
            public static readonly float small_button_max_width = 30;
            
            public static readonly string path_icons = "Packages/com.unity.automated-testing/Editor/Automators/RecordedPlayback/Windows/RecordedPlayback/icons/";

            public static Texture icon_record => AssetDatabase.LoadAssetAtPath<Texture>($"{path_icons}icon_record.png");
            public static Texture icon_play => AssetDatabase.LoadAssetAtPath<Texture>($"{path_icons}icon_play.png");
            public static Texture icon_stop => AssetDatabase.LoadAssetAtPath<Texture>($"{path_icons}icon_stop.png");
            public static Texture icon_locate => AssetDatabase.LoadAssetAtPath<Texture>($"{path_icons}icon_find.png");

        }
        
        public enum EditorWindowState
        {
            Error = -1,
            Reset,
            NeedsSetUp,
            RecordPlayControls
        }

        private EditorWindowState state = EditorWindowState.Reset;
        private bool isPlayMode = false;
        private bool playModeStartedFromHere = false;
        
        private Vector2 scrollPos = Vector2.zero;
        private Dictionary<string, string> fileRenames = new Dictionary<string, string>();
        private string renameFile;

        [MenuItem("Automated QA/Recorded Playback...", priority=AutomatedQAEditorSettings.MenuItems.RecordedPlayback)]
        public static void ShowWindow()
        {
            RecordedPlaybackWindow wnd = GetWindow<RecordedPlaybackWindow>();
            wnd.Init();
            wnd.Show();
        }

        private void Init()
        {
            titleContent = new GUIContent("Recorded Playback");
            state = EditorWindowState.Reset;
        }

        private void Update()
        {
            switch (state)
            {
                case EditorWindowState.Reset:
                    UpdateStateReset();
                    break;
                case EditorWindowState.NeedsSetUp:
                    UpdateStateNeedsSetup();
                    break;
                case EditorWindowState.RecordPlayControls:
                    UpdateStateRecordPlayControls();
                    break;
                case EditorWindowState.Error:
                default:
                    UpdateStateError();
                    break;
            }
        }

        private void UpdateStateReset()
        {
            if (IsProjectSetUp())
            {
                state = EditorWindowState.RecordPlayControls;
            }
            else
            {
                state = EditorWindowState.NeedsSetUp;
            }
        }

        private void UpdateStateNeedsSetup()
        {
            // Empty
        }
        
        private void UpdateStateRecordPlayControls()
        {
            if (playModeStartedFromHere && 
                EditorApplication.isPlaying &&
                RecordedPlaybackController.Exists() && 
                RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Playback &&
                RecordedPlaybackController.Instance.IsPlaybackCompleted())
            {
                EditorApplication.isPlaying = false;
            }
            
            // poll for state change
            if (EditorApplication.isPlaying && !isPlayMode)
            {
                isPlayMode = true;
                OnEnterPlaymode();
            }
            else if (!EditorApplication.isPlaying && isPlayMode)
            {
                isPlayMode = false;
                OnExitPlaymode();
            }
   
        }

        void OnEnterPlaymode()
        {
        }
        
        void OnExitPlaymode()
        {
            if (playModeStartedFromHere)
            {
                if (RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record)
                {
                    RecordedPlaybackEditorUtils.SaveCurrentRecordingDataAsProjectAsset();
                }
                
                RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.None);
            }
            
            playModeStartedFromHere = false;
        }
        
        
        private void UpdateStateError()
        {
            // Empty
        }
        
        private void OnGUI()
        {
            switch (state)
            {
                case EditorWindowState.NeedsSetUp:
                    GUIStateNeedsSetup();
                    break;
                case EditorWindowState.RecordPlayControls:
                    GUIStateRecordPlayControls();
                    break;
                default:
                    GUIStateError();
                    break;
            }
        }



        private void GUIStateNeedsSetup()
        {
            EditorGUILayout.LabelField("Your project is not set up for Recorded Playback");
        }

        private void GUIStateRecordPlayControls()
        {
            var windowStyle = new GUIStyle(GUIStyle.none);
            windowStyle.margin = StyleGuide.window_margin;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, windowStyle);
                
            GUIRecordControl();
            GUIListRecordings();
         
            EditorGUILayout.EndScrollView();
        }

        private void GUIStateError()
        {
            EditorGUILayout.LabelField("Error. See console output for details.");
        }
        
        
        private bool IsProjectSetUp()
        {
            // TODO
            return true;
        }
        
        private void GUIRecordControl()
        {
            GUIRecordControlButton();
            GUIOutputPath();
        }

        private void GUIRecordControlButton()
        {
            if (EditorApplication.isPlaying && playModeStartedFromHere)
            {
                GUIStopButton();
            }
            else
            {
                GUIStartRecordingButton();
            }
            
        }

        private void GUIStopButton()
        {
            EditorGUILayout.BeginHorizontal();
            
            var stopButton = new GUIContent(" Stop");
            stopButton.image = RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record
                ? StyleGuide.icon_record
                : StyleGuide.icon_stop;
            var stopButtonStyle = new GUIStyle(GUI.skin.button);
            stopButtonStyle.padding = StyleGuide.big_button_padding;
            if (GUILayout.Button(stopButton, stopButtonStyle))
            {
                EditorApplication.isPlaying = false;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void GUIStartRecordingButton()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            EditorGUILayout.BeginHorizontal();
            
            var recordButton = new GUIContent(" Record");
            recordButton.image = StyleGuide.icon_record;
            var recordButtonStyle = new GUIStyle(GUI.skin.button);
            recordButtonStyle.padding = StyleGuide.big_button_padding;
            if (GUILayout.Button(recordButton, recordButtonStyle))
            {
                StartNewRecording();
            }
            GUILayout.FlexibleSpace();

            if (ReportingManager.DoesReportExist(ReportingManager.ReportType.Html))
            {
                var reportButton = new GUIContent("☰ Show Report");
                if (GUILayout.Button(reportButton, recordButtonStyle))
                {
                    ShowHtmlReport();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(5);
        }

        private void StartNewRecording()
        {
            isPlayMode = false;
            playModeStartedFromHere = true;
            StartRecordedPlaybackFromEditor.EnterPlaymodeAndRecord();
        }

        private void ShowHtmlReport() 
        {
            ReportingManager.OpenReportFile(ReportingManager.ReportType.Html);
        }

        private void GUIOutputPath()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Recording asset path");
            
            GUILayout.FlexibleSpace();

            GUILayout.Label($"Assets/{AutomatedQARuntimeSettings.RecordingFolderPath}");

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();

        }
        
        private void GUIListRecordings()
        {
            GUIHeader("Recordings");

            var paths = GetAllRecordingAssetPaths();
            for (int i = 0; i < paths.Count; i++ )
            {
                GUIRecordingAsset(paths[i], i%2==0);
            }
        }


        private void GUIHeader(string text)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

        }

        private List<string> GetAllRecordingAssetPaths()
        {
            if (!Directory.Exists($"Assets/{AutomatedQARuntimeSettings.RecordingFolderPath}"))
            {
                return new List<string>();
            }
            
           var results = new List<string>(AssetDatabase.FindAssets("*", new[] {$"Assets/{AutomatedQARuntimeSettings.RecordingFolderPath}"}));
           for (int i = 0; i < results.Count; i++)
           {
               results[i] = AssetDatabase.GUIDToAssetPath(results[i]);
           }
           
           results.Sort((a, b) => Convert.ToInt32((File.GetCreationTime(b) - File.GetCreationTime(a) ).TotalSeconds)); 

           return results;
        }

        private void GUIRecordingAsset(string recordingFilePath, bool even)
        {
            var row = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(row, new Color(1,1,1,even ? 0.1f : 0));

            if (renameFile == recordingFilePath || fileRenames.ContainsKey(recordingFilePath))
            {
                GUIRecordingRenameView(recordingFilePath);
            }
            else
            {
                var filename = recordingFilePath.Substring($"Assets/{AutomatedQARuntimeSettings.RecordingFolderPath}".Length + 1);
                EditorGUILayout.LabelField(filename);
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(StyleGuide.small_button_max_width));
                GUIRecordingRenameButton(recordingFilePath);
                GUIRecordingFindButton(recordingFilePath);
                GUIRecordingPlayButton(recordingFilePath);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void GUIRecordingFindButton(string recordingFilePath)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            var findButton = new GUIContent(" Find");
            findButton.image = StyleGuide.icon_locate;
            if (GUILayout.Button(findButton))
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(recordingFilePath);
              //  ProjectWindowUtil.StartNameEditingIfProjectWindowExists(recordingFilePath);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void GUIRecordingPlayButton(string recordingFilePath)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            var playButton = new GUIContent(" Play");
            playButton.image = StyleGuide.icon_play;
            if (GUILayout.Button(playButton))
            {
                PlayRecording(recordingFilePath);
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void GUIRecordingRenameButton(string recordingFilePath)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            
            var renameButton = new GUIContent("Rename");
            if (GUILayout.Button(renameButton))
            {
                if (string.IsNullOrEmpty(renameFile) && fileRenames.Count == 0)
                {
                    renameFile = recordingFilePath;
                }
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void GUIRecordingRenameView(string recordingFilePath)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);

            var controlName = "RenameText" + recordingFilePath.Replace(Path.DirectorySeparatorChar, '_');
            GUI.SetNextControlName (controlName);
            
            if (renameFile == recordingFilePath)
            {
                var filename = recordingFilePath.Substring($"Assets/{AutomatedQARuntimeSettings.RecordingFolderPath}".Length + 1);
                fileRenames.Add(recordingFilePath, filename);
            }

            var recordingName = fileRenames[recordingFilePath];
            fileRenames[recordingFilePath] = GUILayout.TextField(recordingName);
            
            if (renameFile == recordingFilePath)
            {
                GUI.FocusControl(controlName);
                if (recordingName.Contains("."))
                {
                    var te = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (te != null)
                    {
                        te.OnFocus();
                        te.cursorIndex = 0;
                        te.selectIndex = recordingName.LastIndexOf('.');
                    }
                }

                renameFile = "";
            }

            var saveButton = new GUIContent("Save");
            if (GUILayout.Button(saveButton))
            {
                GUI.FocusControl(null);
                var renamePath = Path.Combine("Assets", AutomatedQARuntimeSettings.RecordingFolderPath, fileRenames[recordingFilePath]);
                AssetDatabase.MoveAsset(recordingFilePath, renamePath);
                Debug.Log($"Renamed {recordingFilePath} to {renamePath}");
                fileRenames.Remove(recordingFilePath);
            }
            
            var cancelButton = new GUIContent("Cancel");
            if (GUILayout.Button(cancelButton))
            {
                GUI.FocusControl(null);
                fileRenames.Remove(recordingFilePath);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void PlayRecording(string recordingFilePath)
        {
            isPlayMode = false;
            playModeStartedFromHere = true;
            StartRecordedPlaybackFromEditor.EnterPlaymodeAndPlay(recordingFilePath);
        }
 
    }
}