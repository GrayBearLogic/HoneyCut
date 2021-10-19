using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnHeight : MonoBehaviour
{
    public float height;

    private void Update()
    {
        if (transform.position.y <= height)
        {
            Destroy(gameObject);
        }
    }
}
