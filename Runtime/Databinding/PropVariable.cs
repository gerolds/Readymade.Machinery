using Readymade.Databinding;
using Readymade.Machinery.Acting;

namespace Readymade.Databinding {
    /// <inheritdoc />
    public class PropVariable : SoVariable<TokenProp> {
        /// <inheritdoc />
        public override bool CanBeClamped => false;

        /// <inheritdoc />
        protected override TokenProp OnClampValue ( in TokenProp value, in TokenProp min, in TokenProp max ) {
            throw new System.NotImplementedException ();
        }
    }
}