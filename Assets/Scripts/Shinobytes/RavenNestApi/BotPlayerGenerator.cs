using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenNest.SDK
{
    public class BotPlayerGenerator
    {
        public static BotPlayerGenerator Instance;
        private GameManager gameManager;

        public BotPlayerGenerator(GameManager gameManager)
        {
            Instance = this;
       
            this.gameManager = gameManager;
        }

        internal bool NextOnFerry;

#if UNITY_EDITOR
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
            var skills = GenerateSkills(out var suitableTargetIsland);
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
                State = GenerateState(skills, suitableTargetIsland),
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
                gameManager = UnityEngine.GameObject.FindAnyObjectByType<GameManager>();
            }

            var gameItems = gameManager.Items.GetItems();
            var equippable = gameItems.Where(x => x.RequiredAttackLevel <= skills.AttackLevel && x.RequiredDefenseLevel <= skills.DefenseLevel && x.RequiredRangedLevel <= skills.RangedLevel && x.RequiredMagicLevel <= skills.MagicLevel);
            foreach (var eq in equippable)
            {
                items.Add(new InventoryItem
                {
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
        private CharacterState GenerateState(Models.Skills skills, Island island)
        {
            if (NextOnFerry)
            {
                NextOnFerry = false;

                var ferry = UnityEngine.GameObject.FindAnyObjectByType<FerryController>();
                var ferryPosition = ferry.GetNextPlayerPoint().position;
                return new CharacterState
                {
                    Health = skills.HealthLevel,
                    X = ferryPosition.x,
                    Y = ferryPosition.y,
                    Z = ferryPosition.z,
                };
            }
            var i = gameManager.Islands.Get(island);
            return new CharacterState
            {
                Island = island.ToString(),
                Health = skills.HealthLevel,
                X = i.SpawnPosition.x,
                Y = i.SpawnPosition.y,
                Z = i.SpawnPosition.z,
            };
        }


        private Models.Skills GenerateSkills(out Island targetIsland)
        {
            // generate skills in a range suitable for a target island when Random is selected.

            // home, away, ironhill, kyo, heim, atria, eldara

            targetIsland = Island.Home;
            var min = 1;
            var max = 400;
            var finalIsland = Island.Eldara;
            switch (AdminControlData.SpawnBotLevel)
            {
                case SpawnBotLevelStrategy.Max:
                    targetIsland = finalIsland;
                    min = 999;
                    max = 999;
                    break;

                case SpawnBotLevelStrategy.Min:
                    min = 1;
                    max = 2;
                    break;

                case SpawnBotLevelStrategy.Random:
                    targetIsland = (Island)UnityEngine.Random.Range(1, ((int)finalIsland) + 1);
                    min = IslandManager.IslandLevelRangeMin[targetIsland];
                    max = IslandManager.IslandMaxEffect[targetIsland];
                    break;
            }

            var skills = new Models.Skills
            {
                AttackLevel = UnityEngine.Random.Range(min, max),
                CookingLevel = UnityEngine.Random.Range(min, max),
                CraftingLevel = UnityEngine.Random.Range(min, max),
                HealingLevel = UnityEngine.Random.Range(min, max),
                WoodcuttingLevel = UnityEngine.Random.Range(min, max),
                MagicLevel = UnityEngine.Random.Range(min, max),
                MiningLevel = UnityEngine.Random.Range(min, max),
                StrengthLevel = UnityEngine.Random.Range(min, max),
                RangedLevel = UnityEngine.Random.Range(min, max),
                SailingLevel = UnityEngine.Random.Range(min, max),
                SlayerLevel = UnityEngine.Random.Range(min, max),
                DefenseLevel = UnityEngine.Random.Range(min, max),
                FarmingLevel = UnityEngine.Random.Range(min, max),
                FishingLevel = UnityEngine.Random.Range(min, max),
                GatheringLevel = UnityEngine.Random.Range(min, max),
                AlchemyLevel = UnityEngine.Random.Range(min, max),
            };

            skills.HealthLevel = Math.Max(10, (skills.AttackLevel + skills.DefenseLevel + skills.RangedLevel + skills.StrengthLevel + skills.MagicLevel) / 5);

            return skills;
        }
    }
}