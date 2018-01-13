using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;

namespace Shiplike
{
    enum PlayerDirection { Left, Right }

    public class PlayerSprite : Sprite
    {
        private TmxMap map;
        private int x;
        private int y;
        private double world_x;
        private double world_y;
        private List<Rectangle> collisionRects = new List<Rectangle>();
        private AnimationSpec animationSpec;
        private int currentFrame;
        private double lastFrameRendered = 0;
        private PlayerDirection direction;

        /* -- DEBUG variables -- */
        public bool ShowCollisionGeometry { get; set; }
        Texture2D collisionTexture;
        Texture2D backgroundTexture;

        public string CurrentAnimation { get; set; }

        public int Width {
            get {
                return (animationSpec == null) ? Texture.Width : animationSpec.TileSize;
            }
        }

        public int Height {
            get {
                return (animationSpec == null) ? Texture.Height : animationSpec.TileSize;
            }
        }

        public Texture2D Texture { get; }
        public Rectangle Bounds {
            get {
                if (CurrentAnimation == null)
                {
                    return this.Texture.Bounds;
                }
                int width = animationSpec.TileSize;
                int height = animationSpec.TileSize;

                int index = currentFrame + animationSpec.StartTile(CurrentAnimation);
                int tilesPerRow = Texture.Width / animationSpec.TileSize;
                int row = index / tilesPerRow;
                int column = index % tilesPerRow;

                return new Rectangle(width * column, height * row, width, height);
            }
        }

        public PlayerSprite(Texture2D texture, TmxMap map, AnimationSpec animationSpec)
        {
            this.Texture = texture;
            this.map = map;
            this.animationSpec = animationSpec;
            this.currentFrame = 0;

            TmxObject spawnObject = map.ObjectGroups["Player Layer"].Objects[0];
            this.x = (int)spawnObject.X;
            this.y = (int)spawnObject.Y;
            this.world_x = x;
            this.world_y = y;

            collisionTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            collisionTexture.SetData(data: new[] { new Color(0, 255, 0, 100) });
            backgroundTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(data: new[] { new Color(0, 0, 255, 100) });
        }

        public void DrawOn(SpriteBatch spriteBatch)
        {
            var destinationRectangle = new Rectangle(x, y, this.Width, this.Height);

            if (CurrentAnimation == null) {
                spriteBatch.Draw(Texture, destinationRectangle, Color.White);
                return;
            }

            var transform = direction == PlayerDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var origin = new Vector2(animationSpec.TileSize / 2.0f, 0.0f);
            if (ShowCollisionGeometry)
            {
                spriteBatch.Draw(backgroundTexture, destinationRectangle, Bounds, Color.White,
                                 0.0f, origin, transform, 0.0f);
            }

            spriteBatch.Draw(Texture, destinationRectangle, Bounds, Color.White,
                             0.0f, origin, transform, 0.0f);

            if (ShowCollisionGeometry) {
                foreach (var collisionRect in collisionRects) {
                    spriteBatch.Draw(collisionTexture, collisionRect, Color.White);
                }
            }
        }

        public void MoveBy(double deltaX, double deltaY) {
            int oldIndex = TileIndexOf(x, y);

            int new_x = (int)(world_x + deltaX);
            int new_y = (int)(world_y + deltaY);
            int newIndex = TileIndexOf(new_x, new_y);

            // ensure new coordinates are valid
            if (CheckTileCollisions(new_x, new_y))
                return;

            if (deltaX < 0 && direction == PlayerDirection.Right) {
                direction = PlayerDirection.Left;
            }
            else if (deltaX > 0 && direction == PlayerDirection.Left) {
                direction = PlayerDirection.Right;
            }

            world_x += deltaX;
            world_y += deltaY;
        }

        public void Update(GameTime gameTime) {
            x = (int)world_x;
            y = (int)world_y;

            if (animationSpec != null) {
                if (lastFrameRendered > animationSpec.FrameRate) {
                    currentFrame = (currentFrame + 1) % animationSpec.FrameCount(CurrentAnimation);
                    lastFrameRendered = 0;
                }
                else {
                    lastFrameRendered += gameTime.ElapsedGameTime.TotalMilliseconds;
                }
            }
        }

        private int TileIndexOf(int x_pos, int y_pos) {
            if (x_pos < 0 || y_pos < 0) {
                return -1;
            }
            int i = x_pos / map.TileWidth;
            if (i > map.Width - 1) {
                return -1;
            }
            int j = y_pos / map.TileHeight;
            if (j > map.Height - 1) {
                return -1;
            }
            return i + j * map.Width;
        }

        /*
         * Return true if the point (new_x, new_y) is within the boundaries
         * of one of the collision objects in the tile under that point
         */
        public Boolean CheckTileCollisions(int new_x, int new_y) {
            var tiles = map.Layers[0].Tiles;
            int index = TileIndexOf(new_x, new_y);
            if (index < 0)
            {
                // treat off the map as a collision
                return true;
            }
            var tile = tiles[index];
            var tileFrame = tile.Gid - 1;
            var tileSetLookup = map.Tilesets[0].Tiles;

            // if the tile is not in the tile set, no collision is possible
            //if (!tileSetLookup.ContainsKey(tileFrame)) {
            //    return false;
            //}

            var groups = tileSetLookup[tileFrame].ObjectGroups;
            // assume that the object groups on the tile represent collision geometry
            if (groups.Count == 0) {
                return false;
            }

            // Find collision rectangle in world coordinates
            var spriteRect = new Rectangle(new_x, new_y, this.Width, this.Height);

            var collObjects = groups[0];
            if (ShowCollisionGeometry) {
                collisionRects.Clear();
            }
            foreach (var obj in collObjects.Objects)
            {
                // check if collision boundary is a rectangle
                // Tiled editor does not set type, so check attr values
                if (obj.Width > 0 && obj.Height > 0) {
                    int width = (int)Math.Round(obj.Width);
                    int height = (int)Math.Round(obj.Height);
                    int xoffset = (tile.HorizontalFlip) ? map.TileWidth - (int)obj.X - width : (int)obj.X;
                    int yoffset = (tile.VerticalFlip) ? map.TileHeight - (int)obj.Y - height : (int)obj.Y;

                    var rect = new Rectangle(tile.X * map.TileWidth + xoffset,
                         tile.Y * map.TileHeight + yoffset,
                         width,
                         height);

                    if (spriteRect.Intersects(rect))
                    {
                        var worldRect = Rectangle.Intersect(spriteRect, rect);
                        if (ShowCollisionGeometry) {
                            collisionRects.Add(worldRect);
                        }
                        var intersect = new Rectangle(worldRect.X - new_x,
                                                      worldRect.Y - new_y,
                                                      worldRect.Width,
                                                      worldRect.Height);
                        if (!ShowCollisionGeometry && PerPixelCollision(intersect)) {
                            return true;
                        }
                    }
                    if (ShowCollisionGeometry) {
                        foreach (var worldRect in collisionRects) {
                            var intersect = new Rectangle(worldRect.X - new_x,
                                                          worldRect.Y - new_y,
                                                          worldRect.Width,
                                                          worldRect.Height);
                            if (PerPixelCollision(intersect)) {
                                return true;
                            }
                        }
                    }
                }
                else {
                    Console.WriteLine("Unsupported collision object {0}", obj);
                }
            }
            return false;
        }

        /*
         * The sprite's bounding rectangle overlaps with a collision region of
         * a tile, check the overlapping area for non-transparent pixels.
         * If any exist, a collision has occurred.
         */
        private bool PerPixelCollision(Rectangle rect)
        {
            // Get Color data of the overlapping region
            Color[] bitsA = new Color[Bounds.Width * Bounds.Height];
            this.Texture.GetData(0, Bounds, bitsA, 0, bitsA.Length);

            // For each single pixel in the intersecting rectangle
            for (int y = 0; y < rect.Height; ++y)
            {
                for (int x = 0; x < rect.Width; ++x)
                {
                    // Get the color from the texture
                    Color a = bitsA[(x + rect.X) + (y + rect.Y) * Bounds.Width];

                    if (a.A != 0) // If any of the pixels in the overlapping region are not transparent (the alpha channel is not 0), then there is a collision
                        return true;
                }
            }
            // If no collision occurred by now, we're clear.
            return false;
        }
    }
}
