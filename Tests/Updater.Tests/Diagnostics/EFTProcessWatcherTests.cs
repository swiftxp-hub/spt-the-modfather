using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SwiftXP.SPT.TheModfather.Updater.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Environment;
using SwiftXP.SPT.TheModfather.Updater.Logging;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Updater.Tests.Diagnostics;

public class EFTProcessWatcherTests
{
    private readonly Mock<ISimpleLogger> _loggerMock;
    private readonly Mock<ICommandLineArgsReader> _argsReaderMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<IProcessWrapper> _processWrapperMock;

    public EFTProcessWatcherTests()
    {
        _loggerMock = new Mock<ISimpleLogger>();
        _argsReaderMock = new Mock<ICommandLineArgsReader>();
        _processServiceMock = new Mock<IProcessService>();
        _processWrapperMock = new Mock<IProcessWrapper>();
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncReturnsTrueWhenNoProcessFoundAtAll()
    {
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns((int?)null);

        _processServiceMock.Setup(x => x.GetProcessesByName("EscapeFromTarkov"))
                           .Returns(Array.Empty<IProcessWrapper>());

        _processServiceMock.Setup(x => x.GetProcessesByName("EscapeFromTarkov_BE"))
                           .Returns(Array.Empty<IProcessWrapper>());

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
        _loggerMock.Verify(x => x.WriteMessageAsync(It.Is<string>(s => s.Contains("Assuming closed")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncFindsIdViaCliAndWaits()
    {
        int pid = 100;
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns(pid);

        SetupProcessWrapper(pid, hasExited: true);

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
        _processServiceMock.Verify(x => x.GetProcessById(pid), Times.Once);
        _processServiceMock.Verify(x => x.GetProcessesByName(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncFindsIdViaStandardNameAndWaits()
    {
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns((int?)null);

        int pid = 200;
        SetupProcessSearch("EscapeFromTarkov", pid);
        SetupProcessWrapper(pid, hasExited: true);

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
        _loggerMock.Verify(x => x.WriteMessageAsync(It.Is<string>(s => s.Contains("from GetProcessesByName:")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncFindsIdViaBENameAndWaits()
    {
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns((int?)null);

        _processServiceMock.Setup(x => x.GetProcessesByName("EscapeFromTarkov"))
                           .Returns(Array.Empty<IProcessWrapper>());

        int pid = 300;
        SetupProcessSearch("EscapeFromTarkov_BE", pid);
        SetupProcessWrapper(pid, hasExited: true);

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
        _loggerMock.Verify(x => x.WriteMessageAsync(It.Is<string>(s => s.Contains("(BE)")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncHandlesNullProcessWrapperGracefully()
    {
        int pid = 400;
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns(pid);
        _processServiceMock.Setup(x => x.GetProcessById(pid)).Returns((IProcessWrapper?)null);

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncCatchesExceptionsFromProcess()
    {
        int pid = 500;
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns(pid);

        _processServiceMock.Setup(x => x.GetProcessById(pid))
                           .Throws(new ArgumentException("Process dead"));

        EFTProcessWatcher watcher = CreateWatcher();

        bool result = await watcher.WaitForProcessToCloseAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForProcessToCloseAsyncLoopsUntilExitedAndRefreshes()
    {
        int pid = 600;
        _argsReaderMock.Setup(x => x.GetProcessId()).Returns(pid);

        _processWrapperMock.SetupSequence(p => p.HasExited)
                           .Returns(false)
                           .Returns(true);

        _processWrapperMock.Setup(p => p.Id).Returns(pid);
        _processWrapperMock.Setup(p => p.Refresh());
        _processServiceMock.Setup(x => x.GetProcessById(pid)).Returns(_processWrapperMock.Object);

        EFTProcessWatcher watcher = CreateWatcher();

        Stopwatch sw = Stopwatch.StartNew();
        bool result = await watcher.WaitForProcessToCloseAsync();
        sw.Stop();

        Assert.True(result);
        Assert.True(sw.ElapsedMilliseconds >= 450);
        _processWrapperMock.Verify(p => p.Refresh(), Times.AtLeastOnce);
    }

    private EFTProcessWatcher CreateWatcher()
    {
        return new EFTProcessWatcher(
            _loggerMock.Object,
            _argsReaderMock.Object,
            _processServiceMock.Object);
    }

    private void SetupProcessWrapper(int id, bool hasExited)
    {
        _processWrapperMock.Setup(p => p.Id).Returns(id);
        _processWrapperMock.Setup(p => p.HasExited).Returns(hasExited);
        _processServiceMock.Setup(x => x.GetProcessById(id)).Returns(_processWrapperMock.Object);
    }

    private void SetupProcessSearch(string processName, int id)
    {
        Mock<IProcessWrapper> procMock = new();
        procMock.Setup(p => p.Id).Returns(id);

        _processServiceMock.Setup(x => x.GetProcessesByName(processName))
                           .Returns([procMock.Object]);
    }
}