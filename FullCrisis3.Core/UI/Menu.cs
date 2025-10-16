using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3.Core.UI;

public class Menu
{
    private readonly List<MenuItem> _items;
    private int _selectedIndex;
    private bool _mouseHovering;

    public Menu()
    {
        _items = new List<MenuItem>();
        _selectedIndex = 0;
    }

    public void AddItem(string text, Vector2 position)
    {
        _items.Add(new MenuItem(text, position));
    }

    public void UpdateItemBounds(BitmapFont font)
    {
        foreach (var item in _items)
        {
            item.UpdateBounds(font);
        }
    }

    public void Update(InputManager inputManager)
    {
        if (_items.Count == 0) return;

        var mousePos = inputManager.MousePosition;
        var mouseOverItem = false;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Contains(mousePos))
            {
                _selectedIndex = i;
                mouseOverItem = true;
                _mouseHovering = true;
                break;
            }
        }

        if (!mouseOverItem)
        {
            _mouseHovering = false;
        }

        if (!_mouseHovering)
        {
            if (inputManager.IsNavigateUp())
            {
                _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
            }
            else if (inputManager.IsNavigateDown())
            {
                _selectedIndex = (_selectedIndex + 1) % _items.Count;
            }
        }

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].IsSelected = i == _selectedIndex;
        }
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font)
    {
        foreach (var item in _items)
        {
            item.Draw(spriteBatch, font);
        }
    }

    public bool IsItemSelected(InputManager inputManager)
    {
        return inputManager.IsConfirm();
    }

    public string? GetSelectedItemText()
    {
        return _items.Count > 0 ? _items[_selectedIndex].Text : null;
    }
}