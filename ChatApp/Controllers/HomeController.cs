using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dapper;
using ChatApp.Models;

namespace ChatApp.Controllers
{
    public class HomeController : Controller
    {
        DapperRepository repo = new DapperRepository();
        public ActionResult Index()
        {
            ApplicationUser CurrentUser = repo.GetCurrentUser();
            if (CurrentUser != null && !CurrentUser.SetupCompleted)
                RedirectToAction("FinishSetup", "Account");
            return View();
        }
    }
}