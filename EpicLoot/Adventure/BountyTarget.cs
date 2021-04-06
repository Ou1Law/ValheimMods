﻿using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Character))]
    public class BountyTarget : MonoBehaviour
    {
        public const string BountyTargetKey = "BountyTarget";
        public const string MonsterIDKey = "MonsterID";
        public const string IsAddKey = "IsAdd";
        public const string OriginalNameKey = "OriginalName";

        private Character _character;
        private BountyInfo _bountyInfo;
        private string _monsterID;
        private bool _isAdd;

        public void Awake()
        {
            _character = GetComponent<Character>();
            _character.m_onDeath += OnDeath;
        }

        public void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDeath -= OnDeath;
            }
        }

        private void OnDeath()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                var saveData = player.GetAdventureSaveData();
                if (saveData.GetBountyInfoByID(_bountyInfo.ID) != null && _bountyInfo.State == BountyState.InProgress)
                {
                    AdventureDataManager.Bounties.SlayBountyTarget(_bountyInfo, _monsterID, _isAdd);
                }
            }
        }

        public void Setup(BountyInfo bounty, string monsterID, bool isAdd, bool initialSetup)
        {
            _bountyInfo = bounty;
            _monsterID = monsterID;
            _isAdd = isAdd;

            var zdo = _character.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                zdo.Set(BountyTargetKey, _bountyInfo.ID);
                zdo.Set(MonsterIDKey, monsterID);
                zdo.Set(IsAddKey, isAdd);
                if (initialSetup)
                {
                    zdo.Set(OriginalNameKey, _character.m_name);
                }
            }

            if (initialSetup)
            {
                _character.SetLevel(GetMonsterLevel(bounty, monsterID, isAdd));
                _character.SetMaxHealth(GetModifiedMaxHealth(_character, bounty, isAdd));
            }

            var originalName = zdo?.GetString(OriginalNameKey) ?? "unknown";
            _character.m_name = GetCharacterName(originalName, isAdd, bounty.TargetName);
            _character.m_baseAI.SetPatrolPoint();
            _character.m_boss = !isAdd;
        }

        private static float GetModifiedMaxHealth(Character character, BountyInfo bounty, bool isAdd)
        {
            if (isAdd)
            {
                return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.AddsHealthMultiplier;
            }
            else if (bounty.RewardGold > 0)
            {
                return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.GoldHealthMultiplier;
            }
            else
            {
                return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.IronHealthMultiplier;
            }
        }

        private static string GetCharacterName(string originalName, bool isAdd, string targetName)
        {
            return isAdd ? 
                Localization.instance.Localize("$mod_epicloot_bounties_minionname", originalName) 
                : (string.IsNullOrEmpty(targetName) ? originalName : targetName);
        }

        private static int GetMonsterLevel(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (isAdd)
            {
                foreach (var targetInfo in bounty.Adds)
                {
                    if (targetInfo.MonsterID == monsterID)
                    {
                        return targetInfo.Level;
                    }
                }

                return 1;
            }
            else
            {
                return bounty.Target.Level;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    public static class Character_Start_Patch
    {
        public static void Postfix(Character __instance)
        {
            var zdo = __instance.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                var bountyID = zdo.GetString(BountyTarget.BountyTargetKey);
                if (!string.IsNullOrEmpty(bountyID))
                {
                    var bountyInfo = Player.m_localPlayer?.GetAdventureSaveData().GetBountyInfoByID(bountyID);
                    if (bountyInfo != null)
                    {
                        var bountyTarget = __instance.gameObject.AddComponent<BountyTarget>();
                        var monsterID = zdo.GetString(BountyTarget.MonsterIDKey);
                        var isAdd = zdo.GetBool(BountyTarget.IsAddKey);
                        bountyTarget.Setup(bountyInfo, monsterID, isAdd, false);
                    }
                }
            }
        }
    }
}
