using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Chunk {

    public GameObject gameObject;
    public GameObject foliageGameObject;

    MeshFilter meshFilter;
    MeshFilter foliageMeshFilter;

    MeshCollider meshCollider;
    MeshCollider foliageMeshCollider;

    MeshRenderer meshRenderer;
    MeshRenderer foliageMeshRenderer;

    WorldGeneration worldGeneration;
    public ChunkCoordinates chunkCoordinates;

    List<Vector3> meshVerticies = new List<Vector3>();
    List<int> meshTriangles = new List<int>();
    List<Vector2> meshUVs = new List<Vector2>();

    List<Vector3> foliageMeshVerticies = new List<Vector3>();
    List<int> foliageMeshTriangles = new List<int>();
    List<Vector2> foliageMeshUVs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.chunkSizeInVoxels, VoxelData.chunkHeight, VoxelData.chunkSizeInVoxels];

    bool populatedVoxelMap = false;

    public Queue<VoxelModification> modifications = new Queue<VoxelModification>();

    bool active;

    public Chunk(WorldGeneration worldGeneration, ChunkCoordinates chunkCoordinates) {
        this.worldGeneration = worldGeneration;
        this.chunkCoordinates = chunkCoordinates;
    }

    public void Initialize() {
        gameObject = new GameObject();
        gameObject.layer = worldGeneration.meshLayer;
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = worldGeneration.worldMaterial;

        gameObject.transform.parent = worldGeneration.transform;
        Vector2 chunkCoordWorldPosition = new Vector2(chunkCoordinates.x * VoxelData.chunkWidth, chunkCoordinates.z * VoxelData.chunkDepth);
        gameObject.transform.position = new Vector3(chunkCoordWorldPosition.x, 0, chunkCoordWorldPosition.y);
        gameObject.transform.name = "Chunk (" + chunkCoordinates.x + ", " + chunkCoordinates.z + ")";

        foliageGameObject = new GameObject();
        foliageGameObject.layer = worldGeneration.foliageLayer;
        foliageMeshFilter = foliageGameObject.AddComponent<MeshFilter>();
        foliageMeshCollider = foliageGameObject.AddComponent<MeshCollider>();
        foliageMeshRenderer = foliageGameObject.AddComponent<MeshRenderer>();
        foliageMeshRenderer.material = worldGeneration.foliageMaterial;

        foliageGameObject.transform.parent = worldGeneration.transform;
        Vector2 foliageChunkCoordWorldPosition = new Vector2(chunkCoordinates.x * VoxelData.chunkWidth, chunkCoordinates.z * VoxelData.chunkDepth);
        foliageGameObject.transform.position = new Vector3(foliageChunkCoordWorldPosition.x, 0, foliageChunkCoordWorldPosition.y);
        foliageGameObject.transform.name = "Foliage Chunk (" + chunkCoordinates.x + ", " + chunkCoordinates.z + ")";

        FillVoxelMap();
    }

    void FillVoxelMap() {
        // Fill map
        for (int x = 0; x < VoxelData.chunkSizeInVoxels; x++) {
            for (int y = 0; y < VoxelData.chunkHeight; y++) {
                for (int z = 0; z < VoxelData.chunkSizeInVoxels; z++) {
                    voxelMap[x, y, z] = worldGeneration.GenerateVoxelID(Coordinates.LocalToGlobalOffset(new Coordinates(x, y, z), chunkCoordinates));
                }
            }
        }
        populatedVoxelMap = true;

        lock (worldGeneration.ChunkUpdateThreadLock) {
            worldGeneration.chunksToUpdate.Add(this);
        }
    }

    void GenerateVoxel(Coordinates coordinates, byte voxelType) {

        Vector3 worldCoordinates = coordinates.worldPosition;
        if (voxelType != Voxels.Rose.ID && voxelType != Voxels.TallGrass.ID && voxelType != Voxels.ShortGrass.ID) {
            // Bottom face
            Vector3[] bottomVerticies = relativeVoxelVerticies(worldCoordinates);
            if (transparentVoxelAtPosition(coordinates.bottomNeighbor)) {
                meshVerticies.AddRange(bottomVerticies);
                meshTriangles.AddRange(relativeVoxelTriangles(false));
                meshUVs.AddRange(faceUVsFromIndex(new Vector2(voxelType - 1, 0)));
            }
            // Top face
            Vector3[] topVerticies = relativeVoxelVerticies(worldCoordinates + Vector3.up);
            if (transparentVoxelAtPosition(coordinates.topNeighbor)) {
                meshVerticies.AddRange(topVerticies);
                meshTriangles.AddRange(relativeVoxelTriangles(true));
                meshUVs.AddRange(faceUVsFromIndex(new Vector2(voxelType - 1, 2)));
            }
            // Sides
            for (int i = 0; i < 6; i++) {
                if (transparentVoxelAtPosition(coordinates.sideNeighbors[i])) {
                    int[] sideVerticies = new int[4];
                    meshVerticies.Add(bottomVerticies[i]);
                    sideVerticies[0] = meshVerticies.Count - 1;
                    meshVerticies.Add(bottomVerticies[i + 1 < 6 ? i + 1 : 0]);
                    sideVerticies[1] = meshVerticies.Count - 1;
                    meshVerticies.Add(topVerticies[i]);
                    sideVerticies[2] = meshVerticies.Count - 1;
                    meshVerticies.Add(topVerticies[i + 1 < 6 ? i + 1 : 0]);
                    sideVerticies[3] = meshVerticies.Count - 1;
                    foreach (int triangle in VoxelData.sideTriangles) {
                        meshTriangles.Add(sideVerticies[triangle]);
                    }
                    meshUVs.AddRange(sideUVsFromIndex(new Vector2(voxelType - 1, 1)));
                }
            }
        } else {

            int uv = voxelType == Voxels.TallGrass.ID ? 0 : voxelType == Voxels.ShortGrass.ID ? 1 : 2;

            float radius = (Mathf.Sqrt(3) * VoxelData.voxelSize) / 2f;
            float angle = Mathf.PI * 2;
            Vector3 randomPoint = worldCoordinates + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            float oppositeAngle = angle + Mathf.PI;
            Vector3 oppositePoint = worldCoordinates + new Vector3(Mathf.Cos(oppositeAngle) * radius, 0, Mathf.Sin(oppositeAngle) * radius);

            foliageMeshVerticies.Add(randomPoint);
            foliageMeshVerticies.Add(oppositePoint);
            foliageMeshVerticies.Add(randomPoint + (Vector3.up * radius * 2));
            foliageMeshVerticies.Add(oppositePoint + (Vector3.up * radius * 2));

            foliageMeshUVs.AddRange(new Vector2[] { new Vector2((uv * (1f / 3f)), 0), new Vector2(((uv + 1) * (1f / 3f)), 0), new Vector2((uv * (1f / 3f)), 1), new Vector2(((uv + 1) * (1f / 3f)), 1) });

            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 4, foliageMeshVerticies.Count - 2, foliageMeshVerticies.Count - 1 });
            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 1, foliageMeshVerticies.Count - 3, foliageMeshVerticies.Count - 4 });


            foliageMeshVerticies.Add(randomPoint);
            foliageMeshVerticies.Add(oppositePoint);
            foliageMeshVerticies.Add(randomPoint + (Vector3.up * radius * 2));
            foliageMeshVerticies.Add(oppositePoint + (Vector3.up * radius * 2));

            foliageMeshUVs.AddRange(new Vector2[] { new Vector2((uv * (1f / 3f)), 0), new Vector2(((uv + 1) * (1f / 3f)), 0), new Vector2((uv * (1f / 3f)), 1), new Vector2(((uv + 1) * (1f / 3f)), 1) });

            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 1, foliageMeshVerticies.Count - 2, foliageMeshVerticies.Count - 4 });
            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 4, foliageMeshVerticies.Count - 3, foliageMeshVerticies.Count - 1 });

            float secondAngle = angle + (Mathf.PI / 2f);
            Vector3 secondRandomPoint = worldCoordinates + new Vector3(Mathf.Cos(secondAngle) * radius, 0, Mathf.Sin(secondAngle) * radius);
            float secondOppositeAngle = angle - (Mathf.PI / 2f);
            Vector3 secondOppositePoint = worldCoordinates + new Vector3(Mathf.Cos(secondOppositeAngle) * radius, 0, Mathf.Sin(secondOppositeAngle) * radius);

            foliageMeshVerticies.Add(secondRandomPoint);
            foliageMeshVerticies.Add(secondOppositePoint);
            foliageMeshVerticies.Add(secondRandomPoint + (Vector3.up * radius * 2));
            foliageMeshVerticies.Add(secondOppositePoint + (Vector3.up * radius * 2));

            foliageMeshUVs.AddRange(new Vector2[] { new Vector2((uv * (1f / 3f)), 0), new Vector2(((uv + 1) * (1f / 3f)), 0), new Vector2((uv * (1f / 3f)), 1), new Vector2(((uv + 1) * (1f / 3f)), 1) });

            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 4, foliageMeshVerticies.Count - 2, foliageMeshVerticies.Count - 1 });
            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 1, foliageMeshVerticies.Count - 3, foliageMeshVerticies.Count - 4 });

            foliageMeshVerticies.Add(secondRandomPoint);
            foliageMeshVerticies.Add(secondOppositePoint);
            foliageMeshVerticies.Add(secondRandomPoint + (Vector3.up * radius * 2));
            foliageMeshVerticies.Add(secondOppositePoint + (Vector3.up * radius * 2));

            foliageMeshUVs.AddRange(new Vector2[] { new Vector2((uv * (1f / 3f)), 0), new Vector2(((uv + 1) * (1f / 3f)), 0), new Vector2((uv * (1f / 3f)), 1), new Vector2(((uv + 1) * (1f / 3f)), 1) });

            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 1, foliageMeshVerticies.Count - 2, foliageMeshVerticies.Count - 4 });
            foliageMeshTriangles.AddRange(new int[] { foliageMeshVerticies.Count - 4, foliageMeshVerticies.Count - 3, foliageMeshVerticies.Count - 1 });

        }
    }


    bool transparentVoxelAtPosition(Coordinates coordinates) {
        if (voxelInChunk(coordinates)) {
            return Voxels.types[voxelMap[coordinates.x, coordinates.y, coordinates.z]].isTransparent;
        } else return worldGeneration.transparentVoxelAtCoordinates(coordinates, chunkCoordinates);
    }

    bool voxelInChunk(Coordinates coordinates) {
        if (coordinates.x < 0 || coordinates.x >= VoxelData.chunkSizeInVoxels || coordinates.y < 0 || coordinates.y >= VoxelData.chunkHeight || coordinates.z < 0 || coordinates.z >= VoxelData.chunkSizeInVoxels) return false;
        else return true;
    }

    public void EditVoxel(Coordinates coordinates, byte id) {
        voxelMap[coordinates.x, coordinates.y, coordinates.z] = id;
        lock (worldGeneration.ChunkUpdateThreadLock) {
            UpdateSurroundingVoxels(coordinates);
            worldGeneration.chunksToUpdate.Insert(0, this);
        }
    }

    void UpdateSurroundingVoxels(Coordinates coordinates) {
        for (int i = 0; i < 6; i++) {
            if (!voxelInChunk(coordinates.sideNeighbors[i])) {
                Coordinates globalOffset = Coordinates.LocalToGlobalOffset(coordinates.sideNeighbors[i], chunkCoordinates); ;
                if (!worldGeneration.voxelInWorld(globalOffset)) return;
                ChunkCoordinates voxelChunk = new ChunkCoordinates(globalOffset);
                if (!worldGeneration.chunksToUpdate.Contains(worldGeneration.chunkMap[voxelChunk.x, voxelChunk.z])) {
                    worldGeneration.chunksToUpdate.Insert(0, worldGeneration.chunkMap[voxelChunk.x, voxelChunk.z]);
                }
            }
        }
    }

    public void UpdateChunk() {
        while (modifications.Count > 0) {
            VoxelModification mod = modifications.Dequeue();
            Coordinates localOffsetCoordinates = Coordinates.GlobalToLocalOffset(mod.coordinates, chunkCoordinates);
            voxelMap[localOffsetCoordinates.x, localOffsetCoordinates.y, localOffsetCoordinates.z] = mod.id;
        }

        meshVerticies.Clear();
        meshTriangles.Clear();
        meshUVs.Clear();
        foliageMeshVerticies.Clear();
        foliageMeshTriangles.Clear();
        foliageMeshUVs.Clear();

        for (int x = 0; x < VoxelData.chunkSizeInVoxels; x++) {
            for (int y = 0; y < VoxelData.chunkHeight; y++) {
                for (int z = 0; z < VoxelData.chunkSizeInVoxels; z++) {
                    if (Voxels.types[voxelMap[x, y, z]].isSolid) {
                        GenerateVoxel(new Coordinates(x, y, z), voxelMap[x, y, z]);
                    }
                }
            }
        }
        worldGeneration.chunksToDraw.Enqueue(this);
    }

    public void GenerateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = meshVerticies.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.uv = meshUVs.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        Mesh foliage = new Mesh();
        foliage.vertices = foliageMeshVerticies.ToArray();
        foliage.triangles = foliageMeshTriangles.ToArray();
        foliage.uv = foliageMeshUVs.ToArray();
        foliage.RecalculateNormals();
        foliageMeshFilter.mesh = foliage;
        foliageMeshCollider.sharedMesh = foliage;
    }

    Vector3[] relativeVoxelVerticies(Vector3 center) {
        Vector3[] verticies = new Vector3[6];
        for (int i = 0; i < 6; i++) {
            verticies[i] = new Vector3(VoxelData.hexVerticies[i].x + center.x, center.y, VoxelData.hexVerticies[i].y + center.z);
        }
        return verticies;
    }

    int[] relativeVoxelTriangles(bool top) {
        int[] triangles = new int[VoxelData.topHexagonalFace.Length];
        for (int i = 0; i < triangles.Length; i++) {
            triangles[i] = (meshVerticies.Count - 6) + (top ? VoxelData.topHexagonalFace[i] : VoxelData.bottomHexagonalFace[i]);
        }
        return triangles;
    }

    Vector2[] faceUVsFromIndex(Vector2 index) {
        Vector2[] uvs = new Vector2[6];
        for (int i = 0; i < 6; i++) {
            uvs[i] = new Vector2(((VoxelData.hexUVs[i].x + 0.5f + index.x) / Voxels.voxelAmount), ((VoxelData.hexUVs[i].y + 0.5f + index.y) / 3f));
        }
        return uvs;
    }

    Vector2[] sideUVsFromIndex(Vector2 index) {
        Vector2[] uvs = new Vector2[4];
        for (int i = 0; i < 4; i++) {
            uvs[i] = new Vector2(((VoxelData.hexSideUVs[i].x + 0.5f + index.x) / Voxels.voxelAmount), ((VoxelData.hexSideUVs[i].y + index.y) / 3f));
        }
        return uvs;
    }

    public bool isActive {
        get { return active; }
        set {
            active = value;
            if (gameObject != null) {
                gameObject.SetActive(value);
                foliageGameObject.SetActive(value);
            };
        }
    }

    public bool isEditable {
        get {
            if (!populatedVoxelMap)
                return false;
            else return true;
        }
    }
}
