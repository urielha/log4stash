using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4stash.Configuration;
using log4stash.ElasticClient;
using log4stash.ElasticClient.RestSharp;
using log4stash.ErrorHandling;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    class WebElasticClientTests
    {
        private IAuthenticator _authenticator;
        private IRestClientFactory _clientFactory;
        private IRequestFactory _requestFactory;
        private IResponseValidator _responseValidator;
        private IExternalEventWriter _eventWriter;


        private static readonly IServerDataCollection OneServer = new ServerDataCollection()
            {new ServerData {Address = "address", Path = "", Port = 9200}};

        private static readonly IServerDataCollection MultiServers = new ServerDataCollection()
        {
            new ServerData {Address = "address1", Path = "", Port = 9201},
            new ServerData {Address = "address2", Path = "", Port = 9202},
            new ServerData {Address = "address3", Path = "", Port = 9203}
        };

        private static readonly TestCaseData[] ServerTestCases =
        {
            new TestCaseData(OneServer),
            new TestCaseData(MultiServers),
        };

        [SetUp]
        public void Setup()
        {
            _authenticator = Substitute.For<IAuthenticator>();
            _clientFactory = Substitute.For<IRestClientFactory>();
            _requestFactory = Substitute.For<IRequestFactory>();
            _eventWriter = Substitute.For<IExternalEventWriter>();
            _responseValidator = Substitute.For<IResponseValidator>();
        }

        [Test]
        [TestCaseSource("ServerTestCases")]
        public void SHOULD_CREATE_REST_CLIENT_FOR_EVERY_SERVER_IN_COLLECTION(IServerDataCollection servers)
        {
            //Arrange
            const int timeout = 0;

            //Act
            var client = new WebElasticClient(servers, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Assert
            foreach (var server in servers)
            {
                _clientFactory.Received().Create("http://" + server.Address + ":" + server.Port + "/", timeout,
                    _authenticator);
            }
        }

        [Test]
        public void EXCEPTION_IN_RESTSHARP_SHOULD_NOT_THROW_IN_CLIENT()
        {
            //Arrange
            const int timeout = 0;
            var restClient = Substitute.For<IRestClient>();
            _clientFactory.Create(null, 0, null).ReturnsForAnyArgs(restClient);
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            restClient.Execute(request).ThrowsForAnyArgs(new Exception());
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            //Assert
            client.IndexBulk(null);
        }

        [Test]
        public async Task EXCEPTION_IN_RESTSHARP_ASYNC_SHOULD_NOT_THROW_IN_CLIENT()
        {
            //Arrange
            const int timeout = 0;
            var restClient = Substitute.For<IRestClient>();
            _clientFactory.Create(null, 0, null).ReturnsForAnyArgs(restClient);
            restClient.ExecuteTaskAsync(null).ThrowsForAnyArgs(new Exception());
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            //Assert
            await client.IndexBulkAsync(null);
        }

        [Test]
        public void EXCEPTION_IN_RESTSHARP_SHOULD_WRITE_ERROR_TO_EVENT_WRITER()
        {
            //Arrange
            const int timeout = 0;
            var restClient = Substitute.For<IRestClient>();
            _clientFactory.Create(null, 0, null).ReturnsForAnyArgs(restClient);
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var exception = new Exception();
            restClient.Execute(request).ThrowsForAnyArgs(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.IndexBulk(null);

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public async Task EXCEPTION_IN_RESTSHARP_ASYNC_SHOULD_WRITE_ERROR_TO_EVENT_WRITER()
        {
            //Arrange
            const int timeout = 0;
            var restClient = Substitute.For<IRestClient>();
            _clientFactory.Create(null, 0, null).ReturnsForAnyArgs(restClient);
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var exception = new Exception();
            restClient.ExecuteTaskAsync(request).ThrowsForAnyArgs(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            await client.IndexBulkAsync(null);

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public void INVALID_INDEX_RESPONSE_SHOULD_NOT_THROW_EXCEPTION_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.IndexBulk(null);

            //Assert
            _eventWriter.ReceivedWithAnyArgs().Error(null, null, null);
        }

        [Test]
        public async Task INVALID_INDEX_RESPONSE_SHOULD_NOT_THROW_EXCEPTION_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            //Assert
            await client.IndexBulkAsync(null);
        }

        [Test]
        public void INVALID_INDEX_RESPONSE_SHOULD_WRITE_ERROR_TO_EVENT_WRITER_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.IndexBulk(null);

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public async Task INVALID_INDEX_RESPONSE_SHOULD_WRITE_ERROR_TO_EVENT_WRITER_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            await client.IndexBulkAsync(null);

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public void INVALID_PUT_TEMPLATE_RESPONSE_SHOULD_NOT_THROW_EXCEPTION_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            //Assert
            client.PutTemplateRaw("template", "body");
        }
        [Test]
        public async Task INVALID_PUT_TEMPLATE_RESPONSE_SHOULD_NOT_THROW_EXCEPTION_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            //Assert
            await client.PutTemplateRawAsync("template", "body");
        }

        [Test]
        public void INVALID_PUT_TEMPLATE_RESPONSE_SHOULD_WRITE_ERROR_TO_EVENT_WRITER_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.PutTemplateRaw("template", "body");

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public async Task INVALID_PUT_TEMPLATE_RESPONSE_SHOULD_WRITE_ERROR_TO_EVENT_WRITER_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var exception = new Exception();
            _responseValidator.WhenForAnyArgs(x => x.ValidateResponse(null)).Throw(exception);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            await client.PutTemplateRawAsync("template", "body");

            //Assert
            _eventWriter.Received().Error(Arg.Any<Type>(), Arg.Any<string>(), exception);
        }

        [Test]
        public async Task VALID_PUT_TEMPLATE_RESPONSE_SHOULD_NOT_WRITE_ERROR_TO_EVENT_WRITER_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            await client.PutTemplateRawAsync("template", "body");

            //Assert
            _eventWriter.DidNotReceiveWithAnyArgs().Error(null, null, null);
        }

        [Test]
        public void VALID_PUT_TEMPLATE_RESPONSE_SHOULD_NOT_WRITE_ERROR_TO_EVENT_WRITER_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.PutTemplateRaw("template", "body");

            //Assert
            _eventWriter.DidNotReceiveWithAnyArgs().Error(null, null, null);
        }

        [Test]
        public void VALID_INDEX_RESPONSE_SHOULD_NOT_WRITE_ERROR_TO_EVENT_WRITER_WHEN_NOT_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            client.IndexBulk(null);

            //Assert
            _eventWriter.DidNotReceiveWithAnyArgs().Error(null, null, null);
        }

        [Test]
        public async Task VALID_INDEX_RESPONSE_SHOULD_NOT_WRITE_ERROR_TO_EVENT_WRITER_WHEN_ASYNC()
        {
            //Arrange
            const int timeout = 0;
            var request = Substitute.For<IRestRequest>();
            _requestFactory.PrepareRequest(null).ReturnsForAnyArgs(request);
            var client = new WebElasticClient(OneServer, timeout, false, false, _authenticator, _clientFactory,
                _requestFactory, _responseValidator, _eventWriter);

            //Act
            await client.IndexBulkAsync(null);

            //Assert
            _eventWriter.DidNotReceiveWithAnyArgs().Error(null, null, null);
        }

    }
}