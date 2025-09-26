using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    public float plantMaxHp = 10f; //행성 체력
    public float currentHp; //현재 체력
    List<GameObject>[] TowerSpace = new List<GameObject>[12];  //타워 위치 12개

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
