namespace Starter.Api.Common;

public interface IEndpointModule
{
    static abstract void Map(IEndpointRouteBuilder endpoints);
}
