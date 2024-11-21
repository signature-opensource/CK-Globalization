using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CK.Core;

/// <summary>
/// Captures an interpolated string result along with its formatted placeholders.
/// <para>
/// The simplified projection of a FormattedString is a string: this is implicitly castable as a string, <see cref="Text"/> is returned.
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
    readonly ExtendedCultureInfo? _culture;

    /// <summary>
    /// Gets an empty formatted string: empty text, no placeholders and <see cref="NormalizedCultureInfo.Invariant"/>.
    /// This is the <c>default</c> of this value type, defined here for clarity.
    /// </summary>
    public static FormattedString Empty => default;

    /// <summary>
    /// Initializes a <see cref="FormattedString"/> with a plain string (no <see cref="Placeholders"/>).
    /// </summary>
    /// <param name="culture">The culture of this formatted string.</param>
    /// <param name="plainText">The plain text.</param>
    public FormattedString( ExtendedCultureInfo culture, string plainText )
    {
        Throw.CheckNotNullArgument( culture );
        Throw.CheckNotNullArgument( plainText );
        _text = plainText;
        _placeholders = Array.Empty<(int, int)>();
        _culture = culture;
        Debug.Assert( CheckPlaceholders( _placeholders, _text.Length ) );
    }

    /// <summary>
    /// Initializes a <see cref="FormattedString"/> with <see cref="Placeholders"/> using
    /// the provided <paramref name="culture"/>.
    /// <para>
    /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> is used to format the placeholders, but this
    /// captures the whole <see cref="ExtendedCultureInfo.Fallbacks"/> so that the best translation of the
    /// "enveloppe" can be found when the <paramref name="culture"/> is a "user preference" (a mere ExtendedCultureInfo) rather
    /// than a specialized <see cref="NormalizedCultureInfo"/> with its default fallbacks.
    /// </para>
    /// </summary>
    /// <param name="culture">The culture used to format placeholders' content.</param>
    /// <param name="text">The interpolated text.</param>
    public FormattedString( ExtendedCultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text )
    {
        Throw.CheckNotNullArgument( culture );
        (_text, _placeholders) = text.GetResult();
        _culture = culture;
        Debug.Assert( CheckPlaceholders( _placeholders, _text.Length ) );
    }

    FormattedString( string text, (int Start, int Length)[] placeholders, ExtendedCultureInfo culture )
    {
        _text = text;
        _placeholders = placeholders;
        _culture = culture;
    }

    /// <summary>
    /// Intended for wrappers that capture the interpolated string handler.
    /// </summary>
    /// <param name="handler">The interpolated string handler.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>A new formatted string.</returns>
    public static FormattedString Create( ref FormattedStringHandler handler, ExtendedCultureInfo culture )
    {
        Throw.CheckNotNullArgument( culture );
        var (t, p) = handler.GetResult();
        return new FormattedString( t, p, culture );
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
    public static FormattedString CreateFromProperties( string text, (int Start, int Length)[] placeholders, ExtendedCultureInfo culture )
    {
        Throw.CheckNotNullArgument( text );
        Throw.CheckNotNullArgument( placeholders );
        Throw.CheckNotNullArgument( culture );
        Throw.CheckArgument( CheckPlaceholders( placeholders, text.Length ) );
        return new FormattedString( text, placeholders, culture );
    }

    static bool CheckPlaceholders( (int Start, int Length)[] placeholders, int lenText )
    {
        int last = 0;
        foreach( var (start, length) in placeholders )
        {
            if( start < 0 || length < 0 || last > start ) return false;
            last = start + length;
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
    ///     When this is true, <see cref="Culture"/> can be any culture, not necessarily the <see cref="NormalizedCultureInfo.Invariant"/>.
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    public bool IsEmptyFormat => !IsValid || (_text.Length == 0 && _placeholders.Length == 0);

    /// <summary>
    /// Gets the placeholders' occurrence in this <see cref="Text"/>.
    /// </summary>
    public IReadOnlyList<(int Start, int Length)> Placeholders => _placeholders ?? Array.Empty<(int, int)>();

    /// <summary>
    /// Gets the placeholders' content.
    /// </summary>
    /// <returns>A formatted content (with <see cref="Culture"/>) for each placeholders.</returns>
    public ReadOnlyMemory<char>[] GetPlaceholderContents()
    {
        if( _placeholders == null ) return Array.Empty<ReadOnlyMemory<char>>();
        ReadOnlyMemory<char>[] p = new ReadOnlyMemory<char>[_placeholders.Length];
        int i = 0;
        foreach( var (Start, Length) in _placeholders )
        {
            p[i++] = _text.AsMemory( Start, Length );
        }
        return p;
    }

    /// <summary>
    /// Gets the culture that has been used to format the placeholder's content.
    /// </summary>
    public ExtendedCultureInfo Culture => IsValid ? _culture : NormalizedCultureInfo.Invariant;

    /// <summary>
    /// Implicit cast into string: <see cref="Text"/>.
    /// </summary>
    /// <param name="f">This Text.</param>
    public static implicit operator string( FormattedString f ) => f._text ?? String.Empty;

    /// <summary>
    /// Writes the 20 bytes SHA1 of the <see cref="GetFormatString()"/> (without computing it).
    /// </summary>
    /// <param name="destination">Must be at least 20 bytes length.</param>
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
        foreach( var (start, length) in _placeholders )
        {
            int lenBefore = start - tStart;
            hash.AppendData( MemoryMarshal.AsBytes( text.Slice( tStart, lenBefore ) ) );
            tStart = start + length;
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
        Throw.DebugAssert( Base64.GetMaxEncodedToUtf8Length( 20 ) == 28 );
        Span<byte> buffer = stackalloc byte[4 + 28];
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
        // This is NOT a good idea since braces may be need doubling!
        // if( _placeholders.Length == 0 ) return _text;
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
        foreach( var (start, length) in _placeholders )
        {
            int lenBefore = start - tStart;
            var before = text.Slice( tStart, lenBefore );
            tStart = start + length;
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

    /// <summary>
    /// Applies a <see cref="PositionalCompositeFormat"/> to this <see cref="FormattedString"/>'s placeholder contents.
    /// </summary>
    /// <param name="format">The format to apply.</param>
    /// <returns>The formatted string.</returns>
    public string Format( in PositionalCompositeFormat format )
    {
        if( !IsValid || format._placeholders == null ) return string.Empty;
        var f = format.Format();
        int length = f.Length;
        foreach( var p in format._placeholders )
        {
            if( p.ArgIndex < _placeholders.Length ) length += _placeholders[p.ArgIndex].Length;
        }
        return string.Create( length, (f, format._placeholders, _text, _placeholders), ( span, ctx ) =>
        {
            var format = ctx.Item1.AsSpan();
            int lastSource = 0;
            int lenF;
            foreach( var (pI, pN) in ctx.Item2 )
            {
                lenF = pI - lastSource;
                if( lenF > 0 )
                {
                    format.Slice( lastSource, lenF ).CopyTo( span );
                    span = span.Slice( lenF );
                    lastSource += lenF;
                }
                if( pN < ctx.Item4.Length )
                {
                    var (start, length) = ctx.Item4[pN];
                    ctx.Item3.AsSpan( start, length ).CopyTo( span );
                    span = span.Slice( length );
                }
            }
            lenF = format.Length - lastSource;
            if( lenF > 0 )
            {
                format.Slice( lastSource, lenF ).CopyTo( span );
            }
        } );
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

    #region Binary serialization
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
            var n = r.ReadNullableString();
            _culture = n != null ? NormalizedCultureInfo.EnsureNormalizedCultureInfo( n ) : null;
        }
    }

    /// <inheritdoc />
    public void WriteData( ICKBinaryWriter w )
    {
        // Don't bother optimizing the Empty as it should not be used
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
            w.WriteNullableString( _culture?.Name );
        }
    }
    #endregion

}
