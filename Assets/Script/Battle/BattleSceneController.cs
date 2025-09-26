using UnityEditor.SearchService;
using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    void Awake()
    {
        //Main�� ����� ���θ��
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //���θ�� ����
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        //���θ�� �¿� ���
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        //�ڵ� ȸ�� ��� ���
        Screen.orientation = ScreenOrientation.AutoRotation;    }

    
    
}
