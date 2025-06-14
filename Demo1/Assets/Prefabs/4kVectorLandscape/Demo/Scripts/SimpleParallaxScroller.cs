using System.Collections.Generic;
using UnityEngine;

namespace CodeRedCat._4kVectorLandscape.Demo.Scripts
{
//intended for presentation purposes. Use in professional project is limited (see comments)
//takes the first child and uses it to generate a scroller with 3 images. Use it multiple times to get a parallax effect
    public class SimpleParallaxScroller : MonoBehaviour
    {
        [Tooltip("The speed of the parallax effect. This will shift the images, when the camera moves. At a speed of 1 the movement will be exactly as fast as the camera movement.")]
        [SerializeField] private Vector2 parallaxSpeed;
        [Tooltip("The speed of the shifting effect. This will shift the images all the time.")]
        [SerializeField] private Vector2 autoScrollSpeed;
        [Tooltip("Images will overlap a bit to prevent a visible line between them. Overlap is in worldspace units")]
        [SerializeField] private float imageOverlap;
        [Tooltip("If true, the image will not be repeated to the left or right. Use this for unique things in the scene, like the sun or moon.")]
        [SerializeField] private bool oneImage;
        

        private List<Transform> _imageTransforms = new ();
        private float _imageDistance;
        private Transform _camTransRef;
        private Vector2 _lastCamPosition;

        // Start is called before the first frame update
        void Start()
        {
            Transform originalTransform = transform.GetChild(0);
            _camTransRef = Camera.main.transform;
            _lastCamPosition = _camTransRef.position;
        
            GetImageDistance();
        
            if(!oneImage) ConstructSideImages();
        
            FillImageTransformList();
        
        
            void GetImageDistance()
            {
                _imageDistance = originalTransform.GetComponent<SpriteRenderer>().bounds.size.x - imageOverlap;
            }
        
            void FillImageTransformList()
            {
                foreach (Transform childTransform in GetComponentInChildren<Transform>())
                {
                    if (childTransform != transform)
                    {
                        _imageTransforms.Add(childTransform);
                    }
                }
            }
        
            void ConstructSideImages()
            {
                //construct 2 images
                Transform leftImage = Instantiate(originalTransform, transform);
                Transform rightImage = Instantiate(originalTransform, transform);
            
                //move them to the left and right
                leftImage.position = new Vector3(originalTransform.position.x - _imageDistance, originalTransform.position.y, originalTransform.position.z);
                rightImage.position = new Vector3(originalTransform.position.x + _imageDistance, originalTransform.position.y, originalTransform.position.z);
            }
        }

        // Update is called once per frame
        void Update()
        {
            ScrollImages();
            if(!oneImage) ShiftImages();
        }

        //Scrolls the images left and right. This work fine, but could be improved by using the distance traveled from the original position instead
        private void ScrollImages()
        {
            //get parallaxScroll
            Vector2 parallaxScroll = new Vector2((_lastCamPosition.x - _camTransRef.position.x) * parallaxSpeed.x,(_lastCamPosition.y - _camTransRef.position.y) * parallaxSpeed.y);
        
            //apply scroll
            foreach (Transform imageTransform in _imageTransforms)
            {
                Vector3 currentPosition = imageTransform.transform.position;
                float newXPos = currentPosition.x + autoScrollSpeed.x * Time.deltaTime - parallaxScroll.x;
                float newYPos = currentPosition.y + autoScrollSpeed.y * Time.deltaTime - parallaxScroll.y;
                imageTransform.transform.position = new Vector3(newXPos, newYPos, currentPosition.z);
            }
        
            _lastCamPosition = _camTransRef.position;
        }

        //Shifts images if they are too far left or right. Could be improved by adding sorting so that it would be clear, which image is left or right.
        private void ShiftImages()
        {
            float leftThreshHold = _camTransRef.transform.position.x - _imageDistance * 1.6f;
            float rightThreshHold = _camTransRef.transform.position.x + _imageDistance * 1.6f;
        
            foreach (Transform imageTransform in _imageTransforms)
            {
                if (imageTransform.position.x < leftThreshHold)
                {
                    imageTransform.position = new Vector3(imageTransform.position.x + _imageDistance * 3, imageTransform.position.y, imageTransform.position.z);
                }
                if (imageTransform.position.x > rightThreshHold)
                {
                    imageTransform.position = new Vector3(imageTransform.position.x - _imageDistance * 3, imageTransform.position.y, imageTransform.position.z);
                }
            }
        }
    }
}
