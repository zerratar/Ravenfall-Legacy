using UnityEngine;

namespace FlatKit
{
    public class MouseOrbitCamera : MonoBehaviour
    {
        public enum TargetMode
        {
            Transform,
            Position
        }

        public TargetMode targetMode = TargetMode.Position;
        public Transform targetTransform;
        public bool followTargetTransform = true;
        public Vector3 targetOffset = Vector3.zero;
        public Vector3 targetPosition;

        [Space] public float distanceHorizontal = 60.0f;
        public float distanceVertical = 60.0f;
        public float xSpeed = 120.0f;
        public float ySpeed = 120.0f;
        public float damping = 3f;

        [Space] public bool clampAngle = false;
        public float yMinLimit = -20f;
        public float yMaxLimit = 80f;

        [Space] public bool allowZoom = false;
        public float distanceMin = .5f;
        public float distanceMax = 15f;

        float _x = 0.0f;
        float _y = 0.0f;

        [Space] public bool autoMovement = false;
        public float autoSpeedX = 0.2f;
        public float autoSpeedY = 0.1f;
        public float autoSpeedDistance = -0.1f;

        /*[Space] public*/ bool interactive = false;

        private float _lastMoveTime;
        [HideInInspector] public float timeSinceLastMove;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _x = angles.y;
            _y = angles.x;

            // Make the rigid body not change rotation
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.freezeRotation = true;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        xSpeed *= 0.2f;
        ySpeed *= 0.2f;
#endif

            if (targetMode == TargetMode.Transform)
            {
                if (targetTransform != null)
                {
                    targetPosition = targetTransform.position + targetOffset;
                }
                //else
                //{
                //    Debug.LogWarning("Reference transform is not set.");
                //}
            }
        }

        void Update()
        {


            if (targetMode == TargetMode.Transform && followTargetTransform)
            {
                if (targetTransform != null)
                {
                    targetPosition = targetTransform.position + targetOffset;
                }
                //else
                //{
                //    Debug.LogWarning("Reference transform is not set.");
                //}
            }

            //*
            bool isCameraMoving = false;
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
        isCameraMoving = Input.GetTouch(0).deltaPosition.sqrMagnitude > 0f;
#else
            isCameraMoving = Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y")) > 0f;
#endif
            if (isCameraMoving)
            {
                _lastMoveTime = Time.time;
            }

            timeSinceLastMove = Time.time - _lastMoveTime;
            //*/


            //if (TownHouseManager.IsPointerOverUIElement() || PlayerDetails.IsMoving)
            //    interactive = false;
            //else
            //    interactive = true;

            if (interactive && Input.GetMouseButton(0))
            {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
        _x += Input.GetTouch(0).deltaPosition.x * xSpeed * 40f * 0.02f;
        _y -= Input.GetTouch(0).deltaPosition.y * ySpeed * 40f * 0.02f;
#else
                _x += Input.GetAxis("Mouse X") * xSpeed * 40f * 0.02f;
                _y -= Input.GetAxis("Mouse Y") * ySpeed * 40f * 0.02f;
#endif
            }
            else if (autoMovement)
            {
                _x += autoSpeedX * 40f * GameTime.deltaTime * 10f;
                _y -= autoSpeedY * 40f * GameTime.deltaTime * 10f;
                distanceHorizontal += autoSpeedDistance;
            }

            if (clampAngle)
            {
                _y = ClampAngle(_y, yMinLimit, yMaxLimit);
            }

            Quaternion rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_y, _x, 0),
                GameTime.deltaTime * damping);

            if (allowZoom)
            {
                distanceHorizontal = Mathf.Clamp(
                    distanceHorizontal - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
            }

            float rotationX = rotation.eulerAngles.x;
            if (rotationX > 90f)
            {
                rotationX -= 360f;
            }

            float usedDistance = Mathf.Lerp(distanceHorizontal, distanceVertical, Mathf.Abs(rotationX / 90f));
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -usedDistance);
            Vector3 position = rotation * negDistance + targetPosition;

            transform.rotation = rotation;
            transform.position = position;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}