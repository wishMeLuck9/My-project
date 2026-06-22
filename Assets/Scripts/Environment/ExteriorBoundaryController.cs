using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExteriorBoundaryController : MonoBehaviour
{
    private const string BoundaryMessageKey = "hud.boundary.exterior.blocked";
    private const string EscapeMessageKey = "hud.boundary.exterior.escape";

    private static ExteriorBoundaryController instance;

    [SerializeField] private Vector2 minimumHalfExtents = new Vector2(28f, 28f);
    [SerializeField] private float anchorMargin = 3f;
    [SerializeField] private float wallInnerPadding = 0.65f;
    [SerializeField] private float clampInset = 0.08f;
    [SerializeField] private float wallThickness = 5f;
    [SerializeField] private float wallHeight = 40f;
    [SerializeField] private float escapePadding = 10f;
    [SerializeField] private float minimumY = -18f;
    [SerializeField] private float messageCooldown = 2.6f;

    private readonly GameObject[] walls = new GameObject[4];
    private PlayerController3D player;
    private Rigidbody playerBody;
    private Bounds playableBounds;
    private Vector3 returnPosition;
    private Quaternion returnRotation = Quaternion.identity;
    private bool initialized;
    private bool hasReturnPoint;
    private float nextBoundaryMessageAt;
    private float nextEscapeAt;

    public static void EnsureForCurrentScene()
    {
        if (!SceneIds.IsGameplay(SceneManager.GetActiveScene()))
        {
            if (instance != null) Destroy(instance.gameObject);
            return;
        }

        if (instance == null)
        {
            instance = FindFirstObjectByType<ExteriorBoundaryController>(FindObjectsInactive.Include);
        }

        if (instance == null)
        {
            instance = new GameObject("ExteriorBoundaryController").AddComponent<ExteriorBoundaryController>();
        }

        instance.Initialize();
    }

    public static bool TryValidateTargetPosition(Vector3 targetPosition, out Vector3 clampedPosition, bool showMessage = true)
    {
        clampedPosition = targetPosition;
        if (!SceneIds.IsGameplay(SceneManager.GetActiveScene())) return true;

        EnsureForCurrentScene();
        if (instance == null || !instance.EnsureRuntimeReady()) return false;

        clampedPosition = instance.ClampInside(targetPosition);
        bool isAllowed = instance.IsInsidePlayableBounds(targetPosition) && !instance.IsEscaped(targetPosition);
        if (!isAllowed && showMessage) instance.ShowBoundaryMessage();
        return isAllowed;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        EnsureRuntimeReady();
    }

    private void FixedUpdate()
    {
        if (EnsureRuntimeReady()) EnforcePlanarBounds();
    }

    private void LateUpdate()
    {
        if (EnsureRuntimeReady()) EnforcePlanarBounds();
    }

    public void NotifyBoundaryTouched(PlayerController3D touchedPlayer)
    {
        if (touchedPlayer == null) return;

        ShowBoundaryMessage();
        player = touchedPlayer;
        playerBody = player.GetComponent<Rigidbody>();
        EnforcePlanarBounds();
    }

    private bool EnsureRuntimeReady()
    {
        if (!initialized)
        {
            Initialize();
            if (!initialized) return false;
        }

        if (!SceneIds.IsGameplay(SceneManager.GetActiveScene()))
        {
            Destroy(gameObject);
            return false;
        }

        ResolvePlayer();
        if (player == null) return false;

        if (!hasReturnPoint) CaptureReturnPoint();
        return true;
    }

    private void Initialize()
    {
        if (initialized) return;

        ResolvePlayer();
        if (player == null) return;

        CaptureReturnPoint();
        RebuildBounds();
        BuildWalls();
        initialized = true;
    }

    private void ResolvePlayer()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (player != null && playerBody == null) playerBody = player.GetComponent<Rigidbody>();
    }

    private void CaptureReturnPoint()
    {
        Transform respawnPoint = FindFirstObjectByType<ExteriorHuntController>(FindObjectsInactive.Include)?.RespawnPoint;
        if (respawnPoint != null)
        {
            returnPosition = respawnPoint.position;
            returnRotation = respawnPoint.rotation;
            hasReturnPoint = true;
            return;
        }

        if (player == null) return;

        returnPosition = player.transform.position;
        returnRotation = player.transform.rotation;
        hasReturnPoint = true;
    }

    private void RebuildBounds()
    {
        if (TryRebuildBoundsFromSceneWalls()) return;
        RebuildFallbackAnchorBounds();
    }

    private bool TryRebuildBoundsFromSceneWalls()
    {
        float minX = float.NegativeInfinity;
        float maxX = float.PositiveInfinity;
        float minZ = float.NegativeInfinity;
        float maxZ = float.PositiveInfinity;
        bool hasMinX = false;
        bool hasMaxX = false;
        bool hasMinZ = false;
        bool hasMaxZ = false;

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Renderer sceneRenderer in renderers)
        {
            if (sceneRenderer == null || !sceneRenderer.name.StartsWith("Wall_", StringComparison.OrdinalIgnoreCase)) continue;

            Bounds bounds = sceneRenderer.bounds;
            bool isVerticalWall = bounds.size.z > bounds.size.x * 2f;
            bool isHorizontalWall = bounds.size.x > bounds.size.z * 2f;

            if (isVerticalWall)
            {
                if (bounds.center.x < 0f)
                {
                    minX = Mathf.Max(minX, bounds.max.x);
                    hasMinX = true;
                }
                else if (bounds.center.x > 0f)
                {
                    maxX = Mathf.Min(maxX, bounds.min.x);
                    hasMaxX = true;
                }
            }

            if (isHorizontalWall)
            {
                if (bounds.center.z < 0f)
                {
                    minZ = Mathf.Max(minZ, bounds.max.z);
                    hasMinZ = true;
                }
                else if (bounds.center.z > 0f)
                {
                    maxZ = Mathf.Min(maxZ, bounds.min.z);
                    hasMaxZ = true;
                }
            }
        }

        if (!hasMinX || !hasMaxX || !hasMinZ || !hasMaxZ) return TryRebuildBoundsFromGroundRenderer(renderers);

        float padding = Mathf.Max(0f, wallInnerPadding);
        minX += padding;
        maxX -= padding;
        minZ += padding;
        maxZ -= padding;
        if (minX >= maxX || minZ >= maxZ) return false;

        playableBounds = CreatePlanarBounds(minX, maxX, minZ, maxZ);
        return true;
    }

    private bool TryRebuildBoundsFromGroundRenderer(Renderer[] renderers)
    {
        Renderer groundRenderer = null;
        float largestArea = 0f;
        foreach (Renderer sceneRenderer in renderers)
        {
            if (sceneRenderer == null || !sceneRenderer.name.Equals("Ground", StringComparison.OrdinalIgnoreCase)) continue;

            Bounds bounds = sceneRenderer.bounds;
            float area = bounds.size.x * bounds.size.z;
            if (area <= largestArea) continue;

            groundRenderer = sceneRenderer;
            largestArea = area;
        }

        if (groundRenderer == null) return false;

        Bounds groundBounds = groundRenderer.bounds;
        float padding = Mathf.Max(0f, wallInnerPadding);
        float minX = groundBounds.min.x + padding;
        float maxX = groundBounds.max.x - padding;
        float minZ = groundBounds.min.z + padding;
        float maxZ = groundBounds.max.z - padding;
        if (minX >= maxX || minZ >= maxZ) return false;

        playableBounds = CreatePlanarBounds(minX, maxX, minZ, maxZ);
        return true;
    }

    private void RebuildFallbackAnchorBounds()
    {
        Bounds anchorBounds = new Bounds(player != null ? player.transform.position : Vector3.zero, Vector3.zero);
        bool hasAnchor = player != null;

        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<LightFragmentPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<LocationTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<ExteriorPursuer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<SquarePortalController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<NightFragmentEncounter>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<PriceAltar>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<ShadowNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<FinalGateEntryTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<FinalGateOutcomeController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<GuardianController>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        ExteriorHuntController hunt = FindFirstObjectByType<ExteriorHuntController>(FindObjectsInactive.Include);
        if (hunt != null)
        {
            if (!hasAnchor)
            {
                anchorBounds = new Bounds(hunt.transform.position, Vector3.zero);
                hasAnchor = true;
            }
            else
            {
                anchorBounds.Encapsulate(hunt.transform.position);
            }

            if (hunt.RespawnPoint != null) anchorBounds.Encapsulate(hunt.RespawnPoint.position);
        }

        if (!hasAnchor)
        {
            anchorBounds = new Bounds(Vector3.zero, Vector3.zero);
        }

        Vector3 center = anchorBounds.center;
        float halfX = Mathf.Max(minimumHalfExtents.x, anchorBounds.extents.x + anchorMargin);
        float halfZ = Mathf.Max(minimumHalfExtents.y, anchorBounds.extents.z + anchorMargin);
        playableBounds = CreatePlanarBounds(center.x - halfX, center.x + halfX, center.z - halfZ, center.z + halfZ);
    }

    private Bounds CreatePlanarBounds(float minX, float maxX, float minZ, float maxZ)
    {
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        return new Bounds(
            new Vector3(centerX, 0f, centerZ),
            new Vector3(maxX - minX, wallHeight, maxZ - minZ));
    }

    private static void EncapsulateComponentTransforms<T>(ref Bounds bounds, ref bool hasAnchor, T[] components) where T : Component
    {
        foreach (T component in components)
        {
            if (component == null) continue;
            if (!hasAnchor)
            {
                bounds = new Bounds(component.transform.position, Vector3.zero);
                hasAnchor = true;
            }
            else
            {
                bounds.Encapsulate(component.transform.position);
            }
        }
    }

    private void BuildWalls()
    {
        ClearWalls();

        float minX = playableBounds.min.x;
        float maxX = playableBounds.max.x;
        float minZ = playableBounds.min.z;
        float maxZ = playableBounds.max.z;
        float y = wallHeight * 0.5f;
        float width = playableBounds.size.x + wallThickness * 2f;
        float depth = playableBounds.size.z + wallThickness * 2f;

        walls[0] = CreateWall("NorthWall", new Vector3(playableBounds.center.x, y, maxZ + wallThickness * 0.5f), new Vector3(width, wallHeight, wallThickness));
        walls[1] = CreateWall("SouthWall", new Vector3(playableBounds.center.x, y, minZ - wallThickness * 0.5f), new Vector3(width, wallHeight, wallThickness));
        walls[2] = CreateWall("EastWall", new Vector3(maxX + wallThickness * 0.5f, y, playableBounds.center.z), new Vector3(wallThickness, wallHeight, depth));
        walls[3] = CreateWall("WestWall", new Vector3(minX - wallThickness * 0.5f, y, playableBounds.center.z), new Vector3(wallThickness, wallHeight, depth));
    }

    private void ClearWalls()
    {
        for (int i = 0; i < walls.Length; i++)
        {
            if (walls[i] != null) Destroy(walls[i]);
            walls[i] = null;
        }
    }

    private GameObject CreateWall(string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.position = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;

        ExteriorBoundaryWall boundaryWall = wall.AddComponent<ExteriorBoundaryWall>();
        boundaryWall.Configure(this);
        return wall;
    }

    private bool IsInsidePlayableBounds(Vector3 position)
    {
        return position.x >= playableBounds.min.x &&
               position.x <= playableBounds.max.x &&
               position.z >= playableBounds.min.z &&
               position.z <= playableBounds.max.z;
    }

    private bool IsEscaped(Vector3 position)
    {
        return position.y < minimumY ||
               position.x < playableBounds.min.x - escapePadding ||
               position.x > playableBounds.max.x + escapePadding ||
               position.z < playableBounds.min.z - escapePadding ||
               position.z > playableBounds.max.z + escapePadding;
    }

    private Vector3 ClampInside(Vector3 position)
    {
        float inset = Mathf.Max(0.02f, clampInset);
        float minX = playableBounds.min.x + inset;
        float maxX = playableBounds.max.x - inset;
        float minZ = playableBounds.min.z + inset;
        float maxZ = playableBounds.max.z - inset;
        if (minX > maxX) minX = maxX = playableBounds.center.x;
        if (minZ > maxZ) minZ = maxZ = playableBounds.center.z;

        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            position.y,
            Mathf.Clamp(position.z, minZ, maxZ));
    }

    private void ReturnEscapedPlayer()
    {
        if (!hasReturnPoint) CaptureReturnPoint();
        if (!hasReturnPoint) return;

        if (Time.unscaledTime >= nextEscapeAt)
        {
            nextEscapeAt = Time.unscaledTime + 1.2f;
            RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get(EscapeMessageKey), 4.2f);
        }

        player.Teleport(returnPosition, returnRotation);
        player.ApplyTimedSpeedMultiplier(0.72f, 1.3f);
    }

    private void EnforcePlanarBounds()
    {
        Vector3 position = player.transform.position;
        if (IsEscaped(position))
        {
            ReturnEscapedPlayer();
            return;
        }

        if (IsInsidePlayableBounds(position)) return;

        Vector3 clamped = ClampInside(position);
        Vector3 outward = new Vector3(position.x - clamped.x, 0f, position.z - clamped.z);
        MovePlayerTo(clamped, outward);
        ShowBoundaryMessage();
    }

    private void MovePlayerTo(Vector3 position, Vector3 outward)
    {
        if (playerBody != null)
        {
            playerBody.position = position;

            Vector3 velocity = playerBody.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (outward.sqrMagnitude > 0.0001f)
            {
                Vector3 normal = outward.normalized;
                float outwardSpeed = Vector3.Dot(horizontal, normal);
                if (outwardSpeed > 0f) horizontal -= normal * outwardSpeed;
            }

            playerBody.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        player.transform.position = position;
    }

    private void ShowBoundaryMessage()
    {
        if (Time.unscaledTime < nextBoundaryMessageAt) return;

        nextBoundaryMessageAt = Time.unscaledTime + messageCooldown;
        RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get(BoundaryMessageKey), 3.2f);
    }
}

public class ExteriorBoundaryWall : MonoBehaviour
{
    private ExteriorBoundaryController controller;

    public void Configure(ExteriorBoundaryController newController)
    {
        controller = newController;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Notify(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        Notify(collision.collider);
    }

    private void Notify(Collider other)
    {
        PlayerController3D player = other != null ? other.GetComponentInParent<PlayerController3D>() : null;
        if (player != null) controller?.NotifyBoundaryTouched(player);
    }
}
