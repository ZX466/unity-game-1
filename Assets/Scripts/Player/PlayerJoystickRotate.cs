using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerJoystickRotate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public int MovementRange = 100;

    Vector3 m_StartPos;
    
    private Vector3 _mapToScreenPosition;
    public Vector3 MapToScreenPosition => _mapToScreenPosition;

    void Start()
    {
        m_StartPos = transform.position;
    }

    void UpdateVirtualAxes(Vector3 value)
    {
        var delta = m_StartPos - value;
        delta.y = -delta.y;
        delta /= MovementRange;

        _mapToScreenPosition.x = (-0.5f * delta.x + 0.5f) * Screen.width;
        _mapToScreenPosition.y = (0.5f * delta.y + 0.5f) * Screen.height;
    }

    public void OnDrag(PointerEventData data)
    {
        Vector3 newPos = Vector3.zero;

        {
            int delta = (int) (data.position.x - m_StartPos.x);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.x = delta;
        }

        {
            int delta = (int) (data.position.y - m_StartPos.y);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.y = delta;
        }

        transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
        UpdateVirtualAxes(transform.position);
    }


    public void OnPointerUp(PointerEventData data)
    {
        transform.position = m_StartPos;
    }


    public void OnPointerDown(PointerEventData data)
    {
    }
}