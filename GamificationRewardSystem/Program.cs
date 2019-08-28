/*
 * Title: Gamification Reward System
 * Program Description: This program initially is to take a csv file that contains username and stamina to give to the user.
 * NOTE: mongo daemon has to be running for this to work correctly
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace GamificationRewardSystem
{
    class Program
    {
        private static MongoCollection<BsonDocument> PlayerCollection;
        private static IncrementalData data;
        
        //args[0] should be the csv file
        static void Main(string[] args)
        {
            //check if correct amount of arguments are given
            //*
            if(args.Length == 0 || args.Length > 1) //only one argument is used
            {
                Console.WriteLine("Please give a single file's pathname as command argument.\n");
                return;
            }//*/

            //check if given csv file exits
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File does not exist. Please give the CSV file's correct absolute path.\n");
                return;
            }

            MongoServer DBServer = new MongoClient("mongodb://localhost:443").GetServer();
            PlayerCollection = DBServer.GetDatabase("test").GetCollection<BsonDocument>("playersV2");

            //read file line by line
            List<string> lines = File.ReadAllLines(@args[0]).ToList<string>();

            //line format: username, stamina value, active time value (in hours)
            foreach(string line in lines)
            {
                string[] split = line.Split(',');
                //check if username exists in db
                BsonDocument player = PlayerCollection.FindOne(new QueryDocument("Username", split[0]));
                if (player != null)
                {
                    data = JsonConvert.DeserializeObject<IncrementalData>(player.GetValue("Incremental").AsString);

                    //check if null!
                    if (data == null)
                    {
                        //make new instance
                        data = new IncrementalData();
                        //reset reward fields to 0

                        data.stamina.cur = 0.0;
                        data.timeleft.cur = 0.0;
                    }

                    //do something with data such as restoring time or stamina
                    double output;
                    if(double.TryParse(split[1], out output))
                    {
                        if (output + data.stamina.cur >= data.stamina.max)
                            data.stamina.cur = data.stamina.max;
                        else
                            data.stamina.cur += output;
                        
                    }

                    double active = 0;
                    if(double.TryParse(split[2], out active))
                    {
                        data.timeleft.cur += active;
                    }
                    //convert data back to json string
                    string json = JsonConvert.SerializeObject(data);
                    player.Set("Incremental", json);

                    //save changes
                    PlayerCollection.Save(player);
                }
                else
                {
                    //do something since username not found
                }
            }
        }

        private static string getJsonStr(game g)
        {
            if (g == game.incremental)
                return JsonConvert.SerializeObject(data);
            return "";
        }
    }
}
