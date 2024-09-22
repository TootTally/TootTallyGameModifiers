namespace TootTallyGameModifiers
{
    public abstract class GameModifierBase
    {
        public abstract GameModifiers.Metadata Metadata { get; }

        public virtual void Initialize(GameController __instance) { }

        public virtual void Update(GameController __instance) { }

        public virtual void SpecialUpdate(GameController __instance) { }

        public virtual void Remove()
        {
            GameModifierManager.Remove(Metadata.ModifierType);
        }
    }
}
