using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms.VisualStyles;

namespace GraphFunc.Menus
{
    public class FuncParser
    {
        private readonly string _code;

        private int pos = 0;

        public FuncParser(string code)
        {
            _code = code;
        }

        private void ConsumeWhite()
        {
            while (pos < _code.Length && _code[pos] == ' ')
                pos++;
        }

        public Func<double, double> Parse()
            => ParseAdd();

        public Func<double, double> ParseAdd()
        {
            ConsumeWhite();
            var left = ParseMult();
            ConsumeWhite();
            if (pos == _code.Length || _code[pos] == ')')
                return left;
            var ch = _code[pos];
            pos++;
            ConsumeWhite();
            var right = ParseAdd();
            switch (ch)
            {
                case '+':
                    return x => left(x) + right(x);
                case '-':
                    return x => left(x) - right(x);
                default:
                    throw new Exception("");
            }
        }
        public Func<double, double> ParseMult()
        {
            ConsumeWhite();
            var left = ParsePrimitive();
            ConsumeWhite();
            if (pos == _code.Length || _code[pos] == ')' || (_code[pos] != '*' && _code[pos] != '/' && _code[pos] != '^'))
                return left;
            var ch = _code[pos];
            pos++;
            ConsumeWhite();
            var right = ParseMult();
            switch (ch)
            {
                case '*':
                    return x => left(x) * right(x);
                case '/':
                    return x => left(x) / right(x);
                case '^':
                    return x => Math.Pow(left(x), right(x));
                default:
                    throw new Exception("");
            }
        }

        public Func<double, double> ParsePrimitive()
        {
            ConsumeWhite();
            var ch = _code[pos];
            if (ch > '0' && ch < '9')
                return ParseDouble();
            if (ch == '(')
                return ParsePar();
            if (ch > 'a' && ch < 'z' || ch > 'A' && ch < 'Z')
                return ParseId();
            throw new Exception();
        }

        public Func<double, double> ParseDouble()
        {
            var str = new StringBuilder();
            while (pos < _code.Length && (_code[pos] > '0' && _code[pos] < '9' || _code[pos] == '.'))
                str.Append(_code[pos++]);
            var res = double.Parse(str.ToString(), new CultureInfo("en-US"));
            return x => res;
        }

        public Func<double, double> ParsePar()
        {
            pos++;
            var expr = Parse();
            ConsumeWhite();
            pos++;
            return expr;
        }

        private static Dictionary<String, Func<double, double>> _funcs = new Dictionary<string, Func<double, double>>()
        {
            ["log"] = Math.Log,
            ["exp"] = Math.Exp,
        };

        public Func<double, double> ParseId()
        {
            var str = new StringBuilder();
            while (pos < _code.Length && IsLetter(_code[pos]))
                str.Append(_code[pos++]);
            var id = str.ToString();
            switch (id)
            {
                case "x":
                    return x => x;
                default:
                    var par = ParsePar();
                    var func = _funcs[id];
                    return x => func(par(x));
            }
        }

        private static bool IsLetter(char ch)
            => ch > 'a' && ch < 'z' || ch > 'A' && ch < 'Z';
    }
}