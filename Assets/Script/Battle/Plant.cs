using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    public float plantMaxHp = 10f; //�༺ ü��
    public float currentHp; //���� ü��
    List<GameObject>[] TowerSpace = new List<GameObject>[12];  //Ÿ�� ��ġ 12��

    private void awake()
    {
        plantMaxHp = 10;
        currentHp = plantMaxHp;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }
    public void Die()
    {
        if (currentHp <= 0)
        {

        }
        else 
        {
            
        };
    }
}
