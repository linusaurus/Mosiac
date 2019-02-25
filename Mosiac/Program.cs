using System;
using Microsoft.EntityFrameworkCore;
using PurchaseModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;


namespace Mosiac
{

    public class MyContext : DbContext
    {
        //This is working. and debugging is working!
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=192.168.10.3;DataBase=Badger;user id=sa; password=Kx09a32x");
        }

        public DbSet<Part> Part { get; set; }
        public DbSet<OrderReciept> OrderReciept { get; set; }
        public DbSet<Manu> Manu { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<UnitOfMeasure> UnitOfMeasure { get; set; }
        public DbSet<TransActionType> TransActionType { get; set; }
        public DbSet<Job> Job { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrder { get; set; }
        public DbSet<PurchaseLineItem> PurchaseLineItem { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<DeadParts> DeadParts { get; set; }

        
        
    }


    class Program
    {
        const string _commandNamespace = "Mosiac.Commands";
        static Dictionary<string, Dictionary<string, IEnumerable<System.Reflection.ParameterInfo>>> _commandLibraries;
        //static string vers = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        static string vers = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "."
                           + Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString() + "."
                           + Assembly.GetExecutingAssembly().GetName().Version.MinorRevision.ToString();
        static void DisplayAppInformation()
        {

            Console.WriteLine(@" __  __           _            ");
            Console.WriteLine(@"|  \/  |         (_)           ");
            Console.WriteLine(@"| \  / | ___  ___ _  __ _  ___ ");
            Console.WriteLine(@"| |\/| |/ _ \/ __| |/ _` |/ __|");
            Console.WriteLine(@"| |  | | (_) \__ \ | (_| | (__");
            Console.WriteLine(@"|_|  |_|\___/|___/_|\__,_|\___|");



            Console.WriteLine(Mosiac.Commands.DefaultCommands.Filler(80));                                                                          
            Console.WriteLine("Mosiac-Inventory".PadLeft(6));
            Console.WriteLine(String.Format("version {0,-24}".PadLeft(6),vers));
            Console.Write(String.Format("Date    {0,-24}".PadLeft(6), DateTime.Today.ToShortDateString()));
            Console.WriteLine(String.Format("Time     {0}".PadLeft(6), DateTime.Now.ToLongTimeString()));
            Console.WriteLine(Mosiac.Commands.DefaultCommands.Filler(80));
            Console.WriteLine(" ");
            Console.WriteLine(" ");

        }
        public static Employee ActiveUser;

        static void Main(string[] args)
        {
            Console.Title = "Mosiac-Inventory";      //typeof(Program).Name;
           

            Login();
            DisplayAppInformation();
            MyContext ctx = new MyContext();
            var parts = ctx.Part.Where(p => p.ItemDescription.Contains("hinge")).ToList();

            // Any static classes containing commands for use from the 
            // console are located in the Commands namespace. Load 
            // references to each type in that namespace via reflection:
            _commandLibraries = new Dictionary<string, Dictionary<string,
                    IEnumerable<ParameterInfo>>>();

            // Use reflection to load all of the classes in the Commands namespace:
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.Namespace == _commandNamespace
                    select t;
            var commandClasses = q.ToList();

            foreach (var commandClass in commandClasses)
            {
                // Load the method info from each class into a dictionary:
                var methods = commandClass.GetMethods(BindingFlags.Static | BindingFlags.Public);
                var methodDictionary = new Dictionary<string, IEnumerable<ParameterInfo>>();
                foreach (var method in methods)
                {
                    string commandName = method.Name;
                    methodDictionary.Add(commandName, method.GetParameters());
                }
                // Add the dictionary of methods for the current class into a dictionary of command classes:
                _commandLibraries.Add(commandClass.Name, methodDictionary);
            }
            Run();

        }

        public static bool Testlogin(string user, string pass)
        {
           bool result = false;
            using (var ctx = new MyContext())
            {
                try
                {
                    Employee e = ctx.Employee.Where(c => c.Login == user).First();
                    if (e.Password == pass)
                    {
                        result = true;
                        ActiveUser = e;
                    }

                }
                catch { }

                return result;
            }
        }


        static void Login()
        {
            bool Authenticated = false;
            string Password = "";
            while (Authenticated != true)
            {
                Console.WriteLine("Mosiac-Inventory System");
                Console.Write("username : ");
                string userName = Console.ReadLine();
                Console.Write("password : ");
                ConsoleKeyInfo key;

                while (key.Key != ConsoleKey.Enter) 
                {
                    key = Console.ReadKey(true);

                    // Backspace Should Not Work
                    if (key.Key != ConsoleKey.Backspace)
                    {
                        Password += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        Console.Write("\b");
                    }
                }
                // Stops Receving Keys Once Enter is Pressed
              
                Password = Password.TrimEnd();

                if (Testlogin(userName, Password))
                {
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("login Successful!");
                    Console.WriteLine(" ");
                    Console.Write("User = ");
                    Console.Write(ActiveUser.firstname.ToString() + " " + ActiveUser.lastname.ToString());
                    Console.WriteLine(" ");
                    Authenticated = true;
                    Console.Title = String.Format("Mosiac-Inventory : User = {0} {1}", ActiveUser.firstname, ActiveUser.lastname);
                }
                else
                {               
                    Console.WriteLine("Password failed : {0}", userName);
                    //Console.ReadLine();
                }

            }
        }

        public static void  RunPartifyMenu()
        {
            int userInput = 0;
            do
            {
              userInput = DisplayStockTagMenu();

                
            } while (userInput != 5);

            switch (userInput)
            {
                case 1:
                    {

                        Console.WriteLine("Case 1");
                    }

                    break;
            }

        }

        static public int DisplayStockTagMenu()
        {
            Console.WriteLine("Menu");
            Console.WriteLine();
            Console.WriteLine("1. Search");
            Console.WriteLine("2. Partify");
            Console.WriteLine("4. Exit");
            var result = Console.ReadLine();
            return Convert.ToInt32(result);
        }

        static void Run()
        {
            while (true)
            {
                var consoleInput = ReadFromConsole();
                if (string.IsNullOrWhiteSpace(consoleInput)) continue;

                try
                {
                    // Create a ConsoleCommand instance:
                    var cmd = new ConsoleCommand(consoleInput);

                    // Execute the command:
                    string result = Execute(cmd);

                    // Write out the result:
                    WriteToConsole(result);
                }
                catch (Exception ex)
                {
                    // OOPS! Something went wrong - Write out the problem:
                    WriteToConsole(ex.Message);
                }
            }
        }

        static string Execute(ConsoleCommand command)
        {
            // Validate the class name and command name:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            string badCommandMessage = string.Format(""
                + "Unrecognized command \'{0}.{1}\'. "
                + "Please type a valid command.",
                command.LibraryClassName, command.Name);
            // Validate the command name:
            if (!_commandLibraries.ContainsKey(command.LibraryClassName))
            {
                return badCommandMessage;
            }
            var methodDictionary = _commandLibraries[command.LibraryClassName];
            if (!methodDictionary.ContainsKey(command.Name))
            {
                return badCommandMessage;
            }
            // Make sure the corret number of required arguments are provided:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var methodParameterValueList = new List<object>();
            IEnumerable<ParameterInfo> paramInfoList = methodDictionary[command.Name].ToList();
            // Validate proper # of required arguments provided. Some may be optional:
            var requiredParams = paramInfoList.Where(p => p.IsOptional == false);
            var optionalParams = paramInfoList.Where(p => p.IsOptional == true);
            int requiredCount = requiredParams.Count();
            int optionalCount = optionalParams.Count();
            int providedCount = command.Arguments.Count();
            if (requiredCount > providedCount)
            {
                return string.Format(
                    "Missing required argument. {0} required, {1} optional, {2} provided",
                    requiredCount, optionalCount, providedCount);
            }
            // Make sure all arguments are coerced to the proper type, and that there is a 
            // value for every emthod parameter. The InvokeMember method fails if the number 
            // of arguments provided does not match the number of parameters in the 
            // method signature, even if some are optional:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (paramInfoList.Count() > 0)
            {
                // Populate the list with default values:
                foreach (var param in paramInfoList)
                {
                    // This will either add a null object reference if the param is required 
                    // by the method, or will set a default value for optional parameters. in 
                    // any case, there will be a value or null for each method argument 
                    // in the method signature:
                    methodParameterValueList.Add(param.DefaultValue);
                }
                // Now walk through all the arguments passed from the console and assign 
                // accordingly. Any optional arguments not provided have already been set to 
                // the default specified by the method signature:
                for (int i = 0; i < command.Arguments.Count(); i++)
                {
                    var methodParam = paramInfoList.ElementAt(i);
                    var typeRequired = methodParam.ParameterType;
                    object value = null;
                    try
                    {
                        // Coming from the Console, all of our arguments are passed in as 
                        // strings. Coerce to the type to match the method paramter:
                        value = CoerceArgument(typeRequired, command.Arguments.ElementAt(i));
                        methodParameterValueList.RemoveAt(i);
                        methodParameterValueList.Insert(i, value);
                    }
                    catch (ArgumentException ex)
                    {
                        string argumentName = methodParam.Name;
                        string argumentTypeName = typeRequired.Name;
                        string message =
                            string.Format(""
                            + "The value passed for argument '{0}' cannot be parsed to type '{1}'",
                            argumentName, argumentTypeName);
                        throw new ArgumentException(message);
                    }
                }
            }
            // Set up to invoke the method using reflection:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            Assembly current = typeof(Program).Assembly;
            // Need the full Namespace for this:
            Type commandLibaryClass =
                current.GetType(_commandNamespace + "." + command.LibraryClassName);
            object[] inputArgs = null;
            if (methodParameterValueList.Count > 0)
            {
                inputArgs = methodParameterValueList.ToArray();
            }
            var typeInfo = commandLibaryClass;
            // This will throw if the number of arguments provided does not match the number 
            // required by the method signature, even if some are optional:
            try
            {
                var result = typeInfo.InvokeMember(
                    command.Name,
                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                    null, null, inputArgs);
                return result.ToString();
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
        static object CoerceArgument(Type requiredType, string inputValue)
        {
            var requiredTypeCode = Type.GetTypeCode(requiredType);
            string exceptionMessage =
                string.Format("Cannnot coerce the input argument {0} to required type {1}",
                inputValue, requiredType.Name);
            object result = null;
            switch (requiredTypeCode)
            {
                case TypeCode.String:
                    result = inputValue;
                    break;
                case TypeCode.Int16:
                    short number16;
                    if (Int16.TryParse(inputValue, out number16))
                    {
                        result = number16;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int32:
                    int number32;
                    if (Int32.TryParse(inputValue, out number32))
                    {
                        result = number32;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int64:
                    long number64;
                    if (Int64.TryParse(inputValue, out number64))
                    {
                        result = number64;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Boolean:
                    bool trueFalse;
                    if (bool.TryParse(inputValue, out trueFalse))
                    {
                        result = trueFalse;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Byte:
                    byte byteValue;
                    if (byte.TryParse(inputValue, out byteValue))
                    {
                        result = byteValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Char:
                    char charValue;
                    if (char.TryParse(inputValue, out charValue))
                    {
                        result = charValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.DateTime:
                    DateTime dateValue;
                    if (DateTime.TryParse(inputValue, out dateValue))
                    {
                        result = dateValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Decimal:
                    Decimal decimalValue;
                    if (Decimal.TryParse(inputValue, out decimalValue))
                    {
                        result = decimalValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Double:
                    Double doubleValue;
                    if (Double.TryParse(inputValue, out doubleValue))
                    {
                        result = doubleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Single:
                    Single singleValue;
                    if (Single.TryParse(inputValue, out singleValue))
                    {
                        result = singleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt16:
                    UInt16 uInt16Value;
                    if (UInt16.TryParse(inputValue, out uInt16Value))
                    {
                        result = uInt16Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt32:
                    UInt32 uInt32Value;
                    if (UInt32.TryParse(inputValue, out uInt32Value))
                    {
                        result = uInt32Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt64:
                    UInt64 uInt64Value;
                    if (UInt64.TryParse(inputValue, out uInt64Value))
                    {
                        result = uInt64Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                default:
                    throw new ArgumentException(exceptionMessage);
            }
            return result;
        }
        public static void WriteToConsole(string message = "")
        {
            if (message.Length > 0)
            {
                Console.WriteLine(message);
            }
        }
        const string _readPrompt = "inventory> ";
        public static string ReadFromConsole(string promptMessage = "")
        {
            // Show a prompt, and get input:
            Console.Write(_readPrompt + promptMessage);
            return Console.ReadLine();
        }
    }
}

