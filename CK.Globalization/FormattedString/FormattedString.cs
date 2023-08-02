using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Captures an interpolated string result along with its placeholders and
    /// provides the composite format string (https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting)
    /// that can be used as a template for other placeholder values.
    /// <para>
    /// This is implicitly castable as a string: <see cref="Text"/> is returned.
    /// </para>
    /// <para>
    /// Note: We don't use <see cref="Range"/> here because there's no use of any "FromEnd". a simple
    /// value tuple <c>(int Start, int Length)</c> is easier and faster.
    /// </para>
    /// </summary>
    [SerializationVersion( 0 )]
    public readonly struct FormattedString : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        /// <summary>
        /// The maximal number of placeholders that formatted strings support.
        /// </summary>
        public const int MaxPlaceholderCount = 99;

        readonly string? _text;
        readonly (int Start, int Length)[]? _placeholders;
        readonly CultureInfo? _culture;

        /// <summary>
        /// Gets an empty formatted string: empty text, no placeholders and <see cref="CultureInfo.InvariantCulture"/>.
        /// This is the <c>default</c> of this value type, defined here for clarity.
        /// </summary>
        public static FormattedString Empty => default;

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with a plain string (no <see cref="Placeholders"/>)
        /// that is bound to the <see cref="CultureInfo.CurrentCulture"/> (that is a thread static property).
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        public FormattedString( string plainText )
            : this( CultureInfo.CurrentCulture, plainText )
        {
        }

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with a plain string (no <see cref="Placeholders"/>).
        /// </summary>
        /// <param name="culture">The culture of this formatted string.</param>
        /// <param name="plainText">The plain text.</param>
        public FormattedString( CultureInfo culture, string plainText )
        {
            Throw.CheckNotNullArgument( plainText );
            _text = plainText;
            _placeholders = Array.Empty<(int, int)>();
            _culture = culture;
            Debug.Assert( CheckPlaceholders( _placeholders, _text.Length ) );
        }

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with <see cref="Placeholders"/> using
        /// the thread static <see cref="CultureInfo.CurrentCulture"/> to format the placeholder contents.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        public FormattedString( [InterpolatedStringHandlerArgument] FormattedStringHandler text )
        {
            _culture = CultureInfo.CurrentCulture;
            (_text, _placeholders) = text.GetResult();
            Debug.Assert( CheckPlaceholders( _placeholders, _text.Length ) );
        }

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with <see cref="Placeholders"/> using
        /// the provided <paramref name="culture"/>.
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        public FormattedString( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text )
        {
            (_text,_placeholders) = text.GetResult();
            _culture = culture;
            Debug.Assert( CheckPlaceholders( _placeholders, _text.Length ) );
        }

        FormattedString( string text, (int Start, int Length)[] placeholders, CultureInfo culture )
        {
            _text = text;
            _placeholders = placeholders;
            _culture = culture;
        }

        /// <summary>
        /// Creates a <see cref="FormattedString"/>. This is intended to restore an instance from its component:
        /// this can typically be used by serializers/deserializers.
        /// <para>
        /// All parameters are checked (placeholders cannot overlap or cover more than the text).
        /// </para>
        /// </summary>
        /// <param name="text">The <see cref="Text"/>.</param>
        /// <param name="placeholders">The <see cref="Placeholders"/>.</param>
        /// <param name="culture">The <see cref="Culture"/>.</param>
        /// <returns>A new formatted string.</returns>
        public static FormattedString Create( string text, (int Start, int Length)[] placeholders, CultureInfo culture )
        {
            Throw.CheckNotNullArgument( text );
            Throw.CheckNotNullArgument( placeholders );
            Throw.CheckNotNullArgument( culture );
            Throw.CheckArgument( CheckPlaceholders( placeholders, text.Length ) );
            return new FormattedString( text, placeholders, culture );
        }

        /// <summary>
        /// Intended for wrappers that capture the interpolated string handler.
        /// </summary>
        /// <param name="handler">The interpolated string handler.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>A new formatted string.</returns>
        public static FormattedString Create( ref FormattedStringHandler handler, CultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture );
            var (t, p) = handler.GetResult();
            return new FormattedString( t, p, culture );
        }

        static bool CheckPlaceholders( (int Start, int Length)[] placeholders, int lenText )
        {
            int last = 0;
            foreach( var p in placeholders )
            {
                if( p.Start < 0 || p.Length < 0 || last > p.Start ) return false;
                last = p.Start + p.Length;
            }
            return last <= lenText;
        }

        [MemberNotNullWhen( true, nameof( _text ), nameof( _placeholders ), nameof( _culture ) )]
        bool IsValid => _text != null;

        /// <summary>
        /// Gets this formatted string content.
        /// </summary>
        public string Text => _text ?? string.Empty;

        /// <summary>
        /// Gets whether this formatted string is empty: <see cref="Text"/> is empty and there is no <see cref="Placeholders"/>.
        /// <para>
        /// Note that:
        /// <list type="bullet">
        ///   <item>
        ///     Text can be empty and there may be one or more Placeholders. For instance, the format string <c>{0}{1}</c>
        ///     with 2 empty placeholders content leads to an empty Text but this doesn't mean that this FormattedString is empty.
        ///   </item>
        ///   <item>
        ///     When this is true, <see cref="Culture"/> can be any culture, not necessarily the <see cref="CultureInfo.InvariantCulture"/>.
        ///   </item>
        /// </list>
        /// </para>
        /// </summary>
        public bool IsEmptyFormat => !IsValid || (_text.Length == 0 && _placeholders.Length == 0);

        /// <summary>
        /// Gets the placeholders' occurrence in this <see cref="Text"/>.
        /// </summary>
        public IReadOnlyList<(int Start, int Length)> Placeholders => _placeholders ?? Array.Empty<(int,int)>();

        /// <summary>
        /// Gets the placeholders' content.
        /// </summary>
        /// <returns>A formatted content (with <see cref="Culture"/>) for each placeholders.</returns>
        public IEnumerable<ReadOnlyMemory<char>> GetPlaceholderContents()
        {
            if( _placeholders != null )
            {
                foreach( var (Start, Length) in _placeholders )
                {
                    yield return _text.AsMemory( Start, Length );
                }
            }
        }

        /// <summary>
        /// Gets the culture that has been used to format the placeholder's content.
        /// <para>
        /// When deserializing, this culture is set to the <see cref="CultureInfo.InvariantCulture"/> if
        /// the culture cannot be restored properly.
        /// </para>
        /// </summary>
        public CultureInfo Culture => _culture ?? CultureInfo.InvariantCulture;

        /// <summary>
        /// Implicit cast into string.
        /// </summary>
        /// <param name="f">This formatted string.</param>
        public static implicit operator string( FormattedString f ) => f._text ?? String.Empty;

        /// <summary>
        /// Writes the 20 bytes SHA1 of the <see cref="GetFormatString()"/> (without computing it).
        /// </summary>
        /// <param name="destination">Must be at leas 20 bytes length.</param>
        public void WriteFormatSHA1( Span<byte> destination )
        {
            Throw.CheckArgument( destination.Length >= 20 );
            if( !IsValid )
            {
                SHA1Value.Zero.GetBytes().Span.CopyTo( destination );
                return;
            }
            var text = _text.AsSpan();
            using var hash = IncrementalHash.CreateHash( HashAlgorithmName.SHA1 );
            int tStart = 0;
            byte placeholderMark = 0;
            var mark = MemoryMarshal.CreateReadOnlySpan( ref placeholderMark, 1 );
            foreach( var slot in _placeholders )
            {
                int lenBefore = slot.Start - tStart;
                hash.AppendData( MemoryMarshal.AsBytes( text.Slice( tStart, lenBefore ) ) );
                tStart = slot.Start + slot.Length;
                hash.AppendData( mark );
            }
            hash.AppendData( MemoryMarshal.AsBytes( text.Slice( tStart ) ) );
            hash.GetCurrentHash( destination );
        }

        static ReadOnlySpan<byte> _resNamePrefix => "SHA."u8;

        /// <summary>
        /// Gets the "Sha.XXX....XXX" automatic resource name that can be used to identify
        /// the format of this formatted string (regardless of the placeholder values).
        /// </summary>
        /// <returns>A default resource name based on this <see cref="GetFormatString"/>.</returns>
        public string GetSHA1BasedResName()
        {
            Throw.DebugAssert( Base64.GetMaxEncodedToUtf8Length( 20 ) == 29 );
            Span<byte> buffer = stackalloc byte[4 + 29];
            _resNamePrefix.CopyTo( buffer );
            var sha = buffer.Slice( 4 );
            WriteFormatSHA1( sha );
            Base64.EncodeToUtf8InPlace( sha, 20, out int bytesWritten );
            Base64UrlHelper.UncheckedBase64ToUrlBase64NoPadding( sha, ref bytesWritten );
            return Encoding.ASCII.GetString( buffer.Slice( 0, 4 + bytesWritten ) );
        }

        /// <summary>
        /// Returns a <see cref="string.Format(IFormatProvider?, string, object?[])"/> composite format string
        /// with positional placeholders {0}, {1} etc. for each placeholder.
        /// <para>
        /// The purpose of this format string is not to rewrite this message with other contents, it is to ease globalization
        /// process by providing the message's format in order to translate it into different languages.
        /// </para>
        /// </summary>
        /// <returns>The composite format string.</returns>
        public string GetFormatString()
        {
            if( !IsValid ) return String.Empty;
            Throw.DebugAssert( _placeholders.Length < 100 );
            // Worst case is full of { and } (that must be doubled) and all placeholders are empty
            // (that must be filled with {xx}: it is useless to handle the 10 first {x} placeholders).
            // Note: It is enough to blindly double { and } (https://github.com/dotnet/docs/issues/36416).
            var fmtA = ArrayPool<char>.Shared.Rent( _text.Length * 2 + _placeholders.Length * 4 );
            var fmt = fmtA.AsSpan();
            int fHead = 0;
            var text = _text.AsSpan();
            int tStart = 0;
            int cH = '0';
            int cL = '0';
            foreach( var slot in _placeholders )
            {
                int lenBefore = slot.Start - tStart;
                var before = text.Slice( tStart, lenBefore );
                tStart = slot.Start + slot.Length;
                CopyWithDoubledBraces( before, fmt, ref fHead );
                fmt[fHead++] = '{';
                if( cH > '0' ) fmt[fHead++] = (char)cH;
                fmt[fHead++] = (char)cL;
                if( ++cL == ':' )
                {
                    cL = '0';
                    ++cH;
                }
                fmt[fHead++] = '}';
            }
            CopyWithDoubledBraces( text.Slice( tStart ), fmt, ref fHead );
            var s = new string( fmt.Slice( 0, fHead ) );
            ArrayPool<char>.Shared.Return( fmtA );
            return s;
        }

        internal static void CopyWithDoubledBraces( ReadOnlySpan<char> before, Span<char> target, ref int targetIndex )
        {
            int iB = before.IndexOfAny( '{', '}' );
            while( iB >= 0 )
            {
                var b = before[iB];
                before.Slice( 0, ++iB ).CopyTo( target.Slice( targetIndex ) );
                targetIndex += iB;
                target[targetIndex++] = b;
                before = before.Slice( iB );
                iB = before.IndexOfAny( '{', '}' );
            }
            before.CopyTo( target.Slice( targetIndex ) );
            targetIndex += before.Length;
        }

        /// <summary>
        /// Overridden to return this <see cref="Text"/>.
        /// </summary>
        /// <returns>This text.</returns>
        public override string ToString() => Text;

        #region Serialization
        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public FormattedString( ICKBinaryReader r )
            : this( r, r.ReadNonNegativeSmallInt32() )
        {
        }

        /// <inheritdoc />
        public void Write( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( GetType() ) == 0 );
            w.WriteNonNegativeSmallInt32( 0 );
            WriteData( w );
        }

        /// <summary>
        /// Versioned deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="version">The saved version number.</param>
        public FormattedString( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            var t = r.ReadNullableString();
            if( t == null )
            {
                _text = null;
                _placeholders = null;
                _culture = null;
            }
            else
            {
                _text = t;
                int count = r.ReadNonNegativeSmallInt32();
                Throw.CheckData( count <= MaxPlaceholderCount );
                _placeholders = new (int Start, int Length)[count];
                for( int i = 0; i < count; ++i )
                {
                    ref var s = ref _placeholders[i];
                    s.Start = r.ReadNonNegativeSmallInt32();
                    s.Length = r.ReadNonNegativeSmallInt32();
                }
                var n = r.ReadString();
                // First idea was to throw if the culture cannot be found but it seems
                // a better idea to never throw at this level...
                // If there's only the invariant culture, we also avoid the exception.
                if( n.Length > 0 && !Util.IsGlobalizationInvariantMode )
                {
                    try
                    {
                        // don't use predefinedOnly: true overload here.
                        // If it happens that a culture is not predefined (Nls for windows, Icu on linux)
                        // this has less chance to throw.
                        _culture = CultureInfo.GetCultureInfo( n );
                    }
                    catch( CultureNotFoundException )
                    {
                        _culture = CultureInfo.InvariantCulture;
                        ActivityMonitor.StaticLogger.Error( $"CultureInfo named '{n}' cannot be resolved. Using InvariantCulture for FormattedString '{_text}'." );
                    }
                }
                else
                {
                    _culture = CultureInfo.InvariantCulture;
                }
            }
        }

        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            // Don't bother optimizing the InvariantEmpty as it should not be used
            // frequently and if it is, only the caller can serialize a marker and
            // deserialize the singleton.
            w.WriteNullableString( _text );
            if( IsValid )
            {
                w.WriteNonNegativeSmallInt32( _placeholders.Length );
                foreach( var (start, length) in _placeholders )
                {
                    w.WriteNonNegativeSmallInt32( start );
                    w.WriteNonNegativeSmallInt32( length );
                }
                w.Write( _culture.Name );
            }
        }
        #endregion

    }
}