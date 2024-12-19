namespace Readymade.Machinery.Acting
{
    public interface ISlot
    {
        public bool IsUnlocked(IActor actor);
        public bool IsUnlocked(IInventory<SoProp> inventory);
        public bool IsAccepting(SoProp prop);
    }
}