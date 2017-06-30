using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Diagnostics;
using ChatApp.Models;

namespace ChatApp.Hubs
{
    public static class UserCounter
    {
        public static HashSet<string> Users = new HashSet<string>();
    }
    [Authorize]
    public class ChatHub : Hub
    {
        DapperRepository repo = new DapperRepository();
        public override System.Threading.Tasks.Task OnConnected()
        {
            string userName = Context.User.Identity.Name;
            UserCounter.Users.Add(userName);
            Clients.Caller.getOnlineUsers(UserCounter.Users);
            Clients.All.userOnline(userName);
            return base.OnConnected();
        }
        public void SendMessage(object message)
        {
            Clients.Others.addMessage(message);
        }

        public void deleteFriend(string userName)
        {
            var invName = Context.User.Identity.Name;
            repo.deleteFriend(invName, userName);
            Clients.Caller.deleteFriendVisual(userName);
        }

        public void blacklistFriend(string userName)
        {
            var invName = Context.User.Identity.Name;
            repo.addBlacklist(invName, userName);
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            string userName = Context.User.Identity.Name;
            UserCounter.Users.Remove(userName);
            Clients.All.userOffline(userName);
            return base.OnDisconnected(stopCalled);
        }
    }
}