﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Unity.AutomatedQA
{
    public class Driver : MonoBehaviour
    {
        private AQALogger logger;

        public static Driver Perform {
            get 
            {
                if (_instance == null)
                {
                    _instance = RecordedPlaybackController.Instance.gameObject.AddComponent<Driver>();
                }
                return _instance;
            }
        }
        private static Driver _instance;

        public static List<RecordingInputModule.TouchData> Steps = new List<RecordingInputModule.TouchData>();
        private static (int index, RecordingInputModule.TouchData touch) LastAction;

        private const float WAIT_TIME_AFTER_NON_RECORDED_ACTIONS = 1f;

        private void Awake()
        {
            logger = new AQALogger();
        }

        public static void Reset()
        {
            Steps = new List<RecordingInputModule.TouchData>();
            LastAction = default((int, RecordingInputModule.TouchData));
        }

        public IEnumerator Click(string querySelector, float holdDurationBeforeReleasingClick = 0f)
        {
            yield return WaitFor(() => ElementQuery.Find(querySelector) != null);
            GameObject go = ElementQuery.Find(querySelector);
            yield return Click(go: go, querySelector: querySelector, holdDurationBeforeReleasingClick: holdDurationBeforeReleasingClick);
        }

        public IEnumerator Click(GameElement ge, float holdDurationBeforeReleasingClick = 0f)
        {
            yield return Click(ge: ge, holdDurationBeforeReleasingClick: holdDurationBeforeReleasingClick);
        }

        public IEnumerator Click(GameObject go, float holdDurationBeforeReleasingClick = 0f)
        {
            yield return Click(go: go, holdDurationBeforeReleasingClick: holdDurationBeforeReleasingClick);
        }

        private IEnumerator Click(GameObject go = null, GameElement ge = null, string querySelector = "", float holdDurationBeforeReleasingClick = 0f)
        {
            if (go == null && ge == null)
            {
                logger.LogError($"Could not find GameObject provided to driver Click() method, or null object was provided as an argument.{(string.IsNullOrEmpty(querySelector) ? string.Empty : $" [Query String: {querySelector}]")}");
            }

            if (ge == null && go != null)
            {
                ge = go.GetComponent<GameElement>();
                if(ge != null)
                    querySelector = ElementQuery.ConstructQuerySelectorString(go);
            }

            if (go == null && ge != null)
            {
                go = ge.gameObject;
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            float x = (rect.position.x + rect.sizeDelta.x) / 2f;
            float y = (rect.position.y + rect.sizeDelta.y) / 2f;
            Vector2 position = new Vector2(x, y);

            RecordingInputModule.TouchData clickStart = GetTouchDataTemplate(go);
            clickStart.querySelector = querySelector;
            clickStart.eventType = RecordingInputModule.TouchData.type.press;
            clickStart.position = position;

            RecordingInputModule.TouchData clickRelease = GetTouchDataTemplate(go);
            clickRelease.querySelector = querySelector;
            clickRelease.eventType = RecordingInputModule.TouchData.type.release;
            clickRelease.timeDelta = holdDurationBeforeReleasingClick;

            Perform.RegisterSteps(clickStart, clickRelease);
            yield return Action(clickStart);
            yield return Action(clickRelease);
            yield return new WaitForSeconds(WAIT_TIME_AFTER_NON_RECORDED_ACTIONS); // Add additional wait since no time delta is associated with non-recorded actions.
        }

        public IEnumerator Drag(string querySelectorDragObject, string querySelectorTargetOfDrag, float duration = 1f)
        {
            yield return WaitFor(() => ElementQuery.Find(querySelectorDragObject) != null && ElementQuery.Find(querySelectorTargetOfDrag) != null);
            GameObject go1 = ElementQuery.Find(querySelectorDragObject);
            GameObject go2 = ElementQuery.Find(querySelectorTargetOfDrag);
            if (go1 == null || go2 == null)
                logger.LogError("Could not find one or both of the GameObjects with the required query strings.");

            Vector2 pos = go2.GetComponent<RectTransform>() ? Camera.main.ScreenToWorldPoint(go2.GetComponent<RectTransform>().position) : go2.transform.position;
            yield return Drag(position: pos, go: go1, duration: duration);
        }

        public IEnumerator Drag(string querySelectorDragObject, Vector2 targetDropPosition, float duration = 1f)
        {
            yield return WaitFor(() => ElementQuery.Find(querySelectorDragObject) != null);
            GameObject go = ElementQuery.Find(querySelectorDragObject);
            yield return Drag(targetDropPosition, go: go, duration: duration, querySelectorDragObject: querySelectorDragObject);
        }

        public IEnumerator Drag(GameElement ge, GameElement target, float duration = 1f)
        {
            Vector2 pos = target.GetComponent<RectTransform>() ? Camera.main.ScreenToWorldPoint(target.GetComponent<RectTransform>().position) : target.transform.position;
            yield return Drag(pos, go: ge.gameObject, ge: ge, duration: duration);
        }

        public IEnumerator Drag(GameElement ge, Vector2 position, float duration = 1f)
        {
            yield return Drag(position, go: ge.gameObject, ge: ge, duration: duration);
        }

        public IEnumerator Drag(GameObject go, GameObject target, float duration = 1f)
        {
            Vector2 pos = target.GetComponent<RectTransform>() ? Camera.main.ScreenToWorldPoint(target.GetComponent<RectTransform>().position) : target.transform.position;
            yield return Drag(pos, go: go, duration: duration);
        }

        public IEnumerator Drag(GameObject go, Vector2 position, float duration = 1f)
        {
            yield return Drag(position, go: go, duration: duration);
        }

        private IEnumerator Drag(Vector2 position, GameObject go = null, GameElement ge = null, float duration = 1f, string querySelectorDragObject = "") 
        {
            if (go == null && ge == null)
            {
                logger.LogError("Could not find GameObject provided to driver Click() method, or null object was provided as an argument.");
            }

            if (ge == null && go != null)
            {
                ge = go.GetComponent<GameElement>();
                if (ge != null)
                    querySelectorDragObject = ElementQuery.ConstructQuerySelectorString(go);
            }

            if (go == null && ge != null)
            {
                go = ge.gameObject;
            }

            RecordingInputModule.TouchData clickDraggable = GetTouchDataTemplate(go);
            clickDraggable.querySelector = querySelectorDragObject;
            clickDraggable.eventType = RecordingInputModule.TouchData.type.press;
            clickDraggable.position = go.transform.position;

            RecordingInputModule.TouchData dragStart = GetTouchDataTemplate(go);
            dragStart.querySelector = querySelectorDragObject;
            dragStart.eventType = RecordingInputModule.TouchData.type.drag;
            dragStart.position = go.GetComponent<RectTransform>() ? Camera.main.ScreenToWorldPoint(go.GetComponent<RectTransform>().position) : go.transform.position;
            dragStart.timeDelta = duration;

            RecordingInputModule.TouchData dragFinish = GetTouchDataTemplate(go);
            dragFinish.eventType = RecordingInputModule.TouchData.type.release;
            dragFinish.position = position;
            dragFinish.positional = true;
            dragFinish.timeDelta = duration;

            Perform.RegisterSteps(clickDraggable, dragStart, dragFinish);

            yield return Action(clickDraggable);
            yield return Action(dragStart);
            yield return Action(dragFinish);
            yield return new WaitForSeconds(WAIT_TIME_AFTER_NON_RECORDED_ACTIONS); // Add additional wait since no time delta is associated with non-recorded actions.
        }

        /// <summary>
        /// Find GameElement using query selector and type text into field.
        /// </summary>
        public IEnumerator SendKeys(string querySelector, string text, float duration = -1f)
        {
            yield return WaitFor(() => ElementQuery.Find(querySelector) != null);
            GameObject go = ElementQuery.Find(querySelector);
            yield return SendKeys(go: go, text: text, querySelector: querySelector, duration: duration);
        }

        /// <summary>
        /// Provide GameElement and type text into field.
        /// </summary>
        public IEnumerator SendKeys(GameElement ge, string text, float duration = -1f)
        {
            yield return SendKeys(go: ge.gameObject, ge: ge, text: text, duration: duration);
        }

        /// <summary>
        /// Provide GameObject and type text into field.
        /// </summary>
        public IEnumerator SendKeys(GameObject go, string text, float duration = -1f)
        {
            yield return SendKeys(go: go, text: text, duration: duration);
        }

        /// <summary>
        /// Type text into field.
        /// </summary>
        private IEnumerator SendKeys(GameObject go = null, GameElement ge = null, string text = "", string querySelector = "", float duration = -1f)
        {
            if (go == null && ge == null)
            {
                logger.LogError("Could not find GameObject provided to driver Click() method, or null object was provided as an argument.");
            }

            if (ge == null && go != null)
            {
                ge = go.GetComponent<GameElement>();
                if (ge != null)
                    querySelector = ElementQuery.ConstructQuerySelectorString(go);
            }

            if (go == null && ge != null)
            {
                go = ge.gameObject;
            }

            // If no explicit duration is supplied, assume the typing rate of 8 characters a second.
            float calculatedDuration = duration >= 0f ? duration : (text.Length / 8f);

            RecordingInputModule.TouchData clickInput = GetTouchDataTemplate(go);
            clickInput.querySelector = querySelector;
            clickInput.eventType = RecordingInputModule.TouchData.type.press;
            clickInput.position = go.transform.position;

            RecordingInputModule.TouchData releaseInput = GetTouchDataTemplate(go);
            releaseInput.eventType = RecordingInputModule.TouchData.type.release;

            RecordingInputModule.TouchData typeIntoInput = GetTouchDataTemplate(go);
            typeIntoInput.eventType = RecordingInputModule.TouchData.type.input;
            typeIntoInput.inputText = text;
            typeIntoInput.inputDuration = calculatedDuration;

            Perform.RegisterSteps(clickInput, releaseInput, typeIntoInput);

            yield return Action(clickInput);
            yield return Action(releaseInput);
            yield return Action(typeIntoInput);
            yield return new WaitForSeconds(calculatedDuration); // Duration of text typing.
            yield return new WaitForSeconds(WAIT_TIME_AFTER_NON_RECORDED_ACTIONS); // Add additional wait since no time delta is associated with non-recorded actions.
        }

        /// <summary>
        /// Grabs the requested step's index, and waits until the step is executed.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IEnumerator Action(RecordingInputModule.TouchData action)
        {
            if (action == EMIT_COMPLETE)
            {
                RecordingInputModule.Instance.AddFullTouchData(EMIT_COMPLETE);
                Steps.Add(EMIT_COMPLETE);
            }
            int index = RecordingInputModule.Instance.GetTouchData().FindIndex(x => x == action);

            /*
                The InterpolateDragEvents method adds multiple TouchData events (not part of a recording) to "smooth" a drag between two points.
                These dynamically-generated events were added when drag start was invoked (previous DoAction call). We need to adjust our requested index based on this behavior.
            */
            if (LastAction != default((int, RecordingInputModule.TouchData)) &&
                LastAction.touch.eventType == RecordingInputModule.TouchData.type.drag)
            {
                yield return PerformInterpolatedDragActions(LastAction.index + 1, index - LastAction.index, LastAction.touch.timeDelta);
            }
            LastAction = (index, action);

            // Some events have variable durations to finish execution and return control. That duration should not be considered when determining an event has timed out.
            float durationOfEventToAddToTimeout = 0f;
            if (action.eventType == RecordingInputModule.TouchData.type.input)
            {
                durationOfEventToAddToTimeout = action.inputDuration;
            }
            durationOfEventToAddToTimeout += action.timeDelta;

            float timeout = AutomatedQARuntimeSettings.DynamicWaitTimeout + durationOfEventToAddToTimeout;
            while (timeout > 0 && !RecordingInputModule.Instance.UpdatePlay(index))
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            Assert.IsTrue(!AnyErrors(), GetError($"{action.scene}: {action.objectHierarchy} > {action.objectName}]."));
            if (timeout <= 0)
            {
                logger.LogError($"Timed out waiting to perform next step [{action.eventType} {action.objectHierarchy} > {action.objectName}].");
            }
        }

        public IEnumerator EmitTestComplete()
        {
            yield return Action(EMIT_COMPLETE);
        }

        /// <summary>
        /// Only use this when registering an action BEFORE test execution has begun. If multiple steps are registered individually before any are executed, 
        /// then each added data will be inserted before the previous one, yielding unintended order of execution. Use Driver.Perform.RegisterSteps during test 
        /// execution to register multiple steps in a row.
        /// </summary>
        /// <param name="data"></param>
        public void RegisterStep(RecordingInputModule.TouchData data)
        {
            Perform.RegisterSteps(new RecordingInputModule.TouchData[] { data });
        }

        /// <summary>
        /// This will determine the correct placement of touchdata added either before or while test execution is in progress.
        /// </summary>
        /// <param name="data"></param>
        public void RegisterSteps(params RecordingInputModule.TouchData[] data)
        {
            if (LastAction != default((int, RecordingInputModule.TouchData)))
            {
                // Step is being registered during test execution.
                List<RecordingInputModule.TouchData> newSteps = new List<RecordingInputModule.TouchData>();
                int index = Steps.FindIndex(x => x == LastAction.touch) + 1;
                if (index < Steps.Count)
                {
                    for (int x = 0; x < Steps.Count; x++)
                    {
                        if (x == index)
                        {
                            newSteps.AddRange(data);
                        }
                        newSteps.Add(Steps[x]);
                    }
                    Steps = newSteps;
                }
                else
                {
                    Steps.AddRange(data);
                }
                RecordingInputModule.Instance.InsertTouchData(index, data);
            }
            else {
                // Step is being registered before test execution starts.
                Steps.AddRange(data);
                foreach (RecordingInputModule.TouchData td in data)
                    RecordingInputModule.Instance.AddTouchData(td);
            }
        }

        protected IEnumerator PerformInterpolatedDragActions(int startIndex, int interpolatedEventCount, float duration = 2f)
        {
            float durationBetweenEvents = duration / interpolatedEventCount;
            for (int x = 0; x < interpolatedEventCount; x++)
            {
                float timeout = AutomatedQARuntimeSettings.DynamicWaitTimeout;
                while (timeout > 0 && !RecordingInputModule.Instance.UpdatePlay(startIndex + x))
                {
                    yield return null;
                    timeout -= Time.deltaTime;
                }
                if (timeout <= 0)
                {
                    logger.Log($"Timed out trying to perform an interpolated drag event (events automatically generated between drag start and finish to make for a smooth drag).");
                }
                yield return new WaitForSeconds(durationBetweenEvents);
            }
        }

        protected IEnumerator WaitFor(Func<bool> condition)
        {
            float timer = 0;
            while (!condition.Invoke() && timer <= AutomatedQARuntimeSettings.DynamicWaitTimeout)
            {

                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup < start + 1f)
                {

                    yield return null;

                }
                timer++;

            }
            yield return null;
        }

        private static bool AnyErrors()
        {
            return ReportingManager.ReportData.AllLogs.FindAll(x => x.Type.ToLower() == "error" || x.Type.ToLower() == "exception").Any();
        }

        protected string GetError(string stepInfo)
        {
            if (AnyErrors())
            {
                ReportingManager.Log log = ReportingManager.ReportData.AllLogs.FindAll(x => x.Type.ToLower() == "error" || x.Type.ToLower() == "exception").Last();
                return $"{log.Message}{(log.StackTrace.Length > 0 ? $" [StackTrace: \n{log.StackTrace}]" : string.Empty)}";
            }
            return $"No errors or exceptions are generated by {stepInfo}";
        }

        private static RecordingInputModule.TouchData EMIT_COMPLETE = new RecordingInputModule.TouchData
        {
            pointerId = -1,
            eventType = RecordingInputModule.TouchData.type.none,
            timeDelta = 2f,
            position = new Vector3(0, 0),
            positional = false,
            waitSignal = "",
            emitSignal = "playbackComplete",
            objectName = "",
            objectTag = "",
            objectHierarchy = "",
            objectOffset = new Vector3(0, 0)
        };

        private RecordingInputModule.TouchData GetTouchDataTemplate(GameObject go) 
        {
            return new RecordingInputModule.TouchData
            {
                timeDelta = 0.1f,
                position = new Vector3(0.7883416f, 0.4949187f),
                positional = false,
                scene = SceneManager.GetActiveScene().name,
                waitSignal = "",
                emitSignal = "",
                keyCode = "",
                inputDuration = 0f,
                inputText = "",
                objectName = go.name,
                objectTag = go.tag,
                objectHierarchy = string.Join("/", RecordingInputModule.Instance.GetHierarchy(go)),
                objectOffset = new Vector2(0, 0)
            };
        }
    }
}