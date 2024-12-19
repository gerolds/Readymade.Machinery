using System;
using UnityEngine.Events;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// Describes a event on a <see cref="T:Builder.Machinery.Runtime.Acting.PropProviderComponent" />.
    /// </summary>
    [Serializable]
    public class ProviderUnityEvent : UnityEvent<ProviderEventArgs>
    {
    }
}