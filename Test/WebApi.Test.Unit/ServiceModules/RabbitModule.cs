﻿using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.RabbitBus;

namespace WebApi.Test.Unit;

/// <summary>
/// Rabbit服务注册
/// </summary>
public class RabbitModule : AppModule
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddRabbitBus(c =>
        {
            c.Host = "localhost";
            c.Port = 5672;
            c.UserName = "guest";
            c.PassWord = "guest";
            c.RetryCount = 5;
            c.VirtualHost = "/";
        });
    }
}