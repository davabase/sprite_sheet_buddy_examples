using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Content;

namespace Animation;

class SpriteSheet
{
    /// <summary>
    /// Cache for textures to avoid loading the same texture multiple times.
    /// </summary>
    static readonly Dictionary<string, Texture2D> textureCache = new();

    /// <summary>
    /// The texture associated with this sprite sheet.
    /// </summary>
    public Texture2D texture = null;

    /// <summary>
    /// Dictionary of animations contained in this sprite sheet.
    /// </summary>
    public Dictionary<string, Animation> animations = new();

    /// <summary>
    /// The current animation being played.
    /// </summary>
    public Animation current = null;

    /// <summary>
    /// The name of the current animation being played.
    /// </summary>
    public string currentName = null;

    /// <summary>
    /// A list representing empty events when no events exist in the current animation frame.
    /// </summary>
    private readonly List<string> emptyEvents = new();

    /// <summary>
    /// Reads and parses a sprite sheet from an uncompressed XML file.
    /// </summary>
    /// <param name="filename">The filename of the XML file.</param>
    /// <param name="contentManager">The content manager used to load textures.</param>
    /// <returns>A new SpriteSheet object.</returns>
    public static SpriteSheet ReadXml(string filename, ContentManager contentManager)
    {
        string filePath = contentManager.RootDirectory + "/" + filename;
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        return ParseXml(fileStream, filename, contentManager);
    }

    /// <summary>
    /// Reads and parses a sprite sheet from a compressed XML file (gzip).
    /// </summary>
    /// <param name="filename">The filename of the compressed XML file.</param>
    /// <param name="contentManager">The content manager used to load textures.</param>
    /// <returns>A new SpriteSheet object.</returns>
    public static SpriteSheet ReadCompressedXml(string filename, ContentManager contentManager)
    {
        string filePath = contentManager.RootDirectory + "/" + filename;
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress);
        return ParseXml(gzipStream, filename, contentManager);
    }

    /// <summary>
    /// Parses the XML stream and constructs the SpriteSheet object.
    /// </summary>
    /// <param name="stream">The XML stream.</param>
    /// <param name="filename">The original filename of the XML file.</param>
    /// <param name="contentManager">The content manager used to load textures.</param>
    /// <returns>A new SpriteSheet object.</returns>
    public static SpriteSheet ParseXml(Stream stream, string filename, ContentManager contentManager)
    {
        // XML structure parsing logic.
        using XmlReader reader = XmlReader.Create(stream);
        reader.MoveToContent();

        // Read the file version attribute, if backwards compatibility breaks, we will have to use this to determine how to parse the file.
        string version = reader.GetAttribute("Version");

        string texturePath = reader.GetAttribute("Texture");
        texturePath = Path.GetFileNameWithoutExtension(texturePath);

        string relPath = Path.GetDirectoryName(filename);
        texturePath = Path.Combine(relPath, texturePath);

        SpriteSheet spriteSheet = new();

        if (textureCache.ContainsKey(texturePath))
        {
            spriteSheet.texture = textureCache[texturePath];
        }
        else
        {
            spriteSheet.texture = contentManager.Load<Texture2D>(texturePath);
            textureCache[texturePath] = spriteSheet.texture;
        }

        reader.ReadStartElement("SpriteSheet");
        while (reader.Read())
        {
            if (reader.IsStartElement("Animation"))
            {
                string animationName = reader.GetAttribute("Name");
                Animation animation = new(spriteSheet.texture, new());
                reader.ReadStartElement("Animation");

                while (reader.IsStartElement("Frame"))
                {
                    int x = int.Parse(reader.GetAttribute("X"));
                    int y = int.Parse(reader.GetAttribute("Y"));
                    int width = int.Parse(reader.GetAttribute("Width"));
                    int height = int.Parse(reader.GetAttribute("Height"));
                    float pivotX = float.Parse(reader.GetAttribute("PivotX"));
                    float pivotY = float.Parse(reader.GetAttribute("PivotY"));
                    float time = float.Parse(reader.GetAttribute("Time"));
                    List<string> events = new();
                    List<Shape> shapes = new();

                    reader.ReadStartElement("Frame");

                    while (reader.IsStartElement("Event"))
                    {
                        string eventName = reader.GetAttribute("Name");
                        events.Add(eventName);
                        reader.ReadStartElement("Event");
                        reader.ReadEndElement();
                    }

                    while (reader.IsStartElement("Shape"))
                    {
                        string shapeType = reader.GetAttribute("Type");
                        string shapeTag = reader.GetAttribute("Tag");
                        int shapeX = int.Parse(reader.GetAttribute("X"));
                        int shapeY = int.Parse(reader.GetAttribute("Y"));
                        int shapeWidth = int.Parse(reader.GetAttribute("Width"));
                        int shapeHeight = int.Parse(reader.GetAttribute("Height"));
                        int shapeAngle = int.Parse(reader.GetAttribute("Angle"));

                        shapes.Add(new()
                        {
                            type = shapeType == "Ellipse" ? ShapeType.Ellipse : ShapeType.Rectangle,
                            tag = shapeTag,
                            x = shapeX,
                            y = shapeY,
                            width = shapeWidth,
                            height = shapeHeight,
                            angle = shapeAngle
                        });
                        reader.ReadStartElement("Shape");
                        reader.ReadEndElement();
                    }

                    animation.frames.Add(new Frame()
                    {
                        region = new Rectangle(x, y, width, height),
                        pivot = new Vector2(pivotX, pivotY),
                        time = time,
                        events = events,
                        shapes = shapes
                    });
                }
                reader.ReadEndElement();

                spriteSheet.animations.Add(animationName, animation);
            }
        }
        return spriteSheet;
    }

    /// <summary>
    /// Plays the specified animation by name.
    /// </summary>
    /// <param name="name">The name of the animation to play.</param>
    /// <param name="start">The starting frame (default is 0).</param>
    /// <param name="speed">The speed of the animation (default is 1).</param>
    /// <param name="reversed">Whether to play the animation in reverse (default is false).</param>
    /// <param name="loop">Whether the animation should loop (default is true).</param>
    /// <param name="interrupt">Whether to interrupt the currently playing animation (default is false).</param>
    public void Play(string name, int start = 0, float speed = 1, bool reversed = false, bool loop = true, bool interrupt = false)
    {
        if (animations.TryGetValue(name, out var animation))
        {
            if (currentName == name && current.state == AnimationState.playing && !interrupt)
            {
                return;
            }
            animation.Play(start, speed, reversed, loop);
            current = animation;
            currentName = name;
        }
        else
        {
            Console.WriteLine($"Animation {name} not found");
        }
    }

    /// <summary>
    /// Plays the currently set animation.
    /// </summary>
    /// <param name="start">The starting frame (default is 0).</param>
    /// <param name="speed">The speed of the animation (default is 1).</param>
    /// <param name="reversed">Whether to play the animation in reverse (default is false).</param>
    /// <param name="loop">Whether the animation should loop (default is true).</param>
    public void Play(int start = 0, float speed = 1, bool reversed = false, bool loop = true)
    {
        current?.Play(start, speed, reversed, loop);
    }

    /// <summary>
    /// Pauses the currently playing animation.
    /// </summary>
    /// <param name="paused">Whether to pause (default is true).</param>
    public void Pause(bool paused = true)
    {
        current?.Pause(paused);
    }

    /// <summary>
    /// Stops the currently playing animation.
    /// </summary>
    public void Stop()
    {
        current?.Stop();
    }

    /// <summary>
    /// Retrieves a list of events triggered in the current frame of the animation.
    /// </summary>
    /// <returns>A list of event names.</returns>
    public List<string> GetEvents()
    {
        if (current == null)
        {
            return emptyEvents;
        }
        return current.GetEvents();
    }

    /// <summary>
    /// Updates the current animation based on the elapsed game time.
    /// </summary>
    /// <param name="gameTime">Game time passed since the last update.</param>
    public void Update(GameTime gameTime)
    {
        current?.Update(gameTime);
    }

    /// <summary>
    /// Draws the current frame of the animation.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch used to draw the animation.</param>
    /// <param name="position">Position to draw the sprite.</param>
    /// <param name="rotation">Rotation of the sprite (default is 0).</param>
    /// <param name="scale">Scaling factor (default is 1).</param>
    /// <param name="color">Color tint of the sprite (default is White).</param>
    /// <param name="effects">Sprite effects (default is None).</param>
    /// <param name="flipPivots">Whether to mirror the pivot point based on flip effects (default is true).</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position, float rotation = 0, Vector2? scale = null, Color? color = null, SpriteEffects effects = SpriteEffects.None, bool flipPivots = true)
    {
        current?.Draw(spriteBatch, position, rotation, scale, color, effects, flipPivots);
    }
}

/// <summary>
/// Enum representing the type of shape: either Rectangle or Ellipse.
/// </summary>
enum ShapeType
{
    Rectangle,
    Ellipse
}

/// <summary>
/// Represents a shape within a frame, such as a rectangle or ellipse, and its associated properties.
/// </summary>
class Shape
{
    /// <summary>
    /// The type of the shape (Rectangle or Ellipse).
    /// </summary>
    public ShapeType type;

    /// <summary>
    /// A tag that can be used to identify or categorize the shape.
    /// </summary>
    public string tag;

    /// <summary>
    /// The x-coordinate of the shape's position.
    /// </summary>
    public float x;

    /// <summary>
    /// The y-coordinate of the shape's position.
    /// </summary>
    public float y;

    /// <summary>
    /// The width of the shape.
    /// </summary>
    public float width;

    /// <summary>
    /// The height of the shape.
    /// </summary>
    public float height;

    /// <summary>
    /// The rotation angle of the shape in degrees.
    /// </summary>
    public float angle;
}

/// <summary>
/// Represents a single frame in an animation, including its region, pivot, time duration, events, and shapes.
/// </summary>
struct Frame
{
    /// <summary>
    /// The rectangle representing the portion of the texture used for this frame.
    /// </summary>
    public Rectangle region;

    /// <summary>
    /// The pivot point used for rotation and scaling.
    /// </summary>
    public Vector2 pivot;

    /// <summary>
    /// The time duration (in seconds) for which this frame is displayed.
    /// </summary>
    public float time;

    /// <summary>
    /// A list of events triggered during this frame.
    /// </summary>
    public List<string> events;

    /// <summary>
    /// A list of shapes that are associated with this frame.
    /// </summary>
    public List<Shape> shapes;
}

/// <summary>
/// Enum representing the state of an animation: playing, paused, or stopped.
/// </summary>
public enum AnimationState
{
    playing,
    paused,
    stopped
}

/// <summary>
/// Represents an animation consisting of multiple frames, including controls for playback state, speed, looping, and reversing.
/// </summary>
class Animation
{
    /// <summary>
    /// The texture containing the frames for this animation.
    /// </summary>
    readonly Texture2D texture;

    /// <summary>
    /// A list of frames that make up the animation.
    /// </summary>
    public List<Frame> frames;

    /// <summary>
    /// The index of the current frame being displayed.
    /// </summary>
    public int currentFrame = 0;

    /// <summary>
    /// The index of the previous frame, used to detect when the frame changes.
    /// </summary>
    int previousFrame = -1;

    /// <summary>
    /// A timer that tracks the elapsed time for the current frame.
    /// </summary>
    double timer = 0;

    /// <summary>
    /// The current state of the animation (playing, paused, or stopped).
    /// </summary>
    public AnimationState state = AnimationState.stopped;

    /// <summary>
    /// The speed at which the animation plays. A value of 1 represents normal speed.
    /// </summary>
    float speed = 1;

    /// <summary>
    /// Whether the animation plays in reverse.
    /// </summary>
    bool reversed = false;

    /// <summary>
    /// Whether the animation loops when it reaches the end.
    /// </summary>
    bool loop = true;

    /// <summary>
    /// A list of empty events used as a placeholder when no events are present.
    /// </summary>
    private readonly List<string> emptyEvents = new();

    /// <summary>
    /// A list of empty shapes used as a placeholder when no shapes are present.
    /// </summary>
    private readonly List<Shape> emptyShapes = new();

    /// <summary>
    /// Constructor for creating an Animation with the specified texture and list of frames.
    /// </summary>
    /// <param name="texture">The texture containing the frames of the animation.</param>
    /// <param name="frames">A list of frames for this animation.</param>
    public Animation(Texture2D texture, List<Frame> frames)
    {
        this.texture = texture;
        this.frames = frames;
    }

    /// <summary>
    /// Starts or restarts the animation at the specified frame, with the given speed, direction, and loop settings.
    /// </summary>
    /// <param name="start">The starting frame index.</param>
    /// <param name="speed">The speed of the animation (default is 1).</param>
    /// <param name="reversed">Whether the animation plays in reverse (default is false).</param>
    /// <param name="loop">Whether the animation loops (default is true).</param>
    public void Play(int start = 0, float speed = 1, bool reversed = false, bool loop = true)
    {
        currentFrame = Math.Clamp(start, 0, frames.Count - 1);
        if (speed > 0)
        {
            this.speed = speed;
        }
        this.reversed = reversed;
        this.loop = loop;
        state = AnimationState.playing;
    }

    /// <summary>
    /// Pauses or resumes the animation.
    /// </summary>
    /// <param name="paused">Whether to pause the animation (default is true).</param>
    public void Pause(bool paused = true)
    {
        if (paused && state == AnimationState.playing)
        {
            state = AnimationState.paused;
        }
        else if (!paused && state == AnimationState.paused)
        {
            state = AnimationState.playing;
        }
    }

    /// <summary>
    /// Stops the animation and resets its state.
    /// </summary>
    public void Stop()
    {
        state = AnimationState.stopped;
    }

    /// <summary>
    /// Updates the animation based on the elapsed game time.
    /// </summary>
    /// <param name="gameTime">Game time passed since the last update.</param>
    public void Update(GameTime gameTime)
    {
        if (state == AnimationState.playing)
        {
            previousFrame = currentFrame;
            if (timer > frames[currentFrame].time)
            {
                timer = 0;
                if (!reversed)
                {
                    currentFrame++;
                    if (currentFrame >= frames.Count)
                    {
                        if (loop)
                        {
                            currentFrame = 0;
                        }
                        else
                        {
                            currentFrame = frames.Count - 1;
                            previousFrame = currentFrame;
                            state = AnimationState.stopped;
                        }
                    }
                }
                else
                {
                    currentFrame--;
                    if (currentFrame < 0)
                    {
                        if (loop)
                        {
                            currentFrame = frames.Count - 1;
                        }
                        else
                        {
                            currentFrame = 0;
                            previousFrame = 0;
                            state = AnimationState.stopped;
                        }
                    }
                }
            }
            else
            {
                timer += gameTime.ElapsedGameTime.TotalSeconds * speed;
            }
        }
    }

    /// <summary>
    /// Gets the events triggered by the current frame of the animation.
    /// Call this after the Update method.
    /// </summary>
    /// <returns>A list of event names.</returns>
    public List<string> GetEvents()
    {
        if (previousFrame != currentFrame)
        {
            return frames[currentFrame].events;
        }
        return emptyEvents;
    }

    /// <summary>
    /// Gets the shapes associated with the current frame of the animation.
    /// Call this after the Update method.
    /// </summary>
    /// <returns>A list of shapes.</returns>
    public List<Shape> GetShapes()
    {
        if (previousFrame != currentFrame)
        {
            return frames[currentFrame].shapes;
        }
        return emptyShapes;
    }

    /// <summary>
    /// Gets the elapsed time for the current frame.
    /// </summary>
    /// <returns>The elapsed time for the current frame.</returns>
    public float GetTime()
    {
        return (float)timer;
    }

    /// <summary>
    /// Gets the total duration of the entire animation by summing the time of each frame.
    /// </summary>
    /// <returns>The total duration of the animation.</returns>
    public float GetTotalTime()
    {
        float time = 0;
        foreach (Frame frame in frames)
        {
            time += frame.time;
        }
        return time;
    }

    /// <summary>
    /// Draws the current frame of the animation.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch used to draw the frame.</param>
    /// <param name="position">The position at which to draw the frame.</param>
    /// <param name="rotation">Rotation angle for the frame (default is 0).</param>
    /// <param name="scale">Scaling factor for the frame (default is 1).</param>
    /// <param name="color">Color tint for the frame (default is white).</param>
    /// <param name="effects">Sprite effects like flipping (default is None).</param>
    /// <param name="flipPivots">Whether to mirror the pivot point based on flip effects (default is true).</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position, float rotation = 0, Vector2? scale = null, Color? color = null, SpriteEffects effects = SpriteEffects.None, bool flipPivots = true)
    {
        if (scale == null)
        {
            scale = Vector2.One;
        }
        if (color == null)
        {
            color = Color.White;
        }

        if (currentFrame < frames.Count)
        {
            Frame frame = frames[currentFrame];

            Vector2 origin = frame.pivot;
            if (flipPivots)
            {
                if (effects.HasFlag(SpriteEffects.FlipHorizontally))
                {
                    origin.X = frame.region.Width - origin.X;
                }
                if (effects.HasFlag(SpriteEffects.FlipVertically))
                {
                    origin.Y = frame.region.Height - origin.Y;
                }
            }

            spriteBatch.Draw(texture, position, frame.region, (Color)color, rotation, origin, (Vector2)scale, effects, 0);
        }
    }
}
