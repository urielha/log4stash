using System;
using System.Collections.Generic;
using log4stash.Bulk;
using log4stash.ElasticClient;
using log4stash.Extensions;
using log4stash.FileAccess;
using log4stash.LogEventFactory;
using log4stash.Timing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    class ElasticSearchAppenderTests
    {
        private IElasticClientFactory _elasticClientFactory;
        private IElasticsearchClient _elasticClient;
        private ITolerateCallsFactory _tolerateCallsFactory;
        private IIndexingTimer _timer;
        private ILogBulkSet _bulk;
        private ILogEventFactory _logEventFactory;
        private IFileAccessor _fileAccessor;
        private IElasticAppenderFilter _elasticFilters;

        [SetUp]
        public void Setup()
        {
            _elasticClientFactory = Substitute.For<IElasticClientFactory>();
            _elasticClient = Substitute.For<IElasticsearchClient>();
            _timer = Substitute.For<IIndexingTimer>();
            _tolerateCallsFactory = Substitute.For<ITolerateCallsFactory>();
            _bulk = Substitute.For<ILogBulkSet>();
            _logEventFactory = Substitute.For<ILogEventFactory>();
            _elasticFilters = Substitute.For<IElasticAppenderFilter>();
            _fileAccessor = Substitute.For<IFileAccessor>();
            _elasticClientFactory.CreateClient(null, 0, false, false, null).ReturnsForAnyArgs(_elasticClient);
        }

        [Test]
        public void BULK_SHOULD_BE_RESET_WHEN_TIMER_ELAPSED()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor);
            _bulk.ResetBulk().Returns(new List<InnerBulkOperation>());
            
            //Act
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _bulk.Received().ResetBulk();
        }

        [Test]
        public void EMPTY_BULK_IS_NOT_INDEXED_WHEN_TIMER_ELAPSES()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor);
            _bulk.ResetBulk().Returns(new List<InnerBulkOperation>());

            //Act
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulk(null);
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulkAsync(null);
        }

        [Test]
        public void BULK_IS_INDEXED_ASYNC_WHEN_TIMER_ELAPSES()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor);
            var bulk = new List<InnerBulkOperation> {new InnerBulkOperation()};
            _bulk.ResetBulk().Returns(bulk);
            appender.IndexAsync = true;
            appender.ActivateOptions();

            //Act
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _elasticClient.Received().IndexBulkAsync(bulk);
        }

        [Test]
        public void BULK_IS_INDEXED_WHEN_TIMER_ELAPSES()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.IndexAsync = false;
            appender.ActivateOptions();

            //Act   
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _elasticClient.Received().IndexBulk(bulk);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public void CALLS_TOLERATOR_CREATED_WHEN_PROPERTY_CHANGED(int numOfSeconds)
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor); var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            appender.ActivateOptions();

            //Act   
            appender.TolerateLogLogInSec = numOfSeconds;

            //Assert
            _tolerateCallsFactory.Received().Create(numOfSeconds);
        }

        [Test]
        public void INDEX_BULK_ASYNC_EXCEPTION_IS_NOT_THROWN_IN_APPENDER()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor) {IndexAsync = true};
            _elasticClient.WhenForAnyArgs(x => x.IndexBulkAsync(null)).Throw(new Exception());
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
        }

        [Test]
        public void INDEX_BULK_EXCEPTION_IS_NOT_THROWN_IN_APPENDER()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor) {IndexAsync = false};
            _elasticClient.WhenForAnyArgs(x => x.IndexBulkAsync(null)).Throw(new Exception());
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
        }

        [Test]
        public void TEMPLATE_IS_NOT_PUT_WHEN_IS_NOT_VALID()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventFactory, _elasticFilters, _fileAccessor)
            {
                IndexAsync = false, Template = new TemplateInfo()
            };

            //Act   
            appender.ActivateOptions();

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().PutTemplateRaw(null, null);
        }


    }
}
