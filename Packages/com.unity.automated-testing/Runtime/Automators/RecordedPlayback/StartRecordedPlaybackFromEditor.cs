#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace Unity.RecordedPlayback.Editor
{
    [ExecuteInEditMode]
    public class StartRecordedPlaybackFromEditor : MonoBehaviour
    {
        
        public static void EnterPlaymodeAndRecord()
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Record);
            RecordedPlaybackPersistentData.CleanRecordingData();
            CreateInitializer();

            EditorApplication.isPlaying = true;
        }
        
        public static void EnterPlaymodeAndPlay(string recordingFilePath)
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback, recordingFilePath);
            RecordedPlaybackPersistentData.SetRecordingDataFromFile(recordingFilePath);
            CreateInitializer();

            EditorApplication.isPlaying = true;
        }
        
        public static void EnterExtendModeAndRecord()
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Extend);
            CreateInitializer();
            EditorApplication.isPlaying = true;
        }

        private static void CreateInitializer()
        {
            new GameObject("StartRecordedPlaybackFromEditor").AddComponent<StartRecordedPlaybackFromEditor>();
        }
        
        private IEnumerator Start()
        {
            // Wait for 1 frame to avoid initializing too early
            yield return null;

            if (Application.isPlaying && RecordedPlaybackPersistentData.GetRecordingMode() != RecordingMode.None)
            {
                ReportingManager.IsAutomatorTest = false;
                RecordedPlaybackController.Instance.Begin();
            }

            if (EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Destroys the StartRecordedPlaybackFromEditor unless it is currently transitioning to playmode
                DestroyImmediate(this.gameObject);
            }
        }


    }
}
#endif
