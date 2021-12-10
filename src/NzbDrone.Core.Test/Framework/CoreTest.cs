using System;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;
using NzbDrone.Test.Common;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Test.Framework
{
    public abstract class CoreTest : TestBase
    {
        protected virtual void UseRealHttp()
        {
            Mocker.GetMock<IPlatformInfo>().SetupGet(c => c.Version).Returns(new Version("3.0.0"));
            Mocker.GetMock<IOsInfo>().SetupGet(c=>c.Version).Returns("1.0.0");
            Mocker.GetMock<IOsInfo>().SetupGet(c=>c.Name).Returns("TestOS");

            Mocker.SetConstant<IHttpProxySettingsProvider>(new HttpProxySettingsProvider(Mocker.Resolve<ConfigService>()));
            Mocker.SetConstant<ICreateManagedWebProxy>(new ManagedWebProxyFactory(Mocker.Resolve<CacheManager>()));
            Mocker.SetConstant<IHttpDispatcher>(new ManagedHttpDispatcher(Mocker.Resolve<IHttpProxySettingsProvider>(), Mocker.Resolve<ICreateManagedWebProxy>(), Mocker.Resolve<UserAgentBuilder>(), Mocker.Resolve<IPlatformInfo>(), Mocker.GetMock<ILogger<ManagedHttpDispatcher>>().Object));
            Mocker.SetConstant<IHttpProvider>(new HttpProvider(Mocker.GetMock<ILogger<HttpProvider>>().Object));
            Mocker.SetConstant<IHttpClient<CoreTest>>(new HttpClient<CoreTest>(Array.Empty<IHttpRequestInterceptor>(), Mocker.Resolve<CacheManager>(), Mocker.Resolve<RateLimitService>(), Mocker.Resolve<IHttpDispatcher>(), Mocker.Resolve<UserAgentBuilder>(), Mocker.GetMock<ILoggerFactory>().Object));
            Mocker.SetConstant<ISonarrCloudRequestBuilder>(new SonarrCloudRequestBuilder());
        }
    }

    public abstract class CoreTest<TSubject> : CoreTest where TSubject : class
    {
        private TSubject _subject;

        [SetUp]
        public void CoreTestSetup()
        {
            _subject = null;
        }

        protected TSubject Subject
        {
            get
            {
                if (_subject == null)
                {
                    _subject = Mocker.Resolve<TSubject>();
                }

                return _subject;
            }

        }
        
        protected override void UseRealHttp()
        {
            base.UseRealHttp();
            Mocker.SetConstant<IHttpClient<TSubject>>(new HttpClient<TSubject>(Array.Empty<IHttpRequestInterceptor>(), Mocker.Resolve<CacheManager>(), Mocker.Resolve<RateLimitService>(), Mocker.Resolve<IHttpDispatcher>(), Mocker.Resolve<UserAgentBuilder>(), Mocker.GetMock<ILoggerFactory>().Object));
        }
    }
}
