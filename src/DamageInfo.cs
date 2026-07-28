using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;


namespace MatchZy
{

    public partial class MatchZy
    {

        private void InitPlayerDamageInfo()
        {
            foreach (var key in playerData.Keys) {
                if (!playerData[key].IsValid) continue;
                if (playerData[key].IsBot) continue;
                int attackerId = key;
                foreach (var key2 in playerData.Keys) {
                    if (key == key2) continue;
                    if (!playerData[key2].IsValid || playerData[key2].IsBot) continue;
                    if (playerData[key].TeamNum == playerData[key2].TeamNum) continue;
                    if (playerData[key].TeamNum == 2) {
                        if (playerData[key2].TeamNum != 3) continue;
                        int targetId = key2;
                        if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
                            playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

                        if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
                            attackerInfo[targetId] = targetInfo = new DamagePlayerInfo();
                    } else if (playerData[key].TeamNum == 3) {
                        if (playerData[key2].TeamNum != 2) continue;
                        int targetId = key2;
                        if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
                            playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

                        if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
                            attackerInfo[targetId] = targetInfo = new DamagePlayerInfo(); 
                    }
                }
            }
        }

		public Dictionary<int, Dictionary<int, DamagePlayerInfo>> playerDamageInfo = new Dictionary<int, Dictionary<int, DamagePlayerInfo>>();
		private void UpdatePlayerDamageInfo(EventPlayerHurt @event, int targetId)
		{
            CCSPlayerController? attacker = @event.Attacker;

            if (!IsPlayerValid(attacker)) return;
			int attackerId = (int)attacker!.UserId!;
			if (!playerDamageInfo.TryGetValue(attackerId, out var attackerInfo))
				playerDamageInfo[attackerId] = attackerInfo = new Dictionary<int, DamagePlayerInfo>();

			if (!attackerInfo.TryGetValue(targetId, out var targetInfo))
				attackerInfo[targetId] = targetInfo = new DamagePlayerInfo();

			targetInfo.DamageHP += @event.DmgHealth;
			targetInfo.Hits++;
		}

        private void ShowDamageInfo()
        {
            if (!enableDamageReport.Value) return;
            try
            {
                foreach (var entry in playerDamageInfo)
                {
                    int attackerId = entry.Key;
                    if (!playerData.TryGetValue(attackerId, out var attackerController)) continue;
                    if (attackerController == null || !attackerController.IsValid) continue;
                    if (attackerController.Connected != PlayerConnectedState.Connected) continue;

                    foreach (var (targetId, targetEntry) in entry.Value)
                    {
                        int damageGiven = targetEntry.DamageHP;
                        if (damageGiven <= 0) continue;

                        if (!playerData.TryGetValue(targetId, out var targetController)) continue;
                        if (targetController == null || !targetController.IsValid) continue;
                        if (targetController.Connected != PlayerConnectedState.Connected) continue;
                        if (!targetController.PlayerPawn.IsValid || targetController.PlayerPawn.Value == null) continue;

                        int targetHP = targetController.PlayerPawn.Value.Health < 0 ? 0 : targetController.PlayerPawn.Value.Health;
                        string targetName = targetController.PlayerName;

                        PrintToPlayerChat(attackerController, $"- {targetName} [{ChatColors.Green}{targetHP} hp{ChatColors.Default}] {damageGiven}");
                    }
                }
                playerDamageInfo.Clear();
            }
            catch (Exception e)
            {
                Log($"[ShowDamageInfo FATAL] An error occurred: {e.Message}");
            }

        }
    }

	public class DamagePlayerInfo
	{
		public int DamageHP { get; set; } = 0;
		public int Hits { get; set; } = 0;
	}
}
