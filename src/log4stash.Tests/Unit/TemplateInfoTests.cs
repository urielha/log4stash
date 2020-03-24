using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using log4net.Core;
using log4stash.FileAccess;
using NSubstitute;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    class TemplateInfoTests
    {
        private IFileAccessor _fileAccessor;

        [SetUp]
        public void Setup()
        {
            _fileAccessor = Substitute.For<IFileAccessor>();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_NAME_IS_NULL()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor) {Name = null, FileName = "file"};

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_NAME_IS_EMPTY()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor) { Name = string.Empty, FileName = "file" };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILENAME_IS_NULL()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor) { Name = "name", FileName = null };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILENAME_IS_EMPTY()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor) { Name = "name", FileName = string.Empty };

            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeFalse();
        }

        [Test]
        public void TEMPLATE_IS_NOT_VALID_WHEN_FILE_DOES_NOT_EXIST()
        {
            //Arrange
            var template = new TemplateInfo(_fileAccessor) { Name = "name", FileName = "file" };
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
            var template = new TemplateInfo(_fileAccessor) { Name = "name", FileName = "file" };
            _fileAccessor.Exists(template.FileName).Returns(true);
            //Act   
            template.ActivateOptions();

            //Assert
            template.IsValid.Should().BeTrue();
        }

    }
}
