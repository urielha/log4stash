using FluentAssertions;
using log4stash.ErrorHandling;
using log4stash.FileAccess;
using NSubstitute;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    class TemplateInfoTests
    {
        private IFileAccessor _fileAccessor;
        private IExternalEventWriter _eventWriter;

        [SetUp]
        public void Setup()
        {
            _fileAccessor = Substitute.For<IFileAccessor>();
            _eventWriter= Substitute.For<IExternalEventWriter>();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_NAME_IS_NULL()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) {Name = null, FileName = "file"};

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_NAME_IS_EMPTY()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) { Name = string.Empty, FileName = "file" };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILENAME_IS_NULL()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) { Name = "name", FileName = null };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILENAME_IS_EMPTY()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) { Name = "name", FileName = string.Empty };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILE_DOES_NOT_EXIST()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) { Name = "name", FileName = "file" };
            _fileAccessor.Exists(template.FileName).Returns(false);
            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_VALID_WHEN_ALL_PARAMETERS_ARE_VALID()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor, _eventWriter) { Name = "name", FileName = "file" };
            _fileAccessor.Exists(template.FileName).Returns(true);
            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeTrue();
        }

    }
}
