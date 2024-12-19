using Readymade.Machinery.Acting;

namespace Readymade.Machinery.Acting {
    public interface IPropCount<T> where T : IProp {
        public long Count { get; }
        public T Identity { get; }
    }
}