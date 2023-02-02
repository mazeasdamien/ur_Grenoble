using DDS_protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UR_velocity : MonoBehaviour
{
    public List<Transform> robotParts = new List<Transform>();

    public List<float> velocityInRadPerSecond = new List<float>();
    public List<float> previousAngle = new List<float>();

    void Update()
    {
        if (previousAngle[0] != 0)
        {
            velocityInRadPerSecond[0] = (robotParts[0].localEulerAngles.z - previousAngle[0]) * Mathf.Deg2Rad / Time.deltaTime;
        }
        if (previousAngle[1] != 0)
        {
            velocityInRadPerSecond[1] = (robotParts[1].localEulerAngles.y - previousAngle[1]) * Mathf.Deg2Rad / Time.deltaTime;
        }
        if (previousAngle[2] != 0)
        {
            velocityInRadPerSecond[2] = (robotParts[2].localEulerAngles.z - previousAngle[2]) * Mathf.Deg2Rad / Time.deltaTime;
        }
        if (previousAngle[3] != 0)
        {
            velocityInRadPerSecond[3] = (robotParts[3].localEulerAngles.z - previousAngle[3]) * Mathf.Deg2Rad / Time.deltaTime;
        }
        if (previousAngle[4] != 0)
        {
            velocityInRadPerSecond[4] = (robotParts[4].localEulerAngles.y - previousAngle[4]) * Mathf.Deg2Rad / Time.deltaTime;
        }
        if (previousAngle[5] != 0)
        {
            velocityInRadPerSecond[5] = (robotParts[5].localEulerAngles.y - previousAngle[5]) * Mathf.Deg2Rad / Time.deltaTime;
        }

        previousAngle[0] = robotParts[0].localEulerAngles.z;
        previousAngle[1] = robotParts[1].localEulerAngles.y;
        previousAngle[2] = robotParts[2].localEulerAngles.z;
        previousAngle[3] = robotParts[3].localEulerAngles.z;
        previousAngle[4] = robotParts[4].localEulerAngles.y;
        previousAngle[5] = robotParts[5].localEulerAngles.y;
    }
}
