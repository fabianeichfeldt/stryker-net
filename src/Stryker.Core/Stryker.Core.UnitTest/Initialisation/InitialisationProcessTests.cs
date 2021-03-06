﻿using Microsoft.CodeAnalysis;
using Moq;
using Stryker.Core.Exceptions;
using Stryker.Core.Initialisation;
using Stryker.Core.Initialisation.ProjectComponent;
using Stryker.Core.Options;
using Stryker.Core.Reporters;
using Stryker.Core.TestRunners;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Stryker.Core.UnitTest.Initialisation
{
    public class InitialisationProcessTests
    {
        [Fact]
        public void InitialisationProcess_ShouldCallNeededResolvers()
        {
            var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
            var inputFileResolverMock = new Mock<IInputFileResolver>(MockBehavior.Strict);
            var initialBuildProcessMock = new Mock<IInitialBuildProcess>(MockBehavior.Strict);
            var initialTestProcessMock = new Mock<IInitialTestProcess>(MockBehavior.Strict);
            var assemblyReferenceResolverMock = new Mock<IAssemblyReferenceResolver>(MockBehavior.Strict);

            testRunnerMock.Setup(x => x.RunAll(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new TestRunResult { Success = true }); // testrun is successful
            inputFileResolverMock.Setup(x => x.ResolveInput(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Core.Initialisation.ProjectInfo
                {
                    TestProjectPath = "c:/test",
                    ProjectContents = new FolderComposite
                    {
                        Name = "ProjectRoot",
                        Children = new Collection<ProjectComponent>
                        {
                            new FileLeaf
                            {
                                Name = "SomeFile.cs"
                            }
                        }
                    },
                    ProjectUnderTestAssemblyName = "ExampleProject.dll",
                    ProjectUnderTestPath = @"c:\ExampleProject\",
                    TargetFramework = "netcoreapp2.0"
                });
            initialTestProcessMock.Setup(x => x.InitialTest(It.IsAny<ITestRunner>())).Returns(999);
            initialBuildProcessMock.Setup(x => x.InitialBuild(It.IsAny<string>(), It.IsAny<string>()));
            assemblyReferenceResolverMock.Setup(x => x.ResolveReferences(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Enumerable.Empty<PortableExecutableReference>());

            var target = new InitialisationProcess(
                inputFileResolverMock.Object, 
                initialBuildProcessMock.Object,
                initialTestProcessMock.Object,
                testRunnerMock.Object, 
                assemblyReferenceResolverMock.Object);

            var options = new StrykerOptions();

            var result = target.Initialize(options);

            inputFileResolverMock.Verify(x => x.ResolveInput(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void InitialisationProcess_ShouldThrowOnFailedInitialTestRun()
        {
            var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
            var inputFileResolverMock = new Mock<IInputFileResolver>(MockBehavior.Strict);
            var initialBuildProcessMock = new Mock<IInitialBuildProcess>(MockBehavior.Strict);
            var initialTestProcessMock = new Mock<IInitialTestProcess>(MockBehavior.Strict);
            var assemblyReferenceResolverMock = new Mock<IAssemblyReferenceResolver>(MockBehavior.Strict);

            testRunnerMock.Setup(x => x.RunAll(It.IsAny<int>(), It.IsAny<int>()));
            inputFileResolverMock.Setup(x => x.ResolveInput(It.IsAny<string>(), It.IsAny<string>())).Returns(
                new Core.Initialisation.ProjectInfo
                {
                    TestProjectPath = "c:/test",
                    ProjectContents = new FolderComposite
                    {
                        Name = "ProjectRoot",
                        Children = new Collection<ProjectComponent>
                        {
                            new FileLeaf
                            {
                                Name = "SomeFile.cs"
                            }
                        }
                    },
                    ProjectUnderTestAssemblyName = "ExampleProject.dll",
                    ProjectUnderTestPath = @"c:\ExampleProject\",
                    TargetFramework = "netcoreapp2.0"
                });
            initialBuildProcessMock.Setup(x => x.InitialBuild(It.IsAny<string>(), It.IsAny<string>()));
            initialTestProcessMock.Setup(x => x.InitialTest(It.IsAny<ITestRunner>())).Throws(new StrykerInputException("")); // failing test
            assemblyReferenceResolverMock.Setup(x => x.ResolveReferences(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Enumerable.Empty<PortableExecutableReference>()).Verifiable();

            var target = new InitialisationProcess(
                inputFileResolverMock.Object, 
                initialBuildProcessMock.Object,
                initialTestProcessMock.Object,
                testRunnerMock.Object, 
                assemblyReferenceResolverMock.Object);
            var options = new StrykerOptions();

            var exception = Assert.Throws<StrykerInputException>(() => target.Initialize(options));

            inputFileResolverMock.Verify(x => x.ResolveInput(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            assemblyReferenceResolverMock.Verify();
            initialTestProcessMock.Verify(x => x.InitialTest(testRunnerMock.Object), Times.Once);
        }
    }
}
