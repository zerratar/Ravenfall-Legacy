using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 50.0f;
    [SerializeField] private float lookSpeed = 0.25f;
    [SerializeField] private GameManager gameManager;

    private Vector3 lastMousePosition;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        lastMousePosition = Input.mousePosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager == null ||
            gameManager.RavenNest == null ||
            !gameManager.RavenNest.Authenticated ||
            !gameManager.IsLoaded)
        {
            return;
        }

        var newMousePosition = Input.mousePosition;
        if (Input.GetMouseButton(1)
            || Input.GetKey(KeyCode.LeftControl)
            || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftAlt)
            || Input.GetKey(KeyCode.RightAlt))
        {
            var mouseDeltaPosition = newMousePosition - lastMousePosition;
            mouseDeltaPosition = new Vector3(-mouseDeltaPosition.y * lookSpeed, mouseDeltaPosition.x * lookSpeed, 0);
            mouseDeltaPosition = new Vector3(transform.eulerAngles.x + mouseDeltaPosition.x, transform.eulerAngles.y + mouseDeltaPosition.y, 0);
            transform.eulerAngles = mouseDeltaPosition;
        }

        lastMousePosition = newMousePosition;

        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");

        var moveSides = transform.right * horizontal * Time.deltaTime * moveSpeed;
        var moveForward = transform.forward * vertical * Time.deltaTime * moveSpeed;

        transform.position += moveForward + moveSides;

    }
}
