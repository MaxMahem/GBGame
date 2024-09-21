using System.Collections.Generic;
using GBGame.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Components;

namespace GBGame.Components;

public class Inventory
{
    private int _activeItemIndex = 0;
    public List<Item> Items { get; } = []; 

    private Texture2D _inventorySprite = null!;

    private SpriteFont _font = null!;
    private Color _nameColour = new Color(40, 56, 24);
    private Color _descColour = new Color(96, 112, 80);

    public void AddItem(Item item) 
    {
        item.LoadContent();
        Items.Add(item);
    }

    public void UseActive()
    {
        Items[_activeItemIndex].Use();
    }

    public void LoadContent(Game windowData) 
    {
        _inventorySprite = windowData.Content.Load<Texture2D>("Sprites/UI/ItemFrame");
        _font = windowData.Content.Load<SpriteFont>("Sprites/Fonts/File");
    }

    public void Draw(SpriteBatch batch, Camera2D? camera) 
    {
        Item item = Items[_activeItemIndex];

        Vector2 pos = Vector2.One;
        Vector2 namePos = new Vector2(_inventorySprite.Width + 2, 1);
        Vector2 descPos = new Vector2(_inventorySprite.Height + 2, 8);

        if (camera is not null)
        {
            pos = camera.ScreenToWorld(pos);
            namePos = camera.ScreenToWorld(namePos);
            descPos = camera.ScreenToWorld(descPos);
        }

        batch.Draw(_inventorySprite, pos, Color.White);
        batch.Draw(item.InventorySprite, pos, Color.White);

        batch.DrawString(_font, item.Name, namePos, _nameColour);
        batch.DrawString(_font, item.Description, descPos, _descColour * 0.3f); 
    }

    public int ActiveItemIndex {
        get => _activeItemIndex;
        set {
            if (_activeItemIndex == value) return;

            if (value > Items.Count - 1)
            {
                _activeItemIndex = 0;
                return;
            }

            if (value < 0)
            {
                _activeItemIndex = Items.Count - 1;
                return;
            }

            _activeItemIndex = value;
        }
    }
}
