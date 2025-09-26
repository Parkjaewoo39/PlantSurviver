using UnityEngine;

public class TowerBase : MonoBehaviour
{
    //���� �༺
    public Transform plant;
    public Transform[] baseTransform;
    //Ÿ���ڸ� ������
    public GameObject towerBasePrefab;


    //�ڸ� ��ġ �Ÿ�
    public float distance = 5f;
    //Ÿ���ڸ� ����
    public int towerBaseCount = 12;
    private void Awake()
    {
        baseTransform = new Transform[towerBaseCount];
        distance = 5f;
        distance = 2.9f;
        for (int i = 0; i < towerBaseCount; i++)
        {
            float angle = i * 360f / towerBaseCount - 90f;
            float radius = angle * Mathf.Deg2Rad;//������ �������� ��ȯ

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
