using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    public static float GetNoiseValue(int x, int y, NoiseSettings settings) {
        float noiseValue;

        System.Random rng = new System.Random(VoxelData.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];


        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++) {
            float offsetX = rng.Next(-100000, 100000);
            float offsetY = rng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        if (settings.scale <= 0) {
            settings.scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfSize = VoxelData.worldSizeInVoxels / 2f;

        amplitude = 1;
        frequency = 1;
        float noiseHeight = 0;


        for (int i = 0; i < settings.octaves; i++) {
            float sampleX = (x - halfSize + octaveOffsets[i].x) / settings.scale * frequency;
            float sampleY = (y - halfSize + octaveOffsets[i].y) / settings.scale * frequency;

            float perlinValue = (Mathf.PerlinNoise(sampleX, sampleY) * 2) - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }

        if (noiseHeight > maxNoiseHeight) {
            maxNoiseHeight = noiseHeight;
        } else if (noiseHeight < minNoiseHeight) {
            minNoiseHeight = noiseHeight;
        }

        float normalizedHeight = (noiseHeight + 1) / (2f * maxPossibleHeight);
        noiseValue = normalizedHeight;

        return noiseValue;
    }
}

[System.Serializable]
public struct NoiseSettings {
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
}