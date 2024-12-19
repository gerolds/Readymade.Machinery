namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Delegate used to describe a <see cref="FunGesture"/> handler. 
    /// </summary>
    /// <typeparam name="IActor">The actor type that can execute the delegate.</typeparam>
    /// <typeparam name="TResult">The return type of the delegate.</typeparam>
    public delegate TResult GestureFunc<TResult>((IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture) args);
}