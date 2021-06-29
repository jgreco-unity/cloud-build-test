#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.AutomatedQA.Editor
{
    [ExecuteInEditMode]
    public class StartAutomatedQAFromEditor : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private AutomatedRun.RunConfig runConfig;

        [SerializeField]
        [HideInInspector]
        private string AutomatorName;

        public static void StartAutomatedRun(AutomatedRun run)
        {
            var go = new GameObject("StartAutomatedQAFromEditor");
            var init = go.AddComponent<StartAutomatedQAFromEditor>();
            init.runConfig = run.config;
            init.AutomatorName = run.ToString().Replace("(Unity.AutomatedQA.AutomatedRun)", string.Empty).Trim();
            EditorApplication.isPlaying = true;
        }

        private IEnumerator Start()
        {
            // Wait for 1 frame to avoid initializing too early
            yield return null;

            if (Application.isPlaying)
            {
                if (runConfig == null)
                {
                    Debug.LogError($"runConfig is null");
                }

                ReportingManager.IsAutomatorTest = true;
                ReportingManager.CurrentTestName = AutomatorName;
                CentralAutomationController.Instance.quitOnFinish = true;
                CentralAutomationController.Instance.Run(runConfig);
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