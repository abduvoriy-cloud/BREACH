using System;
using UnityEngine;

namespace Strike240
{
    public enum BotDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>Serializable runtime tuning values generated from the selected difficulty.</summary>
    [Serializable]
    public struct BotDifficultySettings
    {
        public float visionRange;
        public float fieldOfView;
        public float detectionDelay;
        public float reactionTime;
        [Range(0f, 1f)] public float accuracy;
        public float fireInterval;
        public float damagePerShot;
        public float health;
        public float movementSpeed;

        public static BotDifficultySettings For(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return new BotDifficultySettings
                    {
                        visionRange = 18f, fieldOfView = 100f, detectionDelay = 0.65f,
                        reactionTime = 0.8f, accuracy = 0.22f, fireInterval = 0.85f,
                        damagePerShot = 8f, health = 70f, movementSpeed = 2.8f
                    };
                case BotDifficulty.Hard:
                    return new BotDifficultySettings
                    {
                        visionRange = 32f, fieldOfView = 120f, detectionDelay = 0.18f,
                        reactionTime = 0.24f, accuracy = 0.72f, fireInterval = 0.46f,
                        damagePerShot = 14f, health = 120f, movementSpeed = 3.8f
                    };
                default:
                    return new BotDifficultySettings
                    {
                        visionRange = 24f, fieldOfView = 110f, detectionDelay = 0.36f,
                        reactionTime = 0.44f, accuracy = 0.48f, fireInterval = 0.62f,
                        damagePerShot = 12f, health = 100f, movementSpeed = 3.2f
                    };
            }
        }
    }
}
