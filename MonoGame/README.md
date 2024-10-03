## MonoGame Project Setup
After creating your project like normal, by following the [getting started guide](https://docs.monogame.net/articles/getting_started/index.html), copy `Animation.cs` into your project.

Add your sprite sheet image to your `Content` folder using the **Content Builder**, [follow these steps](https://docs.monogame.net/articles/getting_started/4_adding_content.html). Additionally add your sprite sheet atlas from **Sprite Sheet Buddy**, this will be a `.xml` for uncompressed export or `.gz`/`.sprite` for compressed export. Note that the file extension is meaningless for the compressed version.

Make sure you set the `Build Action` in the **Content Builder** to `Copy` instead of `Build` otherwise your build step will fail. This will tell the **Content Builder** to just copy the file to the output folder rather than trying to build it into some other format.

Now both your sprite sheet image and your sprite atlas can be read from within your MonoGame project.

To edit the atlas open `Character.xml` in **Sprite Sheet Buddy**. Note that this xml file is for use in **Sprite Sheet Buddy**, it is not an exported xml file for animation runtimes.

## Using Animation.cs
`Animation.cs` has good documentation for what each function does but the basic setup includes:

* Create a sprite sheet:
```csharp
spriteSheet = SpriteSheet.ReadCompressedXml("Character.sprite", Content)
```
* Play an animation:
```csharp
spriteSheet.Play("run")
```
* Update the animation in the Update function:
```csharp
spriteSheet.Update(gameTime)
```
* Draw the sprite sheet inside a sprite batch lifecycle:
```csharp
spriteSheet.Draw(spriteBatch, Vector2.Zero)
```

The character animation used in the example is by [Anokolisa](https://anokolisa.itch.io/action).