using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TiledSharp;

namespace Shiplike
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

        TmxMap map;
        TmxTileset tileSet;
        Texture2D tileTexture;
        Texture2D playerTexture;

        /* -- DEBUG variables -- */
        bool showCollisionGeometry;
        Texture2D collisionTexture;

        int tileWidth;
        int tileHeight;
        int tilesetTilesWide;
        int tilesetTilesHigh;

        PlayerSprite player;
        KeyboardState oldState;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

            map = new TmxMap("Content/ship.tmx");
            tileSet = map.Tilesets[0];
            tileTexture = Content.Load<Texture2D>(tileSet.Name);

            tileWidth = tileSet.TileWidth;
            tileHeight = tileSet.TileHeight;

            tilesetTilesWide = tileTexture.Width / tileWidth;
            tilesetTilesHigh = tileTexture.Height / tileHeight;

            playerTexture = Content.Load<Texture2D>("dot");
            player = new PlayerSprite(playerTexture, map);

            collisionTexture = new Texture2D(GraphicsDevice, 1, 1);
            collisionTexture.SetData(data: new [] {new Color(255, 0, 0, 100)});
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			// For Mobile devices, this logic will close the Game when the Back button is pressed
			// Exit() is obsolete on iOS
#if !__IOS__ && !__TVOS__
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();
#endif

            KeyboardState newState = Keyboard.GetState();

            if (newState.IsKeyDown(Keys.W))
            {
                player.MoveBy(0, -100 * gameTime.ElapsedGameTime.TotalSeconds);
            }
            if (newState.IsKeyDown(Keys.S))
            {
                player.MoveBy(0, 100 * gameTime.ElapsedGameTime.TotalSeconds);
            }
            if (newState.IsKeyDown(Keys.D))
            {
                player.MoveBy(100 * gameTime.ElapsedGameTime.TotalSeconds, 0);
            }
            if (newState.IsKeyDown(Keys.A))
            {
                player.MoveBy(-100 * gameTime.ElapsedGameTime.TotalSeconds, 0);
            }

            // debugging aid
            bool toggleGeometry = newState.IsKeyDown(Keys.LeftShift) && oldState.IsKeyUp(Keys.LeftShift);
            if (toggleGeometry) {
                showCollisionGeometry = !showCollisionGeometry;
            }


            player.Update();

			base.Update(gameTime);

            oldState = newState;
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            int margin = tileSet.Margin;
            int spacing = tileSet.Spacing;

            spriteBatch.Begin();

            for (var i = 0; i < map.Layers[0].Tiles.Count; i++)
            {
                int gid = map.Layers[0].Tiles[i].Gid;

                // Empty tile, do nothing
                if (gid == 0)
                {

                }
                else
                {
                    int tileFrame = gid - 1;
                    int column = tileFrame % tilesetTilesWide;
                    int row = (int)Math.Floor((double)tileFrame / (double)tilesetTilesWide);

                    float x = (i % map.Width) * map.TileWidth;
                    float y = (float)Math.Floor(i / (double)map.Width) * map.TileHeight;

                    Rectangle tilesetRec = new Rectangle(margin + (tileWidth + spacing) * column,
                                                         margin + (tileHeight + spacing) * row,
                                                         tileWidth,
                                                         tileHeight);

                    spriteBatch.Draw(tileTexture, new Rectangle((int)x, (int)y, tileWidth, tileHeight), tilesetRec, Color.White);

                    if (showCollisionGeometry) {
                        var tileSetLookup = map.Tilesets[0].Tiles;

                        // if the tile is not in the tile set, no collision is possible
                        if (!tileSetLookup.ContainsKey(tileFrame))
                            continue;

                        var groups = tileSetLookup[tileFrame].ObjectGroups;
                        // assume that the object groups on the tile represent collision geometry
                        if (groups.Count == 0)
                            continue;

                        var collObjects = groups[0];
                        foreach (var obj in collObjects.Objects)
                        {
                            // check if collision boundary is a rectangle
                            // Tiled editor does not set type, so check attr values
                            if (obj.Width > 0 && obj.Height > 0)
                            {
                                // rectangle is in tile coordinates
                                var rect = new Rectangle((int)Math.Round(obj.X + x),
                                                         (int)Math.Round(obj.Y + y),
                                                         (int)Math.Round(obj.Width),
                                                         (int)Math.Round(obj.Height));
                                spriteBatch.Draw(collisionTexture, rect, Color.White);
                            }
                        }
                    }
                }
            }

            player.DrawOn(spriteBatch);

            spriteBatch.End();

            base.Draw(gameTime);
		}
	}
}
