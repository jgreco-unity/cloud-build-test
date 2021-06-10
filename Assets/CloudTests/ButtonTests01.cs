using System.Collections;
using Unity.RecordedTesting;
using UnityEngine;
using UnityEngine.UI;

namespace Tests
{
    public class ButtonTests01
    {
        // Use the "RecordingInput" attributed to specify the relative path to the recording file under Assets
//        [RecordedTest("Recordings/recording1.json")]
        public IEnumerator VerifyClickOnce()
        {
            var currTime = Time.time;
            // 3 seconds wait
            while (Time.time - currTime < 3)
            {
                yield return null;
            }

            var buttonCount = GameObject.FindGameObjectWithTag("ButtonCount");
            var buttonClicks = System.Convert.ToInt32(buttonCount.GetComponent<Text>().text);
            CloudTesting.AssertTrue("VerifyClickOnceOK", buttonClicks >= 1);
            CloudTesting.Quit();
            
        }
    
    }
    
}
