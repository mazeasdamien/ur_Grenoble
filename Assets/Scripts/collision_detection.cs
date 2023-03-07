using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collision_detection : MonoBehaviour
{
    public bool isColliding;
    public CollisionManager collisionManager;
    public List<string> collided;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "robotCollider")
        {
            collided.Add(other.gameObject.name);

            if (other.name == "Base")
            {
                if (!collisionManager.isBaseColliding)
                {
                    collisionManager.count++;
                    collisionManager.isBaseColliding = true;
                }
            }
            if (other.name == "Shoulder")
            {
                if (!collisionManager.isShoulderColliding)
                {
                    collisionManager.count++;
                    collisionManager.isShoulderColliding = true;
                }
            }
            if (other.name == "Elbow")
            {
                if (!collisionManager.isElbowColliding)
                {
                    collisionManager.count++;
                    collisionManager.isElbowColliding = true;
                }
            }
            if (other.name == "Wrist1")
            {
                if (!collisionManager.isWrist1Colliding)
                {
                    collisionManager.count++;
                    collisionManager.isWrist1Colliding = true;
                }
            }
            if (other.name == "Wrist2")
            {
                if (!collisionManager.isWrist2Colliding)
                {
                    collisionManager.count++;
                    collisionManager.isWrist2Colliding = true;
                }
            }
            if (other.name == "Wrist3")
            {
                if (!collisionManager.isWrist3Colliding)
                {
                    collisionManager.count++;
                    collisionManager.isWrist3Colliding = true;
                }
            }
            if (other.name == "cam")
            {
                if (!collisionManager.isCamColliding)
                {
                    collisionManager.count++;
                    collisionManager.isCamColliding = true;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "robotCollider")
        {
            collided.Remove(other.gameObject.name);

            if (other.name == "Base")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Base")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isBaseColliding = false;
                }
            }
            if (other.name == "Shoulder")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Shoulder")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isShoulderColliding = false;
                }
            }
            if (other.name == "Elbow")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Elbow")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isElbowColliding = false;
                }
            }
            if (other.name == "Wrist1")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Wrist1")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isWrist1Colliding = false;
                }
            }
            if (other.name == "Wrist2")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Wrist2")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isWrist2Colliding = false;

                }
            }
            if (other.name == "Wrist3")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "Wrist3")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isWrist3Colliding = false;

                }
            }
            if (other.name == "cam")
            {
                bool isStillHere = false;
                foreach (collision_detection c in collisionManager.collision_Detections)
                {
                    foreach (string item in c.collided)
                    {
                        if (item == "cam")
                        {
                            isStillHere = true;
                        }
                    }
                }
                if (!isStillHere)
                {
                    collisionManager.isCamColliding = false;

                }
            }
        }
    }
}