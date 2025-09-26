using UnityEditor.SearchService;
using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    void Awake()
    {
        //Main씬 실행시 가로모드
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //세로모드 금지
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        //가로모드 좌우 허용
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        //자동 회전 모드 허용
        Screen.orientation = ScreenOrientation.AutoRotation;    }

    
    
}
