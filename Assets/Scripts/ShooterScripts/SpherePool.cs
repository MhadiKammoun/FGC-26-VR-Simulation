using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherePool : MonoBehaviour
{
    public GameObject spherePrefab;
    public int poolSize = 500;

    public Transform point1;
    public Transform point2;

    public int batchSize = 100;
    public float batchDelay = 3f;

    private Queue<GameObject> pool;
    private Coroutine spawnRoutine;
    private WaitForSeconds batchWait;

    void Start()
    {
        pool = new Queue<GameObject>(poolSize);
        batchWait = new WaitForSeconds(batchDelay);

        for (int i = 0; i < poolSize; i++)
        {
            // 1. Instantiate in root so it takes the true prefab scale
            GameObject obj = Instantiate(spherePrefab);

            // 2. Parent it while maintaining its world scale (ignores parent's 10.27 scale)
            obj.transform.SetParent(transform, true);

            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            // Prevent multiple overlapping coroutines from depleting the pool at once
            if (spawnRoutine != null)
            {
                Debug.LogWarning("Spawn already in progress.");
                return;
            }

            if (point1 == null || point2 == null)
            {
                Debug.LogError("Spawn bounds points are missing.");
                return;
            }

            Vector3[] testPositions = new Vector3[poolSize];

            // Ensure min/max values are properly ordered for Random.Range
            float minX = Mathf.Min(point1.position.x, point2.position.x);
            float maxX = Mathf.Max(point1.position.x, point2.position.x);
            float minY = Mathf.Min(point1.position.y, point2.position.y);
            float maxY = Mathf.Max(point1.position.y, point2.position.y);
            float minZ = Mathf.Min(point1.position.z, point2.position.z);
            float maxZ = Mathf.Max(point1.position.z, point2.position.z);

            for (int i = 0; i < poolSize; i++)
            {
                testPositions[i] = new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY),
                    Random.Range(minZ, maxZ)
                );
            }

            spawnRoutine = StartCoroutine(SpawnSpheresInBatches(testPositions));
        }
    }

    IEnumerator SpawnSpheresInBatches(Vector3[] spawnPositions)
    {
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (pool.Count > 0)
            {
                GameObject sphere = pool.Dequeue();
                sphere.transform.position = spawnPositions[i];
                sphere.SetActive(true);
            }

            // Wait after each completed batch, skipping the delay after the final sphere
            if ((i + 1) % batchSize == 0 && i < spawnPositions.Length - 1)
            {
                yield return batchWait;
            }
        }

        spawnRoutine = null;
    }

    // Call this from a sphere lifetime script to recycle it back into the pool
    public void ReturnToPool(GameObject sphere)
    {
        sphere.SetActive(false);
        pool.Enqueue(sphere);
    }
}