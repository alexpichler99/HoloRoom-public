using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColorOfChildren : MonoBehaviour
{
    /// <summary>
    /// Changes the color of all child objects
    /// </summary>
    /// <param name="color"></param>
    public void ChangeColor(Color color)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }    
}
