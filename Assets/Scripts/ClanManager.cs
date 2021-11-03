using RavenNest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Assets.Scripts
{
    public class ClanManager
    {
        private readonly GameManager gameManager;

        private readonly ConcurrentDictionary<Guid, Clan> clans
            = new ConcurrentDictionary<Guid, Clan>();

        private readonly ConcurrentDictionary<Guid, List<SkillStat>> clanSkills
            = new ConcurrentDictionary<Guid, List<SkillStat>>();

        public ClanManager(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        internal IReadOnlyList<SkillStat> GetClanSkills(Guid clanId)
        {
            if (!clanSkills.TryGetValue(clanId, out var skills))
                return new List<SkillStat>();
            return skills.ToList();
        }

        internal void UpdateClanSkill(ClanSkillLevelChanged data)
        {
            if (!clans.TryGetValue(data.ClanId, out var clan))
                return;

            var skill = clan.ClanSkills.FirstOrDefault(x => x.Id == data.SkillId);
            if (skill == null)
                return;

            skill.Experience = data.Experience;
            skill.Level = data.Level;

            if (clanSkills.TryGetValue(data.ClanId, out var skills))
            {
                var targetSkill = skills.FirstOrDefault(x => x.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));
                if (targetSkill != null)
                {
                    targetSkill.Set(data.Level, data.Experience);
                }
            }

            if (data.LevelDelta > 0)
            {
                // yay level up!
                GameManager.Log("Clan Skill: " + skill.Name + ", gained " + data.LevelDelta + " new level(s)! Now at " + skill.Level);
            }
        }

        internal void UpdateClanLevel(ClanLevelChanged data)
        {
            if (!clans.TryGetValue(data.ClanId, out var clan))
                return;

            clan.Level = data.Level;
            clan.Experience = data.Experience;
            if (data.LevelDelta > 0)
            {
                // yay level up!
                GameManager.Log("Clan gained " + data.LevelDelta + " new level(s)! Now at " + clan.Level);
            }
        }

        internal void RegisterClan(Clan clan)
        {
            if (clan == null)
                return;

            if (clans.ContainsKey(clan.Id))
            {
                MergeClanSkills(clan);
                return;
            }

            clanSkills[clan.Id] = new List<SkillStat>();
            clanSkills[clan.Id].AddRange(clan.ClanSkills.Select(Map));
            clans[clan.Id] = clan;
        }

        private void MergeClanSkills(Clan clan)
        {
            var newSkills = new List<SkillStat>();
            var exsSkills = clanSkills[clan.Id];
            foreach (var skill in clan.ClanSkills)
            {
                var newSkillLevel = skill.Level;
                var newSkillExp = skill.Experience;

                var targetSkill = exsSkills.FirstOrDefault(x => x.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));
                if (targetSkill == null)
                {
                    // a new skill, map it.
                    newSkills.Add(Map(skill));
                    continue;
                }
                else if (newSkillLevel > targetSkill.Level || (newSkillLevel >= targetSkill.Level && newSkillExp > targetSkill.Experience))
                {
                    targetSkill.Set(newSkillLevel, newSkillExp);
                }
                else
                {
                    newSkills.Add(targetSkill);
                }
            }

            clanSkills[clan.Id] = newSkills;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SkillStat Map(ClanSkill s)
        {
            return new SkillStat
            {
                CurrentValue = s.Level,
                Level = s.Level,
                Experience = s.Experience,
                Name = s.Name
            };
        }
    }
}