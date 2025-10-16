using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FullCrisis3.Core.Graphics;

public class BackgroundAnimation
{
    private readonly GraphicsDevice _graphicsDevice;
    
    // Fallback animated background properties
    private float _time;
    private readonly Random _random;
    private readonly Vector2[] _particles;
    private readonly float[] _particleSpeeds;
    private readonly Color[] _particleColors;

    public BackgroundAnimation(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _random = new Random();
        
        // Initialize particle system for fallback animation
        _particles = new Vector2[50];
        _particleSpeeds = new float[50];
        _particleColors = new Color[50];
        
        InitializeParticles();
    }

    private void InitializeParticles()
    {
        var viewport = _graphicsDevice.Viewport;
        
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i] = new Vector2(
                _random.Next(0, viewport.Width),
                _random.Next(0, viewport.Height)
            );
            _particleSpeeds[i] = _random.Next(10, 50);
            
            // Create a gradient of blue/purple colors
            var colorValue = _random.Next(100, 255);
            _particleColors[i] = new Color(colorValue / 3, colorValue / 2, colorValue);
        }
    }

    public void LoadVideo(string videoPath)
    {
        Console.WriteLine($"Video placeholder: {videoPath}");
        Console.WriteLine("To add video support:");
        Console.WriteLine($"1. Add your .mp4 file to Content/{videoPath}.mp4");
        Console.WriteLine("2. Add it to Content.mgcb with Video processor");
        Console.WriteLine("3. Add MonoGame.Framework.Content.Pipeline.EffectImporter package");
        Console.WriteLine("4. Implement video playback using platform-specific APIs");
    }

    public void Play()
    {
        // Video playback would start here when implemented
    }

    public void Stop()
    {
        // Video playback would stop here when implemented
    }

    public void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update particle animation
        UpdateParticles(gameTime);
    }

    private void UpdateParticles(GameTime gameTime)
    {
        var viewport = _graphicsDevice.Viewport;
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        for (int i = 0; i < _particles.Length; i++)
        {
            // Move particles diagonally
            _particles[i].X += _particleSpeeds[i] * deltaTime * 0.3f;
            _particles[i].Y += _particleSpeeds[i] * deltaTime * 0.2f;
            
            // Wrap around screen
            if (_particles[i].X > viewport.Width + 10)
                _particles[i].X = -10;
            if (_particles[i].Y > viewport.Height + 10)
                _particles[i].Y = -10;
            
            // Add some floating motion
            _particles[i].Y += (float)Math.Sin(_time + i) * 0.5f;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        // Draw animated background (video support can be added later)
        DrawFallbackBackground(spriteBatch, pixelTexture);
    }

    private void DrawFallbackBackground(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var viewport = _graphicsDevice.Viewport;
        
        // Draw gradient background
        var gradientHeight = viewport.Height / 3;
        for (int y = 0; y < viewport.Height; y += 4)
        {
            var progress = (float)y / viewport.Height;
            var color = Color.Lerp(new Color(20, 20, 40), new Color(60, 40, 80), progress);
            spriteBatch.Draw(pixelTexture, new Rectangle(0, y, viewport.Width, 4), color);
        }
        
        // Draw animated particles
        for (int i = 0; i < _particles.Length; i++)
        {
            var alpha = (float)(0.3 + 0.3 * Math.Sin(_time + i * 0.1));
            var color = _particleColors[i] * alpha;
            var size = 2 + (int)(2 * Math.Sin(_time * 2 + i * 0.2));
            
            spriteBatch.Draw(pixelTexture, 
                new Rectangle((int)_particles[i].X, (int)_particles[i].Y, size, size), 
                color);
        }
        
        // Draw some larger floating orbs
        for (int i = 0; i < 5; i++)
        {
            var orbX = (viewport.Width / 6f) * (i + 1) + (float)Math.Sin(_time * 0.5f + i) * 50;
            var orbY = viewport.Height * 0.7f + (float)Math.Cos(_time * 0.3f + i) * 30;
            var orbSize = 20 + (int)(10 * Math.Sin(_time + i));
            var orbAlpha = 0.1f + 0.1f * (float)Math.Sin(_time * 2 + i);
            
            spriteBatch.Draw(pixelTexture,
                new Rectangle((int)orbX, (int)orbY, orbSize, orbSize),
                Color.Blue * orbAlpha);
        }
    }

    public void Dispose()
    {
        // Cleanup resources when video support is added
    }
}