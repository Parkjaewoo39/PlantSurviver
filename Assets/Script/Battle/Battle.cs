using System.Collections.Generic;
using UnityEngine;

public class Battle : MonoBehaviour
{
    
    public GameObject plant;
    public Enemy enemyBase;//�� ������
    public List<Enemy> enemyPull; //�� Ǯ��

    //���� �� ���� �ӵ� ����
   
    public float minRadius; // �ּ� �ݰ�
    public float maxRadius; // �ִ� �ݰ�

    //private float accumulatedTime = 0f; // FixedUpdate�� ���� �ð�
    //private float spawnInterval = 1f; // �� ���� ����(��)

    //plant �� Sun�� ������ ���� �� ���� �Ѵ�. (2D)
    private void FixedUpdate()
    {
       
    }
    public void SpawnEnemy()
    {
        // ��Ȱ��ȭ�� ���� ã�� Ȱ��ȭ
        Enemy enemyToSpawn = null;
        foreach (var enemy in enemyPull)
        {
            if (!enemy.gameObject.activeInHierarchy)
            {
                enemyToSpawn = enemy;
                break;
            }
        }
        // ��Ȱ��ȭ�� ���� ������ ���� ����
        if (enemyToSpawn == null)
        {
            enemyToSpawn = Instantiate(enemyBase);
            enemyPull.Add(enemyToSpawn);
        }
        enemyToSpawn.battle = this; // ���� ������ Battle ��ũ��Ʈ ���� ����

        // sun�� ��ġ�� �������� ���� �ݰ� �� ���� ��ġ ��� (XY ��� 2D, �ּ�/�ִ� �ݰ� ����)
        float radius = Random.Range(minRadius, maxRadius);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        enemyToSpawn.transform.position = plant.transform.position + offset;
        enemyToSpawn.gameObject.SetActive(true);



    }

}
