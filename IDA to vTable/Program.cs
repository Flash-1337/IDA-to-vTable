using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace IDA_to_vTable
{
    internal class Function
    {

        public Function(string _class, string _name, string _params)
        {
            Name = _name;
            Class = _class;
            Params = _params;
        }

        public string Name { get; set; }
        public string Class { get; set; }
        public string Params { get; set; }
    }

    public class FunctionObject
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public string Args { get; set; }
    }

    internal class Program
    {

        static Function ParseFunctionString(string input)
        {
            // Use a regular expression to extract the class name, method name, and parameters
            var regex = new Regex(@"; (?<class>\w+)::(?<method>\w+)\((?<params>[^)]*)\)");
            var match = regex.Match(input);

            // Extract the class name, method name, and parameters from the regex match
            var className = match.Groups["class"].Value;
            var methodName = match.Groups["method"].Value;
            var methodParams = match.Groups["params"].Value;

            return new Function(className, methodName, methodParams);
        }

        static FunctionObject ConvertToObject(string input)
        {
            Regex regex = new Regex(@"^\s*(?<returnType>virtual\s+[\w\s]+\*?)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)");
            Match match = regex.Match(input);
            if (match.Success)
            {
                FunctionObject obj = new FunctionObject
                {
                    ReturnType = match.Groups["returnType"].Value,
                    Name = match.Groups["name"].Value,
                    Args = match.Groups["args"].Value
                };
                return obj;
            }
            return null;
        }

        static bool IsValidFunc(string input)
        {
            Regex regex = new Regex(@"^\s*\.rodata:[0-9A-F]+\s+((dq\s+offset\s+)?[_A-Z0-9]+\d+\w*\s*)?;\s*\w+::\w+\([^)]*\)?$");
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
            if (input.Contains("~") && input.Contains("dq offset")) // Constructor
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
            List<FunctionObject> functionList1 = new();



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

                    functionObjs.Add(new FunctionObject
                    {
                        ReturnType = returnType,
                        Name = function.Name,
                        Args = function.Params
                    });

                    functionNames.Add($"{returnType} {function.Name}({function.Params});");
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
                Console.WriteLine(line);
            }


            Console.WriteLine("Input old vtable");

            string[] lines2;

            try
            {
                lines2 = System.IO.File.ReadAllLines(Console.ReadLine());
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return;
            }

            List<FunctionObject> functionList2 = new();

            // Read all lines
            foreach (string line in lines2)
            {
                // Turn the input from IDA into a Function object
                var function = ConvertToObject(line);

                if (function == null) continue;

                functionList2.Add(function);
            }

        }
    }
}