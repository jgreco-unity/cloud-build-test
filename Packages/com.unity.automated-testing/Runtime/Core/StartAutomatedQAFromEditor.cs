#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.AutomatedQA.Editor
{
    [ExecuteInEditMode]
    public class StartAutomatedQAFromEditor : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private AutomatedRun.RunConfig runConfig;

        public static void StartAutomatedRun(AutomatedRun run)
        {
            var go = new GameObject("StartAutomatedQAFromEditor");
            var init = go.AddComponent<StartAutomatedQAFromEditor>();
            init.runConfig = run.config;
            
            EditorApplication.isPlaying = true;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (runConfig == null)
                {
                    Debug.LogError($"runConfig is null");
                }

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