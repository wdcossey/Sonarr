
using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class WebClientTests : TestBase<HttpProvider>
    {
        [Test]
        public void DownloadString_should_be_able_to_dowload_text_file()
        {
            var jquery = Subject.DownloadString("http://www.google.com/robots.txt");

            jquery.Should().NotBeNullOrWhiteSpace();
            jquery.Should().Contain("Sitemap");
        }

        [TestCase("")]
        [TestCase("http://")]
        public void DownloadString_should_throw_on_error(string url)
        {
            Action action = () => Subject.DownloadString(url);
            action.Should().Throw<Exception>();
            
            Mocker.GetMock<ILogger<HttpProvider>>().ExpectedWarns(Times.Once);
        }
    }
}
