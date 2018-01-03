using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;

namespace Shiplike
{
    public class PlayerSprite
    {
        private Texture2D texture;
        private TmxMap map;
        private int x;
        private int y;
        private double world_x;
        private double world_y;
        private AnimationSpec animationSpec;
        private int currentFrame;
        private double lastFrameRendered = 0;

        public string CurrentAnimation { get; set; }

        public PlayerSprite(Texture2D texture, TmxMap map, AnimationSpec animationSpec)
        {
            this.texture = texture;
            this.map = map;
            this.animationSpec = animationSpec;
            this.currentFrame = 0;

            TmxObject spawnObject = map.ObjectGroups["Player Layer"].Objects[0];
            this.x = (int)spawnObject.X;
            this.y = (int)spawnObject.Y;
            this.world_x = x;
            this.world_y = y;
        }

        public void DrawOn(SpriteBatch spriteBatch)
        {
            Rectangle sourceRectangle, destinationRectangle;

            if (CurrentAnimation == null) {
                destinationRectangle = new Rectangle(x, y, texture.Width, texture.Height);
                spriteBatch.Draw(texture, destinationRectangle, Color.White);
                return;
            }
            int width = animationSpec.TileSize;
            int height = animationSpec.TileSize;
            destinationRectangle = new Rectangle(x, y, width, height);

            int index = currentFrame + animationSpec.StartTile(CurrentAnimation);
            int tilesPerRow = texture.Width / animationSpec.TileSize;
            int row = index / tilesPerRow;
            int column = index % tilesPerRow;

            sourceRectangle = new Rectangle(width * column, height * row, width, height);

            var origin = new Vector2(animationSpec.TileSize / 2.0f, 0.0f);
            spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, Color.White,
                             0.0f, origin, SpriteEffects.None, 0.0f);
        }

        public void MoveBy(double deltaX, double deltaY) {
            int oldIndex = TileIndexOf(x, y);

            int new_x = (int)(world_x + deltaX);
            int new_y = (int)(world_y + deltaY);
            int newIndex = TileIndexOf(new_x, new_y);

            // ensure new coordinates are valid
            if (CheckTileCollisions(new_x, new_y))
                return;

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
            if (!tileSetLookup.ContainsKey(tileFrame)) {
                return false;
            }

            var groups = tileSetLookup[tileFrame].ObjectGroups;
            // assume that the object groups on the tile represent collision geometry
            if (groups.Count == 0) {
                return false;
            }

            // determine tile coordinates of new map position
            int tile_x = new_x % map.TileWidth;
            int tile_y = new_y % map.TileHeight;
            int width = (animationSpec == null) ? texture.Width : animationSpec.TileSize;
            Rectangle newRect = new Rectangle(tile_x, tile_y, width, width);

            var collObjects = groups[0];
            foreach (var obj in collObjects.Objects)
            {
                // check if collision boundary is a rectangle
                // Tiled editor does not set type, so check attr values
                if (obj.Width > 0 && obj.Height > 0) {
                    // rectangle is in tile coordinates
                    var rect = new Rectangle((int)Math.Round(obj.X),
                                             (int)Math.Round(obj.Y),
                                             (int)Math.Round(obj.Width),
                                             (int)Math.Round(obj.Height));
                    
                    if (rect.Intersects(newRect))
                        return true;
                }
                else {
                    Console.WriteLine("Unsupported collision object {0", obj);
                }
            }
            return false;
        }

        public string TileDescription(int index) {
            var tiles = map.Layers[0].Tiles;
            if (index < 0) {
                return "Off the map";
            }
            var tile = tiles[index];
            return String.Format("id = {0}", tile.Gid);
        }
    }
}
