using Microsoft.Xna.Framework;
using FullCrisis3.Core.Input;
using FullCrisis3.Core.Assets;

namespace FullCrisis3.Core.Scenes;

public interface IScene
{
    void Load(InputManager inputManager, AssetManager assetManager);
    void Unload();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}