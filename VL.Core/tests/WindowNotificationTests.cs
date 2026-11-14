using NUnit.Framework;
using Stride.Core.Mathematics;
using VL.Core.Windowing;
using VL.Lib.IO.Notifications;

namespace VL.Core.Tests
{
    public class WindowNotificationTests
    {
        [Test]
        public void WithSenderKeepsContentAndReplacesSender()
        {
            var a = new object();
            var b = new object();
            var n = new WindowResizedNotification(new Int2(800, 600), 1.5f, a);
            var m = (WindowResizedNotification)n.WithSender(b);
            Assert.That(m.Sender, Is.SameAs(b));
            Assert.That(m.ClientSize, Is.EqualTo(new Int2(800, 600)));
            Assert.That(m.ScaleFactor, Is.EqualTo(1.5f));
        }

        [Test]
        public void CloseRequestCanBeHandled()
        {
            var n = new WindowCloseRequestedNotification(new object());
            Assert.That(n.Handled, Is.False);
            n.Handled = true;
            Assert.That(n.WithSender(this).Handled, Is.True);
        }

        [Test]
        public void WindowNotificationsDoNotTransform()
        {
            var n = new WindowMovedNotification(new Int2(1, 2), new object());
            Assert.That(n.Transform(null!), Is.SameAs(n));
        }

        [Test]
        public void DefaultOptions()
        {
            Assert.That(AppWindowOptions.Default.Graphics, Is.EqualTo(WindowGraphicsApi.None));
            Assert.That(AppWindowOptions.Default with { Title = "x" }, Is.Not.EqualTo(AppWindowOptions.Default));
        }

        [Test]
        public void NoDisplaysIsEmpty()
        {
            Assert.That(NoDisplays.Instance.All, Is.Empty);
            Assert.That(NoDisplays.Instance.Primary, Is.Null);
        }
    }
}
