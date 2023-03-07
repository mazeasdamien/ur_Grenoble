using DDS_protocol;
using System.Collections.Generic;
using UnityEngine;

public class UR_target : MonoBehaviour
{
    public Transform target;
    public Transform ik;
    public Transform ikDebug;
    public SIMU_DirectTeleop_SUB sIMU_DirectTeleop_SUB;
    public UR_Joints_Publisher UR_Joints_Publisher;
    public float speed = 0.1f;
    public float speedr = 0.01f;

    private List<string> IK_Solutions = new List<string>();
    public List<double> goodSolution = new List<double>();

    void Update()
    {
        float step = speed * Time.deltaTime;
        //
        if (sIMU_DirectTeleop_SUB.isVR)
        {
            ik.position = Vector3.MoveTowards(ik.position, target.position, step);
            ik.rotation = Quaternion.Lerp(ik.rotation, target.rotation, speedr);
        }
        else if (sIMU_DirectTeleop_SUB.isVR == false && UR_Joints_Publisher.robotstate == 1)
        {
            ik.position = Vector3.MoveTowards(ik.position, target.position, step);
            ik.rotation = Quaternion.Lerp(ik.rotation, target.rotation, speedr);
        }
        else if (sIMU_DirectTeleop_SUB.isVR == false && UR_Joints_Publisher.robotstate == 2)
        {
            ik.position = Vector3.MoveTowards(ik.position, target.position, step);
            ik.rotation = Quaternion.Lerp(ik.rotation, target.rotation, speedr);
        }
        else if (sIMU_DirectTeleop_SUB.isVR == false && UR_Joints_Publisher.robotstate == 3)
        {
            //
            // a verif
            //
            //
            ik.position = ikDebug.position;
            ik.rotation = ikDebug.rotation;
        }
        //

        Matrix4x4 transform_matrix = UR_motion_toolkit.GetTransformMatrix(ik);

        Matrix4x4 mt = Matrix4x4.identity;
        mt.m11 = -1;
        Matrix4x4 mt_inverse = mt.inverse;
        Matrix4x4 result = mt * transform_matrix * mt_inverse;

        double[,] solutions = UR_motion_toolkit.Inverse_kinematic_solution(result);
        IK_Solutions.Clear();
        IK_Solutions = UR_motion_toolkit.DisplaySolutions(solutions);

        goodSolution.Clear();
        goodSolution.Add(solutions[0, 5]);
        goodSolution.Add(solutions[1, 5]);
        goodSolution.Add(solutions[2, 5]);
        goodSolution.Add(solutions[3, 5]);
        goodSolution.Add(solutions[4, 5]);
        goodSolution.Add(solutions[5, 5]);
    }
}