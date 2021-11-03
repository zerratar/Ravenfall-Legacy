using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenNest.SDK
{
    public class BotPlayerGenerator
    {

#if UNITY_EDITOR
        private GameManager gameManager;
#endif
        public PlayerJoinResult Generate(PlayerJoinData joinData)
        {
            return new PlayerJoinResult
            {
                Success = true,
                Player = GeneratePlayer(joinData)
            };
        }

        private static SyntyAppearance GenerateRandomSyntyAppearance()
        {
            var gender = Utility.Random<Gender>();
            var skinColor = GetHexColor(Utility.Random<SkinColor>());
            var hairColor = GetHexColor(Utility.Random<HairColor>());
            var beardColor = GetHexColor(Utility.Random<HairColor>());
            return new SyntyAppearance
            {
                Id = Guid.NewGuid(),
                Gender = gender,
                SkinColor = skinColor,
                HairColor = hairColor,
                BeardColor = beardColor,
                StubbleColor = skinColor,
                WarPaintColor = hairColor,
                EyeColor = "#000000",
                Eyebrows = Utility.Random(0, gender == Gender.Male ? 10 : 7),
                Hair = Utility.Random(0, 38),
                FacialHair = gender == Gender.Male ? Utility.Random(0, 18) : -1,
                Head = Utility.Random(0, 23),
                HelmetVisible = true,
                Cape = -1,
            };
        }
        public enum SkinColor
        {
            Light,
            Medium,
            Dark
        }
        public enum HairColor
        {
            Black,
            Blonde,
            Blue,
            Brown,
            Grey,
            Pink,
            Red
        }

        private Models.Player GeneratePlayer(PlayerJoinData joinData)
        {
            var skills = GenerateSkills();
            var inventoryItems = GenerateEquipment(skills);
            return new Models.Player
            {
                Id = Guid.NewGuid(),
                Identifier = joinData.Identifier,
                Appearance = GenerateRandomSyntyAppearance(),
                CharacterIndex = 0,
                Clan = null,
                ClanRole = null,
                InventoryItems = inventoryItems,
                State = GenerateState(skills),
                Resources = new Resources(),
                Statistics = new Statistics(),
                Skills = skills,
                UserId = joinData.UserId,
                UserName = joinData.UserName,
                Name = joinData.UserName
            };
        }

        private List<InventoryItem> GenerateEquipment(Models.Skills skills)
        {
            var items = new List<InventoryItem>();

#if UNITY_EDITOR && DEBUG
            if (!gameManager)
            {
                gameManager = UnityEngine.GameObject.FindObjectOfType<GameManager>();
            }

            var gameItems = gameManager.Items.GetItems();
            var equippable = gameItems.Where(x => x.RequiredAttackLevel <= skills.AttackLevel && x.RequiredDefenseLevel <= skills.DefenseLevel && x.RequiredRangedLevel <= skills.RangedLevel && x.RequiredMagicLevel <= skills.MagicLevel);
            foreach (var eq in equippable)
            {
                items.Add(new InventoryItem { 
                    ItemId = eq.Id,
                    Id = Guid.NewGuid(),
                    Amount = 1,
                });
            }
#endif

            return items;
        }

        private static string GetHexColor(HairColor color)
        {
            switch (color)
            {
                case HairColor.Blonde:
                    return "#A8912A";
                case HairColor.Blue:
                    return "#0D9BB9";
                case HairColor.Brown:
                    return "#3C2823";
                case HairColor.Grey:
                    return "#595959";
                case HairColor.Pink:
                    return "#DF62C7";
                case HairColor.Red:
                    return "#C52A4A";
                default:
                    return "#000000";
            }
        }

        private static string GetHexColor(SkinColor color)
        {
            switch (color)
            {
                case SkinColor.Light:
                    return "#d6b8ae";
                case SkinColor.Medium:
                    return "#faa276";
                default:
                    return "#40251e";
            }
        }
        private CharacterState GenerateState(Models.Skills skills)
        {
            return new CharacterState
            {
                Health = skills.HealthLevel,
            };
        }

        private Models.Skills GenerateSkills()
        {
            var skills = new Models.Skills
            {
                AttackLevel = UnityEngine.Random.Range(1, 400),
                CookingLevel = UnityEngine.Random.Range(1, 400),
                CraftingLevel = UnityEngine.Random.Range(1, 400),
                HealingLevel = UnityEngine.Random.Range(1, 400),
                WoodcuttingLevel = UnityEngine.Random.Range(1, 400),
                MagicLevel = UnityEngine.Random.Range(1, 400),
                HealthLevel = 10,
                MiningLevel = UnityEngine.Random.Range(1, 400),
                StrengthLevel = UnityEngine.Random.Range(1, 400),
                RangedLevel = UnityEngine.Random.Range(1, 400),
                SailingLevel = UnityEngine.Random.Range(1, 400),
                SlayerLevel = UnityEngine.Random.Range(1, 400),
                DefenseLevel = UnityEngine.Random.Range(1, 400),
                FarmingLevel = UnityEngine.Random.Range(1, 400),
                FishingLevel = UnityEngine.Random.Range(1, 400),

            };

            skills.HealthLevel = (skills.AttackLevel + skills.DefenseLevel + skills.RangedLevel + skills.StrengthLevel + skills.MagicLevel) / 5;

            return skills;
        }
    }
}