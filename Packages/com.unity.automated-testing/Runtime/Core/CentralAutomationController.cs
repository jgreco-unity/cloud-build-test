using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.AutomatedQA
{
    public class CentralAutomationController : MonoBehaviour
    {
        private AutomatedRun.RunConfig runConfig = null;

        public bool quitOnFinish = false;
        
        private List<Automator> automators = new List<Automator>();
        private int currentIndex = 0;
        private bool initialized = false;

        private static CentralAutomationController _instance = null;
        public static CentralAutomationController Instance {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CentralAutomationController");
                    _instance = go.AddComponent<CentralAutomationController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void Run(AutomatedRun.RunConfig runConfig = null)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            this.runConfig = runConfig;

            if (runConfig != null)
            {
                foreach (var automatorConfig in runConfig.automators)
                {
                    AddAutomator(automatorConfig);
                }
            }
            BeginAutomator();

        }

        public static bool Exists()
        {
            return _instance != null;
        }
        
        public T AddAutomator<T>() where T : Automator
        {
            var go = new GameObject(typeof(T).ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent<T>();
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }
        
        public T AddAutomator<T>(AutomatorConfig config) where T : Automator
        {
            var go = new GameObject(typeof(T).ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent<T>();
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init(config);
            return automator;
        }
        
        public Automator AddAutomator(Type AutomatorType)
        {
            var go = new GameObject(AutomatorType.ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent(AutomatorType) as Automator;
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }

        public T AddAutomator<T>(T prefab) where T : Automator
        {
            var automator = Instantiate(prefab, transform);
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }

        public Automator AddAutomator(AutomatorConfig config)
        {
            var automator = AddAutomator(config.AutomatorType);
            automator.Init(config);
            return automator;
        }

        public void Reset()
        {
            foreach (var automator in automators)
            {
                automator.Cleanup();
                Destroy(automator.gameObject);
            }

            runConfig = null;
            quitOnFinish = false;
            automators = new List<Automator>();
            currentIndex = 0;
            initialized = false;

            Destroy(gameObject);
            _instance = null;
        }

        
        private void SubscribeEvents(Automator automator)
        {
            automator.OnAutomationFinished.AddListener(OnAutomationFinished);
        }
        
        
        private void OnAutomationFinished(Automator.AutomationFinishedEvent.Args args)
        {
            currentIndex++;

            if (quitOnFinish && currentIndex >= automators.Count)
            {
                #if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
                #else
                Application.Quit();
                #endif
            }
            
            BeginAutomator();
        }
        
        void BeginAutomator()
        {
            if (currentIndex >= 0 && currentIndex < automators.Count)
            {
                automators[currentIndex].BeginAutomation();
            }

        }

        public bool IsAutomationComplete()
        {
            return currentIndex >= automators.Count;
        }

        public T GetAutomator<T>() where T : Automator
        {
            var results = GetAutomators<T>();
            if (results.Count > 0)
            {
                return results[0];
            }

            return null;
        }
        
        public List<T> GetAutomators<T>() where T : Automator
        {
            List<T> results = new List<T>();

            foreach (var automator in automators)
            {
                if (automator is T)
                {
                    results.Add((T) automator);
                }
            }

            return results;
        }
    }

}
