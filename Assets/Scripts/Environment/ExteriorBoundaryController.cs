using UnityEngine;
using UnityEngine.SceneManagement;

public class ExteriorBoundaryController : MonoBehaviour
{
    private const string BoundaryMessageKey = "hud.boundary.exterior.blocked";
    private const string EscapeMessageKey = "hud.boundary.exterior.escape";

    private static ExteriorBoundaryController instance;

    [SerializeField] private Vector2 minimumHalfExtents = new Vector2(44f, 44f);
    [SerializeField] private float anchorMargin = 20f;
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
        if (SceneManager.GetActiveScene().name != SceneIds.Exterior)
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

        if (SceneManager.GetActiveScene().name != SceneIds.Exterior)
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
        Bounds anchorBounds = new Bounds(player != null ? player.transform.position : Vector3.zero, Vector3.zero);
        bool hasAnchor = player != null;

        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<LightFragmentPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<LocationTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        EncapsulateComponentTransforms(ref anchorBounds, ref hasAnchor, FindObjectsByType<ExteriorPursuer>(FindObjectsInactive.Include, FindObjectsSortMode.None));

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
        playableBounds = new Bounds(
            new Vector3(center.x, 0f, center.z),
            new Vector3(halfX * 2f, wallHeight, halfZ * 2f));
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
        float inset = Mathf.Max(0.25f, wallThickness);
        return new Vector3(
            Mathf.Clamp(position.x, playableBounds.min.x + inset, playableBounds.max.x - inset),
            position.y,
            Mathf.Clamp(position.z, playableBounds.min.z + inset, playableBounds.max.z - inset));
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
