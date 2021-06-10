using System;
using UnityEngine;

public class HighlightElement : MonoBehaviour
{
    
    void Init(GameObject target)
    {

        // Take 4 points around an object's dimensions (with padding) and draw a rectangle with LineRenderer.
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 5;
        lineRenderer.startWidth = lineRenderer.endWidth = 0.05f;

        float halfWidth = target.GetComponent<RectTransform>().sizeDelta.x / 2f;
        float halfHeight = target.GetComponent<RectTransform>().sizeDelta.y / 2f;
        float padding = 10f;
        Vector3 topLeft, topRight, bottomLeft, bottomRight, lineCompletor;
        topLeft = Camera.main.ScreenToWorldPoint(new Vector3(target.transform.position.x - halfWidth - padding, target.transform.position.y + halfHeight + padding, 0));
        float x = Math.Abs((float)Math.Round(topLeft.x, 2));
        float y = Math.Abs((float)Math.Round(topLeft.y, 2));
        topLeft = new Vector3(-x, y, 0);
        topRight = new Vector3(x, y, 0);
        bottomRight = new Vector3(x, -y, 0);
        bottomLeft = new Vector3(-x, -y, 0);

        lineCompletor = topLeft;
        lineRenderer.SetPositions(new Vector3[] { topLeft, topRight, bottomRight, bottomLeft, lineCompletor });

    }
}
