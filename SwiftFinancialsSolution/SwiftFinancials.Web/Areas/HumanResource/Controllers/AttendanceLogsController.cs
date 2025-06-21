using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class AttendanceLogsController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // GET: HumanResource/AttendanceLogs
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            var logs = new List<Attendancelog>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM AttendanceLogs", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(MapReaderToAttendanceLog(reader));
                    }
                }
            }

            return View(logs);
        }

        // GET: HumanResource/AttendanceLogs/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: HumanResource/AttendanceLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Attendancelog model)
        {
            await ServeNavigationMenus();

            model.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                INSERT INTO AttendanceLogs (Id, EmployeeName, Date, TimeIn, TimeOut, Remarks)
                VALUES (@Id, @EmployeeName, @Date, @TimeIn, @TimeOut, @Remarks)", conn))
            {
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@EmployeeName", (object)model.EmployeeName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Date", model.Date);
                cmd.Parameters.AddWithValue("@TimeIn", (object)model.TimeIn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TimeOut", (object)model.TimeOut ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        public async Task<ActionResult> Details(Guid? id)
        {
            await ServeNavigationMenus();

            Attendancelog log = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM AttendanceLogs WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        log = MapReaderToAttendanceLog(reader);
                    }
                }
            }

            if (log == null)
                return HttpNotFound();

            return View(log);
        }
        // GET: HumanResource/AttendanceLogs/Edit/Guid
        public async Task<ActionResult> Edit(Guid? id)
        {
            await ServeNavigationMenus();

            Attendancelog log = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM AttendanceLogs WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        log = MapReaderToAttendanceLog(reader);
                    }
                }
            }

            if (log == null)
                return HttpNotFound();

            return View(log);
        }

        // POST: HumanResource/AttendanceLogs/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Attendancelog model)
        {
            await ServeNavigationMenus();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                UPDATE AttendanceLogs 
                SET EmployeeName = @EmployeeName, 
                    Date = @Date, 
                    TimeIn = @TimeIn, 
                    TimeOut = @TimeOut, 
                    Remarks = @Remarks 
                WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@EmployeeName", (object)model.EmployeeName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Date", model.Date);
                cmd.Parameters.AddWithValue("@TimeIn", (object)model.TimeIn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TimeOut", (object)model.TimeOut ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // Helper method to map SqlDataReader to Attendancelog
        private Attendancelog MapReaderToAttendanceLog(SqlDataReader reader)
        {
            return new Attendancelog
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                EmployeeName = reader["EmployeeName"]?.ToString(),
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                TimeIn = reader["TimeIn"].ToString(),
                TimeOut = reader["TimeOut"].ToString(),
                Remarks = reader["Remarks"].ToString()
            };
        }
    }
}
