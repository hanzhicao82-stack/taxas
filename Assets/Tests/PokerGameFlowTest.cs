using System;
using System.Linq;
using System.Collections;
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

    public IEnumerator Run(AIConfig config, int handsToPlay, int numPlayers)
    {
        Debug.Log("PokerGameFlowTest: starting flow test...");

        // 创建并配置 PokerGame
        var go = new GameObject("PokerGameTestRunner");
        var game = go.AddComponent<PokerGame>();
        game.numPlayers = numPlayers;
        game.aiConfig = config;

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

        // 运行若干手牌并在每手后检查基本不变量（每手之间让出一帧，避免阻塞主线程）
        for (int h = 0; h < handsToPlay; h++)
        {
            // 使用带超时保护的执行，防止任意一手因 AI/逻辑问题卡住整个测试
            float timeoutSec = 60f; // 超时阈值，可根据需要调整
            bool completed = false;
            yield return StartCoroutine(RunWithTimeout(game.StartHandRoutine(), timeoutSec, (ok) => completed = ok));

            if (!completed)
            {
                Debug.LogError($"[ERROR] Hand {h + 1} did not complete within {timeoutSec} seconds (timed out)");
                break;
            }

            int total = game.players.Sum(p => p.stack);
            if (total != initialTotal)
            {
                Debug.LogError($"[FAIL] Chips not conserved after hand {h + 1}: expected {initialTotal}, got {total}");
            }
            else
            {
                Debug.LogWarning($"[OK] After hand {h + 1}: total chips = {total}");
            }

            // 异步点：等待一帧再继续下一手（在 Play 模式下非阻塞）
            yield return null;
        }

        Debug.LogWarning("PokerGameFlowTest: finished");

        // 清理运行时创建的对象（保留用于调试可注释掉）
        // Destroy(go);
    }

    // Helper: run an IEnumerator with a timeout, invoke callback with true if completed, false if timed out
    private IEnumerator RunWithTimeout(IEnumerator routine, float timeoutSeconds, Action<bool> callback)
    {
        var start = Time.realtimeSinceStartup;
        // Drive the inner routine manually
        var enumerator = routine;
        bool moveNext = true;
        while (true)
        {
            if (Time.realtimeSinceStartup - start > timeoutSeconds)
            {
                callback?.Invoke(false);
                yield break;
            }

            try
            {
                moveNext = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in inner routine: {ex}");
                callback?.Invoke(false);
                yield break;
            }

            if (!moveNext)
            {
                callback?.Invoke(true);
                yield break;
            }

            // If the inner yielded an IEnumerator, we should run it to completion before continuing
            var yielded = enumerator.Current;
            if (yielded is IEnumerator nested)
            {
                yield return StartCoroutine(RunWithTimeout(nested, timeoutSeconds - (Time.realtimeSinceStartup - start), callback));
                // If nested timed out (callback false) then propagate timeout
                // Note: callback may have been invoked by nested; check elapsed
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                {
                    yield break;
                }
            }
            else
            {
                // Respect yielded instructions (WaitForSeconds, null, etc.) by yielding it
                yield return yielded;
            }
        }
    }
}
