//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Event;

namespace StarForce
{
    public sealed class AsteroidDeadEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(AsteroidDeadEventArgs).GetHashCode();

        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public Asteroid Asteroid
        {
            get;
            private set;
        }

        public int Score
        {
            get;
            private set;
        }

        public static AsteroidDeadEventArgs Create(Asteroid asteroid, int score)
        {
            AsteroidDeadEventArgs eventArgs = ReferencePool.Acquire<AsteroidDeadEventArgs>();
            eventArgs.Asteroid = asteroid;
            eventArgs.Score = score;
            return eventArgs;
        }

        public override void Clear()
        {
            Asteroid = null;
            Score = 0;
        }
    }
}
