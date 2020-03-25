using System;
using System.Collections.Generic;
using log4net.Core;
using log4stash.Bulk;
using log4stash.ElasticClient;
using log4stash.Extensions;
using log4stash.FileAccess;
using log4stash.LogEvent;
using log4stash.Timing;
using NSubstitute;
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
        private ILogEventConverterFactory _logEventConverterFactory;
        private ILogEventConverter _logEventConverter;
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
            _logEventConverterFactory = Substitute.For<ILogEventConverterFactory>();
            _logEventConverter = Substitute.For<ILogEventConverter>();
            _elasticFilters = Substitute.For<IElasticAppenderFilter>();
            _fileAccessor = Substitute.For<IFileAccessor>();
            _elasticClientFactory.CreateClient(null, 0, false, false, null).ReturnsForAnyArgs(_elasticClient);
            _logEventConverterFactory.Create(FixFlags.All, false).ReturnsForAnyArgs(_logEventConverter);
        }

        [Test]
        public void BULK_SHOULD_BE_RESET_WHEN_TIMER_ELAPSED()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor);
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor);
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor);
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor);
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor); var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor) {IndexAsync = true};
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
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor) {IndexAsync = false};
            _elasticClient.WhenForAnyArgs(x => x.IndexBulkAsync(null)).Throw(new Exception());
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
        }

        [Test]
        public void TEMPLATE_IS_NOT_PUT_WHEN_IS_NULL()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                Template = null
            };

            //Act   
            appender.ActivateOptions();

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().PutTemplateRaw(null, null);
        }

        [Test]
        public void TEMPLATE_IS_NOT_PUT_WHEN_IS_NOT_VALID()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                Template = new TemplateInfo()
            };

            //Act   
            appender.ActivateOptions();

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().PutTemplateRaw(null, null);
        }

        [Test]
        public void TEMPLATE_IS_PUT_WHEN_IS_VALID()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor)
            {
                FileName = "file",
                Name = "template"
            };
            var rawBody = "body";
            _fileAccessor.Exists("file").Returns(true);
            _fileAccessor.ReadAllText(template.FileName).Returns(rawBody);
            template.ActivateOptions();
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                Template = template
            };

            //Act   
            appender.ActivateOptions();

            //Assert
            _elasticClient.Received().PutTemplateRaw(template.Name, rawBody);
        }

        [Test]
        public void BULK_IS_DROPPED_WHEN_OVERFLOW()
        {
            //Arrange
            const int bulkSize = 1;
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                DropEventsOverBulkLimit = true,
                BulkSize = bulkSize
            };
            _bulk.Count.Returns(bulkSize);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            appender.DoAppend(new LoggingEvent(new LoggingEventData()));

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulk(null);
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulkAsync(null);
        }
        
        [Test]
        public void BULK_IS_INDEXED_WHEN_BULK_LIMIT_IS_REACHED()
        {
            //Arrange
            const int bulkSize = 1;
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                DropEventsOverBulkLimit = false,
                BulkSize = bulkSize,
                IndexAsync = false
            };
            _bulk.Count.Returns(bulkSize);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            appender.DoAppend(new LoggingEvent(new LoggingEventData()));

            //Assert
            _elasticClient.Received().IndexBulk(bulk);
            //_elasticClient.DidNotReceiveWithAnyArgs().IndexBulkAsync(null);
        }

        [Test]
        public void BULK_IS_INDEXED_ASYNC_WHEN_BULK_LIMIT_IS_REACHED()
        {
            //Arrange
            const int bulkSize = 1;
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                DropEventsOverBulkLimit = false,
                BulkSize = bulkSize,
                IndexAsync = true
            };
            _bulk.Count.Returns(bulkSize);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            appender.DoAppend(new LoggingEvent(new LoggingEventData()));

            //Assert
            _elasticClient.Received().IndexBulkAsync(bulk);
        }

        [Test]
        public void BULK_IS_NOT_INDEXED_WHEN_EVENT_IS_NULL()
        {
            //Arrange
            const int bulkSize = 1;
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                DropEventsOverBulkLimit = false,
                BulkSize = bulkSize
            };
            _bulk.Count.Returns(bulkSize);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            appender.DoAppend((LoggingEvent) null);

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulk(null);
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulkAsync(null);
        }

        [Test]
        public void BULK_IS_NOT_INDEXED_WHEN_BULK_LIMIT_IS_ZERO()
        {
            //Arrange
            const int bulkSize = 0;
            var appender = new ElasticSearchAppender(_elasticClientFactory, "index", "type", _timer,
                _tolerateCallsFactory,
                _bulk, _logEventConverterFactory, _elasticFilters, _fileAccessor)
            {
                DropEventsOverBulkLimit = false,
                BulkSize = bulkSize,
            };
            _bulk.Count.Returns(bulkSize + 1);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.ActivateOptions();

            //Act   
            appender.DoAppend(new LoggingEvent(new LoggingEventData()));

            //Assert
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulk(null);
            _elasticClient.DidNotReceiveWithAnyArgs().IndexBulkAsync(null);
        }


    }
}
