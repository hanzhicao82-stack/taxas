using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单的运行时回归测试：在 Play 模式下创建一个 `PokerGame` 实例，运行多手牌流程，
/// 检查每手结束后玩家筹码总和保持不变（筹码守恒）并且没有抛出异常。
/// 将脚本放到场景中并在 Play 时自动执行。
/// </summary>
public class PokerGameFlowTest : MonoBehaviour
{
    // 要运行的手数
    public int handsToPlay = 10;
    public int numPlayers = 4;

    void Start()
    {
        Debug.Log("PokerGameFlowTest: starting flow test...");

        // 创建并配置 PokerGame
        var go = new GameObject("PokerGameTestRunner");
        var game = go.AddComponent<PokerGame>();
        game.numPlayers = numPlayers;

        // 初始化玩家并设定统一筹码，确保可预测的初始总量
        game.players = new List<Player>();
        for (int i = 0; i < numPlayers; i++)
        {
            var p = new Player(i, "P" + (i + 1));
            p.stack = 1000; // 每位玩家初始筹码
            game.players.Add(p);
        }

        int initialTotal = game.players.Sum(p => p.stack);
        Debug.Log($"Initial total chips = {initialTotal}");

        // 运行若干手牌并在每手后检查基本不变量
        for (int h = 0; h < handsToPlay; h++)
        {
            try
            {
                game.StartHand();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during hand {h + 1}: {ex}");
                break;
            }

            int total = game.players.Sum(p => p.stack);
            if (total != initialTotal)
            {
                Debug.LogError($"[FAIL] Chips not conserved after hand {h + 1}: expected {initialTotal}, got {total}");
            }
            else
            {
                Debug.Log($"[OK] After hand {h + 1}: total chips = {total}");
            }
        }

        Debug.Log("PokerGameFlowTest: finished");

        // 清理运行时创建的对象（保留用于调试可注释掉）
        // Destroy(go);
    }
}
