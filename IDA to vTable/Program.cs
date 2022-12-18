using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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

        static Function ConvertToObject(string input)
        {
            Regex regex = new (@"^\s*(?<returnType>virtual\s+[\w\s]+\*?)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)");
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

        static Function ParseConstructorString(string input)
        {
            // Player::~Player()
            var regex = new Regex(@"; (?<class>\w+)::~(?<classC>\w+)");
            var match = regex.Match(input);
            
            var className = match.Groups["class"].Value;

            return new Function(className, "~" + className, "");
        }


        static Function ParseString(string input, int index)
        {
            if (input.Contains('~') && input.Contains("dq offset")) // Constructor
                return ParseConstructorString(input);

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
            
            // If the function name does not match any of the above patterns, it is likely to return void
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
                {
                    
                    shouldIncrementIndex = false;
                }
                else
                {
                    // Guess the return type
                    var returnType = GuessReturnType(function.Name);

                    functionList1.Add(new Function("class", function.Name, function.Args, returnType));

                    functionNames.Add($"{returnType} {function.Name}({function.Args});");
                    functionIndexes.Add($"int {function.Name} = {index};");
                }

                if (shouldIncrementIndex)
                    index++;
            }


            foreach (string line in functionNames)
            {
                Console.WriteLine(line);
            }


            foreach (string line in functionIndexes)
            {
                if (!line.Contains('~'))
                    Console.WriteLine(line);
            }

            Console.WriteLine("Enter file name");
            string path = Environment.CurrentDirectory + @"\" + Console.ReadLine();
            System.IO.File.WriteAllLines(path + "vtables.txt", functionNames);
            System.IO.File.WriteAllLines(path + "indexs.txt", functionIndexes);

            Console.WriteLine("Saved");
            Console.ReadKey();
        }
    }
}