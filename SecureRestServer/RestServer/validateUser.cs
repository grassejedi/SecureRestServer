using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestServer
{
    public class validateUser
    {
        private const int TIMEALLOWED = 25;

        public static bool validUser(string custToken)
        {
            using (var db = new Models.DatabaseEntities())
            {
                //var validSession = (from a in db.ActiveSessions
                //                 where custToken == a.token
                //                 select a.token).FirstOrDefault();

                // This is doing basically the same type of thing as above but instead of grabbing just the token, we are selecting the actual entry in the ActiveSession table
                Models.ActiveSession session = db.ActiveSessions.FirstOrDefault(s => s.token == custToken);

                if (session != null)
                {
                    DateTime start = session.InvalidDateTime;
                    DateTime present = DateTime.Now;

                    if (present > start.AddMinutes(TIMEALLOWED))
                    {
                        db.ActiveSessions.Attach(session);
                        db.ActiveSessions.Remove(session);
                        db.SaveChanges();

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }



        public static bool isAdmin(string custToken)
        {

            using (var db = new Models.DatabaseEntities())
            {
                var validAdmin = (from a in db.ActiveSessions
                                  where a.token == custToken
                                  select new
                                  {
                                      a.token,
                                      isAdmin = (from u in db.logins where a.login == u.Id select u.user.isAdmin)
                                  }).ToList();


                if (validAdmin[0].isAdmin.AsEnumerable().FirstOrDefault())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

    }
}