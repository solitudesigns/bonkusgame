using UnityEngine;
using UnityEngine.EventSystems;

public class LeftTouchController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 direction;
    private bool isTouching;

    public void OnPointerDown(PointerEventData eventData)
    {
        isTouching = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isTouching)
        {
            direction = eventData.delta.normalized; // movement direction
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        direction = Vector2.zero;
        isTouching = false;
    }
}
