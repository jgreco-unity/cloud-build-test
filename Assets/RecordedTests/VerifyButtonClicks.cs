using System.Collections;
using NUnit.Framework;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using Unity.RecordedTesting;
using Unity.RecordedTesting.TestTools;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        [RecordedTest("Recordings/recording2.json")]
        public IEnumerator VerifyAfterPlayback()
        {
            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.Instance.IsPlaybackCompleted())
            {
                yield return null;
            }

            var buttonCounter = GameObject.FindGameObjectWithTag("ButtonCount");
            var buttonClicks = System.Convert.ToInt32(buttonCounter.GetComponent<Text>().text);
            Assert.AreEqual(10, buttonClicks);
        }
    }
}