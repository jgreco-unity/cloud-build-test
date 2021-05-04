using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class ExecuteUnityEventAfterTime : MonoBehaviour {
  [SerializeField]
  private StringEvent unityEvent;
  [SerializeField]
  private float delay;

  [SerializeField] private string arg;

  void OnEnable() {
    StartCoroutine(ExecuteAfterDelay());
  }

  private IEnumerator ExecuteAfterDelay() {
    yield return new WaitForSeconds(delay);
    unityEvent.Invoke(arg);
  }

  [System.Serializable]
  public class StringEvent : UnityEvent<string>
  {
  }
}
