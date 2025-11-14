using Enemies;

namespace WaveSurvival.Utils.Extensions
{
    internal static class EnemyAgentExt
    {
        public static void AddOnDeadOnce(this EnemyAgent agent, Action onDead)
        {
            var called = false;
            agent.add_OnDeadCallback(new Action(() =>
            {
                if (called || CheckpointManager.IsReloadingCheckpoint)
                    return;

                onDead?.Invoke();
                called = true;
            }));
        }
    }
}
