using NUnit.Framework;
using Stride.Core.Mathematics;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Core.Tests
{
    [TestFixture]
    public class TouchNotificationTests
    {
        static readonly object Sender = new object();

        static TouchNotification NewCanceledTouchUp()
            => new TouchNotification(TouchNotificationKind.TouchUp, new Vector2(10, 20), new Vector2(100, 200), id: 7, primary: true, new Vector2(3, 4), touchDeviceID: 42, Keys.Shift, Sender, canceled: true);

        [Test]
        public static void ConstructorWithoutCanceledIsNotCanceled()
        {
            var notification = new TouchNotification(TouchNotificationKind.TouchUp, new Vector2(10, 20), new Vector2(100, 200), id: 7, primary: true, new Vector2(3, 4), touchDeviceID: 42, Keys.Shift, Sender);
            Assert.IsFalse(notification.Canceled);
        }

        [Test]
        public static void WithSenderKeepsCanceled()
        {
            var otherSender = new object();
            var notification = (TouchNotification)NewCanceledTouchUp().WithSender(otherSender);
            Assert.IsTrue(notification.Canceled);
            Assert.IsTrue(notification.IsTouchUp);
            Assert.AreEqual(7, notification.Id);
            Assert.AreSame(otherSender, notification.Sender);
        }

        [Test]
        public static void TransformKeepsCanceled()
        {
            var notification = (TouchNotification)NewCanceledTouchUp().Transform(new Scale(2));
            Assert.IsTrue(notification.Canceled);
            Assert.IsTrue(notification.IsTouchUp);
            Assert.AreEqual(7, notification.Id);
            Assert.AreEqual(new Vector2(20, 40), notification.Position);
            Assert.AreEqual(new Vector2(6, 8), notification.ContactArea);
        }

        sealed class Scale(float factor) : INotificationSpaceTransformer
        {
            public Vector2 TransformPosition(Vector2 point) => point * factor;
            public Vector2 TransformSize(Vector2 size) => size * factor;
        }
    }
}
