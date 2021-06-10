using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.RecordedPlayback;
using Unity.AutomatedQA;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Automated QA/Recording Input Module")]
    /// <summary>
    /// A BaseInputModule designed for mouse / keyboard / controller input.
    /// </summary>
    /// <remarks>
    /// Input module for working with, mouse, keyboard, or controller.
    /// </remarks>
    public class RecordingInputModule : PointerInputModule
    {
        public static RecordingInputModule Instance { get; set; }
        private static readonly float screenshotDelay = 0.25f; // TODO: use a better value
        private static readonly float dragRateLimit = 0.05f;
        private static readonly string playbackCompleteSignal = "playbackComplete";
        private static readonly string segmentCompleteSignal = "segmentComplete";

        private static int screenshotCounter = 0;
        private static DateTime start = DateTime.Now;

        private float m_PrevActionTime;
        private Vector2 m_LastMoveVector;
        private int m_ConsecutiveMoveCount = 0;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        private PointerEventData m_InputPointerEvent;

        private InputModuleRecordingData _recordingData = new InputModuleRecordingData { touchData = new List<TouchData>() };
        private List<TouchData> touchData = new List<TouchData>();
        private TouchData activeDrag;
        private string currentEntryScene;

        public RecordingMode RecordingMode 
        {
            get
            {
                return _recordingMode;
            }
        }
        private RecordingMode _recordingMode;

        private StringEvent callback = new StringEvent();

        private Scene? dontDestroyOnLoadScene;

        private HashSet<string> pendingSignals = new HashSet<string>();

        private HashSet<string> hierarchyWarnings = new HashSet<string>();

        protected RecordingInputModule()
        {
        }

        protected override void OnEnable()
        {
            Instance = this;
            InitConfigData();
            InitRecordingData();
            InitScenes();
            SendAnalytics();

            base.OnEnable();
        }

        private void InitConfigData()
        {
            _recordingMode = RecordedPlaybackPersistentData.GetRecordingMode();
        }

        private void InitRecordingData()
        {
            lastEventTime = Time.time;
            if (IsPlaybackActive())
            {
                _recordingData = RecordedPlaybackPersistentData.GetRecordingData<InputModuleRecordingData>();
                RecordedPlaybackPersistentData.RecordedResolution = _recordingData.recordedResolution;
                RecordedPlaybackPersistentData.RecordedAspectRatio = _recordingData.recordedAspectRatio;
                touchData = _recordingData.GetAllTouchData();
                ReportingManager.InitializeDataForNewTest();
            }
            else if (_recordingMode == RecordingMode.Record)
            {
                _recordingData.entryScene = SceneManager.GetActiveScene().name;
            }
        }

        private void InitScenes()
        {
            if (dontDestroyOnLoadScene == null)
            {
                GameObject temp = null;
                try
                {
                    temp = new GameObject();
                    DontDestroyOnLoad(temp);
                    dontDestroyOnLoadScene = temp.scene;
                }
                finally
                {
                    if (temp != null)
                        DestroyImmediate(temp);
                }
            }
        }

        private void SendAnalytics()
        {
            RecordedPlaybackAnalytics.SendRecordedPlaybackEnv();
            if (_recordingMode == RecordingMode.Playback)
            {
                RecordedPlaybackAnalytics.SendRecordingExecution(RecordedPlaybackPersistentData.kRecordedPlaybackFilename,
                    SceneManager.GetActiveScene().name);
            }
        }

        public void SetConfigMode(RecordingMode mode, bool persist = false)
        {
            _recordingMode = mode;
            if (persist)
            {
                RecordedPlaybackPersistentData.SetRecordingMode(mode);
            }
        }

        private int _current_index = 0;

        private playbackExecutionState currentState => pendingSignals.Count > 0 ? playbackExecutionState.wait : playbackExecutionState.play;
        private float waitStartTime = 0f;
        private float timeAdjustment = 0f;
        private float lastEventTime = 0f;

        protected virtual void Update()
        {
            if (!IsPlaybackActive())
            {
                return;
            }
            ReportingManager.CreateMonitoringService();

            switch (currentState)
            {
                case playbackExecutionState.play:
                    UpdatePlay();
                    break;
                case playbackExecutionState.wait:
                    // we don't need to do anything here. we can update our timers when we receive the signal we're waiting on
                    break;
            }
        }

        public void Pause(string signal)
        {
            pendingSignals.Add(signal);
            waitStartTime = GetElapsedTime();
        }

        private void UpdatePlay()
        {
            if (_current_index >= touchData.Count || !(touchData[_current_index].timeDelta <= GetElapsedTime()))
            {
                return;
            }

            var td = touchData[_current_index];
            lastEventTime += td.timeDelta;
            if (td.eventType != TouchData.type.none)
            {
                try
                {
                    DoAction(_current_index);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!string.IsNullOrEmpty(td.waitSignal))
            {
                pendingSignals.Add(td.waitSignal);
                waitStartTime = GetElapsedTime();
            }

            if (_recordingMode == RecordingMode.Extend && _current_index == touchData.Count - 1 && td.emitSignal == playbackCompleteSignal)
            {
                touchData = new List<TouchData>();
                _recordingMode = RecordingMode.Record;
                Debug.Log("Playback complete, begin recording new segment");
            }
            else if (!string.IsNullOrEmpty(td.emitSignal))
            {
                callback.Invoke(td.emitSignal);
            }

            ++_current_index;
        }

        private float GetElapsedTime()
        {
            return Time.time - lastEventTime - timeAdjustment;
        }

        void DoAction(int index)
        {
            if (!IsPlaybackActive())
            {
                return;
            }

            var td = touchData[index];
            if (_recordingMode == RecordingMode.Playback)
            {
                ReportingManager.StepData step = new ReportingManager.StepData();
                step.ActionType = td.eventType.ToString();
                step.Name = $"{(string.IsNullOrEmpty(td.objectName) ? $"{{Position x{td.position.x} y{td.position.y}}}" : td.objectName)}";
                step.Hierarchy = string.IsNullOrEmpty(td.objectHierarchy) ? "{N/A}" : td.objectHierarchy;
                ReportingManager.AddStep(step);
            }
            if (td.eventType != TouchData.type.drag)
            {
                CaptureScreenshots();
            }

            Vector2 pos = GetTouchPosition(td);
            if (_recordingMode == RecordingMode.Playback)
            {
                ReportingManager.UpdateCurrentStep(pos);
            }
            var touch = new Touch();
            touch.fingerId = td.pointerId;
            touch.position = pos;
            touch.rawPosition = pos;
            touch.tapCount = 1;
            touch.pressure = 1.0f; // standard touch is 1
            touch.maximumPossiblePressure = 1.0f;
            touch.type = TouchType.Direct;
            //touch.altitudeAngle = 0;
            //touch.azimuthAngle = 0;
            //touch.radius = 0;
            //touch.radiusVariance = 0;

            if (td.eventType == TouchData.type.press)
            {
                touch.phase = TouchPhase.Began;
                falseTouches.Add(touch);
                VisualFxManager.Instance.TriggerPulseOnTarget(pos, true);
            }
            else if (td.eventType == TouchData.type.drag)
            {

                touch.deltaTime = td.timeDelta;
                touch.phase = TouchPhase.Moved;
                falseTouches.Add(touch);

                if (td.HasObject() && index < touchData.Count - 1)
                {
                    var drop = touchData[index + 1];
                    if (drop.eventType == TouchData.type.release)
                    {
                        Vector2 dropPos = drop.GetScreenPosition();
                        if (drop.HasObject())
                        {
                            dropPos = GetTouchPosition(drop);
                            if (_recordingMode == RecordingMode.Playback)
                            {
                                ReportingManager.UpdateCurrentStep(dropPos);
                            }
                        }

                        touchData = InterpolateDragEvents(index, pos, dropPos);
                    }
                }
                VisualFxManager.Instance.TriggerDragFeedback(false, pos);
            }
            else if (td.eventType == TouchData.type.release)
            {
                touch.phase = TouchPhase.Ended;
                falseTouches.Add(touch);
                VisualFxManager.Instance.TriggerPulseOnTarget(pos, false);
                VisualFxManager.Instance.TriggerDragFeedback(true, pos);
            }
        }

        /// <summary>
        /// Looks for object using the tag and name.
        /// Since grabbing the objPool is not a light operation, the pool can be instantiated outside this method to allow 
        /// only a single call to grab the object pool when it is needed by multiple operations.
        /// </summary>
        /// <param name="td"></param>
        /// <param name="objPool"></param>
        /// <returns></returns>
        private GameObject FindObject(TouchData td, List<GameObject> objPool)
        {
            if (td.objectTag != "Untagged" && !string.IsNullOrEmpty(td.objectTag))
            {
                var gameObjects = GameObject.FindGameObjectsWithTag(td.objectTag);
                foreach (var gameObject in gameObjects)
                {
                    if (gameObject.name == td.objectName)
                    {
                        return gameObject;
                    }
                }
            }

            var maxOutliers = int.MaxValue;
            var foundObjects = 0;
            GameObject result = null;
            foreach (GameObject gameObject in objPool)
            {
                if (gameObject.name == td.objectName)
                {
                    foundObjects++;
                    var h1 = new HashSet<string>(GetHierarchy(gameObject));
                    var h2 = new HashSet<string>(td.objectHierarchy.Split('/'));
                    h1.SymmetricExceptWith(h2);

                    var outliers = h1.Count;
                    if (outliers < maxOutliers || result == null)
                    {
                        result = gameObject;
                        maxOutliers = outliers;
                    }
                }
            }

            if (maxOutliers != 0 && foundObjects > 0 && !hierarchyWarnings.Contains(td.objectHierarchy))
            {
                Debug.LogWarning($"Object hierarchy {td.objectHierarchy} has been changed, please update the recording file with the new path");
                hierarchyWarnings.Add(td.objectHierarchy);
            }

            return result;
        }

        public List<GameObject> GetActiveGameObjects()
        {
            List<GameObject> results = new List<GameObject>();
            var scenes = GetOpenScenes();
            foreach (var scene in scenes)
            {
                results.AddRange(GetChildren(scene.GetRootGameObjects().ToList()));
            }
            return results.FindAll(x => x.activeInHierarchy && x.activeSelf);
        }

        public static List<GameObject> GetChildren(List<GameObject> objs)
        {
            List<GameObject> results = new List<GameObject>();
            foreach (GameObject obj in objs)
            {
                results.Add(obj);
                foreach (Transform trans in obj.transform)
                {
                    results.AddRange(GetChildren(new List<GameObject>() { trans.gameObject }));
                }
            }
            return results;
        }

        private List<Scene> GetOpenScenes()
        {
            var scenes = new List<Scene>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene thisScene = SceneManager.GetSceneAt(s);
                if (thisScene.isLoaded)
                {
                    scenes.Add(thisScene);
                }
            }

            if (dontDestroyOnLoadScene.HasValue)
            {
                scenes.Add(dontDestroyOnLoadScene.Value);
            }

            return scenes;
        }

        private List<string> GetHierarchy(GameObject gameObject)
        {
            var hierarchy = new List<string>();
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                hierarchy.Add(parent.name);
                parent = parent.parent;
            }

            hierarchy.Reverse();
            return hierarchy;
        }

        private RaycastResult? FindRayForObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            List<RaycastResult> raycastResults = new List<RaycastResult>();

            for (float x = 0; x < 1; x += .01f)
            {
                for (float y = 0; y < 1; y += .01f)
                {
                    var testPos = new Vector2(x * Screen.width, y * Screen.height);
                    var result = FindObjectWithRaycast(gameObject, testPos, raycastResults);
                    if (result != null)
                    {
                        return result;
                    }
                    raycastResults.Clear();
                }
            }

            return null;
        }

        private RaycastResult? FindObjectWithRaycast(GameObject gameObject, Vector2 pos, List<RaycastResult> raycastResults = null)
        {
            if (raycastResults == null)
            {
                raycastResults = new List<RaycastResult>();
            }

            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, raycastResults);
            if (raycastResults.Count > 0)
            {
                foreach (var result in raycastResults)
                {
                    var currentObject = result.gameObject;
                    while (currentObject != null)
                    {
                        if (currentObject == gameObject)
                        {
                            return result;
                        }
                        var parent = currentObject.transform.parent;
                        currentObject = parent != null ? parent.gameObject : null;
                    }
                }
            }

            return null;
        }

        private Vector2 GetTouchPosition(TouchData td)
        {
            if (td.positional || !td.HasObject())
            {
                return td.GetScreenPosition();
            }

            List<GameObject> objPool = GetActiveGameObjects();
            var target = FindObject(td, objPool);

            // Check if the GameObject is in the visible camera frustum.
            if (target != null)
            {
                bool hasRectTransform = target.TryGetComponent(out RectTransform rectTransform);
                if (hasRectTransform)
                {
                    // Determine if an event camera is associated with the parent canvas element. Using the correct camera, or null value, is required for generating accurate click coordinates.
                    Camera eventCamera = GetEventCameraForCanvasChild(target);
                     
                    // The local position within the rect transform where our click should be performed.
                    Vector3 localPos = rectTransform.rect.min + td.objectOffset * rectTransform.rect.size + rectTransform.rect.size / 2f;
                    Vector3 posFromOffset = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.TransformPoint(localPos));

                    // Is the target object rendered within the camera frustum, and thus visible on screen.
                    ValidateObjectIsVisibleOnScreen(posFromOffset, td.objectName);

                    // Determine if the click coordinates in the target GameObject would be intercepted by another GameObject that is rendered on top of it.
                    ValidateNoOverlappingObjectWillInterceptClick(target, posFromOffset, td.objectName);
                    return posFromOffset;
                }
            }

            var raycastResult = FindRayForObject(target);
            if (raycastResult == null)
            {
                // Throw error if object is not found to fail unit tests.
                Debug.LogError($"Cannot play recorded action: object {td.objectName} does not exist or is not viewable on screen. Ensure the object exists and reposition the object inside the screen space.");
                // TODO skip this touch event?
                return td.GetScreenPosition();
            }
            return raycastResult.Value.screenPosition;
        }

        private Camera GetEventCameraForCanvasChild(GameObject gameObject)
        {
            Camera eventCamera = null;
            GameObject canvasGo = gameObject;
            while (eventCamera == null && canvasGo != null)
            {
                canvasGo.TryGetComponent(out Canvas canvas);
                if (canvas != null)
                {
                    canvasGo = canvas.gameObject;
                    if (canvas.TryGetComponent(out GraphicRaycaster gfxRaycaster))
                    {
                        eventCamera = gfxRaycaster.eventCamera;
                    }
                }
                canvasGo = canvasGo.transform.parent != null ? canvasGo.transform.parent.gameObject : null;
            }
            return eventCamera;
        }

        private void ValidateObjectIsVisibleOnScreen(Vector3 positionFromoffset, string nameOfObject)
        {
            float distRectX = Vector3.Distance(new Vector3(Screen.width / 2, 0f, 0f), new Vector3(positionFromoffset.x, 0f, 0f));
            float distRectY = Vector3.Distance(new Vector3(0f, Screen.height / 2, 0f), new Vector3(0f, positionFromoffset.y, 0f));

            // Determine if the click coordinates in the target GameObject are off the screen.
            if (distRectX > Screen.width / 2 || distRectY > Screen.height / 2)
            {
                Debug.LogError($"Click position recorded relative to spot within the target GameObject \"{nameOfObject}\" is positioned outside of the camera frustum (is not visible to the camera). " +
                   $"Since this is a GameObject in the UI layer, this may mean that the object has not scaled or positioned properly in the current aspect ratio ({Screen.width}w X {Screen.height}h) and current resolution ({Screen.currentResolution.width} X {Screen.currentResolution.height}) compared to the recorded aspect ratio ({RecordedPlaybackPersistentData.RecordedAspectRatio.x}h X {RecordedPlaybackPersistentData.RecordedAspectRatio.y}w) and recorded resolution ({RecordedPlaybackPersistentData.RecordedResolution.x} X {RecordedPlaybackPersistentData.RecordedResolution.y}).");
            }
        }

        private void ValidateNoOverlappingObjectWillInterceptClick(GameObject target, Vector3 posFromOffset, string nameOfObject)
        {
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = posFromOffset }, raycastResults);
            if (raycastResults.Count > 0)
            {
                if (raycastResults.First().gameObject != target)
                {

                    // First raycast hit may not be the GameObject that we are targetting. It may be a child element, like an icon or text inside of a button.
                    bool isChildOfTargetGameObject = false;
                    Transform tran = raycastResults.First().gameObject.transform.parent;
                    while (tran != null)
                    {
                        if (tran.gameObject == target)
                        {
                            isChildOfTargetGameObject = true;
                            break;
                        }
                        tran = tran.parent == null ? null : tran.parent.transform;
                    }
                    // Handle exceptions where !isChildOfTargetGameObject is not an error state.
                    bool exceptionFound = false;
                    /*
                     * If the target is a slider, it is possible for a touch event to not fire on the target slider, while still successfully performing a drag. 
                     * This only happens when rapidly dragging a slider back and forth.
                     * If the slider is in the raycastResults list, but not at the top of stack, then it is legitimately blocked by another GameObject.
                    */
                    exceptionFound = !raycastResults.FindAll(rcr => rcr.gameObject == target).Any() && target.GetComponent<Slider>();
                    if (!isChildOfTargetGameObject && !exceptionFound)
                    {
                        Debug.LogError($"Click position recorded relative to spot within the target GameObject \"{nameOfObject}\" would be caught by the wrong GameObject, which is overlapping the target GameObject at the click coordinates. " +
                        $"Since this is a GameObject in the UI layer, this may mean that the object has not scaled or positioned properly in the current aspect ratio ({Screen.width}w X {Screen.height}h) and current resolution ({Screen.currentResolution.width} X {Screen.currentResolution.height}) compared to the recorded aspect ratio ({RecordedPlaybackPersistentData.RecordedAspectRatio.x}h X {RecordedPlaybackPersistentData.RecordedAspectRatio.y}w) and recorded resolution ({RecordedPlaybackPersistentData.RecordedResolution.x} X {RecordedPlaybackPersistentData.RecordedResolution.y}).");
                    }
                }
            }
        }

        [SerializeField] private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField] private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField] private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField] private string m_CancelButton = "Cancel";

        [SerializeField] private float m_InputActionsPerSecond = 10;

        [SerializeField] private float m_RepeatDelay = 0.5f;

        [SerializeField] private bool m_ForceModuleActive;

        /// <summary>
        /// Force this module to be active.
        /// </summary>
        /// <remarks>
        /// If there is no module active with higher priority (ordered in the inspector) this module will be forced active even if valid enabling conditions are not met.
        /// </remarks>
        public bool forceModuleActive
        {
            get { return m_ForceModuleActive; }
            set { m_ForceModuleActive = value; }
        }

        /// <summary>
        /// Number of keyboard / controller inputs allowed per second.
        /// </summary>
        public float inputActionsPerSecond
        {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        /// <summary>
        /// Delay in seconds before the input actions per second repeat rate takes effect.
        /// </summary>
        /// <remarks>
        /// If the same direction is sustained, the inputActionsPerSecond property can be used to control the rate at which events are fired. However, it can be desirable that the first repetition is delayed, so the user doesn't get repeated actions by accident.
        /// </remarks>
        public float repeatDelay
        {
            get { return m_RepeatDelay; }
            set { m_RepeatDelay = value; }
        }

        /// <summary>
        /// Name of the horizontal axis for movement (if axis events are used).
        /// </summary>
        public string horizontalAxis
        {
            get { return m_HorizontalAxis; }
            set { m_HorizontalAxis = value; }
        }

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        public string verticalAxis
        {
            get { return m_VerticalAxis; }
            set { m_VerticalAxis = value; }
        }

        /// <summary>
        /// Maximum number of input events handled per second.
        /// </summary>
        public string submitButton
        {
            get { return m_SubmitButton; }
            set { m_SubmitButton = value; }
        }

        /// <summary>
        /// Input manager name for the 'cancel' button.
        /// </summary>
        public string cancelButton
        {
            get { return m_CancelButton; }
            set { m_CancelButton = value; }
        }

        private bool ShouldIgnoreEventsOnNoFocus()
        {
            if (IsPlaybackActive())
            {
                return false;
            }

            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isRemoteConnected)
                        return false;
#endif
                    return true;
                default:
                    return false;
            }
        }

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                if (m_InputPointerEvent != null && m_InputPointerEvent.pointerDrag != null && m_InputPointerEvent.dragging)
                {
                    ReleaseMouse(m_InputPointerEvent, m_InputPointerEvent.pointerCurrentRaycast.gameObject);
                }

                m_InputPointerEvent = null;

                return;
            }

            m_LastMousePosition = m_MousePosition;
            m_MousePosition = input.mousePosition;
        }

        private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                AddTouchData(pointerEvent, TouchData.type.release, pointerEvent.pointerPress);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                var dropObject = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                AddTouchData(pointerEvent, TouchData.type.release, dropObject);
            }
            else
            {
                AddTouchData(pointerEvent, TouchData.type.release);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }

            m_InputPointerEvent = pointerEvent;
        }

        public override bool IsModuleSupported()
        {
            var input1 = input;
            return m_ForceModuleActive || input1.mousePresent || input1.touchSupported;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;

            var shouldActivate = m_ForceModuleActive;
            shouldActivate |= input.GetButtonDown(m_SubmitButton);
            shouldActivate |= input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= input.GetMouseButtonDown(0);

            if (input.touchCount > 0)
                shouldActivate = true;

            return shouldActivate;
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void ActivateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            base.ActivateModule();

            var mousePosition = input.mousePosition;
            m_MousePosition = mousePosition;
            m_LastMousePosition = mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        public override void Process()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                return;
            }

            bool usedEvent = SendUpdateEventToSelectedObject();

            // case 1004066 - touch / mouse events should be processed before navigation events in case
            // they change the current selected GameObject and the submit button is a touch / mouse button.

            // touch needs to take precedence because of the mouse emulation layer
            // TODO: use synthetic touch data here

            if (IsPlaybackActive())
            {
                ProcessSyntheticTouchEvents();
            }
            else
            {
                if (!ProcessTouchEvents() && input.mousePresent)
                {
                    ProcessMouseEvent();
                }
            }

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                {
                    usedEvent |= SendMoveEventToSelectedObject();
                }

                if (!usedEvent)
                {
                    SendSubmitEventToSelectedObject();
                }
            }
        }

        private bool ProcessTouchEvents()
        {
            for (int i = 0; i < input.touchCount; ++i)
            {
                Touch touch = input.GetTouch(i);

                if (touch.type == TouchType.Indirect)
                    continue;

                var pointer = GetTouchPointerEventData(touch, out var pressed, out var released);

                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                    RemovePointerData(pointer);
            }

            return input.touchCount > 0;
        }

        private List<Touch> falseTouches = new List<Touch>();

        private void ProcessSyntheticTouchEvents()
        {
            if (!IsPlaybackActive())
            {
                return;
            }

            foreach (var touch in falseTouches)
            {
                if (touch.type == TouchType.Indirect)
                {
                    continue;
                }

                var pointer = GetTouchPointerEventData(touch, out var pressed, out var released);

                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                {
                    RemovePointerData(pointer);
                }
            }

            falseTouches.Clear();
        }

        /// <summary>
        /// This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.
        /// </summary>
        /// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
        /// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
        /// <param name="released">This is true only for the last frame of a touch event.</param>
        /// <remarks>
        /// This method can be overridden in derived classes to change how touch press events are handled.
        /// </remarks>
        private void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didn't find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

                m_InputPointerEvent = pointerEvent;

                var activeObject = pointerEvent.pointerPress != null ? pointerEvent.pointerPress : pointerEvent.pointerDrag;
                AddTouchData(pointerEvent, TouchData.type.press, activeObject);
            }

            // PointerUp notification
            if (!released)
            {
                return;
            }

            // Debug.Log("Executing press up on: " + pointer.pointerPress);
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                AddTouchData(pointerEvent, TouchData.type.release, pointerEvent.pointerPress);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                var dropObject = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                AddTouchData(pointerEvent, TouchData.type.release, dropObject);
            }
            else
            {
                AddTouchData(pointerEvent, TouchData.type.release);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // send exit events as we need to simulate this on touch up on touch device
            ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
            pointerEvent.pointerEnter = null;

            m_InputPointerEvent = pointerEvent;
        }

        /// <summary>
        /// Calculate and send a submit event to the current selected object.
        /// </summary>
        private void SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return;

            var data = GetBaseEventData();
            if (input.GetButtonDown(m_SubmitButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

            if (input.GetButtonDown(m_CancelButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }

            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }

            return move;
        }

        /// <summary>
        /// Calculate and send a move event to the current selected object.
        /// </summary>
        /// <returns>If the move event was used by the selected object.</returns>
        private bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);

            // If direction didn't change at least 90 degrees, wait for delay before allowing consecutive event.
            if (similarDir && m_ConsecutiveMoveCount == 1)
            {
                if (time <= m_PrevActionTime + m_RepeatDelay)
                    return false;
            }
            // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
            else
            {
                if (time <= m_PrevActionTime + 1f / m_InputActionsPerSecond)
                    return false;
            }

            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

            if (axisEventData.moveDir != MoveDirection.None)
            {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return axisEventData.used;
        }

        /// <summary>
        /// Process all mouse events.
        /// </summary>
        private void ProcessMouseEvent(int id = 0)
        {
            var mouseData = GetMousePointerEventData(id);
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Calculate and process any mouse button state changes.
        /// </summary>
        private void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                PressMouse(pointerEvent, currentOverGo);
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ReleaseMouse(pointerEvent, currentOverGo);
            }
        }

        private void PressMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didn't find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            m_InputPointerEvent = pointerEvent;

            var activeObject = pointerEvent.pointerPress != null ? pointerEvent.pointerPress : pointerEvent.pointerDrag;
            AddTouchData(pointerEvent, TouchData.type.press, activeObject);
        }

        private static float move_x;
        private static float move_y;

        protected override void ProcessMove(PointerEventData pointerEvent)
        {
            if (move_x == pointerEvent.delta.x && move_y == pointerEvent.delta.y)
            {
                base.ProcessMove(pointerEvent);
                return;
            }

            move_x = pointerEvent.delta.x;
            move_y = pointerEvent.delta.y;

            //AddTouchData(td);

            base.ProcessMove(pointerEvent);
        }

        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            base.ProcessDrag(pointerEvent);

            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
            {
                return;
            }

            if (pointerEvent.dragging && activeDrag == null)
            {
                activeDrag = AddTouchData(pointerEvent, TouchData.type.drag, pointerEvent.pointerDrag);
            }
        }

        private TouchData AddTouchData(PointerEventData pointerEvent, TouchData.type type, GameObject activeObject = null)
        {
            TouchData td = null;
            if (_recordingMode == RecordingMode.Record)
            {
                td = new TouchData
                {
                    pointerId = pointerEvent.pointerId,
                    eventType = type,
                    timeDelta = Time.time - lastEventTime,
                    position = new Vector2(pointerEvent.position.x / Screen.width,
                        pointerEvent.position.y / Screen.height),
                    positional = activeObject == null,
                    scene = SceneManager.GetActiveScene().name
                };

                if (activeObject != null)
                {
#if UNITY_EDITOR
                    UnityEditor.Selection.activeGameObject = activeObject;
#endif
                    td.objectName = activeObject.name;
                    td.objectTag = activeObject.tag;
                    td.objectHierarchy = string.Join("/", GetHierarchy(activeObject));

                    RectTransform rectTransform;
                    if (activeObject.TryGetComponent(out rectTransform))
                    {
                        var rect = rectTransform.rect;
                        Vector2 localPos;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pointerEvent.position, pointerEvent.pressEventCamera, out localPos);

                        // offset is calculated in the range (-0.5, -0.5) to (0.5, 0.5) relative to the object's center
                        td.objectOffset = (localPos - rect.min - rect.size / 2f) / rect.size;
                    }
                }

                if (td.eventType == TouchData.type.release)
                {
                    activeDrag = null;
                }

                if (td.eventType != TouchData.type.drag)
                {
                    CaptureScreenshots();
                }
                lastEventTime += td.timeDelta;
                touchData.Add(td);
            }

            return td;
        }

        private List<TouchData> InterpolateDragEvents(int index, Vector2 startPos, Vector2 endPos)
        {
            var newTouchData = new List<TouchData>();
            for (int i = 0; i < touchData.Count; i++)
            {
                var td = touchData[i];
                newTouchData.Add(td);
                if (i == index && td.eventType == TouchData.type.drag)
                {
                    newTouchData.AddRange(GetInterpolatedDragEvents(i, startPos, endPos));
                    var drop = touchData[++i];
                    if (drop.eventType == TouchData.type.release)
                    {
                        drop.timeDelta = 0f;
                    }
                    newTouchData.Add(drop);
                }
            }

            return newTouchData;
        }

        private List<TouchData> GetInterpolatedDragEvents(int index, Vector2 startPos, Vector2 endPos)
        {
            var result = new List<TouchData>();
            var td = touchData[index];
            if (index < touchData.Count - 1 && td.eventType == TouchData.type.drag && touchData[index + 1].eventType == TouchData.type.release)
            {
                var drop = touchData[index + 1];
                var deltaPos = endPos - startPos;
                for (float i = dragRateLimit; i < drop.timeDelta + dragRateLimit; i += dragRateLimit)
                {
                    var delta = i > drop.timeDelta ? dragRateLimit - (i - drop.timeDelta) : dragRateLimit;
                    var totalDelta = Math.Min(i / drop.timeDelta, 1);
                    var screenPos = totalDelta * deltaPos + startPos;
                    var interpolatedTouch = new TouchData
                    {
                        pointerId = td.pointerId,
                        eventType = TouchData.type.drag,
                        timeDelta = delta * drop.timeDelta,
                        position = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height),
                        positional = true,
                        scene = SceneManager.GetActiveScene().name
                    };
                    result.Add(interpolatedTouch);
                }
            }

            return result;
        }

        public void SaveRecordingSegment()
        {
            var endEvent = new TouchData
            {
                eventType = TouchData.type.none,
                timeDelta = GetElapsedTime(),
                emitSignal = segmentCompleteSignal,
                scene = SceneManager.GetActiveScene().name
            };
            touchData.Add(endEvent);

            var epoch = ((DateTimeOffset)start).ToUnixTimeSeconds();
            var filename = $"recording_segment_{_recordingData.recordings.Count}_{epoch}.json";
            var filepath = Path.Combine(AutomatedQARuntimeSettings.PersistentDataPath, filename);
            Debug.Log($"Writing {filepath}");
            var recordedSegment = new InputModuleRecordingData(touchData);
            recordedSegment.entryScene = string.IsNullOrEmpty(currentEntryScene) ? _recordingData.entryScene : currentEntryScene;
            recordedSegment.SaveToFile(filepath);

            touchData = new List<TouchData>();
            _recordingData.recordings.Add(new Recording { filename = filename });
            _recordingData.recordingType = InputModuleRecordingData.type.composite;
            currentEntryScene = SceneManager.GetActiveScene().name;
            lastEventTime += endEvent.timeDelta;
        }

        public void CaptureScreenshots()
        {
            if (AutomatedQARuntimeSettings.EnableScreenshots)
            {
                StartCoroutine(CaptureScreenshot(0f));
                StartCoroutine(CaptureScreenshot(screenshotDelay));
            }
        }

        private IEnumerator CaptureScreenshot(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            string folder = Path.Combine(AutomatedQARuntimeSettings.PackageAssetsFolderName, $"{_recordingMode} screenshots {start.ToString("yyyy-MM-dd-THH-mm-ss")}");
            string path = Path.Combine(Application.persistentDataPath, folder);
            string filename = $"{_recordingMode} screenshot {screenshotCounter++}.png";

            // ScreenCapture.CaptureScreenshot tries to handle pushing into persistent data path for us
            // but this forces us to handle for the other cases... :(
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            string file = Path.Combine(folder, filename);
#else
            string file = Path.Combine(path, filename);

#endif

            Directory.CreateDirectory(path);
           if (_recordingMode == RecordingMode.Playback)
            {
                ReportingManager.AddScreenshot(file);
            }
            yield return CaptureScreenshot(file);
        }

        // On Mac manually capture the screenshot to avoid "ignoring depth surface" warnings that come from ScreenCapture.CaptureScreenshot
        IEnumerator CaptureScreenshot(string path)
        {
            yield return new WaitForEndOfFrame();
            
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenImage.Apply();
            byte[] imageBytes = screenImage.EncodeToPNG();

            File.WriteAllBytes(path, imageBytes);
#else
            ScreenCapture.CaptureScreenshot(path);
#endif
        }

        /// <summary>
        /// For Windows Store & Android "end state" callback logic.
        /// </summary>
        /// <param name="pause"></param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                if (_recordingMode != RecordingMode.Record)
                {
                    return;
                }
                RecordedPlaybackPersistentData.SetRecordingData(_recordingData);
            }
        }

        private void OnApplicationQuit()
        {
            if (_recordingMode == RecordingMode.Playback)
            {
                RecordedPlaybackPersistentData.CleanRecordingData(true);
            }
            else if (_recordingMode != RecordingMode.Record)
            {
                return;
            }

            if (_recordingData.recordingType == InputModuleRecordingData.type.composite && touchData.Count > 0)
            {
                SaveRecordingSegment();
            }

            _recordingData.touchData = touchData;
            _recordingData.AddPlaybackCompleteEvent(GetElapsedTime());
            _recordingData.recordedAspectRatio = new Vector2(Screen.width, Screen.height);
            _recordingData.recordedResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            RecordedPlaybackPersistentData.SetRecordingData(_recordingData);
        }

        [Serializable]
        public class TouchData
        {
            public int pointerId;
            public type eventType;
            public float timeDelta;
            public Vector2 position;
            public bool positional;
            public string scene;

            public string waitSignal;
            public string emitSignal;

            public string objectName;
            public string objectTag;
            public string objectHierarchy;
            public Vector2 objectOffset;

            public enum type
            {
                none,
                press,
                release,
                move,
                drag
            }

            public bool HasObject()
            {
                return !(string.IsNullOrEmpty(objectName) && string.IsNullOrEmpty(objectTag));
            }

            public Vector2 GetScreenPosition()
            {
                return new Vector2(position.x * Screen.width, position.y * Screen.height);
            }
        }

        [Serializable]
        public class Recording
        {
            public string filename;
        }

        [Serializable]
        public class InputModuleRecordingData : BaseRecordingData
        {
            public string entryScene = string.Empty;
            public type recordingType;
            public Vector2 recordedAspectRatio;
            public Vector2 recordedResolution;
            public List<Recording> recordings;
            public List<TouchData> touchData;

            public InputModuleRecordingData(type recordingType = type.single)
            {
                this.recordingType = recordingType;
                touchData = new List<TouchData>();
                recordings = new List<Recording>();
            }

            public InputModuleRecordingData(List<TouchData> touchData)
            {
                this.touchData = touchData;
                this.recordingType = type.single;
                recordings = new List<Recording>();
            }

            public enum type
            {
                single,
                composite
            }

            public List<TouchData> GetAllTouchData(string segmentDir = null, int depth = 0)
            {
                if (depth >= 10)
                {
                    Debug.LogError($"Recursive limit exceeded while reading file segments,");
                    return new List<TouchData>();
                }

                var baseDir = segmentDir ?? AutomatedQARuntimeSettings.PersistentDataPath;
                List<TouchData> combinedTouchData = new List<TouchData>();
                foreach (var recording in recordings)
                {
                    Debug.Log($"Loading segment {recording.filename}");
                    var segment = FromFile(Path.Combine(baseDir, recording.filename));

                    combinedTouchData.AddRange(segment.GetAllTouchData(segmentDir, depth + 1));

                    // allows a composite recordings to be used as a segment
                    if (combinedTouchData.Count > 0 && combinedTouchData.Last().emitSignal == playbackCompleteSignal)
                    {
                        combinedTouchData.Last().emitSignal = segmentCompleteSignal;
                    }
                }

                foreach (var td in touchData)
                {
                    combinedTouchData.Add(td);
                }

                return combinedTouchData;
            }

            public void AddRecording(string recordingFileName)
            {
                Debug.Log("recordingFileName " + recordingFileName);
                var newRecording = new Recording { filename = recordingFileName };
                Debug.Log("newRecording " + newRecording);
                recordings.Add(newRecording);
            }

            public void AddPlaybackCompleteEvent(float timeDelta = 0f)
            {
                var endEvent = new TouchData
                {
                    eventType = TouchData.type.none,
                    timeDelta = timeDelta,
                    emitSignal = playbackCompleteSignal,
                    scene = SceneManager.GetActiveScene().name
                };

                touchData.Add(endEvent);
            }

            public static InputModuleRecordingData FromFile(string filepath)
            {
                var text = File.ReadAllText(filepath);
                return JsonUtility.FromJson<InputModuleRecordingData>(text);
            }
        }



        private enum playbackExecutionState
        {
            play,
            wait
        }

        public void SendSignal(string name)
        {
            pendingSignals.Remove(name);

            if (currentState == playbackExecutionState.play)
            {
                timeAdjustment += GetElapsedTime() - waitStartTime;
            }
        }


        [Serializable]
        public class StringEvent : UnityEvent<string>
        {
        }

        public bool IsPlaybackCompleted()
        {
            return _recordingMode == RecordingMode.Playback && _current_index >= touchData.Count;
        }

        private bool IsPlaybackActive()
        {
            return _recordingMode == RecordingMode.Playback || _recordingMode == RecordingMode.Extend;
        }
    }
}
