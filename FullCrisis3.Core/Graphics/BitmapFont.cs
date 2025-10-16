using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace FullCrisis3.Core.Graphics;

public class BitmapFont
{
    private readonly Texture2D _fontTexture;
    private readonly Dictionary<char, Rectangle> _characterBounds;
    private readonly int _characterHeight;
    private readonly int _spaceWidth;

    public BitmapFont(GraphicsDevice graphicsDevice)
    {
        _characterHeight = 16;
        _spaceWidth = 8;
        _characterBounds = new Dictionary<char, Rectangle>();
        
        _fontTexture = CreateFontTexture(graphicsDevice);
        InitializeCharacterBounds();
    }

    private Texture2D CreateFontTexture(GraphicsDevice graphicsDevice)
    {
        const int textureWidth = 256;
        const int textureHeight = 128;
        var texture = new Texture2D(graphicsDevice, textureWidth, textureHeight);
        
        var data = new Color[textureWidth * textureHeight];
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Color.Transparent;
        }
        
        DrawBuiltInFont(data, textureWidth, textureHeight);
        
        texture.SetData(data);
        return texture;
    }

    private void DrawBuiltInFont(Color[] data, int textureWidth, int textureHeight)
    {
        var charMap = new Dictionary<char, byte[]>
        {
            ['A'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['B'] = new byte[] {
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['C'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['D'] = new byte[] {
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['E'] = new byte[] {
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 0
            },
            ['F'] = new byte[] {
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 0, 0, 0
            },
            ['G'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 0,
                1, 0, 1, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['H'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['I'] = new byte[] {
                1, 1, 1, 1, 1,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 0
            },
            ['J'] = new byte[] {
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['K'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 0, 0, 1, 0,
                1, 0, 1, 0, 0,
                1, 1, 0, 0, 0,
                1, 0, 1, 0, 0,
                1, 0, 0, 1, 0,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['L'] = new byte[] {
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 0
            },
            ['M'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 1, 0, 1, 1,
                1, 0, 1, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['N'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 1, 0, 0, 1,
                1, 0, 1, 0, 1,
                1, 0, 0, 1, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['O'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['P'] = new byte[] {
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                0, 0, 0, 0, 0
            },
            ['Q'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 1, 0, 1,
                1, 0, 0, 1, 0,
                0, 1, 1, 0, 1,
                0, 0, 0, 0, 0
            },
            ['R'] = new byte[] {
                1, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                1, 0, 1, 0, 0,
                1, 0, 0, 1, 0,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['S'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 0,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['T'] = new byte[] {
                1, 1, 1, 1, 1,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 0, 0
            },
            ['U'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['V'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 0, 1, 0,
                0, 1, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 0, 0
            },
            ['W'] = new byte[] {
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 1, 0, 1,
                1, 0, 1, 0, 1,
                1, 1, 0, 1, 1,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['X'] = new byte[] {
                1, 0, 0, 0, 1,
                0, 1, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 1, 0,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 0
            },
            ['Y'] = new byte[] {
                1, 0, 0, 0, 1,
                0, 1, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 0, 0
            },
            ['Z'] = new byte[] {
                1, 1, 1, 1, 1,
                0, 0, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 0, 0, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 0
            },
            [' '] = new byte[] {
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0
            },
            ['0'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 1, 1,
                1, 0, 1, 0, 1,
                1, 1, 0, 0, 1,
                1, 0, 0, 0, 1,
                1, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['1'] = new byte[] {
                0, 0, 1, 0, 0,
                0, 1, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 0, 1, 0, 0,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            },
            ['2'] = new byte[] {
                0, 1, 1, 1, 0,
                1, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 0, 0, 1, 0,
                0, 0, 1, 0, 0,
                0, 1, 0, 0, 0,
                1, 1, 1, 1, 1,
                0, 0, 0, 0, 0
            },
            ['3'] = new byte[] {
                1, 1, 1, 1, 0,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                0, 1, 1, 1, 0,
                0, 0, 0, 0, 1,
                0, 0, 0, 0, 1,
                1, 1, 1, 1, 0,
                0, 0, 0, 0, 0
            }
        };

        int currentX = 0;
        int currentY = 0;
        const int charWidth = 6;
        const int charHeight = 8;
        const int charsPerRow = 16;

        foreach (var kvp in charMap)
        {
            DrawCharacter(data, textureWidth, kvp.Value, currentX, currentY, charWidth, charHeight);
            
            _characterBounds[kvp.Key] = new Rectangle(currentX, currentY, charWidth, charHeight);
            
            currentX += charWidth;
            if (currentX + charWidth > textureWidth || (currentX / charWidth) >= charsPerRow)
            {
                currentX = 0;
                currentY += charHeight;
            }
        }
    }

    private void DrawCharacter(Color[] data, int textureWidth, byte[] charData, int startX, int startY, int charWidth, int charHeight)
    {
        for (int y = 0; y < charHeight; y++)
        {
            for (int x = 0; x < 5; x++) // Character bitmap is 5 pixels wide
            {
                if (charData[y * 5 + x] == 1)
                {
                    int pixelIndex = (startY + y) * textureWidth + (startX + x);
                    if (pixelIndex >= 0 && pixelIndex < data.Length)
                    {
                        data[pixelIndex] = Color.White;
                    }
                }
            }
        }
    }

    private void InitializeCharacterBounds()
    {
        // Bounds are already set in DrawBuiltInFont
    }

    public Vector2 MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        float width = 0;
        float height = _characterHeight;

        foreach (char c in text)
        {
            if (c == ' ')
            {
                width += _spaceWidth;
            }
            else if (_characterBounds.ContainsKey(char.ToUpper(c)))
            {
                width += _characterBounds[char.ToUpper(c)].Width;
            }
            else
            {
                width += _spaceWidth; // Unknown character
            }
        }

        return new Vector2(width, height);
    }

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float rotation = 0f, Vector2 origin = default, float scale = 1f, SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Vector2 currentPosition = position;

        foreach (char c in text)
        {
            if (c == ' ')
            {
                currentPosition.X += _spaceWidth * scale;
            }
            else if (_characterBounds.ContainsKey(char.ToUpper(c)))
            {
                var bounds = _characterBounds[char.ToUpper(c)];
                
                spriteBatch.Draw(
                    _fontTexture,
                    currentPosition,
                    bounds,
                    color,
                    rotation,
                    origin,
                    scale,
                    effects,
                    layerDepth
                );
                
                currentPosition.X += bounds.Width * scale;
            }
            else
            {
                currentPosition.X += _spaceWidth * scale; // Unknown character
            }
        }
    }
}