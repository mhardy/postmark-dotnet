﻿using NUnit.Framework;
using PostmarkDotNet;
using PostmarkDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postmark.PCL.Tests
{
    [TestFixture]
    public class AdminClientServersTests : ClientBaseFixture
    {
        private PostmarkAdminClient _adminClient;
        private string _serverPrefix;
        private bool? _smtpActivated;
        private string _inboundHookUrl;
        private string _openHookUrl;
        private bool? _postFirstOpenOpenOnly;
        private bool? _trackOpens;
        private int? _inboundSpamThreshold;
        private string _name;
        private string _color;
        private bool? _rawEmailEnabled;
        private string _bounceHookUrl;

        protected override async Task SetupAsync()
        {
            _adminClient = new PostmarkAdminClient(WRITE_ACCOUNT_TOKEN);
            var id = Guid.NewGuid().ToString("n");
            _serverPrefix = "integration-test-server-";

            _name = _serverPrefix + id;
            _color = ServerColors.Purple;
            _rawEmailEnabled = true;
            _smtpActivated = true;
            _inboundHookUrl = "http://www.example.com/inbound/" + id;
            _bounceHookUrl = "http://www.example.com/bounce/" + id;
            _openHookUrl = "http://www.example.com/opened/" + id;
            _postFirstOpenOpenOnly = true;
            _trackOpens = true;
            _inboundSpamThreshold = 30;
            await CompletionSource;
        }

        [TearDown]
        public void Cleanup()
        {
            var servers = _adminClient.GetServersAsync().WaitForResult();
            var pendingDeletes = new List<Task>();
            foreach (var server in servers.Servers)
            {
                if (Regex.IsMatch(server.Name, _serverPrefix))
                {
                    var deleteTask = _adminClient.DeleteServerAsync(server.ID);
                    pendingDeletes.Add(deleteTask);
                }
            }
            Task.WaitAll(pendingDeletes.ToArray());
        }

        [TestCase]
        public async void AdminClient_CanGetServer()
        {
            var allservers = await _adminClient.GetServersAsync();
            var server = await _adminClient.GetServerAsync(allservers.Servers.First().ID);
            Assert.NotNull(server);
            Assert.NotNull(server.Name);
        }

        [TestCase]
        public async void AdminClient_CanListServers()
        {
            var allservers = await _adminClient.GetServersAsync();
            Assert.Greater(allservers.Servers.Count(), 0);
        }

        [TestCase]
        public async void AdminClient_CanEditServer()
        {
            var newServer = await _adminClient.CreateServerAsync(_name, _color, _rawEmailEnabled, _smtpActivated,
                _inboundHookUrl, _bounceHookUrl, _openHookUrl, _postFirstOpenOpenOnly, _trackOpens,
                null, _inboundSpamThreshold);

            var updatedAffix = "updated";

            var updatedServer = await _adminClient.EditServerAsync(newServer.ID, _name + updatedAffix, ServerColors.Yellow,
                !newServer.RawEmailEnabled, !newServer.SmtpApiActivated,
                _inboundHookUrl + updatedAffix, _bounceHookUrl + updatedAffix,
                _openHookUrl + updatedAffix, !newServer.PostFirstOpenOnly,
                !newServer.TrackOpens, null, 5);

            var retrievedServer = await _adminClient.GetServerAsync(newServer.ID);

            Assert.AreEqual(_name + updatedAffix, retrievedServer.Name);
            Assert.AreEqual(ServerColors.Yellow, retrievedServer.Color);
            Assert.AreNotEqual(newServer.Color, retrievedServer.Color);
            Assert.AreEqual(!_rawEmailEnabled, retrievedServer.RawEmailEnabled);
            Assert.AreEqual(!_smtpActivated, retrievedServer.SmtpApiActivated);
            Assert.AreEqual(_inboundHookUrl + updatedAffix, retrievedServer.InboundHookUrl);
            Assert.AreEqual(_bounceHookUrl + updatedAffix, retrievedServer.BounceHookUrl);
            Assert.AreEqual(_openHookUrl + updatedAffix, retrievedServer.OpenHookUrl);
            Assert.AreEqual(!_postFirstOpenOpenOnly, retrievedServer.PostFirstOpenOnly);
            Assert.AreEqual(!_trackOpens, retrievedServer.TrackOpens);
            //Assert.AreEqual(updatedAffix + _inboundDomain, retrievedServer.InboundDomain);
            Assert.AreEqual(5, retrievedServer.InboundSpamThreshold);
            Assert.AreNotEqual(newServer.InboundSpamThreshold, retrievedServer.InboundSpamThreshold);
        }

        [TestCase]
        public async void AdminClient_CanCreateServer()
        {
            var newServer = await _adminClient.CreateServerAsync(_name, _color, _rawEmailEnabled, _smtpActivated,
                _inboundHookUrl, _bounceHookUrl, _openHookUrl, _postFirstOpenOpenOnly, _trackOpens,
                null, _inboundSpamThreshold);

            var retrievedServer = await _adminClient.GetServerAsync(newServer.ID);

            Assert.AreEqual(_name, retrievedServer.Name);
            Assert.AreEqual(_color, retrievedServer.Color);
            Assert.AreEqual(_rawEmailEnabled, retrievedServer.RawEmailEnabled);
            Assert.AreEqual(_smtpActivated, retrievedServer.SmtpApiActivated);
            Assert.AreEqual(_inboundHookUrl, retrievedServer.InboundHookUrl);
            Assert.AreEqual(_bounceHookUrl, retrievedServer.BounceHookUrl);
            Assert.AreEqual(_openHookUrl, retrievedServer.OpenHookUrl);
            Assert.AreEqual(_postFirstOpenOpenOnly, retrievedServer.PostFirstOpenOnly);
            Assert.AreEqual(_trackOpens, retrievedServer.TrackOpens);
            Assert.IsNullOrEmpty(retrievedServer.InboundDomain);
            Assert.AreEqual(_inboundSpamThreshold, retrievedServer.InboundSpamThreshold);

        }

        [TestCase]
        public async void AdminClient_CanDeleteServer()
        {
            var server = await _adminClient.CreateServerAsync(_serverPrefix + Guid.NewGuid().ToString("n"));
            var response = await _adminClient.DeleteServerAsync(server.ID);
            Assert.AreEqual(PostmarkStatus.Success, response.Status);
            Assert.AreEqual(0, response.ErrorCode);
        }
    }
}
