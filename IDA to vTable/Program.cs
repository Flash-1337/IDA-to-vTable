using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IDA_to_vTable
{
    internal class Function
    {

        public Function(string _class, string _name, string _args)
        {
            Class = _class;
            Name = _name;
            Args = _args;
            ReturnType = "virtual int";
        }

        public Function(string _class, string _name, string _args, string _return)
        {
            Class = _class;
            Name = _name;
            Args = _args;
            ReturnType = _return;
        }

        public string Name { get; set; }
        public string Class { get; set; }
        public string Args { get; set; }
        public string ReturnType { get; set; }

        public bool wasChanged { get; set; }
    }
    
    internal class Program
    {

        static Function ParseFunctionString(string input)
        {
            // Use a regular expression to extract the class name, method name, and parameters
            Regex regex = new (@"; (?<class>\w+)::(?<method>\w+)\((?<params>[^)]*)\)");
            Match match = regex.Match(input);

            // Extract the class name, method name, and parameters from the regex match
            string className = match.Groups["class"].Value;
            string methodName = match.Groups["method"].Value;
            string methodParams = match.Groups["params"].Value;

            return new Function(className, methodName, methodParams);
        }

        static Function ConvertToFunction(string input)
        {
            Regex regex = new(@"^\s*(?<returnType>virtual\s+[\w\s]+\*?)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)");
            Match match = regex.Match(input);
            if (match.Success)
            {
                Function obj = new("class",
                    match.Groups["name"].Value,
                    match.Groups["args"].Value,
                    match.Groups["returnType"].Value);
                return obj;
            }
            return null;
        }

        static bool IsValidFunc(string input)
        {
            Regex regex = new(@"^\s*\.rodata:[0-9A-F]+\s+((dq\s+offset\s+)?[_A-Z0-9]+\d+\w*\s*)?;\s*\w+::\w+\([^)]*\)?$");
            return regex.IsMatch(input);
        }

        static bool alreadyFoundDestructor = false;

        static Function ParseDestructorString(string input)
        {
            if (alreadyFoundDestructor)
                return null;

            // Player::~Player()
            var regex = new Regex(@"; (?<class>\w+)::~(?<classC>\w+)");
            var match = regex.Match(input);
            
            var className = match.Groups["class"].Value;

            alreadyFoundDestructor = true;

            return new Function(className, "~" + className, "");
        }


        static Function ParseString(string input, int index)
        {
            if (input.Contains('~') && input.Contains("dq offset")) // Destructor
                return ParseDestructorString(input);

            if (input.Contains("___cxa_pure_virtual")) // Pure virtual function
                return new Function("Class", "Function" + index, "");


            if (!IsValidFunc(input)) // Not a valid function
                return null;

            return ParseFunctionString(input);
        }



        static string GuessReturnType(string name)
        {
            // Check if the function name starts with "is" or "has"
            if (name.StartsWith("is") || name.StartsWith("has") || name.StartsWith("can"))
            {
                // If the function name starts with "is" or "has", it is likely to return a boolean value
                return "virtual bool";
            }
            
            // If the function name does not match any of the above patterns, it is likely to be a normal void
            return "virtual int";
        }


        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;

            Console.WriteLine("Enter file path of IDA vTable");


            // Read the input file

            string[] lines;

            try
            {
                lines = System.IO.File.ReadAllLines(Console.ReadLine());
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return;
            }
            List<string> functionNames = new();
            List<string> functionIndexes = new();
            List<Function> functionList1 = new();



            int index = -1; // -1 because of the typeinfo 

            // Read all lines
            foreach (string line in lines)
            {
                bool shouldIncrementIndex = true;
                // Parse the line
                var function = ParseString(line, index);

                // Check if the line was a valid function
                if (function == null)
                    shouldIncrementIndex = false;
                else
                {
                    // Guess the return type
                    var returnType = GuessReturnType(function.Name);

                    functionList1.Add(new Function("class", function.Name, function.Args, returnType));
                    
                }

                if (shouldIncrementIndex)
                    index++;
            }




            Console.WriteLine("Would you like to merge with a pre-existing vtable? (Experimental - This may not work correctly/at all) y/n");

            if (Console.ReadLine().ToLower().Contains("y"))
            {

                Console.WriteLine("Input path to old table");

                string oldVtablePath = Console.ReadLine();

                // interepret the vtable from the input file

                if (!File.Exists(oldVtablePath))
                {
                    Console.WriteLine("File does not exist");
                    Console.ReadLine();
                    return;
                }

                string[] oldVtableStr = File.ReadAllLines(oldVtablePath);

                List<Function> oldvTableFunctions = new();

                foreach (string line in oldVtableStr)
                {
                    oldvTableFunctions.Add(ConvertToFunction(line));
                }

                foreach (Function newFunc in functionList1)
                {
                    foreach (Function oldFunc in oldvTableFunctions)
                    {
                        if (newFunc == null || oldFunc == null || newFunc.wasChanged || oldFunc.wasChanged)
                            continue;

                        if (newFunc.Name == oldFunc.Name)
                        {
                            if (newFunc.Args != oldFunc.Args)
                                newFunc.Args = oldFunc.Args;

                            if (newFunc.ReturnType != oldFunc.ReturnType)
                                newFunc.ReturnType = oldFunc.ReturnType;


                            newFunc.wasChanged = true;
                            oldFunc.wasChanged = true;

                            continue;
                        }

                        
                    }
                }
            }

            int newIndex = 1;

            Function lastFunction = null;

            foreach (Function func in functionList1)
            {

                functionNames.Add($"{func.ReturnType} {func.Name}({func.Args});");
                
                if (lastFunction != null && lastFunction.Name != func.Name && !func.Name.Contains("~"))
                    functionIndexes.Add($"void {func.Name} = {newIndex};");


                if (!func.Name.Contains("~"))
                    newIndex++;

                lastFunction = func;
            }



            Console.WriteLine("Enter file name");
            string path = Environment.CurrentDirectory + @"\" + Console.ReadLine();
            System.IO.File.WriteAllLines(path + "_vtables.txt", functionNames);
            System.IO.File.WriteAllLines(path + "_indexs.txt", functionIndexes);

            Console.WriteLine("Saved");
            Console.ReadKey();
        }
    }
}