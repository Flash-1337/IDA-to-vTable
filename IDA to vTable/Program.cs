using System.Text.RegularExpressions;
using static System.Boolean;

namespace IDA_to_vTable;

internal static class Program
{
    private static readonly Regex ParseFunctionStringRegex = new(@"; (?<class>\w+)::(?<method>\w+)\((?<params>[^)]*)\)");
    private static readonly Regex ConvertToFunctionRegex = new(@"^\s*(?<returnType>virtual\s+[\w\s]+\*?)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)");
    private static readonly Regex IsValidFunctionRegex = new(@"^\s*\.rodata:[0-9A-F]+\s+((dq\s+offset\s+)?[_A-Z0-9]+\d+\w*\s*)?;\s*\w+::\w+\([^)]*\)?$");
    private static readonly Regex ParseDestructorStringRegex = new(@"; (?<class>\w+)::~(?<classC>\w+)");

    private static Function? ParseFunctionString(string input)
    {
        // Use a regular expression to extract the class name, method name, and parameters
        Match match = ParseFunctionStringRegex.Match(input);

        // Extract the class name, method name, and parameters from the regex match
        string className = match.Groups["class"].Value;
        string methodName = match.Groups["method"].Value;
        string methodParams = match.Groups["params"].Value;

        return new(className, methodName, methodParams);
    }

    private static Function? ConvertToFunction(string input)
    {
        Match match = ConvertToFunctionRegex.Match(input);
        
        if (!match.Success) return null;
        
        Function obj = new("class",
            match.Groups["name"].Value,
            match.Groups["args"].Value,
            match.Groups["returnType"].Value);
        return obj;
    }

    private static bool IsValidFunc(string input)
    {
        return IsValidFunctionRegex.IsMatch(input);
    }

    private static bool _alreadyFoundDestructor;

    private static Function? ParseDestructorString(string input)
    {
        if (_alreadyFoundDestructor)
            return null;

        // Player::~Player()
        Match match = ParseDestructorStringRegex.Match(input);
            
        string className = match.Groups["class"].Value;

        _alreadyFoundDestructor = true;

        return new(className, "~" + className, "");
    }


    private static Function? ParseString(string input, int index)
    {
        if (input.Contains('~') && input.Contains("dq offset")) // Destructor
            return ParseDestructorString(input);

        if (input.Contains("___cxa_pure_virtual")) // Pure virtual function
            return new("Class", "Function" + index, "");


        return !IsValidFunc(input) ? // Not a valid function
            null : ParseFunctionString(input);
    }


    private static string GuessReturnType(string name)
    {
        // Check if the function name starts with "is" or "has"
        if (name.StartsWith("is") || name.StartsWith("has") || name.StartsWith("can"))
        {
            // If the function name starts with "is" or "has", it is likely to return a boolean value
            return "virtual bool";
        }
            
        // If the function name does not match any of the above patterns, it is likely to be a normal void
        return $"virtual {ReturnGuess}";
    }

    public static string? InputPath;
    public static string? OutputPath;
    public static string? MergePath;
    public static string? ReturnGuess = "int";
    public static bool IncludeIndices = true;
    public static bool ThirtyTwoBit;
    public static bool Verbose;
    private const string HelpText =
        @"
Description:
    Simple tool to convert IDA output to a proper vtable

Usage:
    ida2vt [options]

Options:
    -i <path>     (Required) Path to the input file with the vtable from 
    -o <path>     (Required) Path to the output file
    -m <path>     Path to the old vtable file to merge with (Experimental - This may not work correctly/at all)
    -r <type>     Default return type (default: void)
    -c            Excludes comments with indices
    -b            Specifies that the vtable is for a 32-bit binary, making hex indices multiples of 4 instead of 8 (default: false)
    -v            Enables printing verbose output (floods the console) (default: false)
";
    private static void Main(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (i + 1 >= args.Length && args[i] is not "-c" and not "-b" and not "-v")
            {
                Console.WriteLine(HelpText);
                return;
            }
            switch (args[i])
            {
                case "-i":
                    InputPath = args[++i];
                    break;
                case "-o":
                    OutputPath = args[++i];
                    break;
                case "-m":
                    MergePath = args[++i];
                    break;
                case "-r":
                    ReturnGuess = args[++i];
                    break;
                case "-c":
                    IncludeIndices = false;
                    break;
                case "-b":
                    ThirtyTwoBit = true;
                    break;
                case "-v":
                    Verbose = true;
                    break;
                default:
                    Console.WriteLine(HelpText);
                    return;
            }
        }

        if (InputPath == null || OutputPath == null)
        {
            Console.WriteLine(HelpText);
            return;
        }

        // Read the input file

        string[] lines;

        Console.WriteLine("Input path: " + InputPath);
        Console.WriteLine("Output path: " + OutputPath);
        Console.WriteLine("Merge path: " + MergePath);
        Console.WriteLine("Return guess: " + ReturnGuess);
        Console.WriteLine("Include indices: " + IncludeIndices);
        Console.WriteLine("32-bit: " + ThirtyTwoBit);

        try
        {
            lines = File.ReadAllLines(InputPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("The input file could not be read, is the path invalid?");
            Console.ForegroundColor = oldColor;
            return;
        }
        List<string> functionNames = new();
        List<string> functionIndexes = new();
        List<Function?> functionList1 = new();



        int index = -1; // -1 because of the typeinfo 

        // Read all lines
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            // Parse the line
            if (Verbose)
                Console.WriteLine($"Parsing line {i} (index {index})");
            Function? function = ParseString(line, index);

            // Check if the line was a valid function
            if (function == null)
            {
                Console.WriteLine($"Line {i} was not a valid function");
                continue;
            }

            // Guess the return type
            string returnType = GuessReturnType(function.Name);
            if (Verbose)
                Console.WriteLine($"Name: {function.Name}, Args: {function.Args}, Return type: {returnType}");

            functionList1.Add(new("class", function.Name, function.Args, returnType));

            index++;
        }


        if (MergePath != null)
        {
            // interpret the vtable from the input file

            if (!File.Exists(MergePath))
            {
                Console.WriteLine($"Merge file {MergePath} does not exist");
                return;
            }

            Console.WriteLine("Attempting to merge with " + MergePath);
            string[] oldVtableStr = File.ReadAllLines(MergePath);

            List<Function?> oldVTableFunctions = new();

            foreach (string line in oldVtableStr)
            {
                if (Verbose)
                    Console.WriteLine("Parsing line: " + line + " of old vtable");
                oldVTableFunctions.Add(ConvertToFunction(line));
            }
            Console.WriteLine("Finished parsing old vtable, attempting to merge");
            foreach (Function? newFunc in functionList1)
            {
                if (Verbose)
                    Console.WriteLine($"Comparing {newFunc?.Name} to old vtable");
                if (newFunc == null)
                {
                    Console.WriteLine("Skipping null function in new vtable");
                    continue;
                }
                foreach (Function? oldFunc in oldVTableFunctions)
                {
                    if (oldFunc == null || newFunc.wasChanged || oldFunc.wasChanged)
                    {
                        Console.WriteLine("Skipping a function in the old vtable because:");
                        if (oldFunc == null)
                            Console.WriteLine("It is null");
                        if (oldFunc?.wasChanged ?? false)
                            Console.WriteLine("The old vtable function was already changed");
                        if (newFunc.wasChanged)
                        {
                            Console.WriteLine("The new vtable function was already changed");
                            break;
                        }

                        continue;
                    }

                    if (newFunc.Name != oldFunc.Name) continue;
                    
                    if (newFunc.Args != oldFunc.Args)
                    {
                        Console.WriteLine($"Changing args of {newFunc.Name} from {newFunc.Args} to {oldFunc.Args}");
                        newFunc.Args = oldFunc.Args;
                    }

                    if (newFunc.ReturnType != oldFunc.ReturnType)
                    {
                        Console.WriteLine($"Changing return type of {newFunc.Name} from {newFunc.ReturnType} to {oldFunc.ReturnType}");
                        newFunc.ReturnType = oldFunc.ReturnType;
                    }


                    newFunc.wasChanged = true;
                    oldFunc.wasChanged = true;
                }
            }
        }

        int newIndex = 0;

        string lastFunctionName = "";
        Console.WriteLine("Converting to strings");
        foreach (Function? func in functionList1)
        {
            functionNames.Add($"{func?.ReturnType} {func?.Name}({func?.Args});{(IncludeIndices ? $" // {newIndex} (0x{newIndex * (ThirtyTwoBit ? 4 : 8):X})" : string.Empty)}");
            
            if (lastFunctionName != func?.Name && !func!.Name.Contains('~'))
                functionIndexes.Add($"void {func.Name} = {newIndex};{(IncludeIndices ? $" // 0x{newIndex * (ThirtyTwoBit ? 4 : 8):X}" : string.Empty)}");


            if (!func.Name.Contains('~'))
                newIndex++;

            lastFunctionName = func.Name;
        }

        File.WriteAllLines(OutputPath + "_vtables.txt", functionNames);
        File.WriteAllLines(OutputPath + "_indices.txt", functionIndexes);

        Console.WriteLine("Saved");
    }
}