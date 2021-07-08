using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using Unity.RecordedTesting.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.RecordedTesting;


namespace Unity.RecordedPlayback
{

    [Serializable]
    public class RecordedPlaybackAutomatorConfig : AutomatorConfig<RecordedPlaybackAutomator>
    {
        public TextAsset recordingFile = null;
        public bool loadEntryScene = true;
    }
    public class RecordedPlaybackAutomator : Automator<RecordedPlaybackAutomatorConfig>
    {
        public override void BeginAutomation()
        {
            base.BeginAutomation();

            string recordingFileName = "";

            if (config.recordingFile != null)
            {
                Debug.Log($"Using recording asset - recordingFile: {config.recordingFile.name}");
                RecordedPlaybackPersistentData.SetRecordingData(config.recordingFile.text);
                recordingFileName = config.recordingFile.name;
            }
            else
            {
                Debug.Log($"Using RecordedPlaybackPersistentData - kRecordedPlaybackFilename: {RecordedPlaybackPersistentData.kRecordedPlaybackFilename}");
            }

            StartCoroutine(PlayRecording(recordingFileName));
        }

        private IEnumerator PlayRecording(string recordingFileName)
        {
            // Load scene
            var recordingData = RecordedPlaybackPersistentData.GetRecordingData<RecordingInputModule.InputModuleRecordingData>();
            RecordedPlaybackPersistentData.RecordedResolution = recordingData.recordedResolution;
            RecordedPlaybackPersistentData.RecordedAspectRatio = recordingData.recordedAspectRatio;
            yield return LoadEntryScene(recordingData);

            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback, recordingFileName);
            RecordedPlaybackController.Instance.Begin();

            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.Instance.IsPlaybackCompleted())
            {
                yield return null;
            }

            EndAutomation();
        }

        private IEnumerator LoadEntryScene(RecordingInputModule.InputModuleRecordingData recordingData)
        {
            if (config.loadEntryScene)
            {
                Debug.Log($"Load Scene {recordingData.entryScene}");
                var loadSceneAsync = SceneManager.LoadSceneAsync(recordingData.entryScene);
                float timer = AutomatedQARuntimeSettings.DynamicLoadSceneTimeout;
                while (!loadSceneAsync.isDone && timer > 0)
                {
                    yield return new WaitForEndOfFrame();
                    timer -= Time.deltaTime;
                }
                if (!loadSceneAsync.isDone && timer <= 0)
                {
                    Debug.LogError($"Failed to load scene in timeout period. Scene [{recordingData.entryScene}] Timeout [{AutomatedQARuntimeSettings.DynamicLoadSceneTimeout}]");
                }
            }
            yield return WaitForFirstActiveScene(recordingData, 60);
        }

        private IEnumerator WaitForFirstActiveScene(RecordingInputModule.InputModuleRecordingData recordingData, int timeoutSecs)
        {
            var touchData = recordingData.GetAllTouchData();
            if (touchData.Count > 0)
            {
                var startTime = DateTime.UtcNow;
                var firstActionScene = touchData[0].scene;
                if (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    Debug.Log($"Waiting for scene {firstActionScene} to load");
                }
                while (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    var elapsed = DateTime.UtcNow.Subtract(startTime).TotalSeconds;
                    Debug.Log(elapsed);
                    if (elapsed >= timeoutSecs)
                    {
                        Debug.LogError($"Timeout wile waiting for scene {firstActionScene} to load");
                        break;
                    }
                    yield return new WaitForSeconds(1);
                }
            }
        }

        public override void EndAutomation()
        {
            base.EndAutomation();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }
        }
    }
}