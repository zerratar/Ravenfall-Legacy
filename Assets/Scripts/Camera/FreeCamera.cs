using Newtonsoft.Json;
using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    private const string CameraPositionDataFile = "data/camera-positions.json";

    [SerializeField] private float moveSpeed = 50.0f;
    [SerializeField] private float lookSpeed = 0.25f;
    [SerializeField] private GameManager gameManager;

    private Vector3 lastMousePosition;
    private KeyCode[] positionKeys = {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,KeyCode.Alpha8,KeyCode.Alpha9,
    };

    private StoredPosition[] storedPositions = new StoredPosition[10];

    // Start is called before the first frame update
    void Start()
    {
        //Cursor.lockState = CursorLockMode.Confined;
        Cursor.lockState = CursorLockMode.None;
        lastMousePosition = Input.mousePosition;
        LoadPositionData();
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

        var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shiftDown)
        {
            StoreCurrentPosition();
        }

        if (GotoStoredPosition())
        {
            return;
        }

        lastMousePosition = newMousePosition;
        var speedUp = shiftDown ? 5f : 1f;
        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");
        var moveUp = Input.GetKey(KeyCode.E) ? transform.up * Time.deltaTime * moveSpeed * speedUp : Vector3.zero;
        var moveDown = Input.GetKey(KeyCode.Q) ? transform.up * -1 * Time.deltaTime * moveSpeed * speedUp : Vector3.zero;
        var moveSides = transform.right * horizontal * Time.deltaTime * moveSpeed * speedUp;
        var moveForward = transform.forward * vertical * Time.deltaTime * moveSpeed * speedUp;
        transform.position += moveForward + moveSides + moveUp + moveDown;
    }

    private bool GotoStoredPosition()
    {
        for (var i = 0; i < positionKeys.Length; ++i)
        {
            if (Input.GetKeyDown(positionKeys[i]))
            {
                StoredPosition pos = GetStoredPosition(i);
                if (pos != null)
                {
                    SetPosition(pos);
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    private void SetPosition(StoredPosition pos)
    {
        this.transform.position = pos.Position;
        this.transform.rotation = pos.Rotation;
    }

    private StoredPosition GetStoredPosition(int i)
    {
        return storedPositions[i];
    }

    private void StoreCurrentPosition()
    {
        for (var i = 0; i < positionKeys.Length; ++i)
        {
            if (Input.GetKeyDown(positionKeys[i]))
            {
                storedPositions[i] = new StoredPosition
                {
                    Position = this.transform.position,
                    Rotation = this.transform.rotation
                };
                break;
            }
        }

        SavePositionData();
    }

    private void SavePositionData()
    {
        if (!System.IO.Directory.Exists("data"))
            System.IO.Directory.CreateDirectory("data");
        var data = JsonConvert.SerializeObject(storedPositions);
        System.IO.File.WriteAllText(CameraPositionDataFile, data);
    }

    private void LoadPositionData()
    {
        if (!System.IO.File.Exists(CameraPositionDataFile))
            return;

        var data = System.IO.File.ReadAllText(CameraPositionDataFile);
        storedPositions = JsonConvert.DeserializeObject<StoredPosition[]>(data);
    }

    public class StoredPosition
    {
        public V3 Position { get; set; }
        public Q Rotation { get; set; }
    }

    public class V3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static implicit operator V3(Vector3 v)
        {
            return new V3
            {
                X = v.x,
                Y = v.y,
                Z = v.z
            };
        }

        public static implicit operator Vector3(V3 v)
        {
            return new Vector3
            {
                x = v.X,
                y = v.Y,
                z = v.Z
            };
        }
    }

    public class Q
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public static implicit operator Q(Quaternion v)
        {
            return new Q
            {
                X = v.x,
                Y = v.y,
                Z = v.z,
                W = v.w
            };
        }

        public static implicit operator Quaternion(Q v)
        {
            return new Quaternion
            {
                x = v.X,
                y = v.Y,
                z = v.Z,
                w = v.W
            };
        }
    }
}
