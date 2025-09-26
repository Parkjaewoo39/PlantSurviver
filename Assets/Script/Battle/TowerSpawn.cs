using UnityEngine;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

public class TowerSpawn : MonoBehaviour
{
    //��ȯ�� Ÿ�� ����
    public GameObject[] towerPrefab;
    //�ڸ��� �������� �ִ� Ÿ�� ��  
    int towerMaxCount = 3;
    public Dictionary<Transform, List<GameObject>> towerSlot = new Dictionary<Transform, List<GameObject>>();
    //TowerBase���� ������ �ڸ� Transform
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
    
