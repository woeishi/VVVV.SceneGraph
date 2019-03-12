using System.Linq;
using System.Collections.Generic;

namespace SceneGraph.Core.Animations
{
    public class AnimationInfo
    {
        public Animation[] Animations { get; }
        internal AnimationInfo(Animation[] animations)
        {
            Animations = animations;
        }
        public static AnimationInfo Create(Animation[] animations) => animations.Length > 0 ? new AnimationInfo(animations) : null;

        public IEnumerable<string> AnimationNames => Animations.Select(a => a.Name);
    }

    public class Animation
    {
        public string Name => description.Name;
        public float Duration => description.Duration;
        public float TicksPerSecond => description.TicksPerSecond;
        AnimationDescription description;
        public IChannel[] Channels { get; }

        Animation(AnimationDescription description, IEnumerable<IChannel> channels = null)
        {
            this.description = description;
            Channels = channels?.ToArray() ?? new IChannel[0];
        }
        public static Animation Create(string name, float duration = 0, float ticksPerSecond = 0, IEnumerable<IChannel> channels = null)
            => new Animation(new AnimationDescription(name, duration, ticksPerSecond), channels);

        public Animation(Animation other, IEnumerable<IChannel> channels = null)
        {
            description = other.description;
            Channels = channels?.ToArray() ?? new IChannel[0];
        }

        class AnimationDescription
        {
            public string Name { get; }
            public float Duration { get; }
            public float TicksPerSecond { get; }
            internal AnimationDescription(string name, float duration = 0, float ticksPerSecond = 0)
            {
                Name = name;
                Duration = duration;
                TicksPerSecond = ticksPerSecond;
            }
        }
    }
}
