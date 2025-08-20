using SwiftFinancials.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Controllers
{
    public class MTransactionController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
 
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            var transactions = new List<MTransaction>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM dbo.MTransaction ORDER BY ID DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        transactions.Add(ReadTransaction(reader));
                    }
                }
            }

            return View(transactions);
        }

        public async Task<ActionResult> Details(long id)
        {
            await ServeNavigationMenus();

            MTransaction transaction = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM dbo.MTransaction WHERE ID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", id);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        transaction = ReadTransaction(reader);
                    }
                }
            }

            if (transaction == null) return HttpNotFound();

            return View(transaction);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MTransaction transaction)
        {
            await ServeNavigationMenus();

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO dbo.MTransaction
                        (TransactionDate, ClientCode, SessionID, AccountNo, AccountName, DocumentNo, TransactionType, Telephone, Posted, DatePosted, DestinationAccount, LoanNo, Status, Comments, Amount, Charge, Description, ApplicationType)
                        VALUES
                        (@TransactionDate, @ClientCode, @SessionID, @AccountNo, @AccountName, @DocumentNo, @TransactionType, @Telephone, @Posted, @DatePosted, @DestinationAccount, @LoanNo, @Status, @Comments, @Amount, @Charge, @Description, @ApplicationType)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    AddParameters(cmd, transaction);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToAction("Index");
            }
            return View(transaction);
        }

        public async Task<ActionResult> Edit(long id)
        {
            await ServeNavigationMenus();

            MTransaction transaction = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM dbo.MTransaction WHERE ID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", id);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        transaction = ReadTransaction(reader);
                    }
                }
            }

            if (transaction == null) return HttpNotFound();

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(MTransaction transaction)
        {
            await ServeNavigationMenus();

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"UPDATE dbo.MTransaction SET
                        TransactionDate=@TransactionDate, ClientCode=@ClientCode, SessionID=@SessionID, AccountNo=@AccountNo, AccountName=@AccountName, DocumentNo=@DocumentNo, TransactionType=@TransactionType, Telephone=@Telephone,
                        Posted=@Posted, DatePosted=@DatePosted, DestinationAccount=@DestinationAccount, LoanNo=@LoanNo, Status=@Status, Comments=@Comments, Amount=@Amount, Charge=@Charge, Description=@Description, ApplicationType=@ApplicationType
                        WHERE ID = @ID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    AddParameters(cmd, transaction);
                    cmd.Parameters.AddWithValue("@ID", transaction.ID);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToAction("Index");
            }
            return View(transaction);
        }

        public async Task<ActionResult> Delete(long id)
        {
            await ServeNavigationMenus();

            MTransaction transaction = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM dbo.MTransaction WHERE ID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", id);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        transaction = ReadTransaction(reader);
                    }
                }
            }

            if (transaction == null) return HttpNotFound();

            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            await ServeNavigationMenus();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM dbo.MTransaction WHERE ID = @ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        private MTransaction ReadTransaction(SqlDataReader reader)
        {
            return new MTransaction
            {
                ID = Convert.ToInt64(reader["ID"]),
                TransactionDate = reader["TransactionDate"] as DateTime?,
                ClientCode = reader["ClientCode"] as string,
                SessionID = reader["SessionID"] != DBNull.Value ? (string)reader["SessionID"] : null,
                AccountNo = reader["AccountNo"] as string,
                AccountName = reader["AccountName"] as string,
                DocumentNo = reader["DocumentNo"] as string,
                TransactionType = reader["TransactionType"] as string,
                Telephone = reader["Telephone"] as string,
                Posted = reader["Posted"] != DBNull.Value ? (bool?)reader["Posted"] : null,
                DatePosted = reader["DatePosted"] as DateTime?,
                DestinationAccount = reader["DestinationAccount"] as string,
                LoanNo = reader["LoanNo"] as string,
                Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0,
                Comments = reader["Comments"] as string,
                Amount = reader["Amount"] != DBNull.Value ? (string)reader["Amount"] : null,
                Charge = reader["Charge"] != DBNull.Value ? (string)reader["Charge"] : null,
                Description = reader["Description"] as string,
                ApplicationType = reader["ApplicationType"] as string
            };
        }

        private void AddParameters(SqlCommand cmd, MTransaction t)
        {
            cmd.Parameters.AddWithValue("@TransactionDate", (object)t.TransactionDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClientCode", (object)t.ClientCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionID", (object)t.SessionID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNo", (object)t.AccountNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountName", (object)t.AccountName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentNo", (object)t.DocumentNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TransactionType", (object)t.TransactionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Telephone", (object)t.Telephone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Posted", (object)t.Posted ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DatePosted", (object)t.DatePosted ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DestinationAccount", (object)t.DestinationAccount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LoanNo", (object)t.LoanNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object)t.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Comments", (object)t.Comments ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", (object)t.Amount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Charge", (object)t.Charge ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)t.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ApplicationType", (object)t.ApplicationType ?? DBNull.Value);
        }
    }
}