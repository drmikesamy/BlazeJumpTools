using System.Diagnostics;
using System.Collections.Concurrent;
using NSubstitute;
using BlazeJump.Tools.Enums;
using BlazeJump.Tools.Models;
using BlazeJump.Tools.Services.Connections;
using BlazeJump.Tools.Services.Crypto;
using BlazeJump.Tools.Services.UserProfile;
using BlazeJump.Tools.Services.Message;
using BlazeJump.Tools.Services.Notification;
using AutoMapper;
using NBitcoin.Secp256k1;

namespace BlazeJump.Tools.Tests.ServiceTests
{
    [TestFixture]
    public class MessageServiceTests
    {
        private IRelayManager _relayManager;
        private ICryptoService _cryptoService;
        private IUserProfileService _userProfileService;
        private INotificationService _notificationService;
        private IMapper _mapper;
        private MessageService _sut;

        [SetUp]
        public void SetUp()
        {
            _relayManager = Substitute.For<IRelayManager>();
            _cryptoService = Substitute.For<ICryptoService>();
            _userProfileService = Substitute.For<IUserProfileService>();
            _notificationService = Substitute.For<INotificationService>();
            _mapper = Substitute.For<IMapper>();
            _sut = new MessageService(_relayManager, _cryptoService, _userProfileService, _notificationService, _mapper);
        }

        [Test]
        public void ProcessMessageQueueEvent_ProcessesQueuedMessages()
        {
            // Arrange
            var queue = new ConcurrentQueue<NMessage>();
            queue.Enqueue(
                new NMessage
                {
                    SubscriptionId = "second", MessageType = MessageTypeEnum.Event,
                    Event = new NEvent
                    {
                        Kind = KindEnum.Metadata, 
                        Id = "secondEventId", 
                        Pubkey = "secondUserId",
                    }
                });
            
            queue.Enqueue(
                new NMessage
                {
                    SubscriptionId = "first", MessageType = MessageTypeEnum.Event,
                    Event = new NEvent
                    {
                        Kind = KindEnum.Text, 
                        Id = "firstEventId", 
                        Pubkey = "firstUserId",
                        Tags = new List<EventTag>
                        {
                            new EventTag
                            {
                                Key = TagEnum.e,
                                Value = "taggedParentEventId",
                                Value3 = "reply"
                            },
                            new EventTag
                            {
                                Key = TagEnum.e,
                                Value = "taggedRootEventId",
                                Value3 = "root"
                            },
                        }
                    }
                });
            
            queue.Enqueue(
                new NMessage { SubscriptionId = "third", MessageType = MessageTypeEnum.Eose });
            
            _relayManager.ReceivedMessages.Returns(queue);

            // Act
            _relayManager.ProcessMessageQueue += Raise.Event<EventHandler>(new object(), null);

            // Assert
            var hasRelations = _sut.RelationRegister.TryGetRelations(new List<string>() { "firstEventId" }, RelationTypeEnum.EventChildren, out var firstEventRootId);
            
            Assert.That(_sut.MessageStore.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task FetchNEventsByFilter_ShouldCallQueryRelays()
        {
            // Arrange
            var filters = new List<Filter> { new Filter() }; // Add at least one filter
            var subscriptionId = "test_subscription";
            var requestMessageType = MessageTypeEnum.Req;

            // Act
            await _sut.Fetch(filters, subscriptionId, requestMessageType);

            // Assert
            await _relayManager.Received(1).QueryRelays(subscriptionId, requestMessageType, filters, Arg.Any<int>());
        }

        [Test]
        public void VerifyNEvent_ShouldCallCryptoServiceWithCorrectParameters()
        {
            // Arrange
            var nEvent = new NEvent { Sig = "signature", Pubkey = "pubkey" };
            _cryptoService.Verify("signature", Arg.Any<string>(), "pubkey").Returns(true);

            // Act
            var result = _sut.Verify(nEvent);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SendNEvent_ShouldCallRelayManagerSendWithCorrectParams()
        {
            // Arrange
            var kind = KindEnum.Text;
            var message = "test message";
            
            // Use a valid secp256k1 private key to generate a valid public key
            byte[] privateKey = new byte[32];
            privateKey[0] = 0x01; // Ensure it's a valid private key (not all zeros)
            var context = Context.Instance;
            if (ECPrivKey.TryCreate(privateKey, context, out var privKey))
            {
                var validPubKey = privKey.CreatePubKey();
                _cryptoService.EtherealPublicKey.Returns(validPubKey);
            }
            
            _cryptoService.Sign(Arg.Any<string>()).Returns("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
            _relayManager.Relays.Returns(new List<string> { "wss://test.ws" });
            _userProfileService.NPubKey.Returns("pubkey");
            
            // Mock mapper to return a SignableNEvent
            _mapper.Map<NEvent, SignableNEvent>(Arg.Any<NEvent>()).Returns(callInfo => 
            {
                var nEvent = callInfo.Arg<NEvent>();
                return new SignableNEvent
                {
                    Content = nEvent.Content,
                    Created_At = nEvent.Created_At,
                    Kind = nEvent.Kind,
                    Tags = nEvent.Tags,
                    Pubkey = nEvent.Pubkey
                };
            });

            NEvent? capturedNEvent = null;
            _relayManager.SendNEvent(Arg.Do<NEvent>(n => capturedNEvent = n), Arg.Any<string>())
                .Returns(Task.CompletedTask);

            // Act
            var nEvent = _sut.CreateNEvent(KindEnum.Text, message);
            await _sut.Send(kind, nEvent);

            // Assert
            await _relayManager.Received(1).SendNEvent(Arg.Any<NEvent>(), Arg.Any<string>());
            Assert.That(capturedNEvent, Is.Not.Null);
            Assert.That(capturedNEvent!.Content, Is.EqualTo(message));
        }
    }
}