using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Listeners;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Unity.RecordedPlayback
{
    public class RecordedPlaybackController : MonoBehaviour
    {
        private RecordingInputModule inputModule = null;
        private bool initialized = false;

        private static RecordedPlaybackController _instance = null;
        public static RecordedPlaybackController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("RecordedPlaybackController");
                    _instance = go.AddComponent<RecordedPlaybackController>();

                    // Singleton, persist between scenes to record/play across multiple scenes. 
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void Begin()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            if (!ReportingManager.IsTestWithoutRecordingFile && RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Playback && !File.Exists(RecordedPlaybackPersistentData.GetRecordingDataFilePath()))
            {
                Debug.LogError($"Recorded Playback file does not exist.");
                return;
            } 

            if (inputModule == null)
            {
                inputModule = gameObject.AddComponent<RecordingInputModule>();
            }
            if (RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record)
            {
                gameObject.AddComponent<KeyInputHandler>();
            }
            SetEventSystem();
            VisualFxManager.SetUp(Instance.transform);
        }

        public void Reset()
        {
            Destroy(gameObject);
            _instance = null;
        }

        public bool IsPlaybackCompleted()
        {
            return inputModule != null && inputModule.IsPlaybackCompleted();
        }

        public void SaveRecordingSegment()
        {
            if (inputModule != null)
            {
                inputModule.SaveRecordingSegment();
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoadSetup;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoadSetup;
        }

        void SceneLoadSetup(Scene scene, LoadSceneMode mode)
        {
            SetEventSystem();
        }

        public static bool Exists()
        {
            return _instance != null;
        }

        public bool IsInitialized()
        {
            return initialized;
        }

        /// <summary>
        /// Check if an EventSystem already exists at the time of recording or playback start.
        /// If one exists, set our EventSystem variables to the values defined by the existing system.
        /// Finally, disable the pre-existing system. There can only be one active EventSystem.
        /// </summary>
        void SetEventSystem()
        {
            if (!initialized)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                GameObject inputObj = new List<GameObject>(FindObjectsOfType<GameObject>()).Find(x =>
                    x != gameObject && x.GetComponent<BaseInputModule>() && x.GetComponent<EventSystem>());
                if (inputObj == null)
                {
                    Debug.Log("No existing Event System & Input Module was found");
                    return;
                }

                RecordingInputModule ourModule = inputModule;
                StandaloneInputModule theirModule = inputObj.GetComponent<StandaloneInputModule>();
                BaseInputModule theirBaseModule = inputObj.GetComponent<BaseInputModule>();
                if (theirModule != null)
                {
                    ourModule.cancelButton = theirModule.cancelButton;
                    ourModule.submitButton = theirModule.submitButton;
                    ourModule.verticalAxis = theirModule.verticalAxis;
                    ourModule.horizontalAxis = theirModule.horizontalAxis;
                    ourModule.inputActionsPerSecond = theirModule.inputActionsPerSecond;
                    ourModule.repeatDelay = theirModule.repeatDelay;
                }

                EventSystem ourEventSystem = ourModule.GetComponent<EventSystem>();
                EventSystem theirEventSystem = inputObj.GetComponent<EventSystem>();
                ourEventSystem.firstSelectedGameObject = theirEventSystem.firstSelectedGameObject;
                ourEventSystem.sendNavigationEvents = theirEventSystem.sendNavigationEvents;
                ourEventSystem.pixelDragThreshold = theirEventSystem.pixelDragThreshold;

                theirBaseModule.enabled = theirEventSystem.enabled = false;
            }

            EventSystem.current = GetComponent<EventSystem>();
        }
    }
}