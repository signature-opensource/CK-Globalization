using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Only contains the CompositeFormat definition we need.</summary>
    [AttributeUsage( AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    sealed class StringSyntaxAttribute : Attribute
    {
        /// <summary>The syntax identifier for strings containing composite formats.</summary>
        public const string CompositeFormat = nameof( CompositeFormat );

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.
        /// </summary>
        public StringSyntaxAttribute( string syntax )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSyntaxAttribute"/> class.
        /// </summary>
        public StringSyntaxAttribute( string syntax, params object?[] arguments )
        {
        }
    }
}
#endif
