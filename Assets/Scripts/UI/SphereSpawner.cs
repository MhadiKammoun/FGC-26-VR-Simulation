using System;
using System.Collections;
using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    [Header("Prefab & Hierarchy")]
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;

    [Header("Pool Setup")]
    [SerializeField] private int poolSize = 500;
    [SerializeField] private int preloadPerFrame = 50;

    [Header("Spawning Cadence")]
    [SerializeField] private int batchSize = 50;
    [SerializeField] private float batchInterval = 0.5f;

    private struct PooledSphere
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rigidbody;
    }

    private PooledSphere[] pool;
    private int availableIndex = 0;
    private bool isInitialized = false;
    private bool isSpawning = false;

    public bool IsSpawning => isSpawning;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        pool = new PooledSphere[poolSize];
    }

    private IEnumerator Start()
    {
        Transform parent = poolContainer != null ? poolContainer : transform;

        // 1. Get the original intended scale directly from the prefab
        Vector3 prefabScale = spherePrefab.transform.localScale;

        // 2. Calculate the compensated scale so the parent's scale never distorts the spheres
        Vector3 parentLossy = parent.lossyScale;
        Vector3 compensatedScale = new Vector3(
            parentLossy.x != 0f ? prefabScale.x / parentLossy.x : prefabScale.x,
            parentLossy.y != 0f ? prefabScale.y / parentLossy.y : prefabScale.y,
            parentLossy.z != 0f ? prefabScale.z / parentLossy.z : prefabScale.z
        );

        int created = 0;
        while (created < poolSize)
        {
            int batchEnd = Mathf.Min(created + preloadPerFrame, poolSize);
            for (int i = created; i < batchEnd; i++)
            {
                // Instantiate under parent
                GameObject obj = Instantiate(spherePrefab, parent);
                obj.SetActive(false);

                // Enforce independent scale
                obj.transform.localScale = compensatedScale;

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }

                pool[i] = new PooledSphere
                {
                    gameObject = obj,
                    transform = obj.transform,
                    rigidbody = rb
                };
            }

            created = batchEnd;
            yield return null;
        }

        isInitialized = true;
    }

    public void StartSpawningField(Action onComplete = null)
    {
        if (isSpawning) return;

        if (!isInitialized)
        {
            StartCoroutine(WaitAndSpawn(onComplete));
            return;
        }

        StartCoroutine(SpawnBatchesRoutine(onComplete));
    }

    private IEnumerator WaitAndSpawn(Action onComplete)
    {
        yield return new WaitUntil(() => isInitialized);
        StartCoroutine(SpawnBatchesRoutine(onComplete));
    }

    private IEnumerator SpawnBatchesRoutine(Action onComplete)
    {
        isSpawning = true;

        Vector3 p1 = point1.position;
        Vector3 p2 = point2.position;
        float minX = Mathf.Min(p1.x, p2.x), maxX = Mathf.Max(p1.x, p2.x);
        float minY = Mathf.Min(p1.y, p2.y), maxY = Mathf.Max(p1.y, p2.y);
        float minZ = Mathf.Min(p1.z, p2.z), maxZ = Mathf.Max(p1.z, p2.z);

        WaitForSeconds waitInterval = new WaitForSeconds(batchInterval);

        while (availableIndex < poolSize)
        {
            int countInBatch = Mathf.Min(batchSize, poolSize - availableIndex);

            for (int i = 0; i < countInBatch; i++)
            {
                PooledSphere item = pool[availableIndex];

                item.transform.position = new Vector3(
                    UnityEngine.Random.Range(minX, maxX),
                    UnityEngine.Random.Range(minY, maxY),
                    UnityEngine.Random.Range(minZ, maxZ)
                );
                item.transform.rotation = UnityEngine.Random.rotation;

                item.gameObject.SetActive(true);

                if (item.rigidbody != null)
                {
                    item.rigidbody.isKinematic = false;
                    item.rigidbody.detectCollisions = true;
                    item.rigidbody.velocity = Vector3.zero;
                    item.rigidbody.angularVelocity = Vector3.zero;
                    item.rigidbody.WakeUp();
                }

                availableIndex++;
            }

            if (availableIndex < poolSize)
            {
                yield return waitInterval;
            }
        }

        isSpawning = false;
        onComplete?.Invoke();
    }

    public void ResetPool()
    {
        StopAllCoroutines();
        isSpawning = false;

        for (int i = 0; i < poolSize; i++)
        {
            if (pool[i].gameObject != null)
            {
                if (pool[i].rigidbody != null)
                {
                    pool[i].rigidbody.isKinematic = true;
                    pool[i].rigidbody.detectCollisions = false;
                }
                pool[i].gameObject.SetActive(false);
            }
        }

        availableIndex = 0;
    }
}