using Unity.RecordedPlayback;
using UnityEngine;

public class ReportingMonitor : MonoBehaviour
{
    public RecordingMode RecordingMode
    {
        get
        {
            return _recordingMode;
        }
        set {
            _recordingMode = value;
        }
    }
    private RecordingMode _recordingMode;

    /// <summary>
    /// For Windows Store & Android "end state".
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (!hasFocus&& RecordingMode == RecordingMode.Playback)
        {
            ReportingManager.FinalizeReport();
        }
#endif
    }

    /// <summary>
    /// For iOS "end state".
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationPause(bool pause)
    {
#if !UNITY_EDITOR
        if (RecordingMode == RecordingMode.Playback)
        {
            ReportingManager.FinalizeReport();
        }
#endif
    }

    /// <summary>
    /// For editor, standalone, and other platform's.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (RecordingMode == RecordingMode.Playback)
        {
            ReportingManager.FinalizeReport();
        }
    }
}
