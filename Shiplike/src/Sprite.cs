using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shiplike
{
    public interface Sprite
    {
        Texture2D Texture { get; }
        Rectangle Bounds { get; }
    }
}
