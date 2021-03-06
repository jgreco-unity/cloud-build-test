using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Automators.RecordedPlayback
{
    public class RecordableInputTests
    {
        [SetUp]
        public void Setup()
        {
            RecordableInput.Reset();
            RecordableInput.playbackActive = true;
        }

        [TearDown]
        public void TearDown()
        {
            RecordableInput.playbackActive = false;
        }

        [Test]
        public void FakeMouseButton_SuccessfullySimulates_UnitTest()
        {
            int testButton = 1;
            Vector2 mousePos = new Vector2(0, 0);
            RecordableInput.FakeButtonDown(testButton, mousePos, mousePos, 0f);

            Assert.True(RecordableInput.GetMouseButtonDown(testButton));
            Assert.AreEqual((Vector3) mousePos, RecordableInput.mousePosition);
            RecordableInput.Update(); // Trigger mouse up event
            Assert.False(RecordableInput.GetMouseButtonDown(testButton));
            Assert.True(RecordableInput.GetMouseButtonUp(testButton));
            RecordableInput.Update(); // Clear fake press
            Assert.False(RecordableInput.GetMouseButtonDown(testButton));
            Assert.False(RecordableInput.GetMouseButtonUp(testButton));
        }

        [Test]
        public void FakeTouch_SuccessfullySimulates_UnitTest()
        {
            var touch = new Touch();
            touch.fingerId = 1;
            touch.position = new Vector2(1, 1);
            touch.phase = TouchPhase.Began;

            RecordableInput.FakeTouch(touch, touch.position, 0f);
            ValidateTouch(touch, RecordableInput.touches[0]);
            RecordableInput.Update(); // Trigger touch move
            touch.phase = TouchPhase.Moved;
            ValidateTouch(touch, RecordableInput.touches[0]);
            RecordableInput.Update(); // Trigger touch end
            touch.phase = TouchPhase.Ended;
            ValidateTouch(touch, RecordableInput.touches[0]);
            RecordableInput.Update(); // Clear fake touch
            Assert.AreEqual(0, RecordableInput.touches.Length);
        }

        private void ValidateTouch(Touch expected, Touch actual)
        {
            Assert.AreEqual(expected.fingerId, actual.fingerId);
            Assert.AreEqual(expected.phase, actual.phase);
        }
    }
}