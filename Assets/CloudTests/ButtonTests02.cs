using System.Collections;
using Unity.RecordedTesting;
using UnityEngine;
using UnityEngine.UI;


namespace Tests
{
    public class ButtonTests02
    {
//        [RecordedTest("Recordings/recording2.json")]
        public IEnumerator VerifyClickTwice()
        {
            var currTime = Time.time;
            while (Time.time - currTime < 10)
            {
                yield return null;
            }

            var buttonCount = GameObject.FindGameObjectWithTag("ButtonCount");

            var buttonClicks = System.Convert.ToInt32(buttonCount.GetComponent<Text>().text);
            CloudTesting.AssertTrue("VerifyClickTwiceOK", buttonClicks >= 2);
            CloudTesting.Quit();
        }
    }
}