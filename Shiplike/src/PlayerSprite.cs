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

        private AnimationSpec animationSpec;
        private int currentFrame;
        private double lastFrameRendered = 0;
        private PlayerDirection direction;

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

        /* -- static geometry for collision detection -- */
        private List<Shape>[] staticShapes;

        /* -- DEBUG variables -- */
        private List<Rectangle> collisionRects = new List<Rectangle>();
        public bool ShowCollisionGeometry { get; set; }
        Texture2D collisionTexture;
        Texture2D backgroundTexture;

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

            initializeStaticShapes();

            collisionTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            collisionTexture.SetData(data: new[] { new Color(0, 255, 0, 100) });
            backgroundTexture = new Texture2D(texture.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(data: new[] { new Color(0, 0, 255, 100) });
        }

        private void initializeStaticShapes() {
            var tiles = map.Layers[0].Tiles;
            staticShapes = new List<Shape>[tiles.Count];

            for (var i = 0; i < tiles.Count; i++) {
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

            if (ShowCollisionGeometry)
            {
                collisionRects.Clear();
            }

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

            // Find collision rectangle in world coordinates
            var spriteRect = new Rectangle(new_x - this.Width / 2, new_y, this.Width, this.Height);

            int left = (new_x - this.Width / 2) / map.TileWidth;
            int right = (new_x + this.Width / 2) / map.TileWidth;
            int top = new_y / map.TileHeight;
            int bottom = (new_y + this.Height) / map.TileHeight;

            for (int row = top; row <= bottom; row++) {
                if (row < 0 || row > map.Height - 1)
                    continue;
                for (int col = left; col <= right; col++) {
                    if (col < 0 || col > map.Width - 1)
                        continue;

                    int index = col + row * map.Width;
                    var intersections = staticShapes[index]
                        .FindAll(shape => spriteRect.Intersects(shape.Bounds))
                        .ConvertAll(shape => Rectangle.Intersect(spriteRect, shape.Bounds));

                    if (ShowCollisionGeometry)
                    {
                        collisionRects.AddRange(intersections);
                        //collisionRects.AddRange(staticShapes[index].ConvertAll(shape => shape.Bounds));
                    }

                    foreach (var intersect in intersections)
                    {
                        intersect.Offset(-(int)new_x + this.Width / 2, -(int)new_y);
                        if (PerPixelCollision(intersect))
                        {
                            return true;
                        }
                    }
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
            for (int y = rect.Y; y < rect.Y + rect.Height; ++y)
            {
                for (int x = rect.X; x < rect.X + rect.Width; ++x)
                {
                    // Get the color from the texture
                    Color a = bitsA[x + y * Bounds.Width];

                    if (a.A != 0) // If any of the pixels in the overlapping region are not transparent (the alpha channel is not 0), then there is a collision
                        return true;
                }
            }
            // If no collision occurred by now, we're clear.
            return false;
        }
    }
}
