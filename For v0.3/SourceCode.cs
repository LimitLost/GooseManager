using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GooseShared;
using SamEngine;
using System.IO;
using System.Diagnostics;

namespace DefaultMod
{
    class TaskArrayElement
    {
        public string taskName;
        public bool isActive;
        public int taskId=0;

        public TaskArrayElement(string taskName_, bool isActive_,int taskId_)
        {
            taskName = taskName_;
            isActive = isActive_;
            taskId = taskId_;
        }
    }

   

    public class ModEntryPoint : IMod
    {
        void IMod.Init()
        {

            InjectionPoints.PreTickEvent += BeforeTick;
            
        }

        bool initialized=false;

        bool walkingActive = true;
        bool runningActive = true;
        bool chargingActive = true;

        List<TaskArrayElement> tasksData = new List<TaskArrayElement>();
        List<TaskArrayElement> activeTasksData= new List<TaskArrayElement>();
        
        //Get All tasks options from file, and save to file tasks that are not in file
        void InitializeTasks()
        {
            
            using (FileStream tasksFile = new FileStream(Path.Combine(API.Helper.getModDirectory(this),"GooseTasks.txt"), FileMode.OpenOrCreate))
            {


                string[] fileData;

                if (tasksFile.Length > 0)
                {
                    byte[] bytes = new byte[tasksFile.Length];
                    tasksFile.Read(bytes, 0, (int)tasksFile.Length);

                    fileData = Encoding.Default.GetString(bytes).Split('\n');

                    foreach (string s in fileData)
                    {
                        if (s.Contains('='))
                        {

                            string[] eq = s.Split(new[] { '=' }, StringSplitOptions.None);



                            if (Boolean.TryParse(eq[1].ToLower(), out bool state))
                            {
                                int taskId = API.TaskDatabase.getTaskIndexByID(eq[0]);
                                if (taskId != -1)
                                {
                                    tasksData.Add(new TaskArrayElement(eq[0], state, taskId));
                                }

                            }

                        }



                    }

                }

                string toAddToFile = "";

                foreach (string task in API.TaskDatabase.getAllLoadedTaskIDs())
                {
                    bool found = false;
                    foreach (TaskArrayElement e in tasksData)
                    {
                        if (e.taskName == task)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        toAddToFile += task + "=True\n";
                        tasksData.Add(new TaskArrayElement(task, true, API.TaskDatabase.getTaskIndexByID(task)));
                    }

                }

                if (!toAddToFile.Equals(""))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(toAddToFile);
                    tasksFile.Write(bytes, 0, bytes.Length);
                }
                bool anyTaskActive = false;
                foreach (TaskArrayElement task in tasksData)
                {
                    if (task.isActive)
                    {
                        anyTaskActive = true;
                        break;
                    }
                }
                if (!anyTaskActive)
                {
                    foreach (TaskArrayElement task in tasksData)
                    {
                        task.isActive = true;

                    }
                }

                foreach (TaskArrayElement task in tasksData)
                {
                    if (task.isActive)
                    {
                        activeTasksData.Add(task);
                    }
                }


            }
            
        }

        //Get All speed tiers from file, and save all speed tiers when file is empty
        void InitializeSpeed()
        {
           
            using (FileStream speedFile = new FileStream(Path.Combine(API.Helper.getModDirectory(this), "GooseSpeedTiers.txt"), FileMode.OpenOrCreate))
            {
                string[] fileData;
                if (speedFile.Length > 0)
                {
                    byte[] bytes = new byte[speedFile.Length];
                    speedFile.Read(bytes, 0, (int)speedFile.Length);

                    fileData = Encoding.Default.GetString(bytes).Split('\n');

                    foreach (string s in fileData)
                    {
                        if (s.Contains('='))
                        {
                            
                            string[] eq = s.Split(new[] { '=' }, StringSplitOptions.None);



                            if (Boolean.TryParse(eq[1].ToLower(), out bool state))
                            {


                                if (Enum.TryParse(eq[0], out GooseEntity.SpeedTiers tier))
                                {
                                    switch(tier)
                                    {
                                        case GooseEntity.SpeedTiers.Walk:
                                            walkingActive = state;
                                            break;
                                        case GooseEntity.SpeedTiers.Run:
                                            runningActive = state;
                                            break;
                                        case GooseEntity.SpeedTiers.Charge:
                                            chargingActive = state;
                                            break;
                                    }
                                    
                                }


                            }

                        }



                    }
                }
                else
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("Walk=True\nRun=True\nCharge=True");
                    speedFile.Write(bytes, 0, bytes.Length);
                }
                
                bool anySpeedActive = false;
                if(runningActive)
                {
                    anySpeedActive = true;
                }
                if (walkingActive)
                {
                    anySpeedActive = true;
                }
                if (chargingActive)
                {
                    anySpeedActive = true;
                }
                if (!anySpeedActive)
                {
                    runningActive = true;
                    walkingActive = true;
                    chargingActive = true;
                }

               

            }
            
        }

        

        Random rng = new Random();

        public void BeforeTick(GooseEntity g)
        {
            
            if(!initialized)
            {

                InitializeTasks();
                InitializeSpeed();

                    initialized = true;
            }

            //If current Task is disabled set task to new random task
            foreach(TaskArrayElement task in tasksData)
            {
                if(g.currentTask== task.taskId)
                {
                    if(!task.isActive)
                    {
                        string newTask = activeTasksData.ElementAt(rng.Next(activeTasksData.Count)).taskName;
                            API.Goose.setCurrentTaskByID(g, newTask);
                            break;
                    }
                    
                }
            }

            //If current Speed Tier is disabled change Speed Tier to new random Speed Tier
            if (g.currentSpeed==g.parameters.RunSpeed)
                {
                    if(!runningActive)
                    {
                    if(walkingActive&& chargingActive)
                    {
                        if(rng.Next(2)==1)
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Charge);
                        }
                        else
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Walk);
                        }
                    }
                    else if(walkingActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Walk);
                    }
                    else if (chargingActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Charge);
                    }

                }
                }
                else if (g.currentSpeed == g.parameters.ChargeSpeed)
                {
                    if (!chargingActive)
                    {
                    if (runningActive && walkingActive)
                    {
                        if (rng.Next(2) == 1)
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Walk);
                        }
                        else
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Run);
                        }
                    }
                    else if (runningActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Run);
                    }
                    else if (walkingActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Walk);
                    }
                    }
                }
                else if (g.currentSpeed == g.parameters.WalkSpeed)
                {
                    if (!walkingActive)
                    {
                    if (runningActive && chargingActive)
                    {
                        if (rng.Next(2) == 1)
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Charge);
                        }
                        else
                        {
                            API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Run);
                        }
                    }
                    else if (runningActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Run);
                    }
                    else if (chargingActive)
                    {
                        API.Goose.setSpeed(g, GooseEntity.SpeedTiers.Charge);
                    }
                }
                }
        }
    }
}
