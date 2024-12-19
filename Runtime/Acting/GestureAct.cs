namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Delegate used to describe a <see cref="FunGesture"/> handler. 
    /// </summary>
    /// <typeparam name="IActor">The actor type that can execute the delegate.</typeparam>
    public delegate void GestureAct((IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture) args);
}