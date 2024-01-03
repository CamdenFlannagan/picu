using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace picu
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ToDo todo = new ToDo();
            PicuParser parser = new PicuParser(todo);
            string path = @"C:\\Users\\camde\\source\\repos\\picu\\ToDo\\List.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path)) { sw.WriteLine(""); }
            }
            using (StreamReader sr = File.OpenText(path))
            {
                string fileLine;
                while ((fileLine = sr.ReadLine()) != null)
                {
                    todo.AddAssignment(parser.ParseFromFile(fileLine));
                }
            }
            
            Console.WriteLine("Welcome to Picu! Type \"help\" for more information.");
            
            bool exit = false;
            while (!exit)
            {
                Console.Write(">>> ");
                string userInput = Console.ReadLine();
                File.WriteAllText(path, todo.ToDoToString());
                exit = parser.Parse(userInput);
            }
        }
    }

    /// <summary>
    /// The Picu Parser parses console input and input from the todo list file
    /// </summary>
    public class PicuParser
    {
        public PicuParser(ToDo toDoList)
        {
            ToDoList = toDoList;
        }

        /// <summary>
        /// Parser input from the console
        /// </summary>
        /// <param name="userInput">The user's input to the console</param>
        /// <returns>Whether or not the application should close</returns>
        public bool Parse(string userInput)
        {
            userInput = userInput.ToLower();
            switch (userInput)
            {
                case "help":
                    Console.WriteLine(
                        "Commands:\n" +
                        "\tnew - adds a new assignment. Format assignment as {subject}, {name}, {due date}.\n" +
                        "\t\tIf using NextThurs time, put a '#' before the date.\n" +
                        "\tcomplete - removes an assignment. Format as {name}, {subject}.\n" +
                        "\tcheck - prints out the todo list.\n" +
                        "\tclear - clears the console\n" +
                        "\tdate - prints out today's date\n" +
                        "\texit - exit the application.\n\n" +
                        "What's NextThurs? It's a way of formatting a date relative to the current week. It's a formal\n" +
                        "way of expressing ideas like \"Next Thursday\" or \"The Monday two weeks from now.\"\n" +
                        "Here's how it works:\n" +
                        "\"1 Thurs\" means \"The Thursday of the week after the current week\"\n" +
                        "\"0 Mon\" means \"The Monday of the current week\"\n" +
                        "\"5 Tues\" means \"The Tuesday of the week 5 weeks after the current week\"\n" +
                        "\"-3 Fri\" means \"The Friday of the week 3 weeks before the current week\"\n");
                    break;
                case "new":
                    Console.Write("Please type assignment info\n>>> ");
                    string newInput = Console.ReadLine();
                    NewAssignment(newInput);
                    Console.WriteLine("Assignment added succesfully!");
                    break;
                case "complete":
                    Console.Write("Which assignment is completed?\n>>> ");
                    newInput = Console.ReadLine();
                    CompleteAssignment(newInput);
                    break;
                case "check":
                    CheckToDo();
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "date":
                    Console.WriteLine("Today is {0}, {1} {2}",
                        DateTime.Now.DayOfWeek,
                        DateTime.Now.ToString("MMMM"),
                        DateTime.Now.Day);
                    break;
                case "exit":
                    return true;
                default:
                    Console.Write("\"{0}\" is not a valid command. Type \"help\" for a list of valid commands\n", userInput);
                    break;
            }
            return false;
        }

        /// <summary>
        /// parse todo list as formatted in the file
        /// </summary>
        /// <param name="line">One line from the saved todo list file</param>
        /// <returns>An assignment object made from the line of data</returns>
        public Assignment ParseFromFile(string line)
        {
            string[] split1 = line.Split(':');
            string subject = split1[0].Trim();
            string[] split2 = split1[1].Split(',');
            string name = split2[0].Trim();
            DateTime dueDate = DateTime.ParseExact(split2[1].Trim(), "mm/dd/yyyy", CultureInfo.InvariantCulture);
            return new Assignment(dueDate, name, subject);
        }

        /// <summary>
        /// removes an assignment from the todo list using the user's input
        /// </summary>
        /// <param name="userInput">Should be formatted as {name}, {subject}</param>
        private void CompleteAssignment(string userInput)
        {
            string[] split = userInput.Split(',');
            string name = split[0].Trim();
            string subject = split[1].Trim();
            ToDoList.CompleteAssignment(name, subject);
        }

        /// <summary>
        /// Adds an assignment to the todo list using the user's input
        /// </summary>
        /// <param name="userInput">Should be formatted as {subject}, {name}, {due date}. Use a '#' in front of the due date if expressing NextThurs time</param>
        private void NewAssignment(string userInput)
        {
            string[] split = userInput.Split(',');
            if (split.Length != 3)
            {
                Console.WriteLine("Please format the assignment as {subject}, {name}, {due date}\n");
                return;
            }
            string subject = split[0].Trim();
            string name = split[1].Trim();
            string dueDateTemp = split[2].Trim();
            DateTime dueDate;
            if (dueDateTemp[0] == '#')
            {
                dueDateTemp = dueDateTemp.Remove(0, 1);
                dueDate = NextThurs.NextThursToDT(new NextThurs(dueDateTemp));
            }
            else
            {
                try
                {
                    dueDate = DateTime.Parse(dueDateTemp);
                }
                catch
                {
                    Console.WriteLine("Not a valid date format!");
                    return;
                }
            }
            ToDoList.AddAssignment(new Assignment(dueDate, name, subject));
        }

        /// <summary>
        /// print out the todo list to the console
        /// </summary>
        private void CheckToDo()
        {
            if (ToDoList.CountAssignments() == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Congratulations! You have no assignments!");
                Console.ResetColor();
                return;
            }
            string s = "s";
            if (ToDoList.CountAssignments() == 1)
                s = "";
            Console.Write("You have {0} assignment{1}:\n", ToDoList.CountAssignments(), s);
            ToDoList.PrintToDo();
        }

        public ToDo ToDoList;
    }

    /// <summary>
    /// The Next Thursday format of representing a day expresses time in relation to today
    /// rather than to the calender. The week is assumed to begin on Monday
    /// 
    /// Examples:
    ///  0 Thurs == The Thursday of the current week
    ///  3 Mon == The Monday of the third week after the current week
    /// -1 Sat == the Saturday of the week before the current week
    /// </summary>
    public class NextThurs
    {
        public NextThurs(string nextThurs)
        {
            string[] split = nextThurs.Split(' ');
            int.TryParse(split[0], out WeekOffset);
            DOWOffset = (DayOfWeek)TimeStuff.StringToDOWInt(split[1]);
        }

        public NextThurs(int weekOffset, DayOfWeek dowOffset)
        {
            WeekOffset = weekOffset;
            DOWOffset = dowOffset;
        }

        /// <summary>
        /// Converts the day in Next Thusday format to the day in Calender format
        /// </summary>
        /// <param name="nextThurs">The day in Next Thursday format</param>
        /// <returns>The day in Calender format</returns>
        public static DateTime NextThursToDT(NextThurs nextThurs)
        {
            DateTime today = DateTime.Now;
            DateTime then = today.AddDays(-TimeStuff.DOWToInt(today.DayOfWeek));
            then = then.AddDays(7*nextThurs.WeekOffset + TimeStuff.DOWToInt(nextThurs.DOWOffset));
            return then;
        }

        public static NextThurs DTToNextThurs(DateTime dt)
        {
            DateTime today = DateTime.Now;
            DayOfWeek dow = dt.DayOfWeek;
            today = today.AddDays(-TimeStuff.DOWToInt(today.DayOfWeek));
            dt = dt.AddDays(-TimeStuff.DOWToInt(dt.DayOfWeek));
            int weekOffset = (dt - today).Days / 7;
            return new NextThurs(weekOffset, dow);
        }
        public int WeekOffset;
        public DayOfWeek DOWOffset;

        public override string ToString() => $"{WeekOffset} {DOWOffset} ";
    }

    public struct Assignment
    {
        public Assignment(NextThurs dueDate, string name, string subject)
        {
            DueDate = NextThurs.NextThursToDT(dueDate);
            Name = name;
            Subject = subject;
        }

        public Assignment(DateTime dueDate, string name, string subject)
        {
            DueDate = dueDate;
            Name = name;
            Subject = subject;
        }

        public DateTime DueDate;
        public string Name;
        public string Subject;

        public override string ToString() => $"{Subject}: {Name}, {NextThurs.DTToNextThurs(DueDate)} ";
    }

    public class ToDo
    {
        public ToDo()
        {
            Assignments = new List<Assignment>();
        }

        public int CountAssignments()
        {
            return Assignments.Count;
        }

        /// <summary>
        /// Adds an assignment to the todo list while keeping the list sorted
        /// </summary>
        /// <param name="assignment">The assignment to add to the todo list</param>
        public void AddAssignment(Assignment assignment)
        {
            if (Assignments.Count == 0)
            {
                Assignments.Add(assignment);
                return;
            }
            int i = 0;
            foreach (var a in Assignments)
            {
                if (DateTime.Compare(assignment.DueDate, a.DueDate) <= 0)
                {
                    Assignments.Insert(i, assignment);
                    return;
                }
                i++;
            }
            Assignments.Add(assignment);
        }

        public void CompleteAssignment(string name, string subject)
        {
            foreach (var assignment in Assignments)
            {
                if (assignment.Name == name && assignment.Subject == subject)
                {
                    Assignments.Remove(assignment);
                    Console.WriteLine("Assignment \"{0}\" in subject \"{1}\" is now complete! Good job!", name, subject);
                    return;
                }
            }
            Console.WriteLine("There is no assignment \"{0}\" in subject \"{1}\"\n", name, subject);
        }

        public void PrintToDo()
        {
            foreach (var assignment in Assignments)
            {
                if ((assignment.DueDate - DateTime.Now).Days < 1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(assignment);
                    Console.ResetColor();
                }
                else
                {    
                    Console.WriteLine("\t{0}", assignment);
                }
            }
        }

        public string ToDoToString()
        {
            string todoList = "";
            foreach (var assignment in Assignments)
            {
                todoList += $"{assignment.Subject}: {assignment.Name}, {assignment.DueDate.ToString("mm/dd/yyyy")}\n";
            }
            return todoList;
        }

        public List<Assignment> Assignments;
    }

    /// <summary>
    /// TimeStuff holds several useful methods for operating on time 
    /// </summary>
    public class TimeStuff
    {

        /// <summary>
        /// Converts the time in Next Thursday format to the time in Calender format
        /// </summary>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static int DOWToInt(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Monday: return 1;
                case DayOfWeek.Tuesday: return 2;
                case DayOfWeek.Wednesday: return 3;
                case DayOfWeek.Thursday: return 4;  
                case DayOfWeek.Friday: return 5;    
                case DayOfWeek.Saturday: return 6;
                case DayOfWeek.Sunday: return 7;
                default: return -1;
            }
        }

        public static int StringToDOWInt(string dow)
        {
            dow = dow.ToLower();
            if (dow == "monday" || dow == "mon" || dow == "m")
                return 1;
            if (dow == "tuesday" || dow == "tues" || dow == "tu" || dow == "t")
                return 2;
            if (dow == "wednesday" || dow == "wedn" || dow == "wed" || dow == "w")
                return 3;
            if (dow == "thursday" || dow == "thurs" || dow == "thur" || dow == "th" || dow == "r")
                return 4;
            if (dow == "friday" || dow == "fri" || dow == "f")
                return 5;
            if (dow == "saturday" || dow == "sat")
                return 6;
            if (dow == "sunday" || dow == "sun")
                return 0;
            return -1;
        }
    }
}


