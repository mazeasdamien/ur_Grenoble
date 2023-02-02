using System;
using System.Collections.Generic;
using UnityEngine;

public static class UR_motion_toolkit
{
    #region Inverse Kinematics
    public static double[,] DH_matrix_UR16e = new double[6, 3] {
        { 0, Mathf.PI / 2.0, 0.1807 },
        { -0.4784, 0, 0 },
        { -0.36, 0, 0 },
        { 0, Mathf.PI / 2.0, 0.17415 },
        { 0, -Mathf.PI / 2.0, 0.11985},
        { 0, 0,0.26585}}; //0.11655

    public static Matrix4x4 Transform_matrix(int n, double[,] e)
    {
        n--;
        var ztheta = Matrix4x4.identity;
        ztheta.m00 = (float)Math.Cos(e[0, n]);
        ztheta.m01 = -(float)Math.Sin(e[0, n]);
        ztheta.m10 = (float)Math.Sin(e[0, n]);
        ztheta.m11 = (float)Math.Cos(e[0, n]);

        var zd = Matrix4x4.identity;
        zd.m23 = (float)DH_matrix_UR16e[n, 2];

        var xa = Matrix4x4.identity;
        xa.m03 = (float)DH_matrix_UR16e[n, 0];

        var xalpha = Matrix4x4.identity;
        xalpha.m11 = (float)Math.Cos(DH_matrix_UR16e[n, 1]);
        xalpha.m12 = -(float)Math.Sin(DH_matrix_UR16e[n, 1]);
        xalpha.m21 = (float)Math.Sin(DH_matrix_UR16e[n, 1]);
        xalpha.m22 = (float)Math.Cos(DH_matrix_UR16e[n, 1]);

        return ztheta * zd * xa * xalpha;
    }

    public static double[,] Inverse_kinematic_solution(Matrix4x4 transform_matrix_unity)
    {

        double[,] theta = new double[6, 8];

        Vector4 P05 = transform_matrix_unity * new Vector4()
        {
            x = 0,
            y = 0,
            z = -(float)DH_matrix_UR16e[5, 2],
            w = 1
        }; ;
        float psi = Mathf.Atan2(P05[1], P05[0]);
        float phi = Mathf.Acos((float)((DH_matrix_UR16e[1, 2] + DH_matrix_UR16e[3, 2] + DH_matrix_UR16e[2, 2]) / Mathf.Sqrt(Mathf.Pow(P05[0], 2) + Mathf.Pow(P05[1], 2))));

        theta[0, 0] = psi + phi + Mathf.PI / 2;
        theta[0, 1] = psi + phi + Mathf.PI / 2;
        theta[0, 2] = psi + phi + Mathf.PI / 2;
        theta[0, 3] = psi + phi + Mathf.PI / 2;
        theta[0, 4] = psi - phi + Mathf.PI / 2;
        theta[0, 5] = psi - phi + Mathf.PI / 2;
        theta[0, 6] = psi - phi + Mathf.PI / 2;
        theta[0, 7] = psi - phi + Mathf.PI / 2;

        for (int i = 0; i < 8; i += 4)
        {
            double t5 = (transform_matrix_unity[0, 3] * Mathf.Sin((float)theta[0, i]) - transform_matrix_unity[1, 3] * Mathf.Cos((float)theta[0, i]) - (DH_matrix_UR16e[1, 2] + DH_matrix_UR16e[3, 2] + DH_matrix_UR16e[2, 2])) / DH_matrix_UR16e[5, 2];
            float th5;
            if (1 >= t5 && t5 >= -1)
            {
                th5 = Mathf.Acos((float)t5);
            }
            else
            {
                th5 = 0;
            }

            if (i == 0)
            {
                theta[4, 0] = th5;
                theta[4, 1] = th5;
                theta[4, 2] = -th5;
                theta[4, 3] = -th5;
            }
            else
            {
                theta[4, 4] = th5;
                theta[4, 5] = th5;
                theta[4, 6] = -th5;
                theta[4, 7] = -th5;
            }
        }

        Matrix4x4 tmu_inverse = transform_matrix_unity.inverse;
        float th0 = Mathf.Atan2((-tmu_inverse[1, 0] * Mathf.Sin((float)theta[0, 0]) + tmu_inverse[1, 1] * Mathf.Cos((float)theta[0, 0])), (tmu_inverse[0, 0] * Mathf.Sin((float)theta[0, 0]) - tmu_inverse[0, 1] * Mathf.Cos((float)theta[0, 0])));
        float th2 = Mathf.Atan2((-tmu_inverse[1, 0] * Mathf.Sin((float)theta[0, 2]) + tmu_inverse[1, 1] * Mathf.Cos((float)theta[0, 2])), (tmu_inverse[0, 0] * Mathf.Sin((float)theta[0, 2]) - tmu_inverse[0, 1] * Mathf.Cos((float)theta[0, 2])));
        float th4 = Mathf.Atan2((-tmu_inverse[1, 0] * Mathf.Sin((float)theta[0, 4]) + tmu_inverse[1, 1] * Mathf.Cos((float)theta[0, 4])), (tmu_inverse[0, 0] * Mathf.Sin((float)theta[0, 4]) - tmu_inverse[0, 1] * Mathf.Cos((float)theta[0, 4])));
        float th6 = Mathf.Atan2((-tmu_inverse[1, 0] * Mathf.Sin((float)theta[0, 6]) + tmu_inverse[1, 1] * Mathf.Cos((float)theta[0, 6])), (tmu_inverse[0, 0] * Mathf.Sin((float)theta[0, 6]) - tmu_inverse[0, 1] * Mathf.Cos((float)theta[0, 6])));

        theta[5, 0] = th0;
        theta[5, 1] = th0;
        theta[5, 2] = th2;
        theta[5, 3] = th2;
        theta[5, 4] = th4;
        theta[5, 5] = th4;
        theta[5, 6] = th6;
        theta[5, 7] = th6;

        for (int i = 0; i <= 7; i += 2)
        {
            double[,] t1 = new double[1, 6];
            t1[0, 0] = theta[0, i];
            t1[0, 1] = theta[1, i];
            t1[0, 2] = theta[2, i];
            t1[0, 3] = theta[3, i];
            t1[0, 4] = theta[4, i];
            t1[0, 5] = theta[5, i];
            Matrix4x4 T01 = Transform_matrix(1, t1);
            Matrix4x4 T45 = Transform_matrix(5, t1);
            Matrix4x4 T56 = Transform_matrix(6, t1);
            Matrix4x4 T14 = T01.inverse * transform_matrix_unity * (T45 * T56).inverse;

            Vector4 P13 = T14 * new Vector4()
            {
                x = 0,
                y = (float)-DH_matrix_UR16e[3, 2],
                z = 0,
                w = 1
            };
            double t3 = (Mathf.Pow(P13[0], 2) + Mathf.Pow(P13[1], 2) - Mathf.Pow((float)DH_matrix_UR16e[1, 0], 2) - Mathf.Pow((float)DH_matrix_UR16e[2, 0], 2)) / (2 * DH_matrix_UR16e[1, 0] * DH_matrix_UR16e[2, 0]);
            double th3;
            if (1 >= t3 && t3 >= -1)
            {
                th3 = Mathf.Acos((float)t3);
            }
            else
            {
                th3 = 0;
            }
            theta[2, i] = th3;
            theta[2, i + 1] = -th3;
        }

        for (int i = 0; i < 8; i++)
        {
            double[,] t1 = new double[1, 6];
            t1[0, 0] = theta[0, i];
            t1[0, 1] = theta[1, i];
            t1[0, 2] = theta[2, i];
            t1[0, 3] = theta[3, i];
            t1[0, 4] = theta[4, i];
            t1[0, 5] = theta[5, i];
            Matrix4x4 T01 = Transform_matrix(1, t1);
            Matrix4x4 T45 = Transform_matrix(5, t1);
            Matrix4x4 T56 = Transform_matrix(6, t1);
            Matrix4x4 T14 = T01.inverse * transform_matrix_unity * (T45 * T56).inverse;

            Vector4 P13 = T14 * new Vector4()
            {
                x = 0,
                y = (float)-DH_matrix_UR16e[3, 2],
                z = 0,
                w = 1
            };

            theta[1, i] = Mathf.Atan2(-P13[1], -P13[0]) - Mathf.Asin((float)(-DH_matrix_UR16e[2, 0] * Mathf.Sin((float)theta[2, i]) / Mathf.Sqrt(Mathf.Pow(P13[0], 2) + Mathf.Pow(P13[1], 2))));

            double[,] t2 = new double[1, 6];
            t2[0, 0] = theta[0, i];
            t2[0, 1] = theta[1, i];
            t2[0, 2] = theta[2, i];
            t2[0, 3] = theta[3, i];
            t2[0, 4] = theta[4, i];
            t2[0, 5] = theta[5, i];
            Matrix4x4 T32 = Transform_matrix(3, t2).inverse;
            Matrix4x4 T21 = Transform_matrix(2, t2).inverse;
            Matrix4x4 T34 = T32 * T21 * T14;
            theta[3, i] = Mathf.Atan2(T34[1, 0], T34[0, 0]);
        }
        return theta;
    }
    #endregion

    public static Matrix4x4 GetTransformMatrix(Transform controller)
    {
        return Matrix4x4.TRS(new Vector3(controller.localPosition.x, controller.localPosition.y, controller.localPosition.z), Quaternion.Euler(controller.localEulerAngles.x, controller.localEulerAngles.y, controller.localEulerAngles.z), new Vector3(1, 1, 1));
    }

    public static List<string> DisplaySolutions(double[,] solutions)
    {
        List<string> info = new List<string>();

        for (int colonnes = 0; colonnes < 8; colonnes++)
        {
            if ((double.IsNaN(solutions[0, colonnes])) == false
                && ((double.IsNaN(solutions[1, colonnes])) == false
                && ((double.IsNaN(solutions[2, colonnes])) == false
                && ((double.IsNaN(solutions[3, colonnes])) == false
                && ((double.IsNaN(solutions[4, colonnes])) == false
                && ((double.IsNaN(solutions[5, colonnes])) == false))))))
            {
                info.Add(new string(
                    $"{Math.Round(Mathf.Rad2Deg * (solutions[0, colonnes]), 2)} " +
                    $"| {Math.Round(Mathf.Rad2Deg * (solutions[1, colonnes]), 2)} " +
                    $"| {Math.Round(Mathf.Rad2Deg * (solutions[2, colonnes]), 2)} " +
                    $"| {Math.Round(Mathf.Rad2Deg * (solutions[3, colonnes]), 2)} " +
                    $"| {Math.Round(Mathf.Rad2Deg * (solutions[4, colonnes]), 2)} " +
                    $"| {Math.Round(Mathf.Rad2Deg * (solutions[5, colonnes]), 2)}"));
            }
            else
            {
                info.Add(new string("NON DISPONIBLE"));
            }
        }

        return info;
    }

    public static void SetSolution(List<string> info, double[,] solutions, int solution_number, List<Transform> robot)
    {
        if (info[solution_number] != "NON DISPONIBLE")
        {
            robot[0].localEulerAngles = new Vector3(0, 0, -(float)(Mathf.Rad2Deg * solutions[0, solution_number]));
            robot[1].localEulerAngles = new Vector3(-90, 0, -(float)(Mathf.Rad2Deg * solutions[1, solution_number]));
            robot[2].localEulerAngles = new Vector3(0, 0, -(float)(Mathf.Rad2Deg * solutions[2, solution_number]));
            robot[3].localEulerAngles = new Vector3(0, 0, -(float)(Mathf.Rad2Deg * solutions[3, solution_number]));
            robot[4].localEulerAngles = new Vector3(-90, 0, -(float)(Mathf.Rad2Deg * solutions[4, solution_number]));
            robot[5].localEulerAngles = new Vector3(90, 0, -(float)(Mathf.Rad2Deg * solutions[5, solution_number]));
        }
        else
        {
            Debug.Log("NO SOLUTION");
        }
    }
}