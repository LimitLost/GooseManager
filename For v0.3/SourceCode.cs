using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GooseShared;
using SamEngine;
using System.IO;


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

        List<TaskArrayElement> tasksData = new List<TaskArrayElement>();
        List<TaskArrayElement> activeTasksData= new List<TaskArrayElement>();

        Random rng = new Random();

        public void BeforeTick(GooseEntity g)
        {
            //Get All tasks options from file, and save to file tasks that are not in file
            if(!initialized)
            {

                using (FileStream tasksFile = new FileStream("GooseTasks.txt",FileMode.OpenOrCreate))
                {
                    
                   
                    string[] fileData;
                    
                    if (tasksFile.Length>0)
                    {
                        byte[] bytes = new byte[tasksFile.Length+1];
                        tasksFile.Read(bytes, 0, (int)tasksFile.Length+1);
                        
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
                        foreach(TaskArrayElement e in tasksData)
                        {
                            if(e.taskName== task)
                            {
                                found = true;
                                break;
                            }
                        }
                        if(!found)
                        {
                            toAddToFile += task + "=True\n";
                            tasksData.Add(new TaskArrayElement(task,true, API.TaskDatabase.getTaskIndexByID(task)));
                        }
                        
                    }

                    if(!toAddToFile.Equals(""))
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes(toAddToFile);
                        tasksFile.Write(bytes, 0, bytes.Length);
                    }
                    bool anyTaskActive = false;
                    foreach (TaskArrayElement task in tasksData)
                    {
                        if(task.isActive)
                        {
                            anyTaskActive = true;
                            break;
                        }
                    }
                    if(!anyTaskActive)
                    {
                        foreach (TaskArrayElement task in tasksData)
                        {
                            task.isActive = true;
                        }
                    }

                    foreach (TaskArrayElement task in tasksData)
                    {
                        if(task.isActive)
                        {
                            activeTasksData.Add(task);
                        }
                    }
                       

                    }
                initialized = true;
            }

            //If currentTask is disabled set task to new random task
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
            
            
        }
    }
}
