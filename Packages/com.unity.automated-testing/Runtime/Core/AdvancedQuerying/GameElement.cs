using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.AutomatedQA
{
    // This class can be attached to a GameObject to give it unique identifiers and mark it as interactable so that it
    // will be picked up by recorded playback object detection.
    public class GameElement : MonoBehaviour
    {
        private static int count = 0;
        #region Attributes
        public string Id;
        public string[] Classes;
        #endregion

        // Add any custom key and value property pairs. Search for them by selector like "[key=value]" with Driver APIs.
        #region Properties
        public Properties[] Properties;
        #endregion

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(Id)) Id = $"item{count++}";
        }

        private IEnumerator Start()
        {
            ElementQuery.Instance.RegisterElement(this);
            yield return null;
            ElementQuery.Instance.ValidatePropertiesAndAttributes(this);
        }

        // This method will be called during playback when both a press and release happen back to back on the same
        // object and can be used to manually invoke actions like OnMouseUpAsButton that are not currently supported.
        public virtual void OnClickAction() {}
    }

    [Serializable]
    public class Properties
    {
        public string PropertyName;
        public string PropertyValue;
    }
}