using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 startPos;
    
    public void OnDrag(PointerEventData eventData)
    {
        transform.position = new Vector3(transform.position.x + eventData.delta.x, 
            transform.position.y + eventData.delta.y, transform.position.z);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = transform.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.position = startPos;
    }
}
