namespace Readymade.Machinery.Acting
{
    public interface IPropConsumer
    {
        bool CanConsume(SoProp prop);
        bool TryConsume(SoProp prop, IActor actor);
        public bool TryGetEffect(SoProp prop, out SoEffect soEffect);
    }
}