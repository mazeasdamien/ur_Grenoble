using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collision_detection : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "robotCollider")
        {
            Debug.Log("robot touching");
        }
    }
}