using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Animation;

namespace Example;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private SpriteSheet spriteSheet;
    private SpriteEffects flip = SpriteEffects.None;
    bool jumping = false;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        spriteSheet = SpriteSheet.ReadCompressedXml("Character.sprite", Content);
        spriteSheet.Play("run");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (jumping)
        {
            if (spriteSheet.current.state == AnimationState.stopped)
            {
                jumping = false;
            }
        }

        flip = SpriteEffects.None;
        if (Mouse.GetState().Position.X < graphics.PreferredBackBufferWidth / 2)
        {
            flip = SpriteEffects.FlipHorizontally;
        }

        if (!jumping)
        {
            spriteSheet.Play("run", spriteSheet.current.currentFrame);
            if (Mouse.GetState().Position.Y > graphics.PreferredBackBufferHeight / 2 + graphics.PreferredBackBufferHeight / 4)
            {
                // Since each animation has the same running frames we can start the animation at the current frame and it will sync up instead of starting at the beginning.
                spriteSheet.Play("run_aim_down", spriteSheet.current.currentFrame);
            }
            else if (Mouse.GetState().Position.Y < graphics.PreferredBackBufferHeight / 2 - graphics.PreferredBackBufferHeight / 4)
            {
                spriteSheet.Play("run_aim_up", spriteSheet.current.currentFrame);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                spriteSheet.Play("jump", loop: false);
                jumping = true;
            }
        }

        spriteSheet.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        spriteSheet.Draw(spriteBatch, new Vector2(graphics.PreferredBackBufferWidth / 2f, graphics.PreferredBackBufferHeight / 2f), scale: new Vector2(5, 5), effects: flip);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
