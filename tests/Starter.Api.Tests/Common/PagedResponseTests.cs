using Starter.Api.Common;

namespace Starter.Api.Tests.Common;

public sealed class PagedResponseTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(3, 2, 2)]
    [InlineData(4, 2, 2)]
    [InlineData(5, 2, 3)]
    public void TotalPages_rounds_up(int total, int size, int pages)
    {
        var response = PagedResponse<int>.Create([], 1, size, total);

        response.TotalPages.Should().Be(pages);
        response.TotalCount.Should().Be(total);
    }
}
