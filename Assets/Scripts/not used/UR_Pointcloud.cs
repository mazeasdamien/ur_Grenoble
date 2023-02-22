using DDS_protocol;
using Rti.Dds.Publication;
using Rti.Dds.Subscription;
using Rti.Types.Dynamic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using K4os.Compression.LZ4;
using Unity.VisualScripting;

public class UR_Pointcloud : MonoBehaviour
{
    public List<VisualEffect> effect = new List<VisualEffect>();
    public UR_CameraColor color;
    public UR_CameraDepth depth;

    Mesh mesh;
    readonly List<Vector3> vertices = new();
    readonly List<Color32> colors = new();
    readonly List<int> indices = new();

    private void Start()
    {
        mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
        };

    }

    void Update()
    {
        if (color.indexColor == depth.indexDepth)
        {
            vertices.Clear();
            colors.Clear();
            indices.Clear();
            mesh.Clear();

            int num = color.Colordata.Length / 3;
            for (int i = 0; i < num; i++)
            {
                indices.Add(i);
            }
            mesh.vertices = new Vector3[num];
            mesh.colors32 = new Color32[num];

            mesh.SetIndices(indices, MeshTopology.Points, 0);

            for (int i = 0; i < num * 3; i += 3)
            {
                vertices.Add(new Vector3
                {
                    x = depth.DepthData[i],
                    y = depth.DepthData[i + 1],
                    z = depth.DepthData[i + 2]
                });
                colors.Add(new Color32
                {
                    r = color.Colordata[i],
                    g = color.Colordata[i + 1],
                    b = color.Colordata[i + 2]
                });
            }

            mesh.vertices = vertices.ToArray();
            mesh.colors32 = colors.ToArray();

            effect[0].SetMesh("RemoteData", mesh);

        }
    }
}
