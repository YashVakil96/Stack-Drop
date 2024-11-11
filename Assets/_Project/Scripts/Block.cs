using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(BoxCollider), typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Add impact effects here
        // e.g., play sound effects, spawn particles, screen shake
    }
}