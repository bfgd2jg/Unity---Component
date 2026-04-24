using UnityEngine;
using UnityEngine.InputSystem;

public class MouseRaycastManager : BaseSingletonMono<MouseRaycastManager>
{
    // 鼠标射线检测的层级
    [SerializeField] LayerMask interactLayer;
    // 当前鼠标指向的可交互对象
    IMouseInteractable currentTarget;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        IMouseInteractable newTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactLayer))
        {
            newTarget = hit.collider.GetComponent<IMouseInteractable>();
        }

        if (newTarget != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.OnMouseExit();

            if (newTarget != null)
                newTarget.OnMouseEnter();

            currentTarget = newTarget;
        }

        if (currentTarget != null)
        {
            currentTarget.OnMouseStay();

            if (Mouse.current.leftButton.wasPressedThisFrame)
                currentTarget.OnMouseClick();
        }
    }
}

// 定义一个接口，所有可被鼠标交互的对象都需要实现这个接口
public interface IMouseInteractable
{
    void OnMouseEnter();
    void OnMouseExit();
    void OnMouseStay();
    void OnMouseClick();
}