using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.Linq;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDBContext _context;
        public DashboardController(ApplicationDBContext context)
        {
            _context = context;
        }
    
        public async Task<IActionResult> Index()
        {
            // Last 7 days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;
            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(transaction => transaction.Category)
                .Where(transaction => transaction.Date >= StartDate && transaction.Date <= EndDate)
                .ToListAsync();

            // Total Income 
            int TotalIncome = SelectedTransactions
                .Where(t => t.Category.Type == "Income")
                .Sum(t => t.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            // Total Expense
            int TotalExpense = SelectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .Sum(t => t.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            // Balance 
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            // Doughnut Chart - Expense By Category 
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.CategoryId)
                .Select(t => new
                {
                    categoryTitleWithIcon = t.First().Category.Icon + " " + t.First().Category.Title,
                    amount = t.Sum(j => j.Amount),
                    formattedAmount = t.Sum(j => j.Amount).ToString("C0")
                })
                .OrderBy(t => t.amount)
                .ToList();

            // Spline Chart - Income vs Expense 
            // Income 
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(t => t.Category.Type == "Income")
                .GroupBy(t => t.Date)
                .Select(t => new SplineChartData()
                {
                    day = t.First().Date.ToString("dd-MMM"),
                    income = t.Sum(a => a.Amount)
                })
                .ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Date)
                .Select(t => new SplineChartData()
                {
                    day = t.First().Date.ToString("dd-MMM"),
                    expense = t.Sum(a => a.Amount)
                })
                .ToList();
            // Combine Income & Expense
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(t => StartDate.AddDays(t).ToString("dd-MMM"))
                .ToArray();
            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new 
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense
                                      };
            // Recent Transactions 
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .Take(8)
                .ToListAsync();


            return View();
        }
    }
    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
