using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField]
    private Transform player;
    [SerializeField]
    private Transform[] spwanPoints;
    [SerializeField]
    private GameObject hazard;
    [SerializeField]
    private GameObject heal;
    private GameObject[] spawned;
    private float timer;
    private bool isWaiting = false;
    void Start()
    {
        spawned = new GameObject[spwanPoints.Length];
        timer = Random.Range(3f, 8f);
    }

    void Update()
    {
        int numSpawn = countSpawned();

        if (numSpawn < 3)
        {
            spawn();
            return;
        }

        if (isWaiting) return;

        StartCoroutine(WaitTime());
        
    }

    int countSpawned()
    {
        int c = 0;
        foreach (GameObject s in spawned) if (s != null) c++;
        return c;
    }

    private IEnumerator WaitTime()
    {
        isWaiting = true;
        Debug.Log("Waitng " + timer);
        yield return new WaitForSecondsRealtime(timer);
        isWaiting = false;
        timer = Random.Range(3f, 8f);
        spawn();
    }

    private void spawn()
    {
        if (countSpawned() >= 10) return;

        Debug.Log("Spawning Item...");
        List<int> points = new List<int>();
        for (int i = 0; i < spwanPoints.Length; i++)
        {
            if (spawned[i] != null) continue;

            float distance = Vector3.Distance(player.position, spwanPoints[i].position);

            if (distance > -1f) points.Add(i);
        }
        
        if (points.Count == 0) return;

        int index = points[Random.Range(0, points.Count)];
        GameObject item = Random.value < .5f ? heal : hazard;
        spawned[index] = Instantiate(item, spwanPoints[index].position, Quaternion.identity, spwanPoints[index]);
        if (item == hazard)
        {
            spawned[index].transform.Rotate(10f, 0 ,0);
        }

        Debug.Log(index + 1);
    }
}
