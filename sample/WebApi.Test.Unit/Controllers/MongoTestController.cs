using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.ComponentModel;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 测试mongodb的一些功能
/// </summary>
[ApiController, Route("api/[controller]"), ApiGroup("MongoTest", "v1", "MongoDB一些测试")]
public class MongoTestController(DbContext db1, DbContext2 db2) : ControllerBase
{
    private readonly FilterDefinitionBuilder<MongoTest> bf = Builders<MongoTest>.Filter;

    /// <summary>
    /// 添加一个动态数据,新版MongoDB不支持动态类型了,可以使用EasilyNET.Mongo.Extension来提供支持,可便于快速测试一些代码.
    /// </summary>
    /// <returns></returns>
    [HttpPost("PostOneTest")]
    public async Task PostOneTest()
    {
        var coll = db1.GetDatabase("newdb1").GetCollection<dynamic>("test.new1");
        await coll.InsertOneAsync(new
        {
            Decimal = 3.235223462346m,
            Double = 3.14d,
            Int32 = 123,
            Data = "test"
        });
    }

    /// <summary>
    /// 向MongoDB中插入.Net6+新增类型,测试自动转换是否生效
    /// </summary>
    /// <returns></returns>
    [HttpPost("MongoPost")]
    public Task MongoPost()
    {
        var o = new MongoTest
        {
            DateTime = DateTime.Now,
            TimeSpan = TimeSpan.FromMilliseconds(50000d),
            DateOnly = DateOnly.FromDateTime(DateTime.Now),
            TimeOnly = TimeOnly.FromDateTime(DateTime.Now),
            NullableDateOnly = DateOnly.FromDateTime(DateTime.Now),
            NullableTimeOnly = null
        };
        _ = db1.Test.InsertOneAsync(o);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 测试从MongoDB中取出插入的数据,再返回到Swagger查看数据JSON转换是否正常
    /// </summary>
    /// <returns></returns>
    [HttpGet("MongoGet")]
    public async Task<IEnumerable<MongoTest>> MongoGet() => await db1.Test.Find(bf.Empty).ToListAsync();

    /// <summary>
    /// 初始化Test2
    /// </summary>
    /// <returns></returns>
    [HttpPost("InitTest2")]
    public async Task InitTest2()
    {
        var os = new List<MongoTest2>();
        for (var i = 0; i < 30; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(i));
            os.Add(new() { Id = Guid.NewGuid().ToString(), Date = date, Index = i });
        }
        var session = await db1.GetStartedSessionAsync();
        await db1.Test2.InsertManyAsync(session, os);
        await session.CommitTransactionAsync();
    }

    /// <summary>
    /// 使用ID查询
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("Test2")]
    public async Task<MongoTest2> GetTest2(string id)
    {
        return await db1.Test2.Find(c => c.Id == id).SingleOrDefaultAsync();
    }

    /// <summary>
    /// 发送长数据,试试MongoDB.ConsoleDebug输出的JSON字符串,是否会截断
    /// </summary>
    /// <returns></returns>
    [HttpPost("LongData")]
    public async Task PostLongData()
    {
        var data = "10086".PadLeft(3000, '*');
        await db1.Database.GetCollection<dynamic>("long.data").InsertOneAsync(new
        {
            Data = data
        });
    }

    /// <summary>
    /// 查询测试
    /// </summary>
    /// <returns></returns>
    [HttpPost("Search")]
    public async Task<dynamic> Search(Search search)
    {
        var result = await db1.Test2.Find(c => c.Date >= search.Start && c.Date <= search.End).ToListAsync();
        return result;
    }

    /// <summary>
    /// 初始化Db2Test
    /// </summary>
    /// <returns></returns>
    [HttpPost("Db2Test")]
    public async Task Db2Test()
    {
        var os = new List<MongoTest>();
        for (var i = 0; i < 30; i++)
        {
            os.Add(new()
            {
                DateTime = DateTime.Now,
                TimeSpan = TimeSpan.FromMilliseconds(50000d),
                DateOnly = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                TimeOnly = TimeOnly.FromDateTime(DateTime.Now.AddDays(i))
            });
        }
        await db2.Test.InsertManyAsync(os);
    }
}

/// <summary>
/// 查询测试
/// </summary>
public class Search
{
    /// <summary>
    /// 开始日期
    /// </summary>
    [DefaultValue(typeof(DateOnly), "2022-11-02")] //对象类型设置默认值没啥用.就当是给开发人员看吧.
    public DateOnly Start { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    [DefaultValue("2022-11-05")]
    public DateOnly End { get; set; }

    /// <summary>
    /// 测试数值类型的参数设置默认值
    /// </summary>
    [DefaultValue(30)]
    public int Index { get; set; }
}