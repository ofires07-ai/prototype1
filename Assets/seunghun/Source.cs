using System;
using UnityEngine;

public class Source : MonoBehaviour
{
    public float capacity = 50f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (capacity <= 0)
        {
            Destroy(gameObject);
        }
    }
    
}
