using UnityEngine;

namespace CodeRedCat._4kVectorLandscape.Demo.Scripts
{
    //a base class to test camera scrolling
    public class CameraScroll : MonoBehaviour
    {
        [Tooltip("The Speed at the start of the timeShiftInterVal")]
        [SerializeField] private Vector2 scrollSpeedA;
        [Tooltip("The Speed at the end of the timeShiftInterVal")]
        [SerializeField] private Vector2 scrollSpeedB;
        [Tooltip("Defines the intervals in which the Value from A will change to B or, after that, back from A to B. The shifts will be repeated indefinitely.")]
        [SerializeField] private float timeShiftInterVal = 30;
        [Tooltip("The Camera Zoom at the end of the timeShiftInterVal. Zoom at the start will be determined by the camera orthographic size.")]
        [SerializeField] private float zoomB = 5;

        private Camera _camera;
        private float _zoomA;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _zoomA = _camera.orthographicSize;
        }

        private void Update()
        {
            float lerpValue = 1 - Mathf.Abs(timeShiftInterVal - Time.time%timeShiftInterVal*2)/timeShiftInterVal;
            Vector2 scrollSpeed = new Vector2(Mathf.Lerp(scrollSpeedA.x, scrollSpeedB.x, lerpValue), Mathf.Lerp(scrollSpeedA.y, scrollSpeedB.y, lerpValue));
            
            Vector2 newPosition = (Vector2)transform.position + scrollSpeed * Time.deltaTime;
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);

            float newZoom = Mathf.Lerp(_zoomA, zoomB, lerpValue);
            _camera.orthographicSize = newZoom;
        }
    }
}