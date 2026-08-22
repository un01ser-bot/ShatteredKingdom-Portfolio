//using System.Collections;
//using UnityEngine;

//public class BossAutoBindTarget : MonoBehaviour
//{
//    [SerializeField] private string playerTag = "Player";
//    [SerializeField] private float retryInterval = 0.2f;

//    private IEnumerator Start()
//    {
//        while (true)
//        {
//            var p = GameObject.FindGameObjectWithTag(playerTag);
//            if (p != null)
//            {
//                TryInject(p.transform);
//                yield break;
//            }
//            yield return new WaitForSeconds(retryInterval);
//        }
//    }

//    private void TryInject(Transform player)
//    {
//        var finalBoss = GetComponent<Final_Boss_Base>();
//        if (finalBoss != null)
//        {
//            finalBoss.BindTargets(player); // 너가 예전에 만들었던 주입 함수가 있으면 이거
//            return;
//        }

//        var wolfBoss = GetComponent<Wolf_Boss_Base>();
//        if (wolfBoss != null)
//        {
//            // Wolf_Boss_Base에 SetTarget(또는 Net_SetTarget) 같은 함수가 있으면 그걸 호출
//            // 없으면 아래처럼 public 함수 하나만 추가하는 게 제일 깔끔함
//            wolfBoss.SetTarget(player);
//            return;
//        }
//    }
//}