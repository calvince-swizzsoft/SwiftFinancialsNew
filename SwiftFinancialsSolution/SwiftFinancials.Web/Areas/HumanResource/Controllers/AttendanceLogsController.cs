using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO.HumanResourcesModule;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class AttendanceLogsController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // GET: HumanResource/AttendanceLogs
        public ActionResult Index()
        {
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
        public ActionResult Create()
        {
            return View();
        }

        // POST: HumanResource/AttendanceLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Attendancelog model)
        {
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

        // GET: HumanResource/AttendanceLogs/Edit/Guid
        public ActionResult Edit(Guid id)
        {
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
        public ActionResult Edit(Attendancelog model)
        {
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
                TimeIn = reader["TimeIn"] as string,
                TimeOut = reader["TimeOut"] as string,
                Remarks = reader["Remarks"] as string
            };
        }
    }
}
