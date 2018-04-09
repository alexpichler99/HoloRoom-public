using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System;
using UnityEngine;

public class Hologram : MonoBehaviour, IInputClickHandler, IManipulationHandler
{
    private static bool moving = false;

    public static bool Moving { get { return moving; } }

    private BoxCollider boxCollider;

    private bool IsMoving;
    private bool IsRotating;
    private GameObject boundingBox;
    private const float raycastDistance = 30f;
    private bool placeable;
    
    private int oldLayer;

    public GameObject boundingBoxPrefab;
    private ChangeColorOfChildren boundingBoxColor;
    
    private GameObject menu;
    public GameObject menuPrefab;
    public float speed = 20;
    public int ignoreLayer = 29;
    public Color boundingBoxColorDefault = Color.white;
    public Color boundingBoxColorError = Color.red;

    private float rotationAngle;

    private int HologramLayer { get { return (1 << 30); } }

    /// <summary>
    /// Called when the GameObject is created.
    /// </summary>
    private void Awake()
    {
        // Get the object's collider.
        boxCollider = gameObject.GetComponent<BoxCollider>();

        if (boundingBoxPrefab != null && boundingBox == null)
        {
            boundingBox = Instantiate(boundingBoxPrefab);

            boundingBox.transform.parent = gameObject.transform;

            boundingBox.transform.localScale = new Vector3(1, 1, 1); // Set the localScale to (1,1,1) because setting transform.parent will manipulate the localScale

            boundingBox.transform.localPosition = new Vector3(0, 0, 0);
        }
    }

    public void ShowMenu()
    {
        if (menuPrefab != null && menu == null)
        {
            menu = Instantiate(menuPrefab);
            menu.SetActive(false);
        }

        // Calculate the maximum scale along the y axis
        var absoluteScaleY = transform.localScale.y * boxCollider.size.y;

        // Set the position of the menu above the object
        menu.transform.position = gameObject.transform.position + gameObject.transform.up * absoluteScaleY / 2;
        menu.transform.parent = gameObject.transform;
        menu.GetComponent<ContainerScript>().selectedHologram = this;
        menu.SetActive(true);
    }

    public void HideMenu()
    {
        if (menu != null)
            menu.SetActive(false);
    }

    public void ShowBoundingBox()
    {
        if (boundingBox != null)
        {
            boundingBox.SetActive(true);
            boundingBoxColor = boundingBox.GetComponent<ChangeColorOfChildren>();
            SetBoundingBoxColor(boundingBoxColorDefault);
        }
    }

    public void HideBoundingBox()
    {
        if (boundingBox != null)
            boundingBox.SetActive(false);
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (IsMoving)
            Move();
    }

    /// <summary>
    /// Set the color for the current BoundingBox
    /// </summary>
    /// <param name="color"></param>
    private void SetBoundingBoxColor(Color color)
    {
        if (boundingBoxColor != null)
            boundingBoxColor.ChangeColor(color);
    }

    private void Move()
    {
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        int layer = HologramLayer | SpatialMappingManager.Instance.LayerMask;

        RaycastHit hitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo, raycastDistance, layer))
        {
            Vector3 pos = hitInfo.point;

            // get absolute position, center and size of the BoxCollider
            Vector3 colliderCenter = Vector3.Scale(this.transform.localScale, boxCollider.center);
            Vector3 colliderPosition = pos + colliderCenter;
            Vector3 colliderSize = Vector3.Scale(this.transform.localScale, boxCollider.size);

            pos = colliderPosition;

            Vector3 extents = colliderSize / 2;

            // The order of these calls is very important
            bool bottomTopHit = MoveBottomAndTop(ref pos, extents.y, layer);

            bool leftRightHit;
            bool frontBackHit;
            var absRotationAngle = Math.Abs(rotationAngle);

            // Execute the front/back and left/right checks in a different order depending on the rotationAngle
            // Fixes https://github.com/alexpichler99/HoloRoom/issues/47#issuecomment-357502478
            if (absRotationAngle  > 45 && absRotationAngle < 135)
            {
                leftRightHit = MoveLeftAndRight(ref pos, extents.x, layer);
                frontBackHit = MoveFrontAndBack(ref pos, extents.z, layer);
            }
            else
            {
                frontBackHit = MoveFrontAndBack(ref pos, extents.z, layer);
                leftRightHit = MoveLeftAndRight(ref pos, extents.x, layer);
            }

            // Redo the bottom and top if one of the sides has hit because if one of the sides has hit, it causes the object to
            // move a bit. This could cause the object to be on a floor.
            if (frontBackHit || leftRightHit)
                bottomTopHit = MoveBottomAndTop(ref pos, extents.y, layer);

            this.transform.position = pos;
            SetBoundingBoxColor(boundingBoxColorDefault);
            placeable = true;
        }
        else
        {
            // Get the largest scale (z axis is ignored)
            var scale = transform.localScale.x > transform.localScale.y ? transform.localScale.x : transform.localScale.y;

            // Use the scale to calculate the distance needed to fit the object (https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html)
            var distance = scale / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // Move this object in front of the camera, if no surface is hit
            this.transform.position = Camera.main.transform.position + Camera.main.transform.forward * distance;

            SetBoundingBoxColor(boundingBoxColorError);
            placeable = false;
        }

        RotateToCamera(rotationAngle);
    }

    public void RotateToCamera(float angleOffset = 0f)
    {
        var rotation = Camera.main.transform.rotation.eulerAngles;

        rotation.x = 0;
        rotation.z = 0;
        rotation.y += angleOffset; // apply the offset

        transform.rotation = Quaternion.Euler(rotation);
    }

    /// <summary>
    /// Checks if the hologram is stuck inside the ceiling or floor and adjusts its position
    /// </summary>
    /// <param name="pos">Center of the object</param>
    /// <param name="yExtent">The height/2 of the object</param>
    /// <param name="layer">The layer that the Raycast uses</param>
    /// <returns>True if the hologram was stuck inside an object</returns>
    private bool MoveBottomAndTop(ref Vector3 pos, float yExtent, int layer)
    {
        // have to add this to the origin in the opposite direction of the raycast direction; the raycast won't hit if the origin is at the same position as the gaze hit
        float add = 0.0001f;
        float bottom = -1;
        float top = -1;
        RaycastHit hitInfo;

        // ray from center to bottom
        bool bottomHit = Physics.Raycast(pos + transform.up * add, transform.up * -1, out hitInfo, yExtent, layer);
        if (bottomHit)
        {
            var surfacePlane = hitInfo.collider.gameObject.GetComponent<SurfacePlane>();

            if (surfacePlane == null || surfacePlane.PlaneType != PlaneTypes.Wall)
                bottom = yExtent - hitInfo.distance;
            else
                bottomHit = false;
        }

        // ray from center of the cube to the top
        bool topHit = Physics.Raycast(pos + transform.up * -1 * add, transform.up, out hitInfo, yExtent, layer);
        if (topHit)
        {
            var surfacePlane = hitInfo.collider.gameObject.GetComponent<SurfacePlane>();

            if (surfacePlane == null || surfacePlane.PlaneType != PlaneTypes.Wall)
                top = yExtent - hitInfo.distance;
            else
                topHit = false;
        }

        if (bottomHit || topHit)
        {
            if (topHit && !bottomHit) //top
                pos += transform.up * -1 * top;
            else if (bottomHit && !topHit) //bottom
                pos += transform.up * bottom;
            else if (bottom < top) //bottom
                pos += transform.up * bottom;
            else //top
                pos += transform.up * -1 * top;
        }

        return topHit || bottomHit;
    }

    /// <summary>
    /// Checks if the hologram is stuck inside another object to its front or back and adjusts its position
    /// </summary>
    /// <param name="pos">Center of the object</param>
    /// <param name="zExtent">The extent of the object along the z axis</param>
    /// <param name="layer">The layer that the Raycast uses</param>
    /// <returns>True if the hologram was stuck inside an object</returns>
    private bool MoveFrontAndBack(ref Vector3 pos, float zExtent, int layer)
    {
        // have to add this to the origin in the opposite direction of the raycast direction; the raycast won't hit if the origin is at the same position as the gaze hit
        float add = 0.0001f;
        float front = -1;
        float back = -1;
        RaycastHit hitInfo;

        bool frontHit = Physics.Raycast(pos + transform.forward * add, transform.forward * -1, out hitInfo, zExtent, layer);
        if (frontHit)
            front = zExtent - hitInfo.distance;

        bool backHit = Physics.Raycast(pos + transform.forward * -1 * add, transform.forward, out hitInfo, zExtent, layer);
        if (backHit)
            back = zExtent - hitInfo.distance;

        if (frontHit || backHit)
        {
            if (frontHit && !backHit) //front
                pos += transform.forward * front;
            else if (backHit && !frontHit) //back
                pos += transform.forward * -1 * back;
            else if (front < back) //front
                pos += transform.forward * front;
            else //back
                pos += transform.forward * -1 * back;
        }

        return frontHit || backHit;
    }

    /// <summary>
    /// Checks if the hologram is stuck inside another object to its left or right and adjusts its position
    /// </summary>
    /// <param name="pos">Center of the object</param>
    /// <param name="xExtent">The extent of the object along the x axis</param>
    /// <param name="layer">The layer that the Raycast uses</param>
    /// <returns>True if the hologram was stuck inside an object</returns>
    private bool MoveLeftAndRight(ref Vector3 pos, float xExtent, int layer)
    {
        float add = 0.0001f;
        float right = -1;
        float left = -1;
        RaycastHit hitInfo;

        // ray from center to right
        bool rightHit = Physics.Raycast(pos + transform.right * -1 * add, transform.right, out hitInfo, xExtent, layer);
        if (rightHit)
            right = xExtent - hitInfo.distance;

        // ray from center to left
        bool leftHit = Physics.Raycast(pos + transform.right * add, transform.right * -1, out hitInfo, xExtent, layer);
        if (leftHit)
            left = xExtent - hitInfo.distance;

        if (rightHit || leftHit)
        {
            if (leftHit && !rightHit) //left
                pos += transform.right * left;
            else if (rightHit && !leftHit) //right
                pos += transform.right * -1 * right;
            else if (left < right) //left
                pos += transform.right * left;
            else //right
                pos += transform.right * -1 * right;
        }

        return rightHit || leftHit;
    }

    /// <summary>
    /// Does all the stuff necessary to finish the placing
    /// </summary>
    private void FinishMoving()
    {
        this.gameObject.layer = oldLayer; // reset the layer when finished moving the hologram
        HideMenu();
        HideBoundingBox();
        IsMoving = false;
        IsRotating = false;

        moving = false;
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (!IsMoving && !IsRotating)
        {
            ShowMenu();
            ShowBoundingBox();
        }
        else if (IsMoving)
        {
            if (placeable)
                FinishMoving();
        }
        else
        {
            HideMenu();
            HideBoundingBox();
            IsMoving = false;
            IsRotating = false;
        }
    }

    public void StartMove()
    {
        if (!IsMoving)
        {
            ShowBoundingBox();
            IsMoving = true;

            var v1 = Camera.main.transform.forward;
            var v2 = transform.forward;

            // Ignore the y axis
            v1.y = 0;
            v2.y = 0;

            // Get angle between the hologram and the camera
            rotationAngle = Vector3.SignedAngle(v1, v2, Vector3.up);

            oldLayer = this.gameObject.layer;
            this.gameObject.layer = ignoreLayer;

            HideMenu();

            moving = true;
        }
    }

    public void StartRotate()
    {
        IsRotating = true;

        HideMenu();
    }

    public void Cancel()
    {
        HideMenu();
        HideBoundingBox();
        IsMoving = false;
        IsRotating = false;
    }

    public void OnManipulationStarted(ManipulationEventData eventData)
    {
        // throw new System.NotImplementedException();
    }


    // Credits: https://github.com/Microsoft/MixedRealityToolkit-Unity/issues/448#issuecomment-270359969
    public void OnManipulationUpdated(ManipulationEventData eventData)
    {
        if (!IsRotating)
            return;

        float multiplier = 1.0f;
        float cameraLocalYRotation = Camera.main.transform.localRotation.eulerAngles.y;

        if (cameraLocalYRotation > 270 || cameraLocalYRotation < 90)
            multiplier = -1.0f;

        var rotation = new Vector3(0, eventData.CumulativeDelta.x * multiplier, 0);

        transform.Rotate(rotation * speed, Space.World);
    }

    public void OnManipulationCompleted(ManipulationEventData eventData)
    {
        // throw new System.NotImplementedException();
    }

    public void OnManipulationCanceled(ManipulationEventData eventData)
    {
        // throw new System.NotImplementedException();
    }
}