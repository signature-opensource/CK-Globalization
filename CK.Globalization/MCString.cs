using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Captures a string composed of an enveloppe in the <see cref="FormatCulture"/> (the <see cref="GetFormatString()"/>)
    /// and <see cref="Placeholders"/> that have been formatted using the <see cref="ContentCulture"/>.
    /// </summary>
    public abstract class MCString
    {
        /// <summary>
        /// Gets the formatted text.
        /// </summary>
        public abstract string Text { get; }

        /// <summary>
        /// Gets the resource name that identifies this string.
        /// <para>
        /// The prefix "SHA." is reserved: it is the prefix for Base64Url SHA1 of the <see cref="GetFormatString"/>
        /// used when no resource name is provided.
        /// </para>
        /// </summary>
        public abstract string ResName { get; }

        /// <summary>
        /// Gets the <see cref="ExtendedCultureInfo.Name"/> that has been used to format the <see cref="Placeholders"/>.
        /// </summary>
        public abstract string ContentCulture { get; }

        /// <summary>
        /// Returns a <see cref="string.Format(IFormatProvider?, string, object?[])"/> composite format string
        /// with positional placeholders {0}, {1} etc. for each placeholders.
        /// <para>
        /// The purpose of this format string is not to rewrite this message with other contents, it is to ease globalization
        /// process by providing the message's format in order to translate it into different languages.
        /// </para>
        /// </summary>
        /// <returns>The composite format string.</returns>
        public abstract string GetFormatString();

        /// <summary>
        /// Gets the culture name (see <see cref="ExtendedCultureInfo.Name"/>) of the <see cref="GetFormatString()"/>
        /// (the "enveloppe" of the <see cref="Text"/>).
        /// It may differ from the <see cref="ContentCulture"/> (the culture used to format the placeholders' content).
        /// </summary>
        public abstract string FormatCulture { get; }

        /// <summary>
        /// Gets the placeholders' occurrence in this <see cref="Text"/>.
        /// </summary>
        public abstract IReadOnlyList<(int Start, int Length)> Placeholders { get; }

        /// <summary>
        /// Gets the placeholders' content.
        /// </summary>
        /// <returns>The <see cref="ContentCulture"/> formatted contents for each placeholders.</returns>
        public abstract IEnumerable<ReadOnlyMemory<char>> GetPlaceholderContents();

        /// <summary>
        /// Gets whether this <see cref="GetFormatString"/> is empty: <see cref="Text"/> is empty and there is no <see cref="Placeholders"/>.
        /// <para>
        /// Note that:
        /// <list type="bullet">
        ///   <item>
        ///     Text can be empty and there may be one or more Placeholders. For instance, the format string <c>{0}{1}</c>
        ///     with 2 empty placeholders content leads to an empty Text but this doesn't mean that this <see cref="MCString"/> is empty.
        ///   </item>
        ///   <item>
        ///     When this is true, <see cref="ContentCulture"/> and <see cref="FormatCulture"/> can be any culture name, not necessarily
        ///     the empty string (<see cref="CultureInfo.InvariantCulture"/>).
        ///   </item>
        /// </list>
        /// </para>
        /// </summary>
        public abstract bool IsEmptyFormat { get; }

        /// <summary>
        /// Overridden to return this <see cref="Text"/>.
        /// </summary>
        /// <returns>This <see cref="Text"/>.</returns>
        public sealed override string ToString() => Text;

        /// <summary>
        /// Implicit cast into string.
        /// </summary>
        /// <param name="f">This string.</param>
        public static implicit operator string( MCString f ) => f.Text;
    }
}
