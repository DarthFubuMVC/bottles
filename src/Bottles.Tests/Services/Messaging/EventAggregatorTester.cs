﻿using System.Collections.Generic;
using System.Threading;
using Bottles.Services.Messaging;
using NUnit.Framework;
using System.Linq;
using FubuTestingSupport;

namespace Bottles.Services.Tests.Messaging
{
    [TestFixture]
    public class EventAggregatorTester
    {
        private RecordingListener theListener;

        [TearDown]
        public void Teardown()
        {
            EventAggregator.Stop();
        }

        [SetUp]
        public void SetUp()
        {
            theListener = new RecordingListener();
            var hub = new MessagingHub();
            hub.AddListener(theListener);

            var remoteListener = new RemoteListener(hub);
            EventAggregator.Start(remoteListener);
        }

        [Test]
        public void send_message_by_category()
        {
            EventAggregator.SendMessage("category1", "some message");

            var expected = new ServiceMessage
            {
                Category = "category1", Message = "some message"
            };

            Wait.Until(() => theListener.Received.Contains(expected));

            theListener.Received.OfType<ServiceMessage>().Single()
                       .ShouldEqual(expected);

        }
    }

    public class RecordingListener : IListener
    {
        private readonly IList<object> _received = new List<object>();

        public IEnumerable<object> Received
        {
            get { return _received; }
        }

        public void Clear()
        {
            _received.Clear();
        }

        public void Receive<T>(T message)
        {
            _received.Add(message);
        }

        
    }
}