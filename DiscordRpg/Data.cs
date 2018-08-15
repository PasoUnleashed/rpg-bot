using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Discord;
using Discord.WebSocket;

namespace DiscordRpg
{
    public class Sqlite3DB
    {
        SQLiteConnection db;
        DiscordSocketClient client;
        //Setup 
        public Sqlite3DB(string db,DiscordSocketClient client)
        {
            this.db = new SQLiteConnection($"Data Source={db};Version=3;");
            this.db.Open();
            this.client = client;
            SetupDB();
        }
        void SetupDB()
        {
            string[] commands = new string[]
            {
                "CREATE TABLE if not exists players (id INTEGER PRIMARY KEY, health INTEGER, location INTEGER, main_guild INTEGER,state TEXT  )",
                "CREATE TABLE if not exists guilds (id INTEGER PRIMARY KEY, state TEXT)",
                "CREATE TABLE if not exists rooms (id INTEGER PRIMARY KEY, guild_id INTEGER,state TEXT)",
                "CREATE TABLE if not exists item_instances (id INTEGER PRIMARY KEY,item_id INTEGER, state TEXT, owner_id INTEGER)",
                "CREATE TABLE if not exists effect_instances (id INTEGER PRIMARY KEY, item_id INTEGER, effect_index INTEGER,state TEXT, date_applied TEXT)",
                "CREATE TABLE if not exists npc_instances (id INTEGER PRIMARY KEY, npc_id INTEGER, location INTEGER, health INTEGER, state INTEGER)"
            };
            Serialize(commands);
        }

        //Store Functions
        void StorePlayer(Player p)
        {
            var sql = "INSERT OR REPLACE INTO players(id,health,location,main_guild,state) VALUES({0}, {1}, {2},{3},{5})" ;
            Serialize(sql);
        }
        void StoreGuild(RpgGuild g)
        {
            var sql = "INSERT OR REPLACE INTO guilds(id,state) VALUES({0},{1});";
            Serialize(sql);
        } 
        void StoreRoom(ItemInstance i)
        {
            var sql = "INSERT OR REPLACE INTO rooms(id,guild_id,state) VALUES({0},{1},{2});";
            Serialize(sql);
        }
        void StoreItemInstance(ItemInstance i)
        {
            var sql = "INSERT OR REPLACE INTO item_instances(id,state,item_id,owner_id) VALUES({0},{1},{2},{3})";
            Serialize(sql);
        }
        void StoreEffectInstance(EffectInstance e)
        {
            var sql = "INSERT OR REPLACE INTO effect_instanced(id,effect_id,effect_index,state,owner_id,affected_id,date_applied) VALUES({0}, {1}, {2},{3},{4});";
            Serialize(sql);
        }
        void StoreNPCInstance(NPCInstance n)
        {
            var sql = "INSERT OR REPLACE INTO npc_instanced(id,npc_id, location,health,state) VALUES({0}, {1}, {2},{3},{4});";
            Serialize(sql);
        }
        public void StoreModel(Model m)
        {
            throw new NotImplementedException();
        }
        // Load Functions
        Player LoadPlayer(Model m, IUser user, IGuild currentguild)
        {
            return LoadPlayer(m, user, currentguild, null);
        }
        Player LoadPlayer(Model m, IUser user, IGuild currentguild, RpgGuild inCon)
        {
            if (user.Id == m.Client.CurrentUser.Id)
            {
                return null;
            }
            var sql = "SELECT * from players where id = " + user.Id;
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            Player p = new Player(m, currentguild.Id, 100, user);

            if (!reader.Read())
            {
                inCon.Players.Add(p);
                p.Mainguildid = inCon.Guildid;
                return p;
            }
            p.Health = (int)reader["health"];
            p.Mainguildid = (ulong)reader["main_guild"];
            p.State = (string)reader["state"];
            LoadEffectInstances(p);
            LoadItemInstances(p);
            if (m.Guilds.TryGetValue(p.Mainguildid, out RpgGuild g))
            {
                if (!g.Players.Exists((player) => player.User.Id == p.User.Id))
                {
                    g.Players.Add(p);
                }
            }
            else
            {
                Console.WriteLine("not found");
            }
            return p;
        }
        Room LoadRoom(Model m,RpgGuild g,ITextChannel c)
        {
            var sql = "SELECT * from rooms where id = " + c.Id;
            Room r = new Room(m,g, c);
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                r.State = (string)reader["state"];
            }
            LoadNPCInstances(r);
            return r;
        }

        public RpgGuild LoadGuild(Model m, ulong guildid)
        {
            var sql = "SELECT * from guilds where id = " + guildid;
            IGuild guild = client.GetGuild(guildid);
            RpgGuild g = new RpgGuild(m, guild);
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            foreach (var i in guild.GetUsersAsync().GetAwaiter().GetResult())
            {
                if (m.Players.TryGetValue(i.Id, out Player p))
                {
                    p.Mainguildid = g.Guildid;
                    g.Players.Add(p);
                }
                else
                {
                    var play = LoadPlayer(m, i, guild,g);
                    if (play!=null)
                    {
                        m.Players.TryAdd(i.Id, play);
                    }
                }
            }
            foreach(var i in guild.GetChannelsAsync().GetAwaiter().GetResult())
            {
                if(i is ITextChannel x)
                {
                    Room rm = LoadRoom(m,g, x);
                    g.Rooms.Add(rm);
                    m.Rooms.TryAdd(rm.Channelid, rm);
                }
            }
            if (reader.Read())
            {
                g.State = (string)reader["state"];
            }
            return g;
        }
        
        void LoadItemInstances(Player p)
        {
            var sql = "SELECT * FROM item_instances WHERE owner_id = "+p.Userid;
            var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            

        }
        void LoadEffectInstances(Player p)
        {
            var sql = "SELECT * FROM effect_instances WHERE affected_id = "+p.Userid;
        }
        void LoadNPCInstances(Room r)
        {
            var sql = "SELECT * FROM npc_instances where location = "+r.Channelid;
        }
        public Model LoadModel(DiscordSocketClient client)
        {
            Model m = new Model(client,this);
            foreach(var i in client.Guilds)
            {
                m.Guilds.TryAdd(i.Id,LoadGuild(m,i.Id));
            }
            return m;
            
        }
        //Delete
        public void DeletePlayer(ulong id)
        {
            var sql = "DELETE FROM players WHERE id="+id;
            var sql1 = "DELETE FROM effect_instances WHERE affected_id=" + id+" or owner_id = "+id;
            var sql2 = "DELETE FROM item_instances WHERE owner_id = " + id;
            Serialize(sql, sql1, sql2);
        }
        void Serialize(params string[] x)
        {
            foreach(var i in x)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = i;
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteRoom(ulong id)
        {
            var sql = "DELETE FROM players WHERE id=" + id;
            Serialize(sql);
        }
        public void DeleteGuild(ulong id)
        {
            var sql = "DELETE FROM guilds WHERE id=" + id;
            var sql1 = "DELETE FROM rooms WHERE guild_id=" + id;
            Serialize(sql, sql1);
        }
        public void DeleteEffect(ulong id)
        {
            var sql = "DELETE FROM players WHERE id=" + id;
            Serialize(sql);
        }
        public void DeleteItem(ulong id)
        {
            var sql = "DELETE FROM players WHERE id=" + id;
            Serialize(sql);
        }
        public void DeleteNpc(ulong id)
        {
            var sql = "DELETE FROM players WHERE id=" + id;
            Serialize(sql);
        }
        
    }
}
