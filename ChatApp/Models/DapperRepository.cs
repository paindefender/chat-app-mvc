using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNet.Identity;
using System.Diagnostics;

namespace ChatApp.Models
{
    public class DapperRepository
    {
        public DapperRepository() { }

        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public ApplicationUser GetCurrentUser()
        {
            ApplicationUser user = null;
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var UId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                user = db.Query<ApplicationUser>("select * from AspNetUsers where Id = @Id", new {Id = UId}).FirstOrDefault();
                return user;
            }
        }

        public void SetInterests(ApplicationUser user, ICollection<string> interests)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string deleteSql = "delete from Interests where UserId = @UserId"; //First delete all the previous interests
                string insertSql = "insert into Interests values(@UserId, @Interest)"; //Insert new interests
                db.Execute(deleteSql, new {UserId = user.Id});
                foreach (var interest in interests)
	            {
                    db.Execute(insertSql, new { UserId = user.Id, Interest = interest.ToLower() });
	            }
            }
        }

        public string GetInterests(string UserId)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string sql = "select Interest from Interests where UserId = @UserId";
                var interests = db.Query<string>(sql, new { UserId = UserId });
                return String.Join(",", interests);
            }
        }

        public void FinishSetup(ApplicationUser user, string ImgPath, char? gender, int? cityId)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string insertSql = "insert into UserInfo(UserId, Gender, CityId, PhotoPath) values (@UserId, @Gender, @CityId, @PhotoPath)";
                string finishedSetupSql = "update AspNetUsers set SetupCompleted = 1 where Id = @Id";
                db.Execute(insertSql, new { UserId = user.Id, Gender = gender, CityId = cityId, PhotoPath = ImgPath });
                db.Execute(finishedSetupSql, new { Id = user.Id });
            }
        }

        public IEnumerable<City> getCities()
        {
            IEnumerable<City> cities = new List<City>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                cities = db.Query<City>("select * from Cities");
                return cities;
            }
        }

        public IEnumerable<UserResultViewModel> getFriendsSearchResults(SearchSelectViewModel info)
        {
            IEnumerable<UserResultViewModel> users = new List<UserResultViewModel>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string allSql = "select * from AspNetUsers a inner join UserInfo u on a.Id=u.UserId left join Cities c on u.CityId = c.CityId left join Interests i on i.UserId = u.UserId";
                Debug.WriteLine(info);
                #region Building Sql
                bool where = true;
                string sql = "";
                var args = new DynamicParameters();
                if (info.UseGender && info.Gender!=null)
                {
                    if (where){
                        sql += " where";
                        where = false;
                    }
                    else
                        sql += " and";
                    sql += " Gender=@Gender";
                }
                if (info.UseCity && info.CityName!=null)
                {
                    info.CityName = info.CityName.ToLower();
                    if (where){
                        sql += " where";
                        where = false;
                    }
                    else
                        sql += " and";
                    sql += " CityName like CONCAT(@CityName, '%')";
                }
                if (info.UseName && info.UserName!=null)
                {
                    info.UserName = info.UserName.ToLower();
                    if (where){
                        sql += " where";
                        where = false;
                    }
                    else
                        sql += " and";
                    sql += " LOWER(UserName) like CONCAT('%', @UserName, '%')";
                }
                if (info.UseInterests && info.Interests!=null)
                {
                    if (where){
                        sql += " where";
                        where = false;
                    }
                    else
                        sql += " and";
                    sql += "";
                    string[] interests = info.Interests.Split(',');
                    int i = 0;
                    
                    foreach (var x in interests)
                    {
                        if (i > 0)
                            sql += " OR";
                        sql += " Interest like @p" + i;
                        args.Add("p" + i, x + "%");
                        i++;
                    }
                }
                #endregion

                Debug.WriteLine(sql);

                if (info == null)
                {
                    return users;
                }
                //users = db.Query<SearchResultViewModel>(allSql);
                args.AddDynamicParams(new { Gender = info.Gender, CityName = info.CityName, UserName = info.UserName});
                users = db.Query<UserResultViewModel>(allSql + sql, args);
                List<UserResultViewModel> good = new List<UserResultViewModel>();
                HashSet<string> dupCheckSet = new HashSet<string>();
                foreach (var entry in users) //Filtering list, checking for dupes
                {
                    if (!dupCheckSet.Contains(entry.UserName))
                    {
                        dupCheckSet.Add(entry.UserName);
                        good.Add(entry);
                    }
                }
                return good;//Filtered list
            }
        }

        public string getIdByUsername(string username){
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string Id = db.Query<string>("select Id from AspNetUsers where UserName = @UserName", new { UserName = username}).SingleOrDefault();
                return Id;
            }
        }

        public void addBlacklist(string userName, string friendName)
        {
            string userId = getIdByUsername(userName);
            string friendId = getIdByUsername(friendName);
            using(IDbConnection db = new SqlConnection(connectionString))
            {
                string insertSql = "insert into Blacklist(UserId, BlockedUserId) values(@UserId, @BlockId)";
		        db.Execute(insertSql, new{UserId = userId, BlockId = friendId});
            }
        }
        public void removeBlacklist(string UserId, string friendName)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string sql = "delete from Blacklist where UserId = @UserId and BlockedUserId = @BlockedUserId";
                db.Execute(sql, new { UserId = UserId, BlockedUserId = getIdByUsername(friendName) });
            }
        }

        public void addFriend(ApplicationUser user, string friendName)
        {
            string friendId = getIdByUsername(friendName);
            using (IDbConnection db = new SqlConnection(connectionString)){
                string insertSql = "insert into Friends(UserId, FriendId) values(@UserId, @FriendId)";
                db.Execute(insertSql, new { UserId = user.Id, FriendId = friendId });
            }
        }

        public void deleteFriend(string userName, string friendName)
        {
            string userId = getIdByUsername(userName);
            string friendId = getIdByUsername(friendName);
            using(IDbConnection db = new SqlConnection(connectionString))
	        {
                string sql = "delete from Friends where (UserId = @UserId and FriendId = @FriendId) or (UserId = @FriendId and FriendId = @UserId)";
                db.Execute(sql, new{FriendId = friendId, UserId = userId});
	        }
        }

        public IEnumerable<UserResultViewModel> getFriendsById(string UserId){
            using(IDbConnection db = new SqlConnection(connectionString))
	        {
                string sql = @"select Id, UserName, Gender, CityName, PhotoPath  from Friends f join AspNetUsers a on a.Id = f.FriendId join UserInfo u on u.UserId = a.Id join Cities c on c.CityId = u.CityId where f.UserId = @UserId 
union 
select Id, UserName, Gender, CityName, PhotoPath  from Friends f join AspNetUsers a on a.Id = f.UserId join UserInfo u on u.UserId = a.Id join Cities c on c.CityId = u.CityId where f.FriendId = @UserId";
                return db.Query<UserResultViewModel>(sql, new { UserId = UserId});
	        }
        }

        public IEnumerable<string> getBlockedUsernames(string UserId)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                string sql = @"select UserName from Blacklist b join AspNetUsers a on a.Id = b.BlockedUserId where b.UserId = @UserId";
                return db.Query<string>(sql, new { UserId = UserId });
            }
        }

        public void addMsg(Message msg, ApplicationUser user)
        {
            DateTime now = DateTime.Now;
            string sqlFormattedNow = now.ToString("yyyyMMdd HH:mm:ss.fff");
            Debug.WriteLine(sqlFormattedNow);
            msg.Body = HttpUtility.HtmlEncode(msg.Body);
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"insert into Messages(UserId, Body, Date) values(@UserId, @Body, @Date);
select CAST(SCOPE_IDENTITY() as int)";
                var id = db.Query<int>(sql, new { UserId = user.Id, Body = msg.Body, Date = sqlFormattedNow }).Single();

                if (msg.UserTarget != null && msg.ConfTarget == null) //Message to a single user
                {
                    sql = "insert into MsgUser(MsgId, UserId) values(@MsgId, @UserId)";
                    db.Execute(sql, new { MsgId = id, UserId = msg.UserTargetId });
                }
                else if (msg.UserTarget == null && msg.ConfTarget != null) //Message to a conference
                {
                    sql = "insert into MsgConf(MsgId, ConfId) values(@MsgId, @ConfId)";
                    db.Execute(sql, new { MsgId = id, ConfId = msg.ConfTarget });
                }
            }
        }

        public IEnumerable<Message> getUserMsg(ApplicationUser user, string toUserId)
        {
     
            IEnumerable<Message> msgList = new List<Message>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "select m.Date as Date, m.UserId as FromId, mu.UserId as UserTargetId, m.Body as Body from Messages m join MsgUser mu on m.MsgId = mu.MsgId where (m.UserId=@Id1 and mu.UserId=@Id2) or (m.UserId=@Id2 and mu.UserId=@Id1)";
                msgList = db.Query<Message>(sql, new { @Id1 = user.Id, @Id2 = toUserId });
            }
            return msgList;
        }

        #region Conference stuff
        public IEnumerable<Message> getConfMsg(int confId)
        {
            IEnumerable<Message> msgList = new List<Message>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"select anu.UserName as fromName, m.Date as Date, m.UserId as fromId, m.Body as Body from MsgConf mc join Messages m on mc.MsgId = m.MsgId join AspNetUsers anu on m.UserId = anu.Id where mc.ConfId = @ConfId";
                var result = db.Query<Message>(sql, new { ConfId = confId });
                return result;
            }
        }
        public int insertNewConf(string confname, ApplicationUser user)
        {
            int id = 0;
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"insert into Confs(Name) values(@Name);
select CAST(SCOPE_IDENTITY() as int)";
                id = db.Query<int>(sql, new { Name = confname }).Single();
                sql = "insert into ConfUsers(ConfId, UserId) values(@ConfId, @UserId)";
                db.Execute(sql, new { ConfId = id, UserId = user.Id });
            }
            return id;
        }

        public IEnumerable<Conference> getConfsById(string id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"select c.ConfId as ConfId, c.Name as ConfName from ConfUsers u join Confs c on u.ConfId = c.ConfId where u.UserId = @UserId";
                var conferences = db.Query<Conference>(sql, new { UserId = id });
                return conferences;
            }
        }

        public string getConfName(int id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "select Name from Confs where ConfId = @Id";
                var name = db.Query<string>(sql, new { Id = id }).Single();
                return name;
            }
        }

        public IEnumerable<UserResultViewModel> getUsersByConferenceId(int confId)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"select Id, UserName, Gender, CityName, PhotoPath from ConfUsers c join AspNetUsers a on c.UserId = a.Id join UserInfo i on c.UserId = i.UserId join Cities ci on ci.CityId = i.CityId where c.ConfId = @ConfId";
                var result = db.Query<UserResultViewModel>(sql, new {ConfId = confId});
                return result;
            }
        }

        public void addUserToConf(string UserId, int ConfId)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = @"insert into ConfUsers(ConfId, UserId) values(@ConfId, @UserId);";
                db.Execute(sql, new { ConfId = ConfId, UserId = UserId});
            }
        }
        #endregion

        public string getEmailById(string Id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "select Email from AspNetUsers where Id = @Id";
                var email = db.Query<string>(sql, new { Id = Id }).Single();
                return email;
            }
        }

        public string getProfilePicture(string Id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "select PhotoPath from UserInfo where UserId = @UserId";
                return db.Query<string>(sql, new { UserId = Id }).Single();
            }
        }

        public void setProfilePicture(string Id, string Path)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "update UserInfo set PhotoPath = @PhotoPath where UserId = @UserId";
                db.Execute(sql, new { PhotoPath = Path, UserId = Id });
            }
        }

        public FinishSetupViewModel getProfileInfo(string Id)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var sql = "select Gender, CityId from UserInfo where UserId = @UserId";
                var info = db.Query<FinishSetupViewModel>(sql, new { UserId = Id }).Single();
                info.Interests = GetInterests(Id);
                return info;
            }
        }

        public void setProfileInfo(FinishSetupViewModel a) { 
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                var Id = System.Web.HttpContext.Current.User.Identity.GetUserId();
                string insertSql = "update UserInfo set Gender = @Gender, CityId = @CityId where UserId = @UserId";
                db.Execute(insertSql, new { UserId = Id, Gender = a.Gender, CityId = a.CityId});
            }
        }
    }
}