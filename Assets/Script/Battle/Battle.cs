using System.Collections.Generic;
using UnityEngine;

public class Battle : MonoBehaviour
{
    
    public GameObject plant;
    public Enemy enemyBase;//적 프리펩
    public List<Enemy> enemyPull; //적 풀링

    //공전 및 자전 속도 조절
   
    public float minRadius; // 최소 반경
    public float maxRadius; // 최대 반경

    //private float accumulatedTime = 0f; // FixedUpdate용 누적 시간
    //private float spawnInterval = 1f; // 적 생성 간격(초)

    //plant 가 Sun의 주위를 공전 및 자전 한다. (2D)
    private void FixedUpdate()
    {
       
    }
    public void SpawnEnemy()
    {
        // 비활성화된 적을 찾아 활성화
        Enemy enemyToSpawn = null;
        foreach (var enemy in enemyPull)
        {
            if (!enemy.gameObject.activeInHierarchy)
            {
                enemyToSpawn = enemy;
                break;
            }
        }
        // 비활성화된 적이 없으면 새로 생성
        if (enemyToSpawn == null)
        {
            enemyToSpawn = Instantiate(enemyBase);
            enemyPull.Add(enemyToSpawn);
        }
        enemyToSpawn.battle = this; // 적이 생성된 Battle 스크립트 참조 설정

        // sun의 위치를 기준으로 일정 반경 내 랜덤 위치 계산 (XY 평면 2D, 최소/최대 반경 적용)
        float radius = Random.Range(minRadius, maxRadius);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        enemyToSpawn.transform.position = plant.transform.position + offset;
        enemyToSpawn.gameObject.SetActive(true);



    }

}
