﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MobileTron3kApi.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController : Controller
    {
        public readonly IConfiguration _configuration;
        public readonly IHostingEnvironment _hostingEnvironment;

        public DashboardController(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("{accountId}")]
        public async Task<IEnumerable<Models.Summary>> Get(int accountId)
        {
            var connectionString = _hostingEnvironment.IsDevelopment() ? _configuration["ConnectionString.Main"] : ConfigurationExtensions.GetConnectionString(_configuration, "Main");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                return (await connection
                    .QueryAsync<Models.Summary>(@"
                        select
                            OrderDate,
                            NumberOfOrders = count(*),
                            ValueOfOrders = sum(OrderTotal)
                        from
                            (
                                select
                                    AccountID,
                                    OrderDate = convert(date, OrderDate at time zone 'UTC' at time zone 'GMT Standard Time'),
                                    OrderTotal,
                                    IsDeleted
                                from
                                    [Order]
                                where
                                    AccountID = @AccountID
                                    and IsDeleted = 0
                                    and OrderDate >= convert(date, dateadd(day, -31, getutcdate()))
                            ) a
                        group by
                            a.OrderDate
                        order by
                            a.OrderDate desc
                        ",
                        new { AccountID = accountId }))
                    .AsList();
            }
        }
    }
}
