using System;
using System.Collections;
using GeneratedAutomationTests;
using NUnit.Framework;
using Unity.CloudTesting;
using Unity.RecordedPlayback;
using Unity.RecordedTesting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class VerifyButtonClicks : RecordedTestSuite
    {
        public override IEnumerator Setup()
        {
            ButtonAction.count = 0;
            return base.Setup();
        }

        [UnityTest]
        [CloudTest]
        [Timeout(10000)]
        [RecordedTest("Recordings/recording1.json")]
        public IEnumerator VerifyClicks()
        {
            var buttonCounter = GameObject.FindGameObjectWithTag("ButtonCount");
            int buttonClicks = 0;

            while (buttonClicks < 5)
            {
                buttonClicks = System.Convert.ToInt32(buttonCounter.GetComponent<Text>().text);
                yield return null;
            }

            Assert.AreEqual(5, buttonClicks);
        }

        [UnityTest]
        [CloudTest]
        [RecordedTest("Recordings/recording2.json")]
        public IEnumerator VerifyAfterPlayback()
        {
            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.IsPlaybackCompleted())
            {
                yield return null;
            }

            var buttonCounter = GameObject.FindGameObjectWithTag("ButtonCount");
            var buttonClicks = Convert.ToInt32(buttonCounter.GetComponent<Text>().text);
            Assert.AreEqual(10, buttonClicks);
        }
    }
}