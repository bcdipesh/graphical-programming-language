﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace graphical_programming_language
{
    /// <summary>
    /// The main class for compiling ShapeCommands.
    /// </summary>
    /// <remarks>
    /// This class implements <see cref="graphical_programming_language.IShapeCompiler"/> interface and provides abstract and virtual methods for child Shape classes to override.
    /// </remarks>
    public class ShapeCompiler : ICompiler
    {
        // Constants that represent operators.

        private const string EQUALS = "=";
        private const string IF = "if";
        private const string ENDIF = "endif";
        private const string WHILE = "while";
        private const string METHOD = "method";
        private const string PARENTHESIS = "()";

        private string[] operators = { EQUALS, IF, ENDIF, WHILE, METHOD, PARENTHESIS };

        private readonly Lexer lexer;
        public Dictionary<string, string> Variables { get; set; }

        private readonly ShapeFactory shapeFactory;
        private readonly Panel outputWindow;
        private readonly RichTextBox programLog;
        private Pen pen;
        private Color fillColor;

        private readonly Regex inputSplitter;

        /// <summary>
        /// Gets and sets the command for the compiler.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets and sets the arguments for the compiler.
        /// </summary>
        public string[] Arguments { get; set; }

        private int xPos;
        private int yPos;
        private int toXPos;
        private int toYPos;
        private int width;
        private int height;
        private int radius;

        private bool isColorFillOn;

        public bool DrawToPanel { get; set; }

        /// <summary>
        /// The default constructor.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of ShapeCompiler, with default values.
        /// </remarks>
        public ShapeCompiler()
        {
            lexer = new Lexer();
            Variables = new Dictionary<string, string>();

            shapeFactory = new ShapeFactory();
            isColorFillOn = false;
            pen = GetPen(Color.Black, 1);

            inputSplitter = new Regex(@"[\s+,]", RegexOptions.Compiled);

            xPos = yPos = toXPos = toYPos = 0;
            width = height = 100;
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="outputWindow">The Panel where output of command after its execution is displayed.</param>
        /// <param name="programLog">The TexBox where the log of the command during its execution is displayed.</param>
        /// <remarks>
        /// Initializes a new instances of Shape, with the given parameters.
        /// </remarks>
        public ShapeCompiler(Panel outputWindow, RichTextBox programLog)
        {
            lexer = new Lexer();
            Variables = new Dictionary<string, string>();

            shapeFactory = new ShapeFactory();
            this.outputWindow = outputWindow;
            this.programLog = programLog;
            isColorFillOn = false;
            pen = GetPen(Color.Black, 1);

            inputSplitter = new Regex(@"[\s+,]", RegexOptions.Compiled);

            xPos = yPos = toXPos = toYPos = 0;
            width = height = 100;
        }

        // Generate a color from the given string.
        private Color GetColor(string color)
        {
            foreach (KnownColor _color in Enum.GetValues(typeof(KnownColor)))
            {
                if (_color.ToString().ToUpper().Equals(color.ToUpper()))
                {
                    return Color.FromName(color);
                }
            }
            return Color.Black;
        }

        /// <summary>
        /// Log program output.
        /// </summary>
        /// <param name="messageColor">The color of the message.</param>
        /// <param name="message">The actual message of the output after a command is run.</param>
        /// <remarks>
        /// Logs the output of the command or program to the ProgramLog window.
        /// </remarks>
        public void LogOutput(Color messageColor, string message)
        {
            programLog.SelectionColor = messageColor;
            programLog.AppendText(message);
            programLog.AppendText(Environment.NewLine);
            programLog.ScrollToCaret();
        }

        /// <summary>
        /// Parses the program.
        /// </summary>
        /// <param name="input">The text to be parsed.</param>
        /// <remarks>Parses the program written in the program window.</remarks>
        public void ParseUsingLexer(string input)
        {
            var tokens = lexer.Advance(input);
            string variable_name = "";

            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                var numbersList = new List<int>();
                var operators = new Queue<string>();

                // Check for variable statement
                if (t.Type == Type.IDENTIFIER) { variable_name = t.GetValue(); }

                // Check for variable in expression
                if (t.Type == Type.OPERATOR && t.GetValue() == "=" && tokens.Count > 3)
                {
                    foreach (var _token in tokens.GetRange(2, tokens.Count - 2))
                    {
                        if (_token.Type == Type.IDENTIFIER)
                            numbersList.Add(int.Parse(Variables[_token.GetValue()]));
                        if (_token.Type == Type.NUMBER)
                            numbersList.Add(int.Parse(_token.GetValue()));
                        if (_token.Type == Type.OPERATOR)
                            operators.Enqueue(_token.GetValue());
                    }
                    var result = numbersList[0];
                    for (int j = 1; j < numbersList.Count; j++)
                    {
                        switch (operators.Dequeue())
                        {
                            case "+":
                                result += numbersList[j];
                                break;

                            case "-":
                                result -= numbersList[j];
                                break;

                            case "/":
                                result /= numbersList[j];
                                break;

                            case "*":
                                result *= numbersList[j];
                                break;
                        }
                    }

                    if (!Variables.ContainsKey(variable_name))
                    {
                        Variables.Add(variable_name, result.ToString());
                    }
                    else
                    {
                        Variables[variable_name] = result.ToString();
                    }

                    break;
                }

                // Assign and store number to var
                if (t.Type == Type.NUMBER)
                {
                    if (!Variables.ContainsKey(variable_name))
                    {
                        Variables.Add(variable_name, t.GetValue());
                    }
                    else
                    {
                        Variables[variable_name] = t.GetValue();
                    }
                }
            }
        }

        /// <summary>
        /// Parse the if statements.
        /// </summary>
        /// <param name="input">The line to be parsed.</param>
        /// <returns>Returns either true or false based on the if expression.</returns>
        public bool ParseUsingIf(string input)
        {
            var tokens = lexer.Advance(input);
            bool result = false;
            string op = "";
            var numbersList = new List<int>();
            foreach (var _token in tokens.GetRange(1, 3))
            {
                if (_token.Type == Type.IDENTIFIER)
                    numbersList.Add(int.Parse(Variables[_token.GetValue()]));
                if (_token.Type == Type.NUMBER)
                    numbersList.Add(int.Parse(_token.GetValue()));
                if (_token.Type == Type.OPERATOR)
                    op = _token.GetValue();
            }

            int left = numbersList[0];
            int right = numbersList[1];

            switch (op)
            {
                case "<":
                    result = left < right;
                    break;

                case ">":
                    result = left > right;
                    break;

                case "<=":
                    result = left <= right;
                    break;

                case ">=":
                    result = left >= right;
                    break;

                case "==":
                    result = left == right;
                    break;

                case "!=":
                    result = left != right;
                    break;
            }

            return result;
        }

        // Check if command is for drawing shape.
        private static bool IsShapeCommand(string command)
        {
            return command.ToUpper().Equals("RECT") || command.ToUpper().Equals("CIRCLE") || command.ToUpper().Equals("TRIANGLE");
        }

        // Draws a shape based on the command and arguments.
        private void DrawShape(string command, string[] arguments)
        {
            try
            {
                if (command.ToUpper().Equals("CIRCLE"))
                {
                    if (arguments.Length == 0) { throw new ArgumentException("Circle command need 1 more parameter that represents its radius"); }
                    else
                    {
                        radius = Variables.ContainsKey(arguments[0]) ? int.Parse(Variables[arguments[0]]) : int.Parse(arguments[0]);
                        width = radius * 2;
                        height = radius * 2;
                    }
                }
                else
                {
                    width = Variables.ContainsKey(arguments[0]) ? int.Parse(Variables[arguments[0]]) : int.Parse(arguments[0]);
                    height = Variables.ContainsKey(arguments[1]) ? int.Parse(Variables[arguments[1]]) : int.Parse(arguments[1]);
                }

                Shape shape = shapeFactory.GetShape(command, fillColor, isColorFillOn, xPos, yPos, width, height);
                if (DrawToPanel)
                {
                    shape.Draw(outputWindow.CreateGraphics(), pen);
                    LogOutput(Color.Black, $"[*] {shape.GetType().Name} drawn at position x -> {xPos}, y -> {yPos} with width -> {width}, height -> {height}");
                }
                else
                {
                    LogOutput(Color.Black, "[*] No errors found in Syntax");
                }

            }
            catch (ArgumentException ex)
            {
                LogOutput(Color.Red, $"[*] Error: {ex.Message}");
            }
            catch (FormatException)
            {
                LogOutput(Color.Red, "[*] Error: Given argument is not in correct format");
            }
            catch (IndexOutOfRangeException)
            {
                string shape = command.ToUpper().Equals("RECT") ? "Rectangle" : command;

                LogOutput(Color.Red, $"[*] Error: Please provide two parameter for drawing {shape}");
            }
        }

        // Draws a line from one point to another.
        private void DrawLine(string[] arguments)
        {
            try
            {
                toXPos = Variables.ContainsKey(arguments[0]) ? int.Parse(Variables[arguments[0]]) : int.Parse(arguments[0]);
                toYPos = Variables.ContainsKey(arguments[1]) ? int.Parse(Variables[arguments[1]]) : int.Parse(arguments[1]);

                Shape shape = shapeFactory.GetShape("line", fillColor, isColorFillOn, xPos, yPos, toXPos, toYPos);
                if (DrawToPanel)
                {
                    shape.Draw(outputWindow.CreateGraphics(), pen);

                    LogOutput(Color.Black, $"[*] Line drawn from position x1 -> {xPos}, y1 -> {yPos} to position x2 -> {toXPos}, y2 -> {toYPos}");

                    xPos = toXPos;
                    yPos = toYPos;
                }
                else
                {
                    LogOutput(Color.Black, "[*] No errors found in Syntax");
                }
            }
            catch (IndexOutOfRangeException)
            {
                LogOutput(Color.Red, "[*] Error: Please provide two parameter to draw a line");
            }
            catch (FormatException)
            {
                LogOutput(Color.Red, "[*] Error: Given argument is not in correct format");
            }
        }

        // Moves or sets pen position.
        private void MovePen(string[] arguments)
        {
            try
            {
                if (DrawToPanel)
                {
                    xPos = Variables.ContainsKey(arguments[0]) ? int.Parse(Variables[arguments[0]]) : int.Parse(arguments[0]);
                    yPos = Variables.ContainsKey(arguments[1]) ? int.Parse(Variables[arguments[1]]) : int.Parse(arguments[1]);

                    LogOutput(Color.Black, $"[*] Pen position set to {xPos}, {yPos}");
                }
                else
                {
                    LogOutput(Color.Black, "[*] No errors found in Syntax");
                }
            }
            catch (IndexOutOfRangeException)
            {
                LogOutput(Color.Red, "[*] Error: Please provide two parameter to move pointer");
            }
            catch (FormatException)
            {
                LogOutput(Color.Red, "[*] Error: Given argument is not in correct format");
            }
        }

        // Sets the pen color and size.
        private void SetPen(string[] arguments)
        {
            try
            {
                if (DrawToPanel)
                {
                    Color color = Variables.ContainsKey(arguments[0]) ? Color.FromName(Variables[arguments[0]]) : Color.FromName(arguments[0]);
                    int size = (arguments.Length == 2) ? int.Parse(arguments[1]) : 1;

                    pen = GetPen(color, size);

                    LogOutput(Color.Black, $"[*] Pen color set to {color.Name} and pen size set to {size}");
                }
                else
                {
                    LogOutput(Color.Black, "[*] No errors found in Syntax");
                }
            }
            catch (IndexOutOfRangeException)
            {
                LogOutput(Color.Red, "[*] Error: Please provide one parameter for selecting the color");
            }
            catch (FormatException)
            {
                LogOutput(Color.Red, "[*] Error: Given argument is not in correct format");
            }
        }

        // Sets the color fill for the shape.
        private void SetColorFill(string[] arguments)
        {
            try
            {
                if (DrawToPanel)
                {
                    if (arguments[0].ToUpper().Equals("ON"))
                    {
                        isColorFillOn = true;
                        fillColor = (arguments.Length == 2) ? GetColor(arguments[1]) : Color.Black;

                        LogOutput(Color.Black, $"[*] Color fill is now {isColorFillOn} and set to {fillColor.Name}");
                    }
                    else if (arguments[0].ToUpper().Equals("OFF"))
                    {
                        isColorFillOn = false;

                        LogOutput(Color.Black, $"[*] Color fill is now {isColorFillOn}");
                    }
                }
                else
                {
                    LogOutput(Color.Black, "[*] No errors found in Syntax");
                }
            }
            catch (IndexOutOfRangeException)
            {
                LogOutput(Color.Red, "[*] Error: Please provide one parameter (on/off) to either turn fill on/off");
            }
            catch (FormatException)
            {
                LogOutput(Color.Red, "[*] Error: Given argument is not in correct format");
            }
        }

        // Resets the pen position and height to default value.
        private void ResetPen()
        {
            if (DrawToPanel)
            {
                xPos = yPos = toXPos = toYPos = 0;
                width = height = 100;

                LogOutput(Color.Black, "[*] Reset pen position to 0, 0");
            }
            else
            {
                LogOutput(Color.Black, "[*] No errors found in Syntax");
            }
        }

        // Clears the output panel.
        private void ClearPanel()
        {
            if (DrawToPanel)
            {
                outputWindow.Refresh();

                LogOutput(Color.Black, "[*] Cleared output panel");
            } else
            {
                LogOutput(Color.Black, "[*] No errors found in Syntax");
            }
        }

        public void Compile(string input)
        {
            string[] data = inputSplitter.Split(input).Where(token => token != String.Empty).ToArray<string>();

            Command = data[0];
            Arguments = new string[data.Length - 1];

            if (data.Length > 1)
            {
                Array.Copy(data, 1, Arguments, 0, Arguments.Length);
            }
        }

        public void Run()
        {
            CommandParser(Command, Arguments);
        }

        // Checks and return a bool value that represents if line has operator.
        private bool LineContainsOperator(string line)
        {
            bool hasOperator = false;

            foreach (string element in operators)
            {
                if (line.Contains(element))
                {
                    hasOperator = true;
                }
            }
            return hasOperator;
        }

        // Checks if the program window is not empty.
        private bool IsInputEmpty(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        // Parse the function command.
        private int ParseFunction(string[] program, int lineNumber, string currentLine)
        {
            var tokens = lexer.Advance(currentLine);
            int functionLineNum = lineNumber;
            for (; functionLineNum < program.Length; functionLineNum++)
            {
                if (program[functionLineNum].Contains("endmethod"))
                {
                    break;
                }
            }
            Variables.Add(tokens[1].GetValue(), lineNumber + "," + (functionLineNum - 1));
            lineNumber = functionLineNum;

            return lineNumber;
        }

        // Runs the function created in the program.
        private int RunFunction(ref int lineNumber, string currentLine)
        {
            int cursor = lineNumber;
            var tokens = lexer.Advance(currentLine);
            var functionLines = Variables[tokens[0].GetValue()].Split(',');
            lineNumber = int.Parse(functionLines[0]);

            return cursor;
        }

        // Runs the loop operation.
        private int RunLoop(string[] program, string input, int lineNumber, string currentLine)
        {
            int whileNum = lineNumber;
            whileNum++;
            while (ParseUsingIf(currentLine))
            {
                if (program[whileNum].Contains("endwhile"))
                {
                    whileNum = lineNumber;
                }
                else
                {
                    ParseProgram(program[whileNum], input);
                }
                whileNum++;
            }
            lineNumber = whileNum;
            return lineNumber;
        }

        // Runs the if statement in the program.
        private int RunIfStatement(string[] program, int lineNumber, string currentLine)
        {
            if (!ParseUsingIf(currentLine))
            {
                bool hasEndIf = false;
                int currentLineNumber = lineNumber;

                for (; lineNumber < program.Length; lineNumber++)
                {
                    if (currentLine.Contains("endif"))
                    {
                        hasEndIf = true;
                        break;
                    }
                }

                if (!hasEndIf)
                {
                    lineNumber = ++currentLineNumber;
                }
            }

            return lineNumber;
        }

        // Compiles and runs the code passed to it.
        private void ExecuteCode(string input)
        {
            if (Variables.Count != 0) Variables.Clear();
            Compile(input);
            Run();
        }

        /// <summary>
        /// Parse commands and programs.
        /// </summary>
        /// <param name="programCode">The text written in the program window.</param>
        /// <param name="commandInput">The text written in the command line window.</param>
        /// <remarks>
        /// Parses the program and command written in the program window and command window.
        /// </remarks>
        public void ParseProgram(string programCode, string commandInput)
        {
            var program = programCode.Split(new string[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            var input = commandInput;
            var cursor = 0;

            if (IsInputEmpty(input))
            {
                if (input.ToUpper().Equals("RUN"))
                {
                    for (int lineNumber = 0; lineNumber < program.Length; lineNumber++)
                    {
                        // If the line is not blank or null
                        string currentLine = program[lineNumber];

                        if (IsInputEmpty(currentLine))
                        {
                            if (LineContainsOperator(currentLine))
                            {
                                if (currentLine.Contains("endmethod"))
                                {
                                    lineNumber = cursor;
                                }
                                else if (currentLine.Contains("method"))
                                {
                                    lineNumber = ParseFunction(program, lineNumber, currentLine);
                                }
                                else if (currentLine.Contains("()"))
                                {
                                    cursor = RunFunction(ref lineNumber, currentLine);
                                }
                                else if (currentLine.Contains("while") && !currentLine.Contains("endwhile"))
                                {
                                    lineNumber = RunLoop(program, input, lineNumber, currentLine);
                                }
                                else if (currentLine.Contains("endif"))
                                {
                                    continue;
                                }
                                else if (currentLine.Contains("if"))
                                {
                                    lineNumber = RunIfStatement(program, lineNumber, currentLine);
                                }
                                else if (currentLine.Contains("="))
                                {
                                    ParseUsingLexer(currentLine);
                                }
                            }
                            else
                            {
                                ExecuteCode(currentLine);
                            }
                        }
                    }
                }
                else
                {
                    ExecuteCode(input);
                }
            }
            else
            {
                LogOutput(Color.Red, "[*] Error: Please provide a command to run");
            }
        }

        /// <summary>
        /// Gets a new Pen Object.
        /// </summary>
        /// <param name="color">Color of the Pen.</param>
        /// <param name="size">Width of the Pen.</param>
        /// <returns>Returns a new Pen Object with the specified Color and Width.</returns>
        public Pen GetPen(Color color, int size)
        {
            return new Pen(color, size);
        }

        // Call appropriate action according to the command and arguments passed to it.
        private void CommandParser(string command, string[] arguments)
        {
            if (IsShapeCommand(command))
            {
                DrawShape(command, arguments);
            }
            else if (command.ToUpper().Equals("DRAWTO"))
            {
                DrawLine(arguments);
            }
            else if (command.ToUpper().Equals("MOVETO"))
            {
                MovePen(arguments);
            }
            else if (command.ToUpper().Equals("PEN"))
            {
                SetPen(arguments);
            }
            else if (command.ToUpper().Equals("FILL"))
            {
                SetColorFill(arguments);
            }
            else if (command.ToUpper().Equals("RESET"))
            {
                ResetPen();
            }
            else if (command.ToUpper().Equals("CLEAR"))
            {
                ClearPanel();
            }
            else if (command.ToUpper().Equals("EXIT"))
            {
                Application.Exit();
            }
            else
            {
                LogOutput(Color.Red, $"[*] Error: Command {command} not found");
            }
        }
    }
}