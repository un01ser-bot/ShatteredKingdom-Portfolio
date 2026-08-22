using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    [SerializeField] private GameObject[] bulletPrefabs;
    [SerializeField] private int bulletCountPerType = 20;

    private Queue<GameObject>[] pools;

    private void Awake()
    {
        pools = new Queue<GameObject>[bulletPrefabs.Length];

        for (int i = 0; i < bulletPrefabs.Length; i++)
        {
            pools[i] = new Queue<GameObject>();

            for (int j = 0; j < bulletCountPerType; j++)
            {
                GameObject obj = Instantiate(bulletPrefabs[i], transform);
                obj.SetActive(false);
                pools[i].Enqueue(obj);
            }
        }
    }

    public GameObject Get(int type, Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (pools[type].Count > 0)
        {
            obj = pools[type].Dequeue();
        }
        else
        {
            obj = Instantiate(bulletPrefabs[type], transform);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Return(int type, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[type].Enqueue(obj);
    }
}