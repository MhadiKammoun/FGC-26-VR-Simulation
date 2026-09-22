using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherePool : MonoBehaviour
{
    public GameObject spherePrefab;
    public int poolSize = 500;

    public GameObject Point_1;
    public GameObject Point_2;

    public int batchSize = 100;      // Spawn 100 at a time
    public float batchDelay = 3f;  // Wait  seconds between batches

    private Queue<GameObject> pool;

    void Start()
    {
        pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(spherePrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Vector3[] testPositions = new Vector3[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                testPositions[i] = new Vector3(
                    Random.Range(Point_1.transform.position.x, Point_2.transform.position.x),
                    Random.Range(Point_1.transform.position.y, Point_2.transform.position.y),
                    Random.Range(Point_1.transform.position.z, Point_2.transform.position.z)
                );
            }

            StartCoroutine(SpawnSpheresInBatches(testPositions));
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

            if ((i + 1) % batchSize == 0)
            {
                yield return new WaitForSeconds(batchDelay);
            }
        }
    }
}