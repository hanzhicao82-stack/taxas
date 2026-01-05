using UnityEngine;

[CreateAssetMenu(fileName = "AIConfig", menuName = "Poker/AI Config")]
public class AIConfig : ScriptableObject
{
    [Header("概率设置")]
    [Tooltip("面对需要跟注时尝试加注的基础概率（会乘以玩家的 aggression）")]
    public float raiseProbability = 0.12f;
    [Tooltip("在无需跟注时尝试下注的基础概率（会乘以玩家的 aggression）")]
    public float betProbability = 0.06f;

    [Header("加注规模")]
    [Tooltip("用于计算加注大小相对于大盲的基准乘数（例如 0.5 表示 0.5 * 大盲）")]
    public float raiseSizeBase = 0.5f;
    [Tooltip("基于 aggression 额外放缩加注规模的系数")]
    public float raiseSizeAggressionScale = 1.0f;

    [Header("其他")]
    [Tooltip("最小加注量相对于大盲的比例（用于向下取整为整数）")]
    public float minRaiseFraction = 0.5f;
    [Tooltip("Monte Carlo 模拟次数，用于估算胜率（次数越多越精确但越慢）")]
    public int simIterations = 200;
    [Tooltip("每次 AI 行动之间的延迟（秒），用于让回合可视化）")]
    public float actionDelay = 0.1f;
}
