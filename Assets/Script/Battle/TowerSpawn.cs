using UnityEngine;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

public class TowerSpawn : MonoBehaviour
{
    //소환될 타워 종류
    public GameObject[] towerPrefab;
    //자리에 같은종류 최대 타워 수  
    int towerMaxCount = 3;
    public Dictionary<Transform, List<GameObject>> towerSlot = new Dictionary<Transform, List<GameObject>>();
    //TowerBase에서 생성된 자리 Transform
    public Transform[] towerTrans;

    void Start()
    {
        TowerBase towerBase = FindAnyObjectByType<TowerBase>();
        towerTrans = towerBase.GetBaseTransform();
        foreach (var baseSlot in towerTrans)
        {
            towerSlot[baseSlot] = new List<GameObject>();
        }

    }

    public void SpawnTower()
    {
        // GameObject spawnTowerPrefabs = 

    }
}
    
