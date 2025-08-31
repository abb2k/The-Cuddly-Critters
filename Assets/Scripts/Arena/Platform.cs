using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Platform : MonoBehaviour
{
    [SerializeField] private InputActionReference movementAction;
    private List<PlatformEffector2D> effectors = new();
    private Dictionary<PlatformEffector2D, float> originalAngle = new();
    void Start()
    {
        movementAction.action.started += OnClicked;
        movementAction.action.canceled += OnClicked;
        effectors = GetComponents<PlatformEffector2D>().ToList();
        foreach (var effector in effectors)
        {
            originalAngle.Add(effector, effector.surfaceArc);
        }
        
    }

    public void OnClicked(InputAction.CallbackContext context)
    {
        Vector2 move = context.ReadValue<Vector2>();
        Debug.Log(move);

        if (move.y < 0)
        {
            effectors.ForEach(e =>
            {
                e.surfaceArc = 0;
            });
        }
        else
        {
            effectors.ForEach(e =>
            {
                e.surfaceArc = originalAngle[e];
            });
        }
    }

    void OnDestroy()
    {
        movementAction.action.started -= OnClicked;
        movementAction.action.canceled -= OnClicked;
    }
}
