using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeScaleOfObjectAbsolute : MonoBehaviour
{
    private Vector3 localScale;

    public bool absoluteX = false;
    public bool absoluteY = false;
    public bool absoluteZ = false;

	// Use this for initialization
	void Start ()
    {
        localScale = transform.localScale;
	}

    private float tolerance = 0.01f;
	
	// Update is called once per frame
	void Update ()
    {
        // Get the parent of the boundingbox
        var parent = this.gameObject.transform.parent.parent.localScale;

        // Use the size of the BoxCollider as scale
        parent = this.gameObject.transform.parent.parent.GetComponent<BoxCollider>().size;

        this.gameObject.transform.parent.localScale = parent;
        this.gameObject.transform.parent.localPosition = new Vector3(this.gameObject.transform.parent.parent.GetComponent<BoxCollider>().center.x,
            this.gameObject.transform.parent.parent.GetComponent<BoxCollider>().center.y, this.gameObject.transform.parent.parent.GetComponent<BoxCollider>().center.z);

        float x = 1, y = 1, z = 1;

        if (absoluteX)
            x = 1 / parent.x;
        if (absoluteY)
            y = 1 / parent.y;
        if (absoluteZ)
            z = 1 / parent.z;

        this.transform.localScale = new Vector3(x * localScale.x + 
            (!absoluteX ? (float)((1 / parent.x) * tolerance) : 0), 
            y * localScale.y + (!absoluteY ? (float)((1 / parent.y) * tolerance) : 0)
            , z * localScale.z + (!absoluteZ ? (float)((1 / parent.z) * tolerance) : 0));
	}
}
