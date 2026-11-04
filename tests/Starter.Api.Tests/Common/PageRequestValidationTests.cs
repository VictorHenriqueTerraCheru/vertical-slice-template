using FluentValidation;
using Starter.Api.Common;

namespace Starter.Api.Tests.Common;

public sealed class PageRequestValidationTests
{
    private sealed class TestValidator : AbstractValidator<PageRequest>
    {
        public TestValidator() => this.AddPageRules(["createdAt", "name"]);
    }

    public PageRequestValidationTests() => ValidationExtensions.ConfigureGlobal();

    private static readonly TestValidator Validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void Page_out_of_range_is_invalid(int page)
    {
        var result = Validator.Validate(new PageRequest { Page = page });

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_out_of_range_is_invalid(int size)
    {
        var result = Validator.Validate(new PageRequest { PageSize = size });

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("pageSize");
    }

    [Fact]
    public void Unknown_SortBy_is_invalid()
    {
        var result = Validator.Validate(new PageRequest { SortBy = "password" });

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("sortBy");
    }

    [Fact]
    public void SortBy_ignores_case()
    {
        Validator.Validate(new PageRequest { SortBy = "CREATEDAT" }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Default_values_are_valid()
    {
        Validator.Validate(new PageRequest()).IsValid.Should().BeTrue();
    }
}
