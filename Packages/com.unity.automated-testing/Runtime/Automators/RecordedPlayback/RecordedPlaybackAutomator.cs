using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


namespace Unity.RecordedPlayback
{
    
    [Serializable]
    public class RecordedPlaybackAutomatorConfig : AutomatorConfig<RecordedPlaybackAutomator>
    {
        public string recordingFilePath = null;
    }
    public class RecordedPlaybackAutomator : Automator<RecordedPlaybackAutomatorConfig>
    {
        public override void BeginAutomation()
        {
            base.BeginAutomation();

            if (!string.IsNullOrEmpty(config.recordingFilePath))
            {
                Debug.Log($"Using recording asset - recordingFilePath: {config.recordingFilePath}");
                RecordedPlaybackPersistentData.SetRecordingDataFromFile(Path.Combine(Application.dataPath, config.recordingFilePath));
            }
            else
            {
                Debug.Log($"Using RecordedPlaybackPersistentData - kRecordedPlaybackFilename: {RecordedPlaybackPersistentData.kRecordedPlaybackFilename}");
            }

            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback);
            RecordedPlaybackController.Instance.Begin();

            StartCoroutine(HandlePlaybackCompletion());
        }       
        
        IEnumerator HandlePlaybackCompletion()
        {
            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.Instance.IsPlaybackCompleted())
            {
                yield return null;
            }

            EndAutomation();
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