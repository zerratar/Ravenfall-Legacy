using UnityEngine;

public class ArenaGateController : MonoBehaviour
{
    [SerializeField] private ArenaGateState state;
    [SerializeField] private float openPosition = 2.075f;
    [SerializeField] private float closedPosition = 0f;

    [SerializeField] private float openSpeed = 4f;
    [SerializeField] private float closeSpeed = 1f;

    private float animationTimer = 0f;

    private Vector3 openState => new Vector3(0, openPosition, 0);
    private Vector3 closedState => new Vector3(0, closedPosition, 0);

    public ArenaGateState State => state;

    // Update is called once per frame
    void Update()
    {
        if (state == ArenaGateState.Closing)
        {
            animationTimer += Time.deltaTime;
            var proc = animationTimer / closeSpeed;
            if (proc >= 1)
            {
                proc = 1f;
                state = ArenaGateState.Closed;
            }
            transform.localPosition = Vector3.Lerp(openState, closedState, proc);
        }
        else if (state == ArenaGateState.Opening)
        {
            animationTimer += Time.deltaTime;
            var proc = animationTimer / closeSpeed;
            if (proc >= 1)
            {
                proc = 1f;
                state = ArenaGateState.Open;
            }
            transform.localPosition = Vector3.Lerp(closedState, openState, proc);
        }
    }

    public void Open()
    {
        if (state == ArenaGateState.Open)
        {
            return;
        }

        animationTimer = 0;
        state = ArenaGateState.Opening;
    }

    public void Close()
    {
        if (state == ArenaGateState.Closed)
        {
            return;
        }

        animationTimer = 0;
        state = ArenaGateState.Closing;
    }
}

public enum ArenaGateState
{
    Open,
    Opening,
    Closed,
    Closing
}