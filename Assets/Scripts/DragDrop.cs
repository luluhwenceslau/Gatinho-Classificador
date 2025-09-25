using UnityEngine;

public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Vector3 posInicial;

    void Start()
    {
        posInicial = transform.position;  
    }

    void OnMouseDown()
    {
        offset = transform.position - GetMouseWorldPos();
        dragging = true;
    }

    void OnMouseUp()
    {
        dragging = false;

        GatinhoAprendiz.Instance.TentarAnalisarObjeto(gameObject);
    }

    void Update()
    {
        if (dragging)
        {
            transform.position = GetMouseWorldPos() + offset;
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePoint.z = 0f;
        return mousePoint;
    }

    public void VoltarPosicaoInicial()
    {
        transform.position = posInicial;
    }
}
