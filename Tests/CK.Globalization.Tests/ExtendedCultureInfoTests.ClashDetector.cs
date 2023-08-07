using CK.Core;
using CommunityToolkit.HighPerformance;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CK.Globalization.Tests
{
    public partial class ExtendedCultureInfoTests
    {
        sealed class ClashDetector
        {
            public ClashDetector()
            {
                // We add the 0 => Invariant exception.
                NameHash = new Dictionary<int, object>() { { 0, "" } };
                Clashes = new HashSet<int>();
            }

            public Dictionary<int, object> NameHash { get; }

            public HashSet<int> Clashes { get; }

            public void Add( int hash, string n )
            {
                if( !NameHash.TryAdd( hash, n ) )
                {
                    var e = NameHash[hash];
                    if( e is string s )
                    {
                        if( n != s ) NameHash[hash] = new List<string>() { s, n };
                    }
                    else
                    {
                        var l = (List<string>)e;
                        if( !l.Contains( n ) ) l.Add( n );
                    }
                    Clashes.Add( hash );
                }
            }
            public void Dump( string name )
            {
                Console.WriteLine( $"<{name}>" );
                int num = 0;
                foreach( var clash in Clashes )
                {
                    if( NameHash[clash] is List<string> l )
                    {
                        Console.WriteLine( $"{clash} = \"{l.Concatenate( "\" and \"" )}\"." );
                        ++num;
                    }
                }
                Console.WriteLine( $"</{name}> ==> {num}" );
            }
        }

        // Make then static so we cath ALL occurences.
        static ClashDetector dbJ2Clashes = new ClashDetector();
        static ClashDetector sha1Clashes = new ClashDetector();

        [TestCase( 3712, 100000 )]
        [TestCase( 42, 100000 )]
        [TestCase( 37120, 100000 )]
        [TestCase( 420, 100000 )]
        [TestCase( 37, 100000 )]
        [TestCase( 4, 100000 )]
        [Explicit]
        public void Djb2_and_sha1_hash_exploration( int seed, int count )
        {
            var r = new Random( seed );
            var allNames = new List<string>();

            foreach( var cc in CultureInfo.GetCultures( CultureTypes.AllCultures ) )
            {
                var c = NormalizedCultureInfo.GetNormalizedCultureInfo( cc );
                var n = c.Name;
                // Skip the 0 => Invariant exception.
                if( n.Length > 0 )
                {
                    int hash = n.GetDjb2HashCode();
                    dbJ2Clashes.Add( hash, n );
                    sha1Clashes.Add( hash, n );
                    allNames.Add( n );
                }
            }
            // This is absolutely not a guaranty of anything.
            // "ca-es-valencia" (13 characters) is the longest (as of today on my computer).
            // BCP47 states that subtags cannot be longer than 8 characters: a 3 levels tag
            // may then be "8-8-8" = 26 characters.
            allNames.Select( n => n.Length ).Max().Should().BeLessThan( 16 );

            using IncrementalHash sha1 = IncrementalHash.CreateHash( HashAlgorithmName.SHA1 );
            int oCount = allNames.Count();
            for( int i = 0; i < count; i++ )
            {
                var len = r.Next( 8 ) + 2;
                var extName = Enumerable.Range( 0, len ).Select( i => allNames[r.Next( oCount )] ).Concatenate( ',' );
                var cExt = ExtendedCultureInfo.GetExtendedCultureInfo( extName );

                dbJ2Clashes.Add( cExt.Name.GetDjb2HashCode(), cExt.Name );

                sha1.AppendData( Encoding.ASCII.GetBytes( cExt.Name ) );
                var sh = MemoryMarshal.Cast<byte, int>( sha1.GetHashAndReset() );
                sha1Clashes.Add( sh[0], cExt.Name );

            }
            dbJ2Clashes.Dump( "DBJ2" );
            sha1Clashes.Dump( "SHA1" );
        }


    }
}
