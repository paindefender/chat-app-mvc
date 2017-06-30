using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatApp.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public char Gender { get; set; }
        public int CityId { get; set; }
        public string PhotoPath { get; set; }
    }
    public class Message
    {
        public string fromName { get; set; }
        public DateTime Date { get; set; }
        public string fromId { get; set; }
        public string UserTarget { get; set; }
        public int? ConfTarget { get; set; }
        public string UserTargetId { get; set; }
        public string Body { get; set; }

    }
    public class City
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string CityNameAccent { get; set; }
        public string Country { get; set; }
    }
    public class Friendship
    {
        public int UserId { get; set; }
        public int FriendId { get; set; }
    }

    public class Interest
    {
        public int UserId { get; set; }
        public string InterestName { get; set; }
    }
    public class MsgToConf
    {
        public int MessageId { get; set; }
        public int ConferenceId { get; set; }
    }
    public class ConfToUsers
    {
        public int ConfId { get; set; }
        public int UserId { get; set; }
    }
    public class Conference
    {
        public int ConfId { get; set; }
        public string ConfName { get; set; }
    }
    public class SearchSelectViewModel
    {
        public string UserName { get; set; }
        public bool UseName { get; set; }
        public char? Gender { get; set; }
        public bool UseGender { get; set; }
        public string CityName { get; set; }
        public bool UseCity { get; set; }
        public string Interests { get; set; }
        public bool UseInterests { get; set; }
        public override string ToString(){
            string s = "";
            s = "{" + UseName + ": " + UserName + ", " + UseGender + ": " + Gender + ", " + UseCity + ": " + CityName + ", " + UseInterests + ": " + Interests + "}";
            return s;
        }
    }

    public class UserResultViewModel
    {
        public string UserName { get; set; }
        public char Gender { get; set; }
        public string Interest { get; set; }
        public string CityName { get; set; }
        public string PhotoPath { get; set; }

        //For hashset:
        public override int GetHashCode() 
        {
            return UserName.GetHashCode();
        }
        public override bool Equals(Object obj) 
        {
            return Equals(obj as UserResultViewModel);
        }

        public bool Equals(UserResultViewModel obj)
        {
            return obj != null && obj.UserName == UserName;
        }
    }
}