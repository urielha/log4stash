using System;
using System.Collections.Generic;
using log4stash.Bulk;
using log4stash.Extensions;
using log4stash.LogEventFactory;
using log4stash.Timing;
using NSubstitute;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    class ElasticSearchAppenderTests
    {
        private IElasticsearchClient _elasticClient;
        private ITolerateCallsFactory _tolerateCallsFactory;
        private IIndexingTimer _timer;
        private ILogBulkSet _bulk;
        private ILogEventFactory _logEventFactory;

        [SetUp]
        public void Setup()
        {
            _elasticClient = Substitute.For<IElasticsearchClient>();
            _timer = Substitute.For<IIndexingTimer>();
            _tolerateCallsFactory = Substitute.For<ITolerateCallsFactory>();
            _bulk = Substitute.For<ILogBulkSet>();
            _logEventFactory = Substitute.For<ILogEventFactory>();
        }

        [Test]
        public void BULK_SHOULD_BE_RESET_WHEN_TIMER_ELAPSED()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClient, "index", "type", _timer, _tolerateCallsFactory,
                _bulk, _logEventFactory);
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
            var appender = new ElasticSearchAppender(_elasticClient, "index", "type", _timer, _tolerateCallsFactory, _bulk);
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
            var appender = new ElasticSearchAppender(_elasticClient, "index", "type", _timer, _tolerateCallsFactory, _bulk);
            var bulk = new List<InnerBulkOperation> {new InnerBulkOperation()};
            _bulk.ResetBulk().Returns(bulk);
            appender.IndexAsync = true;
            
            //Act
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _elasticClient.Received().IndexBulkAsync(bulk);
        }

        [Test]
        public void BULK_IS_INDEXED_WHEN_TIMER_ELAPSES()
        {
            //Arrange
            var appender = new ElasticSearchAppender(_elasticClient, "index", "type", _timer, _tolerateCallsFactory, _bulk);
            var bulk = new List<InnerBulkOperation> { new InnerBulkOperation() };
            _bulk.ResetBulk().Returns(bulk);
            appender.IndexAsync = false;

            //Act
            _timer.Elapsed += Raise.Event<EventHandler<object>>(this, null);

            //Assert
            _elasticClient.Received().IndexBulk(bulk);
        }


    }
}
