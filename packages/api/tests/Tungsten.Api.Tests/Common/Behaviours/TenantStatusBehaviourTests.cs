using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Behaviours;

namespace Tungsten.Api.Tests.Common.Behaviours;

public class TenantStatusBehaviourTests
{
    public record TestCommand(string Name) : IRequest<Result<string>>;

    [Fact]
    public async Task Handle_ActiveTenant_ProceedsToHandler()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("ACTIVE");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_SuspendedTenant_NonAdmin_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("SUSPENDED");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(Result<string>.Success("should not reach")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("suspended", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_SuspendedTenant_PlatformAdmin_ProceedsToHandler()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("SUSPENDED");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("PLATFORM_ADMIN");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("admin ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_TrialTenant_ProceedsToHandler()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("TRIAL");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var result = await behaviour.Handle(new TestCommand("test"), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_CancelledTenant_ReturnsFailureWithDistinctMessage()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("CANCELLED");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var result = await behaviour.Handle(new TestCommand("test"), _ => Task.FromResult(Result<string>.Success("nope")), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Contains("cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NoHttpContext_SkipsCheck()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        var accessor = new HttpContextAccessor { HttpContext = null };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("worker ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
