using EasilyNET.WebCore.Middleware;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 中间件扩展,用于统一处理中间件调用扩展
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用全局API耗时监控中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseResponseTime(this IApplicationBuilder builder) => builder.UseMiddleware<ResponseTimeMiddleware>();
}