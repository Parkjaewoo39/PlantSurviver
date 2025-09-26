using UnityEngine;

public class TowerBase : MonoBehaviour
{
    //기준 행성
    public Transform plant;
    public Transform[] baseTransform;
    //타워자리 프리팹
    public GameObject towerBasePrefab;


    //자리 위치 거리
    public float distance = 5f;
    //타워자리 개수
    public int towerBaseCount = 12;
    private void Awake()
    {
        baseTransform = new Transform[towerBaseCount];
        distance = 5f;
        distance = 2.9f;
        for (int i = 0; i < towerBaseCount; i++)
        {
            float angle = i * 360f / towerBaseCount - 90f;
            float radius = angle * Mathf.Deg2Rad;//각도를 라디안으로 변환

            Vector3 position = new Vector3
                (plant.position.x + distance * Mathf.Cos(radius),
                 plant.position.y + distance * Mathf.Sin(radius),
                 0f);


            //Instantiate(towerBasePrefab, position, Quaternion.identity, transform);
            GameObject towerBase = Instantiate(towerBasePrefab, position, Quaternion.identity, transform);
            baseTransform[i] = towerBase.transform;

        }
    }
    public Transform[] GetBaseTransform()
    {
        return baseTransform;
    }
}
