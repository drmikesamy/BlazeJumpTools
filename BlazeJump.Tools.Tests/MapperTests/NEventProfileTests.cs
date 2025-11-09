using System.Diagnostics;
using NSubstitute;
using BlazeJump.Tools.Enums;
using BlazeJump.Tools.Models;
using BlazeJump.Tools.Services.Connections;
using BlazeJump.Tools.Services.Crypto;
using BlazeJump.Tools.Services.UserProfile;
using BlazeJump.Tools.Services.Message;
using BlazeJump.Tools.Services.Notification;
using AutoMapper;
using BlazeJump.Tools.Mappers;
using NUnit.Framework.Internal.Execution;

namespace BlazeJump.Tools.Tests.MapperTests
{
	[TestFixture]
	public class NEventProfileTests
	{
		[Test]
		public void Automapper_ConfigIsValid()
		{
			var config = new MapperConfiguration(cfg => cfg.AddProfile<NEventProfile>());
			config.AssertConfigurationIsValid();
		}
		[Test]
		public void Automapper_CreateSignableNEvent_MapsCorrectly()
		{
			var config = new MapperConfiguration(cfg => cfg.AddProfile<NEventProfile>());
			var mapper = config.CreateMapper();

			var nEvent = new NEvent
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
						},
				Content = "TestContent",
				Created_At = 1234,
				Verified = true
			};

			var signableNEvent = mapper.Map<NEvent, SignableNEvent>(nEvent);

			Assert.That(signableNEvent.Id, Is.EqualTo(0));
			Assert.That(signableNEvent.Pubkey, Is.EqualTo("firstUserId"));
			Assert.That(signableNEvent.Created_At, Is.EqualTo(1234));
			Assert.That(signableNEvent.Tags[0].Value, Is.EqualTo("taggedParentEventId"));
			Assert.That(signableNEvent.Tags[1].Value, Is.EqualTo("taggedRootEventId"));
			Assert.That(signableNEvent.Content, Is.EqualTo("TestContent"));
		}
	}
}