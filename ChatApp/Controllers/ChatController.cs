using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Dapper;
using ChatApp.Models;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using ChatApp.Helpers;
namespace ChatApp.Controllers
{
    public class ChatController : Controller
    {
        DapperRepository repo = new DapperRepository();
        [Authorize]
        // GET: Chat
        public ActionResult Index()
        {
            ApplicationUser CurrentUser = repo.GetCurrentUser();
            if (CurrentUser != null && !CurrentUser.SetupCompleted)
            {
                return RedirectToAction("FinishSetup", "Account");
            }
            else
            {
                return View(CurrentUser);
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult Search(SearchSelectViewModel model)
        {
            return View(model);
        }

        [HttpPost]
        public ActionResult AddFriend(string friendName)
        {
            Debug.WriteLine(friendName);
            ApplicationUser user = repo.GetCurrentUser();
            repo.addFriend(user, friendName);
            return new EmptyResult();
        }

        [HttpPost]//Email notifications are handled here
        public ActionResult SendMessage(Message msg)
        {
            msg.fromName = User.Identity.Name;
            if (msg.UserTarget != null)
            { //Sending message to user
                msg.UserTargetId = repo.getIdByUsername(msg.UserTarget);
                if (!ChatApp.Hubs.UserCounter.Users.Contains(msg.UserTarget))
                { // If target user is not online
                    Task.Factory.StartNew(() => //Spawn new thread, kills server resources
                    {
                        var body = "<h5>You have a new message from user " + msg.fromName + "</h5><p>" + msg.Body + "</p>";
                        var header = "New message from " + msg.fromName + "!";
                        var email = repo.getEmailById(msg.UserTargetId);
                        EmailHelper mail = new EmailHelper(body, header, email);
                        mail.Send();
                    });
                }
            }
            else
            { //Sending message to conference
                var confId = msg.ConfTarget ?? default(int);
                var confUsers = repo.getUsersByConferenceId(confId);
                Task.Factory.StartNew(() =>
                {
                    var body = "<p><b>" + msg.fromName + ": </b>" + msg.Body + "</p>";
                    var header = "New message from conference " + repo.getConfName(confId) + "!";
                    EmailHelper mail = new EmailHelper(body, header, null);

                    foreach (var user in confUsers)
                    {//Add offline users to Bcc
                        if (!ChatApp.Hubs.UserCounter.Users.Contains(user.UserName)){
                            var email = repo.getEmailById(repo.getIdByUsername(user.UserName));
                            mail.AddCc(email);
                        }
                    }
                    mail.Send();
                });
            }
            repo.addMsg(msg, repo.GetCurrentUser());
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult GetUsrMessages(string userName)
        {
            ApplicationUser currentUser = repo.GetCurrentUser();
            string toUserId = repo.getIdByUsername(userName);
            Debug.WriteLine("username = " + userName);
            IEnumerable<Message> msgList = repo.getUserMsg(currentUser, toUserId);
            foreach (var item in msgList)
            {
                Debug.WriteLine(item.Body);
            }
            var json = JsonConvert.SerializeObject(msgList);
            return Content(json, "application/json");
        }

        [HttpPost]
        public ActionResult AddNewConf(string confname)
        {
            Debug.WriteLine("Adding new conf");
            ApplicationUser user = repo.GetCurrentUser();
            int id = repo.insertNewConf(confname, user);
            Debug.WriteLine("Id is " + id);
            var json = JsonConvert.SerializeObject(new { confId = id});
            return Content(json, "application/json");
        }

        [HttpPost]
        public ActionResult GetConfUsers(int confId)
        {
            var users = repo.getUsersByConferenceId(confId);
            var json = JsonConvert.SerializeObject(users);
            return Content(json, "application/json");
        }

        [HttpPost]
        public ActionResult AddUserToConf(string userName, int confId)
        {
            var id = repo.getIdByUsername(userName);
            repo.addUserToConf(id, confId);
            return new EmptyResult();
        }

        [HttpPost]

        public ActionResult GetConfMessages(int confId)
        {
            var msgs = repo.getConfMsg(confId);
            var json = JsonConvert.SerializeObject(msgs);
            return Content(json, "application/json");
        }
    }
}