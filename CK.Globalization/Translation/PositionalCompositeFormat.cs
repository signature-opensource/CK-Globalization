using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Parsed composite format with position only placeholders (no ",alignment" nor ":format" specifiers).
    /// <para>
    /// This has been designed to be cached and is optimized for the <see cref="Format(ReadOnlyMemory{char}[])"/> operation:
    /// <list type="bullet">
    ///  <item>
    ///     <see cref="TryParse(string, out PositionalCompositeFormat, out string?)"/> should be tried once and must
    ///     succeed or an alternate format should be used.
    ///  </item>
    ///  <item>
    ///     The original format string is not stored. <see cref="GetFormatString"/> recreates a string on demand.
    ///  </item>
    /// </list>
    /// </para>
    /// </summary>
    public readonly struct PositionalCompositeFormat
    {
        readonly string? _pureFormat;
        readonly (int Index, int ArgIndex)[]? _placeholders;
        readonly int _formatLength;
        readonly int _expectedArgumentCount;

        PositionalCompositeFormat( string pureFormat, (int Index, int ArgIndex)[] placeholders, int expectedArgumentCount, int formatLength )
        {
            _pureFormat = pureFormat;
            _placeholders = placeholders;
            _expectedArgumentCount = expectedArgumentCount;
            _formatLength = formatLength;
        }

        /// <summary>
        /// Empty object is the <c>default</c>.
        /// </summary>
        public static PositionalCompositeFormat Invalid => default;

        /// <summary>
        /// Gets the number of expected arguments: it is the maximum <see cref="Placeholder.ArgIndex"/> + 1
        /// that appear in the placeholders.
        /// <para>
        /// This is between 0 and <see cref="FormattedString.MaxPlaceholderCount"/> + 1.
        /// </para>
        /// </summary>
        public int ExpectedArgumentCount => _expectedArgumentCount;

        /// <summary>
        /// Calls <see cref="TryParse(string, out PositionalCompositeFormat, out string?)"/> and throws a <see cref="FormatException"/>
        /// on error.
        /// </summary>
        /// <param name="format">The format string to parse. Must not be bull or empty.</param>
        /// <returns>A non default composite format.</returns>
        public static PositionalCompositeFormat Parse( string format )
        {
            if( !TryParse( format, out var placeholders, out var error ) ) Throw.FormatException( error );
            return placeholders;
        }

        /// <summary>
        /// Tries to parse a positional only format string (only <c>{0}</c>, <c>{1}</c> etc. placeholders are allowed, without alignement nor format
        /// specifier) into a <see cref="PositionalCompositeFormat"/>.
        /// The <paramref name="format"/> must not be empty.
        /// </summary>
        /// <param name="format">The format string to parse. Must not be null or empty.</param>
        /// <param name="compositeFormat">A non default composite format on success.</param>
        /// <param name="error">A non null error string on error.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParse( string format, out PositionalCompositeFormat compositeFormat, [NotNullWhen(false)]out string? error )
        {
            Throw.CheckNotNullOrEmptyArgument( format );
            // The pureFormat is necessarily smaller than the format.
            var pureFormat = ArrayPool<char>.Shared.Rent( format.Length );
            try
            {
                var s = format.AsSpan();
                int head = s.IndexOfAny( '{', '}' );
                if( head < 0 )
                {
                    error = null;
                    compositeFormat = new PositionalCompositeFormat( format, Array.Empty<(int, int)>(), 0, format.Length );
                    return true;
                }
                var sPure = pureFormat.AsSpan();
                s.Slice( 0, head ).CopyTo( sPure );
                sPure = sPure.Slice( head );
                var p = new List<(int Index, int ArgIndex)>();
                int maxArgIndex = -1;
                char start, c;
                for(; ; )
                {
                    start = s[head];
                    if( ++head == s.Length ) return OnErrorEndOfString( format, out compositeFormat, out error );
                    c = s[head];
                    if( c == start )
                    {
                        // This handles {{ and }}.
                        sPure[0] = c;
                        sPure = sPure.Slice( 1 );
                        if( ++head == s.Length ) break;
                    }
                    else if( c == '}' ) return OnError( $"Unexpected '}}' in '{format}' at {head}.", out compositeFormat, out error );
                    else
                    {
                        int ix = c - '0';
                        if( ix < 0 || ix >= 10 )
                        {
                            return OnErrorInvalidIndex( format, out compositeFormat, out error, head );
                        }
                        if( ++head == s.Length ) return OnErrorEndOfString( format, out compositeFormat, out error );
                        c = s[head];
                        if( c != '}' )
                        {
                            int d2 = c - '0';
                            bool isDigit = d2 >= 0 && d2 < 10;
                            if( isDigit )
                            {
                                // This is crucial since we recreate the format string. 
                                if( ix == 0 )
                                {
                                    return OnError( $"Argument number must not start with 0 in '{format}' at {head - 1}.", out compositeFormat, out error );
                                }
                                if( ++head == s.Length ) return OnErrorEndOfString( format, out compositeFormat, out error );
                                ix = ix * 10 + d2;
                                c = s[head];
                            }
                        }
                        if( c != '}' )
                        {
                            return c == ',' || c == ':'
                                    ? OnError( $"No alignment nor format specifier are allowed, expected '}}' in '{format}' at {head}.", out compositeFormat, out error )
                                    : OnError( $"Expected '}}' in '{format}' at {head}.", out compositeFormat, out error );
                        }
                        if( maxArgIndex < ix ) maxArgIndex = ix;
                        p.Add( ( pureFormat.Length - sPure.Length, ix ) );
                        if( ++head == s.Length ) break;
                    }
                    var remainder = s.Slice( head );
                    var h = remainder.IndexOfAny( '{', '}' );
                    if( h < 0 )
                    {
                        remainder.CopyTo( sPure );
                        sPure = sPure.Slice( remainder.Length );
                        break;
                    }
                    remainder.Slice( 0, h ).CopyTo( sPure );
                    sPure = sPure.Slice( h );
                    head += h;
                }
                error = null;
                compositeFormat = new PositionalCompositeFormat( new string( pureFormat.AsSpan( 0, pureFormat.Length - sPure.Length ) ),
                                                                 p.ToArray(),
                                                                 maxArgIndex + 1,
                                                                 format.Length );
                return true;
            }
            finally
            {
                ArrayPool<char>.Shared.Return( pureFormat );
            }

            static bool OnErrorEndOfString( string format, out PositionalCompositeFormat compositeFormat, out string? error )
            {
                return OnError( $"Unexpected end of string: '{format}'.", out compositeFormat, out error );
            }

            static bool OnErrorInvalidIndex( string format, out PositionalCompositeFormat compositeFormat, out string? error, int pos )
            {
                return OnError( $"Expected argument index between 0 and 99 in '{format}' at {pos}.", out compositeFormat, out error );
            }

            static bool OnError( string e, out PositionalCompositeFormat compositeFormat, out string? error )
            {
                error = e;
                compositeFormat = default;
                return false;
            }
        }

        /// <summary>
        /// Returns the original composite format string with positional placeholders {0}, {1} etc. for each placeholder.
        /// <para>
        /// This format recomputed from the internal state that is optimized for <see cref="Format(ReadOnlyMemory{char}[])"/>.
        /// </para>
        /// </summary>
        /// <returns>The original format string.</returns>
        public string GetFormatString()
        {
            if( _pureFormat == null ) return String.Empty;
            Throw.DebugAssert( _placeholders != null && _placeholders.Length < 100 );
            // Even if there's no placeholders, { and } must be doubled.
            return string.Create( _formatLength, this, ( span, f ) =>
            {
                var pureFormat = f._pureFormat.AsSpan();
                int fStart = 0;
                int head = 0;
                int low, high;
                foreach( var p in f._placeholders! )
                {
                    int lenBefore = p.Index - fStart;
                    var before = pureFormat.Slice( fStart, lenBefore );
                    fStart = p.Index;
                    FormattedString.CopyWithDoubledBraces( before, span, ref head );
                    span[head++] = '{';
                    high = Math.DivRem( p.ArgIndex, 10, out low );
                    if( high > 0 ) span[head++] = (char)('0' + high);
                    span[head++] = (char)('0' + low);
                    span[head++] = '}';
                }
                FormattedString.CopyWithDoubledBraces( pureFormat.Slice( fStart, pureFormat.Length - fStart ), span, ref head );
            } );
        }

        /// <summary>
        /// Applies this format to no arguments.
        /// This is more for API completion, ease tests and disambiguate between <see cref="Format(ReadOnlyMemory{char}[])"/>
        /// and <see cref="Format(string[])"/> than to be used directly.
        /// </summary>
        /// <returns>A formatted string.</returns>
        public string Format() => _pureFormat ?? string.Empty;

        /// <summary>
        /// Applies this format to existing arguments. This is more for API completion and ease tests than to be used directly.
        /// <para>
        /// There can be less <paramref name="args"/> than <see cref="ExpectedArgumentCount"/>: a missing
        /// aregument is skipped (replaced by an empty string).
        /// </para>
        /// </summary>
        /// <param name="args">Arguments for the <see cref="Placeholders"/> substitution.</param>
        /// <returns>A formatted string.</returns>
        public string Format( params string[] args )
        {
            var a = new ReadOnlyMemory<char>[args.Length];
            int i = 0;
            foreach( var s in args ) a[i++] = s.AsMemory();
            return Format( a );
        }

        /// <summary>
        /// Applies this format to existing arguments, substituting the placeholders with the provided arguments.
        /// <para>
        /// There can be less <paramref name="args"/> than <see cref="ExpectedArgumentCount"/>: a missing
        /// aregument is skipped (replaced by an empty string).
        /// </para>
        /// </summary>
        /// <param name="args">Arguments for the <see cref="Placeholders"/> substitution.</param>
        /// <returns>A formatted string.</returns>
        public string Format( params ReadOnlyMemory<char>[] args )
        {
            if( _pureFormat == null ) return string.Empty;
            int len = ComputeLength( args );
            return string.Create( len, (this, args), ( span, ctx ) =>
            {
                var format = ctx.Item1._pureFormat.AsSpan();
                int lastSource = 0;
                int lenF;
                foreach( var (pI, pN) in ctx.Item1._placeholders! )
                {
                    lenF = pI - lastSource;
                    if( lenF > 0 )
                    {
                        format.Slice( lastSource, lenF ).CopyTo( span );
                        span = span.Slice( lenF );
                        lastSource += lenF;
                    }
                    if( pN < args.Length )
                    {
                        var p = args[ pN ];
                        p.Span.CopyTo( span );
                        span = span.Slice( p.Length );
                    }
                }
                lenF = format.Length - lastSource;
                if( lenF > 0 )
                {
                    format.Slice( lastSource, lenF ).CopyTo( span );
                }
            } );
        }

        int ComputeLength( ReadOnlyMemory<char>[] args )
        {
            Throw.DebugAssert( _pureFormat != null && _placeholders != null );
            int result = _pureFormat.Length;
            foreach( var p in _placeholders )
            {
                if( p.ArgIndex < args.Length ) result += args[p.ArgIndex].Length;
            }
            return result;
        }
    }
}
