using System;
using System.Collections.Generic;
using Unity.RecordedPlayback;

namespace UnityEngine.EventSystems
{
    /**
     * This class is a drop in replacement for the Input class with support for recording and playback in the
     * Automated QA package. Currently supports touch and mouse events only with other input types coming soon.
     */
    public class RecordableInput
    {
        internal static bool playbackActive = false;

        // fake mouse data
        private static Dictionary<int, FakePress> fakeMousePresses = new Dictionary<int, FakePress>();
        private static HashSet<int> fakeMouseDownEvents = new HashSet<int>();
        private static HashSet<int> fakeMouseUpEvents = new HashSet<int>();
        private static Vector2 fakeMousePos;

        private static List<Touch> fakeTouches = new List<Touch>();
        private static Dictionary<int, FakePress> fakeTouchPresses = new Dictionary<int, FakePress>();

        private struct FakePress
        {
            public float startTime;
            public float endTime;
            public Vector2 startPos;
            public Vector2 endPos;
        }

        public static int touchCount
        {
            get
            {
                if (IsPlaybackActive())
                {
                    return fakeTouches.Count;
                }

                return Input.touchCount;
            }
        }

        public static Touch[] touches
        {
            get
            {
                int numTouches = touchCount;
                Touch[] touchArray = new Touch[numTouches];
                for (int index = 0; index < numTouches; ++index)
                    touchArray[index] = GetTouch(index);
                return touchArray;
            }
        }

        public static Touch GetTouch(int index)
        {
            if (IsPlaybackActive())
            {
                if (index < fakeTouches.Count)
                {
                    var touch = fakeTouches[index];
                    if (fakeTouchPresses.ContainsKey(touch.fingerId))
                    {
                        touch.position = GetInterpolatedPosition(fakeTouchPresses[touch.fingerId]);
                    }
                    return touch;
                }
            }

            return Input.GetTouch(index);
        }

        public static Vector3 mousePosition
        {
            get
            {
                if (IsPlaybackActive())
                {
                    return fakeMousePresses.ContainsKey(0) ? GetInterpolatedPosition(fakeMousePresses[0]) : fakeMousePos;
                }

                return Input.mousePosition;
            }
        }

        public static bool GetMouseButton(int button)
        {
            var isActive = IsPlaybackActive();
            if (fakeMousePresses.ContainsKey(button))
            {
                var now = Time.time;
                var press = fakeMousePresses[button];
                isActive = isActive && now >= press.startTime && now <= press.endTime;
            }

            return isActive || Input.GetMouseButton(button);
        }

        public static bool GetMouseButtonDown(int button)
        {
            var fakeDown = false;
            if (IsPlaybackActive() && fakeMousePresses.ContainsKey(button))
            {
                fakeDown = fakeMouseDownEvents.Contains(button);
            }

            return fakeDown || Input.GetMouseButtonDown(button);
        }

        public static bool GetMouseButtonUp(int button)
        {
            var fakeUp = false;
            if (IsPlaybackActive() && fakeMousePresses.ContainsKey(button))
            {
                fakeUp = fakeMouseUpEvents.Contains(button);
            }

            return fakeUp || Input.GetMouseButtonUp(button);
        }

        // These methods and properties do not yet support recording, however they are provided so this file can be
        // used as a drop-in replacement for the existing Input class.
        public static Vector3 acceleration => Input.acceleration;
        public static int accelerationEventCount => Input.accelerationEventCount;
        public static AccelerationEvent[] accelerationEvents => Input.accelerationEvents;
        public static bool anyKey => Input.anyKey;
        public static bool anyKeyDown => Input.anyKeyDown;
        public static bool backButtonLeavesApp => Input.backButtonLeavesApp;
        public static Compass compass => Input.compass;
        public static bool compensateSensors => Input.compensateSensors;
        public static Vector2 compositionCursorPos => Input.compositionCursorPos;
        public static string compositionString => Input.compositionString;
        public static DeviceOrientation deviceOrientation => Input.deviceOrientation;
        public static Gyroscope gyro => Input.gyro;
        public static IMECompositionMode imeCompositionMode => Input.imeCompositionMode;
        public static bool imeIsSelected => Input.imeIsSelected;
        public static string inputString => Input.inputString;
        public static bool mousePresent => Input.mousePresent;
        public static Vector2 mouseScrollDelta => Input.mouseScrollDelta;
        public static bool multiTouchEnabled => Input.multiTouchEnabled;
        public static bool simulateMouseWithTouches => Input.simulateMouseWithTouches;
        public static bool stylusTouchSupported => Input.stylusTouchSupported;
        public static bool touchPressureSupported => Input.touchPressureSupported;

        public static AccelerationEvent GetAccelerationEvent(int index)
        {
            return Input.GetAccelerationEvent(index);
        }

        public static float GetAxis(string axisName)
        {
            return Input.GetAxis(axisName);
        }

        public static float GetAxisRaw(string axisName)
        {
            return Input.GetAxisRaw(axisName);
        }

        public static bool GetButton(string buttonName)
        {
            return Input.GetButton(buttonName);
        }

        public static bool GetButtonDown(string buttonName)
        {
            return Input.GetButtonDown(buttonName);
        }

        public static bool GetButtonUp(string buttonName)
        {
            return Input.GetButtonUp(buttonName);
        }

        public static string[] GetJoystickNames()
        {
            return Input.GetJoystickNames();
        }

        public static bool GetKey(KeyCode keyCode)
        {
            return Input.GetKey(keyCode);
        }

        public static bool GetKey(string name)
        {
            return Input.GetKey(name);
        }

        public static bool GetKeyDown(KeyCode keyCode)
        {
            return Input.GetKeyDown(keyCode);
        }

        public static bool GetKeyDown(string name)
        {
            return Input.GetKeyDown(name);
        }

        public static bool GetKeyUp(KeyCode keyCode)
        {
            return Input.GetKeyUp(keyCode);
        }

        public static bool GetKeyUp(string name)
        {
            return Input.GetKeyUp(name);
        }

        public static void ResetInputAxes()
        {
            Input.ResetInputAxes();
        }

        // Internal methods used for playback of recorded data
        internal static void FakeButtonDown(int button, Vector2 downPos, Vector2 upPos, float duration = 1f)
        {
            var fakeMousePress = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
                startPos = downPos,
                endPos = upPos
            };
            fakeMousePresses[button] = fakeMousePress;
            fakeMouseUpEvents.Remove(button);
            fakeMouseDownEvents.Add(button);
        }

        internal static void FakeTouch(Touch touch, Vector2 releasePos, float duration)
        {
            var press = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
                startPos = touch.position,
                endPos = releasePos
            };
            fakeTouchPresses[touch.fingerId] = press;

            for (int i = 0; i < fakeTouches.Count; i++)
            {
                if (fakeTouches[i].fingerId == touch.fingerId)
                {
                    fakeTouches[i] = touch;
                    return;
                }
            }
            fakeTouches.Add(touch);
        }

        internal static void Update()
        {
            for (int i = fakeTouches.Count - 1; i >= 0; i--)
            {
                switch (fakeTouches[i].phase)
                {
                    case TouchPhase.Began:
                        var newTouch = fakeTouches[i];
                        newTouch.phase = TouchPhase.Moved;
                        fakeTouches[i] = newTouch;
                        break;
                    case TouchPhase.Ended:
                        fakeTouches.RemoveAt(i);
                        break;
                    case TouchPhase.Moved:
                        var endingTouch = fakeTouches[i];
                        if (fakeTouchPresses.ContainsKey(endingTouch.fingerId) && Time.time >= fakeTouchPresses[endingTouch.fingerId].endTime)
                        {
                            endingTouch.phase = TouchPhase.Ended;
                            fakeTouches[i] = endingTouch;
                        }
                        break;
                }
            }

            fakeMouseDownEvents.Clear();
            foreach (var button in fakeMouseUpEvents)
            {
                if (button == 0 && fakeMousePresses.ContainsKey(button))
                {
                    fakeMousePos = fakeMousePresses[button].endPos;
                }
                fakeMousePresses.Remove(button);
            }
            fakeMouseUpEvents.Clear();

            foreach (var button in fakeMousePresses.Keys)
            {
                var press = fakeMousePresses[button];
                if (Time.time >= press.endTime)
                {
                    fakeMouseUpEvents.Add(button);
                }
            }
        }

        internal static void Reset()
        {
            fakeMousePos = new Vector2();
            fakeTouches.Clear();
            fakeTouchPresses.Clear();
            fakeMousePresses.Clear();
            fakeMouseDownEvents.Clear();
            fakeMouseUpEvents.Clear();
        }

        private static Vector2 GetInterpolatedPosition(FakePress fakePress)
        {
            var now = Time.time;
            var deltaTime = Math.Min(1, (now - fakePress.startTime) / (fakePress.endTime - fakePress.startTime));
            return fakePress.startPos + deltaTime * (fakePress.endPos - fakePress.startPos);
        }

        private static bool IsPlaybackActive()
        {
            return playbackActive || RecordedPlaybackController.IsPlaybackActive();
        }
    }
}