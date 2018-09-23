using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ContactsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncVAsyncController : ControllerBase
    {
        private readonly string _connectionString;
        public SyncVAsyncController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("sync")]
        public IActionResult SyncGet()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("WAITFOR DELAY '00:00:02';", conn))
                {
                    conn.Open();
                    cmd.ExecuteScalar();
                    conn.Close();
                }
            }

            return Ok();
        }

        [HttpGet("async")]
        public async Task<IActionResult> AsyncGet()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("WAITFOR DELAY '00:00:02';", conn))
                {
                    await conn.OpenAsync();
                    await cmd.ExecuteScalarAsync();
                    conn.Close();
                }
            }

            return Ok();
        }

    }
}