using System;
using System.Collections.Generic;

namespace Shiplike
{
    public class Animation
    {
        private Dictionary<string, Tuple<int, int>> animations;

        public int Width { get; }
        public int Height { get; }
        public int FrameRate { get; set; }

        public Animation(int width, int height, int rate)
        {
            this.animations = new Dictionary<string, Tuple<int, int>>();
            this.Width = width;
            this.Height = height;
            this.FrameRate = rate;
        }

        public void addAnimation(string animationKey, int startTile, int tileCount)
        {
            if (animations.ContainsKey(animationKey)) {
                throw new ArgumentException("Duplicate animation keys are not permitted");
            }
            animations.Add(animationKey, new Tuple<int, int>(startTile, tileCount));
        }

        public int StartTile(string animationKey)
        {
            if (!animations.ContainsKey(animationKey)) {
                throw new ArgumentException("No such key {0}", animationKey);
            }
            return animations[animationKey].Item1;
        }

        public int FrameCount(string animationKey)
        {
            if (!animations.ContainsKey(animationKey))
            {
                throw new ArgumentException("No such key {0}", animationKey);
            }
            return animations[animationKey].Item2;
        }
}
}