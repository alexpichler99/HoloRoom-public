using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateNewHologram : MonoBehaviour, IInputClickHandler
{
    public Hologram prefab;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (!Hologram.Moving && prefab != null)
        {
            var hologram = Instantiate(prefab);

            if (hologram != null)
            {
                hologram.RotateToCamera(); // Make the hologram face the camera before we start to move
                hologram.StartMove();
            }
        }
    }
}