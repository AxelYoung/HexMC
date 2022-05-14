using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData {

    public static readonly int seed = 53;

    public static readonly float voxelSize = 0.5373f;

    public static readonly int worldChunkSize = 16;
    public static readonly int chunkSizeInVoxels = 16;
    public static readonly int viewDistance = 8;
    public static readonly int worldSizeInVoxels = worldChunkSize * chunkSizeInVoxels;
    public static readonly float chunkWidth = VoxelData.chunkSizeInVoxels * VoxelData.voxelSize * 1.5f;
    public static readonly float chunkDepth = VoxelData.chunkSizeInVoxels * VoxelData.voxelSize * Mathf.Sqrt(3);
    public static readonly Vector2 worldSize = new Vector2(chunkWidth * worldChunkSize, chunkDepth * worldChunkSize);
    public static readonly int chunkHeight = 64;


    public static Vector2[] hexVerticies = null;
    public static Vector2[] hexUVs = null;
    public static Vector2[] hexSideUVs = null;

    // Returns verticies of a hexagon
    public static Vector2[] CalculateHexagon(float size) {
        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++) {
            float angleDegrees = 60 * i;
            float angleRadians = Mathf.PI / 180 * angleDegrees;
            Vector3 point = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            points[i] = point * size;
        }
        return points;
    }

    // Returns triangle order of hexagon
    public readonly static int[] topHexagonalFace = new int[] {
        5, 4, 0,
        4, 3, 0,
        3, 2, 0,
        2, 1, 0
    };

    public readonly static int[] bottomHexagonalFace = new int[] {
        0, 4, 5,
        0, 3, 4,
        0, 2, 3,
        0, 1, 2
    };

    public readonly static int[] sideTriangles = new int[] {
        0, 3, 1,
        0, 2, 3
    };

    // Returns side uv positions
    public static Vector2[] CalculateSideUVs() {
        Vector2[] uvs = new Vector2[] {
            new Vector2(hexUVs[2].x, 0),
            new Vector2(hexUVs[1].x, 0),
            new Vector2(hexUVs[2].x, 1),
            new Vector2(hexUVs[1].x, 1)
            };
        return uvs;
    }

}