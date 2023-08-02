//using FluentAssertions;
//using NUnit.Framework;
//using System;
//using System.Globalization;

//namespace CK.Core.Tests
//{
//    [TestFixture]
//    public class ResultMessageTests
//    {
//        [Test]
//        public void ResultMessage_Error()
//        {
//            FluentActions.Invoking( () => ResultMessage.Error( "" ) ).Should().Throw<ArgumentException>();

//            var m1 = ResultMessage.Error( "text" );
//            m1.Message.Text.Should().Be( "text" );
//            m1.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m1.Type.Should().Be( ResultMessageType.Error );
//            m1.MessageCode.Should().BeNull();
//            CheckSerialization( m1 );

//            var m2 = ResultMessage.Error( "text", "Code" );
//            m2.Message.Text.Should().Be( "text" );
//            m2.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m2.Type.Should().Be( ResultMessageType.Error );
//            m2.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m2 );

//            int v = 3712;

//            var m3 = ResultMessage.Error( $"Hello {v}!", "Policy.Salutation" ); //==> "Hello {0}!"
//            m3.Message.Text.Should().Be( "Hello 3712!" );
//            m3.Message.GetFormatString().Should().Be( "Hello {0}!" );
//            m3.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m3.Type.Should().Be( ResultMessageType.Error );
//            m3.MessageCode.Should().Be( "Policy.Salutation" );
//            CheckSerialization( m3 );

//            var culture = CultureInfo.GetCultureInfo( "aa" );

//            var m4 = ResultMessage.Error( culture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
//            m3.Message.GetFormatString().Should().Be( "{0} Goodbye {1}" );
//            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
//            m4.Message.Culture.Should().BeSameAs( culture );
//            m4.Type.Should().Be( ResultMessageType.Error );
//            m4.MessageCode.Should().Be( "Policy.Salutation" );
//            CheckSerialization( m4 );

//            static void CheckSerialization( ResultMessage m )
//            {
//                var cS = m.DeepClone();
//                cS.Message.Should().BeEquivalentTo( m.Message );
//                cS.MessageCode.Should().Be( m.MessageCode );
//                cS.Type.Should().Be( m.Type );
//                cS.ToString().Should().Be( m.ToString() );

//                var cV = SimpleSerializable.DeepCloneVersioned( m );
//                cV.Message.Should().BeEquivalentTo( m.Message );
//                cV.MessageCode.Should().Be( m.MessageCode );
//                cV.Type.Should().Be( m.Type );
//                cV.ToString().Should().Be( m.ToString() );
//            }
//        }

//        [Test]
//        public void ResultMessage_Warn()
//        {
//            FluentActions.Invoking( () => ResultMessage.Warn( "" ) ).Should().Throw<ArgumentException>();

//            var m1 = ResultMessage.Warn( "text" );
//            m1.Message.Text.Should().Be( "text" );
//            m1.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m1.Type.Should().Be( ResultMessageType.Warn );
//            m1.MessageCode.Should().BeNull();
//            CheckSerialization( m1 );

//            var m2 = ResultMessage.Warn( "text", "Code" );
//            m2.Message.Text.Should().Be( "text" );
//            m2.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m2.Type.Should().Be( ResultMessageType.Warn );
//            m2.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m2 );

//            int v = 3712;

//            var m3 = ResultMessage.Warn( $"t{v}", "Code" );
//            m3.Message.Text.Should().Be( "t3712" );
//            m3.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m3.Type.Should().Be( ResultMessageType.Warn );
//            m3.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m3 );

//            var culture = CultureInfo.GetCultureInfo( "aa" );

//            var m4 = ResultMessage.Warn( culture, $"{v}X{v}", "Code" );
//            m4.Message.Text.Should().Be( "3712X3712" );
//            m4.Message.Culture.Should().BeSameAs( culture );
//            m4.Type.Should().Be( ResultMessageType.Warn );
//            m4.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m4 );

//            static void CheckSerialization( ResultMessage m )
//            {
//                var cS = m.DeepClone();
//                cS.Message.Should().BeEquivalentTo( m.Message );
//                cS.MessageCode.Should().Be( m.MessageCode );
//                cS.Type.Should().Be( m.Type );
//                cS.ToString().Should().Be( m.ToString() );

//                var cV = SimpleSerializable.DeepCloneVersioned( m );
//                cV.Message.Should().BeEquivalentTo( m.Message );
//                cV.MessageCode.Should().Be( m.MessageCode );
//                cV.Type.Should().Be( m.Type );
//                cV.ToString().Should().Be( m.ToString() );
//            }
//        }

//        [Test]
//        public void ResultMessage_Info()
//        {
//            FluentActions.Invoking( () => ResultMessage.Info( "" ) ).Should().Throw<ArgumentException>();

//            var m1 = ResultMessage.Info( "text" );
//            m1.Message.Text.Should().Be( "text" );
//            m1.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m1.Type.Should().Be( ResultMessageType.Info );
//            m1.MessageCode.Should().BeNull();
//            CheckSerialization( m1 );

//            var m2 = ResultMessage.Info( "text", "Code" );
//            m2.Message.Text.Should().Be( "text" );
//            m2.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m2.Type.Should().Be( ResultMessageType.Info );
//            m2.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m2 );

//            int v = 3712;

//            var m3 = ResultMessage.Info( $"t{v}", "Code" );
//            m3.Message.Text.Should().Be( "t3712" );
//            m3.Message.Culture.Should().BeSameAs( CultureInfo.CurrentCulture );
//            m3.Type.Should().Be( ResultMessageType.Info );
//            m3.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m3 );

//            var culture = CultureInfo.GetCultureInfo( "aa" );

//            var m4 = ResultMessage.Info( culture, $"{v}X{v}", "Code" );
//            m4.Message.Text.Should().Be( "3712X3712" );
//            m4.Message.Culture.Should().BeSameAs( culture );
//            m4.Type.Should().Be( ResultMessageType.Info );
//            m4.MessageCode.Should().Be( "Code" );
//            CheckSerialization( m4 );

//            static void CheckSerialization( ResultMessage m )
//            {
//                var cS = m.DeepClone();
//                cS.Message.Should().BeEquivalentTo( m.Message );
//                cS.MessageCode.Should().Be( m.MessageCode );
//                cS.Type.Should().Be( m.Type );
//                cS.ToString().Should().Be( m.ToString() );

//                var cV = SimpleSerializable.DeepCloneVersioned( m );
//                cV.Message.Should().BeEquivalentTo( m.Message );
//                cV.MessageCode.Should().Be( m.MessageCode );
//                cV.Type.Should().Be( m.Type );
//                cV.ToString().Should().Be( m.ToString() );
//            }
//        }

//    }
//}
