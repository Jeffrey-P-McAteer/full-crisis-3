using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Graphics;
using System.Collections.Generic;

namespace FullCrisis3.Core.Assets;

public class AssetManager
{
    private readonly Dictionary<string, BitmapFont> _fonts;
    private readonly Dictionary<string, Texture2D> _textures;
    private readonly GraphicsDevice _graphicsDevice;

    public AssetManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _fonts = new Dictionary<string, BitmapFont>();
        _textures = new Dictionary<string, Texture2D>();
    }

    public void LoadFont(string name)
    {
        if (!_fonts.ContainsKey(name))
        {
            _fonts[name] = new BitmapFont(_graphicsDevice);
        }
    }

    public void LoadTexture(string name, Texture2D texture)
    {
        if (!_textures.ContainsKey(name))
        {
            _textures[name] = texture;
        }
    }

    public BitmapFont GetFont(string name)
    {
        return _fonts.TryGetValue(name, out var font) ? font : throw new KeyNotFoundException($"Font '{name}' not found");
    }

    public Texture2D GetTexture(string name)
    {
        return _textures.TryGetValue(name, out var texture) ? texture : throw new KeyNotFoundException($"Texture '{name}' not found");
    }

    public bool HasFont(string name) => _fonts.ContainsKey(name);
    public bool HasTexture(string name) => _textures.ContainsKey(name);
}