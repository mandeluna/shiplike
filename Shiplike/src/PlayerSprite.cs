using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;

namespace Shiplike
{
    enum PlayerDirection { Left, Right }

    public class PlayerSprite : Sprite
    {
        private TmxMap map;

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Acceleration { get; set; }

        private Animation animation;
        private int currentFrame;
        private double lastFrameRendered = 0;
        private PlayerDirection direction;

        public string CurrentAnimation { get; set; }

        public int Width
        {
            get
            {
                return (animation == null) ? Texture.Width : animation.Width;
            }
        }

        public int Height
        {
            get
            {
                return (animation == null) ? Texture.Height : animation.Height;
            }
        }

        public Texture2D Texture { get; }
        public Rectangle Bounds
        {
            get
            {
                if (CurrentAnimation == null)
                {
                    return this.Texture.Bounds;
                }
                int width = animation.Width;
                int height = animation.Height;

                int index = currentFrame + animation.StartTile(CurrentAnimation);
                int tilesPerRow = Texture.Width / animation.Width;
                int row = index / tilesPerRow;
                int column = index % tilesPerRow;

                return new Rectangle(width * column, height * row, width, height);
            }
        }

        public bool IsOnGround { get; set; }
        public bool CanClimbUp { get; set; }
        public bool CanClimbDown { get; set; }

        // damping of collision impact
        private static float ELASTICITY = 0.25f;

        /* -- static geometry for collision detection -- */
        private List<Shape>[] staticShapes;

#if DEBUG
        private List<Rectangle> collisionRects = new List<Rectangle>();
        public bool ShowCollisionGeometry { get; set; }
        Texture2D collisionTexture;
        Texture2D backgroundTexture;
#endif

        public PlayerSprite(Texture2D texture, TmxMap map, Animation animationSpec)
        {
            this.Texture = texture;
            this.map = map;
            this.animation = animationSpec;
            this.currentFrame = 0;

            TmxObject spawnObject = map.ObjectGroups["Player"].Objects[0];
            Position = new Vector2((float)spawnObject.X, (float)spawnObject.Y);
            Velocity = Vector2.Zero;
            Acceleration = new Vector2(0.0f, 300.0f);

            initializeStaticShapes();
#if DEBUG
            collisionTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            collisionTexture.SetData(data: new[] { new Color(0, 255, 0, 100) });
            backgroundTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(data: new[] { new Color(0, 0, 255, 100) });
#endif
        }

        private void initializeStaticShapes()
        {
            var tiles = map.Layers[0].Tiles;
            staticShapes = new List<Shape>[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                staticShapes[i] = Shapes(tile);
            }
        }

        private List<Shape> Shapes(TmxLayerTile tile)
        {
            var shapes = new List<Shape>();

            var tileFrame = tile.Gid - 1;
            var tileSetLookup = map.Tilesets[0].Tiles;

            if (!tileSetLookup.ContainsKey(tileFrame))
            {
                return shapes;
            }

            var groups = tileSetLookup[tileFrame].ObjectGroups;
            // assume that the object groups on the tile represent collision geometry
            if (groups.Count == 0)
            {
                return shapes;
            }

            var collObjects = groups[0];
            foreach (var obj in collObjects.Objects)
            {
                // check if collision boundary is a rectangle
                // Tiled editor does not set type, so check attr values
                if (obj.Width > 0 && obj.Height > 0)
                {
                    int width = (int)Math.Round(obj.Width);
                    int height = (int)Math.Round(obj.Height);
                    int xoffset = (tile.HorizontalFlip) ? map.TileWidth - (int)obj.X - width : (int)obj.X;
                    int yoffset = (tile.VerticalFlip) ? map.TileHeight - (int)obj.Y - height : (int)obj.Y;

                    var rect = new Rectangle(tile.X * map.TileWidth + xoffset,
                         tile.Y * map.TileHeight + yoffset,
                         width,
                         height);

                    shapes.Add(new Shape(rect));
                }
                else
                {
                    Console.WriteLine("Unsupported collision object {0}", obj);
                }
            }
            return shapes;
        }

        public void DrawOn(SpriteBatch spriteBatch)
        {
            var destinationRectangle = new Rectangle((int)Position.X, (int)Position.Y, this.Width, this.Height);

            if (CurrentAnimation == null)
            {
                spriteBatch.Draw(Texture, destinationRectangle, Color.White);
                return;
            }

            var transform = direction == PlayerDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var origin = new Vector2(animation.Width / 2.0f, 0.0f);

            spriteBatch.Draw(Texture, destinationRectangle, Bounds, Color.White,
                             0.0f, origin, transform, 0.0f);
#if DEBUG
            if (ShowCollisionGeometry) {
                foreach (var collisionRect in collisionRects) {
                    spriteBatch.Draw(collisionTexture, collisionRect, Color.White);
                }
            }
#endif
        }

        public void Update(GameTime gameTime)
        {
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (animation != null)
            {
                if (lastFrameRendered > animation.FrameRate)
                {
                    currentFrame = (currentFrame + 1) % animation.FrameCount(CurrentAnimation);
                    lastFrameRendered = 0;
                }
                else
                {
                    lastFrameRendered += gameTime.ElapsedGameTime.TotalMilliseconds;
                }
            }

            float deltaX = Velocity.X * seconds;
            float deltaY = Velocity.Y * seconds;

            int new_x = (int)(Position.X + deltaX);
            int new_y = (int)(Position.Y + deltaY);
#if DEBUG
            if (ShowCollisionGeometry) {
                collisionRects.Clear();
            }
#endif
            if (deltaX < 0 && direction == PlayerDirection.Right)
            {
                direction = PlayerDirection.Left;
            }
            else if (deltaX > 0 && direction == PlayerDirection.Left)
            {
                direction = PlayerDirection.Right;
            }

            // ensure new coordinates are valid
            Vector2 impact = CheckTileCollisions(new_x, new_y);
            if (impact != Vector2.Zero) {
                impact.Normalize();
                Velocity = -impact * ELASTICITY * Velocity;
                if (!IsOnGround) {
                    return;
                }
                deltaX = Velocity.X * seconds;
                deltaY = Velocity.Y * seconds;
            }

            // don't allow player to sink below the ground
            if (deltaY > 0 && IsOnGround) {
                deltaY = 0;
            }
            Position += new Vector2(deltaX, deltaY);

            // don't apply gravitational acceleration if player is on the ground
            float dy = IsOnGround ? 0 : Acceleration.Y * seconds;
            Velocity += new Vector2(Acceleration.X * seconds, dy);
        }

        private int TileIndexOf(int x_pos, int y_pos)
        {
            if (x_pos < 0 || y_pos < 0)
            {
                return -1;
            }
            int i = x_pos / map.TileWidth;
            if (i > map.Width - 1)
            {
                return -1;
            }
            int j = y_pos / map.TileHeight;
            if (j > map.Height - 1)
            {
                return -1;
            }
            return i + j * map.Width;
        }

        /*
         * Return an impact vector if the point (new_x, new_y) is within the boundaries
         * of one of the collision objects in the tile under that point, and it contains
         * at least one non-transparent pixel of the sprite.
         * 
         * If a collision has occurred, return a Vector indicating the impact
         * location (in the opposite direction of the position vector at the point
         * of impact).
         * 
         * Return a Zero vector if no collision has occurred (Vector2 is a value
         * type so we cannot return null in thise case).
         */
        public Vector2 CheckTileCollisions(int new_x, int new_y)
        {
            // Find collision rectangle in world coordinates
            var spriteRect = new Rectangle(new_x - this.Width / 2, new_y, this.Width, this.Height);

            int left = (new_x - this.Width / 2) / map.TileWidth;
            int right = (new_x + this.Width / 2) / map.TileWidth;
            int top = new_y / map.TileHeight;
            int bottom = (new_y + this.Height) / map.TileHeight;

            Point foot = new Point((int)Position.X, (int)Position.Y + this.Height);

            IsOnGround = false;
            CanClimbUp = false;
            CanClimbDown = false;

            for (int row = top; row <= bottom; row++)
            {
                if (row < 0 || row > map.Height - 1)
                    continue;
                for (int col = left; col <= right; col++)
                {
                    if (col < 0 || col > map.Width - 1)
                        continue;

                    int index = col + row * map.Width;
                    var intersections = staticShapes[index]
                        .FindAll(shape => spriteRect.Intersects(shape.Bounds))
                        .ConvertAll(shape => Rectangle.Intersect(spriteRect, shape.Bounds));

                    IsOnGround |= staticShapes[index]
                        .Find(shape => shape.Bounds.Contains(foot)) != null;
#if DEBUG
                    if (ShowCollisionGeometry)
                    {
                        collisionRects.AddRange(intersections);
                    }
#endif
                    foreach (var intersect in intersections)
                    {
                        intersect.Offset(-(int)new_x + this.Width / 2, -(int)new_y);
                        Vector2 collision = PerPixelCollision(intersect);
                        if (collision != Vector2.Zero)
                        {
                            // since we aren't going to accept the new position at new_x, new_y
                            // find the impact vector which is in the opposite direction of that
                            return new Vector2(collision.X, collision.Y - this.Height / 2);
                        }
                    }
                }
            }

            return Vector2.Zero;
        }

        /*
         * The sprite's bounding rectangle overlaps with a collision region of
         * a tile, check the overlapping area for non-transparent pixels.
         */
        private Vector2 PerPixelCollision(Rectangle rect)
        {
            // Get Color data of the overlapping region
            Color[] bitsA = new Color[Bounds.Width * Bounds.Height];
            this.Texture.GetData(0, Bounds, bitsA, 0, bitsA.Length);

            // For each single pixel in the intersecting rectangle
            for (int y = rect.Y; y < rect.Y + rect.Height; ++y)
            {
                for (int x = rect.X; x < rect.X + rect.Width; ++x)
                {
                    // Get the color from the texture
                    Color a = bitsA[x + y * Bounds.Width];

                    // If any of the pixels in the overlapping region are not transparent
                    // (the alpha channel is not 0), then there is a collision
                    if (a.A != 0)
                        return new Vector2(x, y);
                }
            }
            // If no collision occurred by now, we're clear.
            return Vector2.Zero;
        }
    }
}
