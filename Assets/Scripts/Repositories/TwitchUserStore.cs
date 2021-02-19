using System;
using System.Collections.Generic;
using System.Linq;

public class TwitchUserStore
{
    private readonly List<ITwitchUser> Items = new List<ITwitchUser>();
    private readonly object Mutex = new object();
    public ITwitchUser Get(string username)
    {
        lock (Mutex)
        {
            var user = this.Items.FirstOrDefault(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                user = new TwitchUser(username, null, 1000);
                this.Store(user);
            }
            return user;
        }
    }

    public ITwitchUser Store(ITwitchUser item)
    {
        lock (Mutex)
        {
            var index = this.Items.FindIndex(x => x.Equals(item) || x.GetHashCode() == item.GetHashCode() && item.GetType() == x.GetType());
            if (index != -1)
            {
                this.Items[index] = item;
            }
            else
            {
                this.Items.Add(item);
            }
        }

        return item;
    }
}
