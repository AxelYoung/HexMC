using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

    [Header("Movement")]
    [SerializeField] float speed;
    [SerializeField] float sprintMultiplier;
    [SerializeField] float jumpHeight;

    CharacterController controller;
    bool sprinting => Input.GetKey(sprintKey) && !crouching;
    bool jumping => Input.GetKey(jumpKey) && controller.isGrounded;
    [SerializeField] Vector3 moveDir;

    [Header("Crouching")]
    [SerializeField] float crouchingHeightMulitplier;
    [SerializeField] float crouchingSpeedMultiplier;
    [SerializeField] float crouchAnimLength;

    bool crouch => Input.GetKeyDown(crouchKey) && !crouchAnimPlaying && controller.isGrounded;
    bool crouching;
    bool crouchAnimPlaying;
    float standingHeight;
    Vector3 standingCenter = Vector3.zero;

    [Header("Physics")]
    [SerializeField] float gravity;
    [SerializeField] float maxGroundDistance;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float defaultVelocity;

    Transform feet;
    bool fallen = false;

    [Header("Camera")]
    [SerializeField] float sensitivity;
    [SerializeField] float bobSpeed;
    [SerializeField] float bobAmount;

    Camera cam;
    float xRotation;
    float defaultCamPos;
    float cameraTimer;

    [Header("Footsteps")]
    [SerializeField] float defaultFootstepVol;
    [SerializeField] AudioClip[] stoneAudioClips;
    [SerializeField] AudioClip[] grassAudioClips;
    AudioSource audioSource;

    bool stepped = false;


    [Header("Interaction")]
    [SerializeField] float reach;
    [SerializeField] Image crosshair;

    bool place => Input.GetKeyDown(placeKey);
    [SerializeField] int currentItemIndex = 0;

    [Header("Controls")]
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode crouchKey = KeyCode.C;
    [SerializeField] KeyCode breakKey = KeyCode.Mouse0;
    [SerializeField] KeyCode placeKey = KeyCode.Mouse1;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode inventoryKey = KeyCode.E;

    [SerializeField] GameObject highlightObject;

    [SerializeField] WorldGeneration world;

    bool breaking;
    float breakLength = 0.5f;
    float breakAmount = 0f;
    float breakIncrement = 0.04f;
    int steppedBreakPercentage;

    Coordinates cursorPlacement;

    public RawImage selectedItem;
    bool inventoryOpen = false;
    bool craftingOpen = false;
    public GameObject inventoryObject;

    Inventory inventory;

    public LayerMask layerMask;

    public Animator armAnim;

    ParticleSystem.TextureSheetAnimationModule textureAnim;

    public GameObject craftingUI;



    void Start() {
        CalculateMesh();
        CreateVoxelArm();
        CreateSpriteMeshObject();
        Cursor.lockState = CursorLockMode.Locked;
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        audioSource = GetComponent<AudioSource>();
        cam = transform.GetChild(0).GetComponent<Camera>();
        feet = transform.GetChild(1);
        defaultCamPos = cam.transform.localPosition.y;
        standingHeight = controller.height;
        CreateCursor();
        textureAnim = breakingParticles.textureSheetAnimation;
    }

    void Update() {
        if (!inventoryOpen && !craftingOpen) {
            Jump();
            Camera();
            Movement();
            Interaction();
            armAnim.SetBool("Breaking", breaking);
        }
        if (!craftingOpen) {
            Inventory();
        } else {
            if (Input.GetKeyUp(KeyCode.E)) {
                craftingOpen = false;
                craftingUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        Physics();
    }

    void FixedUpdate() {
        if (breaking) Breaking();
    }

    void Camera() {
        // Recive input from two axises, adjust sensitivity
        Vector2 input = new Vector2(Input.GetAxis("Mouse X") * sensitivity, Input.GetAxis("Mouse Y") * sensitivity);

        // Clamp X rotation
        xRotation -= input.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotations to cam
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * input.x);

        // Head bob only when grounded and moving
        if (controller.isGrounded) {
            if (moveDir.x != 0 || moveDir.z != 0) {
                // Calculate bob speed and amount based on mulitiplers, bob is simple sin wave
                cameraTimer += Time.deltaTime * (crouching ? bobSpeed * crouchingSpeedMultiplier : sprinting ? bobSpeed * sprintMultiplier : bobSpeed);
                float bob = defaultCamPos + Mathf.Sin(cameraTimer) * (crouching ? bobAmount * crouchingSpeedMultiplier : sprinting ? bobAmount * sprintMultiplier : bobAmount);

                // Apply bob and pass through to footstep function
                cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, bob, cam.transform.localPosition.z);
                Footsteps(bob);
            } else {
                //(REMOVED FOR NOW, MIGHT REIMPLEMENT (slightly jarring)) Reset to default pos when stopped
                //cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, defaultCamPos, cam.transform.localPosition.z);
            }
        }

    }


    void Footsteps(float bob) {
        if (!stepped) {
            // Checks if close to lowest point of head bob
            if (Mathf.Sin(cameraTimer) < 0) {
                PlayStepSound();
                stepped = true;
            }
        } else if (Mathf.Sin(cameraTimer) > 0) {
            stepped = false;
        }
    }



    public Material cursorMaterial;
    GameObject cursorObject;
    Mesh cursorMesh;
    MeshFilter cursorMeshFilter;
    MeshRenderer cursorMeshRenderer;
    List<Vector3> meshVerticies = new List<Vector3>();
    List<int> meshTriangles = new List<int>();
    List<Vector2> meshUVs = new List<Vector2>();


    public Material worldMaterial;

    List<Vector2> voxelUVs = new List<Vector2>();

    GameObject voxelArm;
    Mesh armMesh;
    MeshFilter armMeshFilter;

    List<Vector2> armUVs = new List<Vector2>();

    public GameObject arm;
    public ParticleSystem breakingParticles;
    public GameObject burstParticle;

    void CreateVoxelArm() {
        voxelArm = new GameObject();
        armMesh = new Mesh();
        armMeshFilter = voxelArm.AddComponent<MeshFilter>();
        MeshRenderer voxelMeshRenderer = voxelArm.AddComponent<MeshRenderer>();
        voxelMeshRenderer.material = worldMaterial;
        armMesh.vertices = meshVerticies.ToArray();
        armMesh.triangles = meshTriangles.ToArray();
        voxelArm.transform.parent = armAnim.transform;
        voxelArm.transform.localPosition = new Vector3(-0.2f, 0, 0.2f);
        voxelArm.transform.localRotation = Quaternion.Euler(80, 45, -100);
        voxelArm.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        voxelMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }


    GameObject spriteObject;
    MeshFilter spriteMeshFilter;
    MeshRenderer spriteMeshRenderer;

    void CreateSpriteMeshObject() {
        spriteObject = new GameObject();
        spriteMeshFilter = spriteObject.AddComponent<MeshFilter>();
        spriteMeshRenderer = spriteObject.AddComponent<MeshRenderer>();
        spriteObject.transform.parent = armAnim.transform;
        spriteObject.transform.localPosition = new Vector3(1f, 0, 1.1f);
        spriteObject.transform.localRotation = Quaternion.Euler(215, 10, 40);
        spriteObject.transform.localScale = new Vector3(2f, 2f, 2f);
        spriteMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void UpdateSpriteMesh() {
        spriteMeshFilter.mesh = SpriteMeshify.Meshify(Resources.Load<Texture2D>("Sprites/" + inventory.inventory[currentItem].ID.ToString()));
        spriteMeshRenderer.material = Resources.Load<Material>("Materials/" + inventory.inventory[currentItem].ID.ToString());

    }

    public void UpdateArmUVs(byte ID) {
        armUVs.Clear();
        armUVs.AddRange(voxelFaceUVsFromIndex(new Vector2(ID - 1, 0)));
        armUVs.AddRange(voxelFaceUVsFromIndex(new Vector2(ID - 1, 2)));
        for (int i = 0; i < 6; i++) {
            armUVs.AddRange(voxelSideUVsFromIndex(new Vector2(ID - 1, 1)));
        }
        armMesh.uv = armUVs.ToArray();
        armMesh.RecalculateNormals();
        armMeshFilter.mesh = armMesh;
    }

    void CreateVoxelEntity(Vector3 position, byte voxelType) {
        voxelUVs.Clear();
        GameObject voxel = new GameObject();
        Mesh voxelMesh = new Mesh();
        MeshFilter voxelMeshFilter = voxel.AddComponent<MeshFilter>();
        MeshRenderer voxelMeshRenderer = voxel.AddComponent<MeshRenderer>();
        voxelMeshRenderer.material = worldMaterial;
        voxelMesh.vertices = meshVerticies.ToArray();
        voxelMesh.triangles = meshTriangles.ToArray();
        MeshCollider voxelCollider = voxel.AddComponent<MeshCollider>();
        UnityEngine.Physics.IgnoreCollision(controller, voxelCollider, true);
        voxelCollider.sharedMesh = voxelMesh;
        voxelCollider.convex = true;
        Rigidbody rb = voxel.AddComponent<Rigidbody>();
        voxelUVs.AddRange(voxelFaceUVsFromIndex(new Vector2(voxelType - 1, 0)));
        voxelUVs.AddRange(voxelFaceUVsFromIndex(new Vector2(voxelType - 1, 2)));
        for (int i = 0; i < 6; i++) {
            voxelUVs.AddRange(voxelSideUVsFromIndex(new Vector2(voxelType - 1, 1)));
        }
        voxelMesh.uv = voxelUVs.ToArray();
        voxelMesh.RecalculateNormals();
        voxelMeshFilter.mesh = voxelMesh;
        voxel.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
        voxel.transform.position = position;
        voxel.AddComponent<VoxelEntity>().Setup(transform, voxelType);
        Vector3 force = new Vector3(Random.Range(-1f, 1f), 1, Random.Range(-1f, 1f)) * 20;
        rb.AddForce(force);
        rb.AddRelativeTorque(force);
    }

    Vector2[] voxelFaceUVsFromIndex(Vector2 index) {
        Vector2[] uvs = new Vector2[6];
        for (int i = 0; i < 6; i++) {
            uvs[i] = new Vector2(((VoxelData.hexUVs[i].x + 0.5f + index.x) / Voxels.voxelAmount), ((VoxelData.hexUVs[i].y + 0.5f + index.y) / 3f));
        }
        return uvs;
    }

    Vector2[] voxelSideUVsFromIndex(Vector2 index) {
        Vector2[] uvs = new Vector2[4];
        for (int i = 0; i < 4; i++) {
            uvs[i] = new Vector2(((VoxelData.hexSideUVs[i].x + 0.5f + index.x) / Voxels.voxelAmount), ((VoxelData.hexSideUVs[i].y + index.y) / 3f));
        }
        return uvs;
    }


    void CreateCursor() {
        cursorObject = new GameObject();
        cursorMesh = new Mesh();
        cursorObject.transform.parent = highlightObject.transform;
        cursorMeshFilter = cursorObject.AddComponent<MeshFilter>();
        cursorMeshRenderer = cursorObject.AddComponent<MeshRenderer>();
        cursorMeshRenderer.material = cursorMaterial;
        cursorMesh.vertices = meshVerticies.ToArray();
        cursorMesh.triangles = meshTriangles.ToArray();
        cursorObject.transform.localScale = new Vector3(1.001f, 1.002f, 1.001f);
        cursorObject.transform.localPosition -= new Vector3(0, 0, 0.001f);
        cursorObject.SetActive(false);
        UpdateCursorUVs(0);
    }

    void CalculateMesh() {
        // Bottom face
        Vector3[] bottomVerticies = relativeVoxelVerticies(Vector3.zero);
        meshVerticies.AddRange(bottomVerticies);
        meshTriangles.AddRange(relativeVoxelTriangles(false));
        // Top face
        Vector3[] topVerticies = relativeVoxelVerticies(Vector3.up);
        meshVerticies.AddRange(topVerticies);
        meshTriangles.AddRange(relativeVoxelTriangles(true));
        // Sides
        for (int i = 0; i < 6; i++) {
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
        }
    }

    void UpdateCursorUVs(int state) {
        meshUVs.Clear();
        meshUVs.AddRange(faceUVsFromIndex(state));
        meshUVs.AddRange(faceUVsFromIndex(state));
        for (int i = 0; i < 6; i++) {
            meshUVs.AddRange(sideUVsFromIndex(state));
        }
        cursorMesh.uv = meshUVs.ToArray();
        cursorMeshFilter.mesh = cursorMesh;
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

    Vector2[] faceUVsFromIndex(int index) {
        Vector2[] uvs = new Vector2[6];
        for (int i = 0; i < 6; i++) {
            uvs[i] = new Vector2(((VoxelData.hexUVs[i].x + 0.5f + index) / 10), ((VoxelData.hexUVs[i].y + 0.5f)));
        }
        return uvs;
    }

    Vector2[] sideUVsFromIndex(int index) {
        Vector2[] uvs = new Vector2[4];
        for (int i = 0; i < 4; i++) {
            uvs[i] = new Vector2(((VoxelData.hexSideUVs[i].x + 0.5f + index) / 10), (VoxelData.hexSideUVs[i].y));
        }
        return uvs;
    }


    void PlayStepSound() {
        // Raycast below to detect ground type
        RaycastHit hit;
        UnityEngine.Physics.Raycast(cam.transform.position, Vector3.down, out hit, controller.height + 0.5f);

        // Ground type determined by tag, play random audio clip from array of sounds to corresponding ground type
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds/Breaking/" + Voxels.types[world.blockIDAtCoordinates(Coordinates.WorldToCoordinates(hit.point - (hit.normal * 0.1f)))].soundType.ToString());
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.25f);
    }


    void Movement() {
        if (crouch || (crouching && Input.GetKeyDown(sprintKey))) StartCoroutine(Crouch());

        // Get movement input for axises and translate into local Vector3
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        float yDir = moveDir.y;
        moveDir = (transform.right * input.x + transform.forward * input.y) * (crouching ? speed * crouchingSpeedMultiplier : sprinting ? speed * sprintMultiplier : speed);
        moveDir.y = yDir;
    }

    void Jump() {
        if (jumping) {
            moveDir.y = jumpHeight;
        }
    }

    void Physics() {

        // If grounded set to default velocity and play sound
        if (controller.isGrounded) {
            if (!fallen) {
                //audioSource.volume = sprintMultiplier * defaultFootstepVol;
                //PlayStepSound();
                fallen = true;
            }
        } else {
            fallen = false;
            // Continously add gravity and apply velocity to player
            moveDir.y -= gravity * Time.deltaTime;
        }


        if (controller.velocity.y < -1 && controller.isGrounded) {
            moveDir.y = 0;
        }

        controller.Move(moveDir * Time.deltaTime);
    }

    IEnumerator Crouch() {
        // Check if object above player before standing
        if (crouching && UnityEngine.Physics.Raycast(transform.position, Vector3.up, 2f)) yield break;

        crouchAnimPlaying = true;

        // Reset time of animation to 0 and set variables according to mulipliers
        float elapsedTime = 0;
        float targetHeight = crouching ? standingHeight : standingHeight * crouchingHeightMulitplier;
        float currentHeight = controller.height;
        Vector3 targetCenter = crouching ? standingCenter : standingCenter + (Vector3.up * crouchingHeightMulitplier);
        Vector3 currentCenter = controller.center;

        while (elapsedTime < crouchAnimLength) {
            // Lerp both height and center of player controller from starting to goal height and center
            controller.height = Mathf.Lerp(currentHeight, targetHeight, elapsedTime / crouchAnimLength);
            controller.center = Vector3.Lerp(currentCenter, targetCenter, elapsedTime / crouchAnimLength);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        controller.height = targetHeight;
        controller.center = targetCenter;

        crouching = !crouching;

        crouchAnimPlaying = false;
    }

    ChunkCoordinates chunkCoordinates;
    Chunk chunk;
    Coordinates localCoordinates;
    Coordinates globalCoordinates;

    public int currentItem = 0;

    bool broken = false;

    void BreakBlock() {
        chunkCoordinates = new ChunkCoordinates(globalCoordinates);
        chunk = world.chunkMap[chunkCoordinates.x, chunkCoordinates.z];
        GameObject burst = Instantiate(burstParticle);
        burst.transform.position = globalCoordinates.worldPosition + (Vector3.up * 0.5f);
        ParticleSystem.TextureSheetAnimationModule anim;
        ParticleSystem particleSystem;
        particleSystem = burst.GetComponent<ParticleSystem>();
        anim = particleSystem.textureSheetAnimation;
        byte ID = world.blockIDAtCoordinates(globalCoordinates);
        anim.startFrame = (ID + (Voxels.voxelAmount - 1)) / (Voxels.voxelAmount * 3f);
        anim.numTilesX = Voxels.voxelAmount;
        particleSystem.Play();
        localCoordinates = Coordinates.GlobalToLocalOffset(globalCoordinates, chunkCoordinates);
        if (Voxels.types[chunk.voxelMap[localCoordinates.x, localCoordinates.y, localCoordinates.z]].blockDrop != 0) {
            if (Voxels.types[chunk.voxelMap[localCoordinates.x, localCoordinates.y, localCoordinates.z]].toolRequired) {
                if (inventory.inventory[currentItem].ID > 200) {
                    if (Voxels.types[chunk.voxelMap[localCoordinates.x, localCoordinates.y, localCoordinates.z]].effectiveTool == Tools.tools[inventory.inventory[currentItem].ID - 201].type) {
                        CreateVoxelEntity(globalCoordinates.worldPosition + (Vector3.up * 0.5f), Voxels.types[chunk.voxelMap[localCoordinates.x, localCoordinates.y, localCoordinates.z]].blockDrop);
                    }
                }
            } else {
                CreateVoxelEntity(globalCoordinates.worldPosition + (Vector3.up * 0.5f), Voxels.types[chunk.voxelMap[localCoordinates.x, localCoordinates.y, localCoordinates.z]].blockDrop);
            }
        }
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds/Broke/" + Voxels.types[world.blockIDAtCoordinates(globalCoordinates)].soundType.ToString());
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 1f);
        chunk.EditVoxel(localCoordinates, 0);
        SetBreakAmount(0);
    }

    void SetBreakAmount(float amount) {
        breakAmount = amount;
        float breakPercentage = breakAmount / breakLength;
        if (breakLength == 0) {
            breakPercentage = 100;
        }
        steppedBreakPercentage = Mathf.FloorToInt(breakPercentage * 10);
        UpdateCursorUVs(steppedBreakPercentage);
    }

    void Breaking() {
        SetBreakAmount(breakAmount + breakIncrement);
        if (steppedBreakPercentage >= 10 && !broken) {
            broken = true;
            BreakBlock();
        }
    }

    void Inventory() {
        if (Input.GetKeyDown(inventoryKey)) {
            inventoryOpen = !inventoryOpen;
            Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            inventoryObject.SetActive(inventoryOpen);
            moveDir = new Vector3(0, moveDir.y, 0);
        }
    }

    public void UpdateArm() {
        if (inventory.inventory[currentItem].ID != 0) {
            if (inventory.inventory[currentItem].ID <= 100) {
                spriteObject.SetActive(false);
                arm.SetActive(false);
                voxelArm.SetActive(true);
                UpdateArmUVs(inventory.inventory[currentItem].ID);
            } else {
                arm.SetActive(false);
                voxelArm.SetActive(false);
                spriteObject.SetActive(true);
                UpdateSpriteMesh();
            }
        } else {
            arm.SetActive(true);
            voxelArm.SetActive(false);
            spriteObject.SetActive(false);
        }
    }

    float soundTimer = 2f;
    float soundTimerMax = 0.2f;

    void Interaction() {
        RaycastHit hit;
        UnityEngine.Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, reach, layerMask);
        if (hit.transform != null) {
            globalCoordinates = Coordinates.WorldToCoordinates(hit.point + (cam.transform.forward * 0.01f));
            if (!globalCoordinates.Equals(cursorPlacement)) {
                highlightObject.SetActive(true);
                highlightObject.transform.position = globalCoordinates.worldPosition;
                byte ID = world.blockIDAtCoordinates(globalCoordinates);
                textureAnim.startFrame = (ID + (Voxels.voxelAmount - 1)) / (Voxels.voxelAmount * 3f);
                textureAnim.numTilesX = Voxels.voxelAmount;
                SetBreakAmount(0);
                cursorPlacement = globalCoordinates;
                broken = false;
            }
            if (Input.GetKey(breakKey)) {
                breakLength = world.blockSpeedAtCoordinates(globalCoordinates);
                if (inventory.inventory[currentItem].ID > 200) {
                    if (Voxels.types[world.blockIDAtCoordinates(globalCoordinates)].effectiveTool == Tools.tools[inventory.inventory[currentItem].ID - 201].type) {
                        breakLength = breakLength * Tools.tools[inventory.inventory[currentItem].ID - 201].multiplier;
                    }
                }
                if (soundTimer < soundTimerMax) {
                    soundTimer += Time.deltaTime;
                } else {
                    soundTimer = 0;
                    AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds/Breaking/" + Voxels.types[world.blockIDAtCoordinates(globalCoordinates)].soundType.ToString());
                    audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.33f);
                }
                breaking = true;
                if (breakingParticles.isPlaying == false) {
                    byte ID = world.blockIDAtCoordinates(globalCoordinates);
                    textureAnim.startFrame = (ID + (Voxels.voxelAmount - 1)) / (Voxels.voxelAmount * 3f);
                    textureAnim.numTilesX = Voxels.voxelAmount;
                    breakingParticles.Play();
                }
                cursorObject.SetActive(true);
            }

            if (!Input.GetKey(breakKey) && breaking) {
                breaking = false;
                soundTimer = 2;
                breakingParticles.Stop();
                cursorObject.SetActive(false);
                cursorPlacement = Coordinates.zero;
                SetBreakAmount(0);
            }

            if (place) {
                Coordinates voxelInView = Coordinates.WorldToCoordinates(hit.point + (cam.transform.forward * 0.01f));
                if (world.blockIDAtCoordinates(voxelInView) == Voxels.CraftingTable.ID) {
                    craftingOpen = true;
                    craftingUI.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    moveDir = new Vector3(0, moveDir.y, 0);
                } else {
                    globalCoordinates = Coordinates.WorldToCoordinates(hit.point - (cam.transform.forward * 0.01f));
                    chunkCoordinates = new ChunkCoordinates(globalCoordinates);
                    chunk = world.chunkMap[chunkCoordinates.x, chunkCoordinates.z];
                    localCoordinates = Coordinates.GlobalToLocalOffset(globalCoordinates, chunkCoordinates);
                    Coordinates playerHead = Coordinates.GlobalToLocalOffset(Coordinates.WorldToCoordinates(transform.position + (Vector3.up * 0.5f)), chunkCoordinates);
                    Coordinates playerLegs = Coordinates.GlobalToLocalOffset(Coordinates.WorldToCoordinates(transform.position + (Vector3.down * 0.5f)), chunkCoordinates);
                    if (!localCoordinates.Equals(playerHead) && !localCoordinates.Equals(playerLegs)) {
                        if (inventory.inventory[currentItem].quantity > 0 && inventory.inventory[currentItem].ID <= 100) {
                            armAnim.SetTrigger("Place");
                            chunk.EditVoxel(localCoordinates, inventory.inventory[currentItem].ID);
                            AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds/Broke/" + Voxels.types[world.blockIDAtCoordinates(globalCoordinates)].soundType.ToString());
                            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 1);
                            inventory.RemoveItem(currentItem, 1);
                        }
                    }
                }
            }
        } else {
            if (!cursorPlacement.Equals(Coordinates.zero)) {
                cursorPlacement = Coordinates.zero;
            }
            if (breaking) {
                breaking = false;
                breakingParticles.Stop();
                cursorObject.SetActive(false);
                SetBreakAmount(0);
                soundTimer = 2;
            }
            if (highlightObject.activeInHierarchy) {
                highlightObject.SetActive(false);
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0) currentItem--;
        if (Input.GetAxisRaw("Mouse ScrollWheel") < 0) currentItem++;

        if (currentItem < 0) {
            currentItem = 8;
        }
        if (currentItem > 8) {
            currentItem = 0;
        }

        UpdateArm();

        selectedItem.transform.localPosition = new Vector3(-80 + (currentItem * 20), 0, 0);

        //if (currentItemIndex > inventory.holdableItems.Count) currentItemIndex = 0;
        //else if (currentItemIndex < 0) currentItemIndex = inventory.holdableItems.Count;

        //if (currentItemIndex > 0) inventory.holdableItems[currentItemIndex - 1].SetActive(true);

    }
}
