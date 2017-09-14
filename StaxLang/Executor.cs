﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaxLang {
    /* To add:
     *     exponent
     *     log
     *     invert
     *     arbitrary range
     *     bitwise ops on int32 (&, |, ^, ~)
     *     -1 constant
     *  
     * To downgrade:
     *     head/tail
     *     min/max
     *     
     */

    public class Executor {
        public TextWriter Output { get; private set; }

        private double Index = 0; // loop iteration
        private dynamic X = 0.0; // register - default to numeric value of first input
        private dynamic Y = 0.0; // register - default to first input
        private dynamic Z = 0.0; // register - default to empty string
        private dynamic _ = 0.0; // implicit iterator

        public Executor(TextWriter output = null) {
            Output = output ?? Console.Out;
        }

        public void Run(string program, string[] input) {
            Z = S2A("");

            if (input.Length > 0) {
                Y = S2A(input[0]);
                if (double.TryParse(input[0], out var d)) X = d;
            }

            var stack = new Stack<dynamic>(input.Reverse().Select(S2A));
            try {
                Run(program, stack, new Stack<dynamic>());
                stack.Reverse().ToList().ForEach(e => Print(e));
            }
            catch (InvalidOperationException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        private void Run(string program, Stack<dynamic> stack, Stack<dynamic> side) {
            int ip = 0;

            while (ip < program.Length) {
                switch (program[ip]) {
                    case '`':
                        ++ip;
                        DoDump(program, ip, stack);
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        stack.Push(ParseNumber(program, ref ip));
                        break;
                    case ' ':
                    case '\n':
                    case '\r':
                        ++ip;
                        break;
                    case ';': // peek from side stack
                        ++ip;
                        stack.Push(side.Peek());
                        break;
                    case ',': // pop from side stack
                        ++ip;
                        stack.Push(side.Pop());
                        break;
                    case '[': // push to side stack
                        ++ip;
                        side.Push(stack.Pop());
                        break;
                    case '"': // "literal"
                        stack.Push(ParseString(program, ref ip));
                        break;
                    case '\'': // single char 'x
                        ++ip;
                        stack.Push(S2A(program.Substring(ip++, 1)));
                        break;
                    case '{': // block
                        stack.Push(ParseBlock(program, ref ip));
                        break;
                    case '}': // do-over
                        ip = 0;
                        break;
                    case '!': // not
                        ++ip;
                        stack.Push(IsTruthy(stack.Pop()) ? 0.0 : 1.0);
                        break;
                    case '+':
                        ++ip;
                        DoPlus(stack);
                        break;
                    case '-':
                        ++ip;
                        DoMinus(stack);
                        break;
                    case '*':
                        ++ip;
                        DoStar(stack, side);
                        break;
                    case '/':
                        ++ip;
                        DoSlash(stack);
                        break;
                    case '%':
                        ++ip;
                        DoPercent(stack);
                        break;
                    case '@': // read index
                        ++ip;
                        DoReadIndex(stack);
                        break;
                    case '&': // assign index
                        ++ip;
                        DoAssignIndex(stack);
                        break;
                    case ':': // copy 2nd
                        ++ip; {
                            var top = stack.Pop();
                            stack.Push(stack.Peek());
                            stack.Push(top);
                        }
                        break;
                    case '~':
                        ++ip;
                        stack.Peek().RemoveAt(0); // tail
                        break;
                    case '#': // to number
                        ++ip;
                        stack.Push(ToNumber(stack.Pop()));
                        break;
                    case '$': // to string
                        ++ip;
                        stack.Push(ToString(stack.Pop()));
                        break;
                    case '<':
                        ++ip;
                        DoLessThan(stack);
                        break;
                    case '>':
                        ++ip;
                        DoGreaterThan(stack);
                        break;
                    case '=':
                        ++ip;
                        DoEqual(stack);
                        break;
                    case 'v':
                        ++ip;
                        if (IsFloat(stack.Peek())) stack.Push(stack.Pop() - 1); // decrement
                        else if (IsArray(stack.Peek())) stack.Push(S2A(A2S(stack.Pop()).ToLower())); // lower
                        else throw new Exception("Bad type for v");
                        break;
                    case '^':
                        ++ip;
                        if (IsFloat(stack.Peek())) stack.Push(stack.Pop() + 1); // increment
                        else if (IsArray(stack.Peek())) stack.Push(S2A(A2S(stack.Pop()).ToUpper())); // uppper
                        else throw new Exception("Bad type for ^");
                        break;
                    case '(':
                        ++ip;
                        PadRight(stack);
                        break;
                    case ')':
                        ++ip;
                        PadLeft(stack);
                        break;
                    case ']': // singleton
                        ++ip;
                        stack.Push(new List<object> { stack.Pop() });
                        break;
                    case '?': // if
                        ++ip;
                        DoIf(stack, side);
                        break;
                    case 'a': // alphabet
                        ++ip;
                        stack.Push(S2A("abcdefghijklmnopqrstuvwxyz"));
                        break;
                    case 'A': // 10 (0xA)
                        ++ip;
                        stack.Push(10.0);
                        break;
                    case 'c': // copy
                        ++ip;
                        stack.Push(Clone(stack.Peek()));
                        break;
                    case 'C': // dig
                        ++ip;
                        stack.Push(Clone(stack.ElementAt((int)stack.Pop())));
                        break;
                    case 'd': // discard
                        ++ip;
                        stack.Pop();
                        break;
                    case 'e': // empty array
                        ++ip;
                        stack.Push(S2A(""));
                        break;
                    case 'E': // explode (de-listify)
                        ++ip;
                        DoExplode(stack);
                        break;
                    case 'f': // filter
                        ++ip;
                        DoFilter(stack, side);
                        break;
                    case 'F': // for loop
                        ++ip;
                        DoFor(stack, side);
                        break;
                    case 'h':
                        ++ip;
                        if (IsFloat(stack.Peek())) stack.Push(Math.Floor(stack.Pop() / 2)); // half
                        if (IsArray(stack.Peek())) stack.Push(stack.Pop()[0]); // head
                        break;
                    case 'H':
                        ++ip;
                        if (IsFloat(stack.Peek())) stack.Push(stack.Pop() * 2); // double
                        if (IsArray(stack.Peek())) stack.Push(stack.Peek()[stack.Pop().Count - 1]); // last
                        break;
                    case 'i': // iteration index
                        ++ip;
                        stack.Push(Index);
                        break;
                    case 'I': // get index
                        ++ip;
                        DoGetIndex(stack);
                        break;
                    case 'l': // listify string or n elements
                        ++ip;
                        DoListify(stack);
                        break;
                    case 'L': // listify stack
                        ++ip;
                        var newList = stack.ToList();
                        stack.Clear();
                        stack.Push(newList);
                        break;
                    case 'm': // do map
                        ++ip;
                        DoMap(stack, side);
                        break;
                    case 'M': // transpose
                        ++ip;
                        DoTranspose(stack);
                        break;
                    case 'n': // push newline
                        ++ip;
                        stack.Push(S2A(Environment.NewLine));
                        break;
                    case 'N': // negate
                        ++ip;
                        DoNegate(stack);
                        break;
                    case 'O': // order
                        ++ip;
                        DoOrder(stack, side);
                        break;
                    case 'p': // print inline
                        ++ip;
                        Print(stack.Pop(), false);
                        break;
                    case 'P': // print
                        ++ip;
                        Print(stack.Pop());
                        break;
                    case 'q': // min
                        ++ip;
                        stack.Push(((List<object>)stack.Pop()).Min() ?? double.MaxValue);
                        break;
                    case 'Q': // max
                        ++ip;
                        stack.Push(((List<object>)stack.Pop()).Max() ?? double.MinValue);
                        break;
                    case 'r': // 0 range
                        ++ip;
                        if (IsFloat(stack.Peek())) stack.Push(Enumerable.Range(0, (int)stack.Pop()).Select(Convert.ToDouble).Cast<object>().ToList());
                        else if (IsArray(stack.Peek())) {
                            var result = new List<object>(stack.Pop());
                            result.Reverse();
                            stack.Push(result); 
                        }
                        else throw new Exception("Bad type for r");
                        break;
                    case 'R': // 1 range
                        ++ip;
                        stack.Push(Enumerable.Range(1, (int)stack.Pop()).Select(Convert.ToDouble).Cast<object>().ToList());
                        break;
                    case 's': // swap
                        ++ip; {
                            var top = stack.Pop();
                            var bottom = stack.Pop();
                            stack.Push(top);
                            stack.Push(bottom);
                        }
                        break;
                    case 'S': // space
                        ++ip;
                        stack.Push(S2A(" "));
                        break;
                    case 't': // trim left
                        ++ip;
                        stack.Push(S2A(A2S(stack.Pop()).TrimStart()));
                        break;
                    case 'T': // trim right
                        ++ip;
                        stack.Push(S2A(A2S(stack.Pop()).TrimEnd()));
                        break;
                    case 'u': // unique
                        ++ip;
                        DoUnique(stack);
                        break;
                    case 'w': // while
                        ++ip;
                        DoWhile(stack, side);
                        break;
                    case '_':
                        ++ip;
                        stack.Push(Clone(_));
                        break;
                    case 'x': // read;
                        ++ip;
                        stack.Push(Clone(X));
                        break;
                    case 'X': // write
                        ++ip;
                        X = stack.Peek();
                        break;
                    case 'y': // read;
                        ++ip;
                        stack.Push(Clone(Y));
                        break;
                    case 'Y': // write
                        ++ip;
                        Y = stack.Peek();
                        break;
                    case 'z': // read;
                        ++ip;
                        stack.Push(Clone(Z));
                        break;
                    case 'Z': // write
                        ++ip;
                        Z = stack.Peek();
                        break;
                    case '|': // extended operations
                        switch (program[++ip]) {
                            case '/': // div mod
                                ++ip; {
                                    double b = stack.Pop();
                                    double a = stack.Pop();
                                    stack.Push(Math.Floor(a / b));
                                    stack.Push(a % b);
                                }
                                break;
                            case 'b': // base convert
                                ++ip;
                                DoBaseConvert(stack);
                                break;
                            case 'f': // prime factorize
                                ++ip;
                                stack.Push(PrimeFactors(stack.Pop()));
                                break;
                            case 'g':
                                ++ip;
                                DoGCD(stack);
                                break;
                            case 'r': // regex replace
                                ++ip;
                                DoReplace(stack, side);
                                break;
                            case 's':
                                ++ip;
                                DoSum(stack);
                                break;
                            case 't':
                                ++ip;
                                DoTranslate(stack);
                                break;
                            default: throw new Exception($"Unknown extended character '{program[ip]}'");
                        }
                        break;
                    default: throw new Exception($"Unknown character '{program[ip]}'");
                }
            }
        }

        private void DoReplace(Stack<dynamic> stack, Stack<dynamic> side) {
            var replace = stack.Pop();
            var search = stack.Pop();
            var text = stack.Pop();

            if (IsArray(text) && IsArray(search)) {
                string ts = A2S(text), ss = A2S(search);
                if (IsArray(replace)) {
                    stack.Push(S2A(ts.Replace(ss, A2S(replace))));
                    return;
                }
                else if (IsBlock(replace)) {
                    var initial = _;
                    string result = "";
                    var matches = Regex.Matches(ts, ss);
                    int consumed = 0;
                    foreach (Match match in matches) {
                        result += ts.Substring(consumed, match.Index - consumed);
                        stack.Push(_ = S2A(match.Value));
                        Run(replace.Program, stack, side);
                        result += A2S(stack.Pop());
                        consumed = match.Index + match.Length;
                    }
                    result += ts.Substring(consumed);
                    stack.Push(S2A(result));
                    _ = initial;
                    return;
                }
                else {
                    throw new Exception("Bad types for replace");
                }
            }
            else {
                throw new Exception("Bad types for replace");
            }
        }

        private void DoTranslate(Stack<dynamic> stack) {
            var translation = stack.Pop();
            var input = stack.Pop();

            if (IsArray(input) && IsArray(translation)) {
                var result = new List<object>();
                var map = new Dictionary<char, char>();
                var ts = A2S(translation);

                for (int i = 0; i < ts.Length; i += 2) map[ts[i]] = ts[i + 1];
                foreach (var e in A2S(input)) result.AddRange(S2A("" + (map.ContainsKey(e) ? map[e] : e)));
                stack.Push(result);
            }
            else {
                throw new Exception("Bad types for translate");
            }
        }

        private void DoNegate(Stack<dynamic> stack) {
            var arg = stack.Pop();

            if (IsFloat(arg)) {
                stack.Push(-arg);
            }
            else {
                throw new Exception("Bad type for negate");
            }
        }

        private void DoDump(string program, int ip, Stack<dynamic> stack) {
            int i = 0;
            this.Output.WriteLine("program: {0}", program.Substring(ip));
            foreach (var e in stack) {
                var formatted = e;
                if (IsArray(e)) {
                    formatted = '"' + A2S(e) + '"';
                    if (((string)formatted).Any(char.IsControl)) formatted = '[' + string.Join(", ", e) + ']';
                }
                this.Output.WriteLine("{0:##0}: {1}", i++, formatted);
            }
            this.Output.WriteLine();
        }

        private void DoUnique(Stack<dynamic> stack) {
            var arg = stack.Pop();

            if (IsArray(arg)) {
                var result = new List<object>();
                var seen = new HashSet<object>();
                foreach (var e in arg) {
                    var key = IsArray(e) ? A2S(e) : e;
                    if (!seen.Contains(key)) {
                        result.Add(e);
                        seen.Add(key);
                    }
                }
                stack.Push(result);
            }
            else {
                throw new Exception("Bad type for unique");
            }
        }

        private void DoLessThan(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            stack.Push(a < b ? 1.0 : 0.0);
        }

        private void DoGreaterThan(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            stack.Push(a > b ? 1.0 : 0.0);
        }

        private void DoOrder(Stack<dynamic> stack, Stack<dynamic> side) {
            var arg = stack.Pop();

            if (IsArray(arg)) {
                arg.Sort();
                stack.Push(arg);
            }
            else if (IsBlock(arg)) {
                var list = stack.Pop();
                var combined = new List<(object val, IComparable key)>();

                var initial = (_, Index);
                Index = 0;
                foreach (var e in list) {
                    _ = e;
                    stack.Push(e);
                    Run(arg.Program, stack, side);
                    combined.Add((e, stack.Pop()));
                    ++Index;
                }
                (_, Index) = initial;

                stack.Push(combined.OrderBy(e => e.key).Select(e => e.val).ToList());
            }
            else {
                throw new Exception("Bad types for order");
            }
        }

        private void DoBaseConvert(Stack<dynamic> stack) {
            int @base = (int)stack.Pop();
            var number = stack.Pop();

            if (IsFloat(number)) {
                long n = (long)number;
                string result = "";
                while (n > 0) {
                    result = "0123456789abcdefghijklmnopqrstuvwxyz"[(int)(n % @base)] + result;
                    n /= @base;
                }
                if (result == "") result = "0";
                stack.Push(S2A(result));
            }
            else if (IsArray(number)) {
                string s = A2S(number).ToLower();
                double result = 0;
                foreach (var c in s) result = result * @base + "0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(c);
                stack.Push(result);
            }
            else {
                throw new Exception("Bad types for base convert");
            }
        }

        private void DoGetIndex(Stack<dynamic> stack) {
            var element = stack.Pop();
            var list = stack.Pop();

            if (IsArray(list)) {
                for (int i = 0; i < list.Count; i++) {
                    if (IsArray(element)) {
                        bool match = true;
                        for (int j = 0; j < element.Count; j++) {
                            if (!AreEqual(list[i + j], element[j])) {
                                match = false;
                                break;
                            }
                        }
                        if (match) {
                            stack.Push((double)i);
                            return;
                        }
                    }
                    else if (AreEqual(element, list[i])) {
                        stack.Push((double)i);
                        return;
                    }
                }
                stack.Push(-1.0);
                return;
            }
            else {
                throw new Exception("Bad types for get-index");
            }
        }

        private void DoExplode(Stack<dynamic> stack) {
            var arg = stack.Pop();

            if (IsArray(arg)) {
                foreach (var item in arg) stack.Push(item);
            }
        }

        private void DoAssignIndex(Stack<dynamic> stack) {
            var element = stack.Pop();
            var index = (int)stack.Pop();
            var list = stack.Pop();

            if (IsArray(list)) {
                var result = new List<object>(list);
                index = ((index % result.Count) + result.Count) % result.Count;
                result[index] = element;
                stack.Push(result);
            }
            else {
                throw new Exception("Bad type for index assign");
            }

        }

        private void DoReadIndex(Stack<dynamic> stack) {
            int index = (int)stack.Pop();
            var list = stack.Pop();

            if (IsArray(list)) {
                index %= list.Count;
                index += list.Count;
                index %= list.Count;
                stack.Push(list[index]);
            }
            else {
                throw new Exception("Bad type for index");
            }
        }

        private void DoListify(Stack<dynamic> stack) {
            var arg = stack.Pop();
            if (IsFloat(arg)) {
                var list = new List<object>();
                for (int i = 0; i < arg; i++) list.Insert(0, stack.Pop());
                stack.Push(list);
            }
            else {
                throw new Exception("Bad type for listify");
            }
        }

        private void PadLeft(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsFloat(a)) (a, b) = (b, a);

            if (IsArray(a) && IsFloat(b)) {
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.InsertRange(0, Enumerable.Repeat((object)32.0, (int)b - a.Count));
                if (a.Count > b) a.RemoveRange(0, a.Count - (int)b);
                stack.Push(a);
            }
            else {
                throw new Exception("bad types for padleft");
            }
        }

        private void PadRight(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsFloat(a)) (a, b) = (b, a);

            if (IsArray(a) && IsFloat(b)) {
                a = new List<object>(a);
                if (b < 0) b += a.Count;
                if (a.Count < b) a.AddRange(Enumerable.Repeat((object)32.0, (int)b - a.Count));
                if (a.Count > b) a.RemoveRange((int)b, a.Count - (int)b);
                stack.Push(a);
            }
            else {
                throw new Exception("bad types for padright");
            }
        }

        private void DoWhile(Stack<dynamic> stack, Stack<dynamic> side) {
            Block block = stack.Pop();
            do Run(block.Program, stack, side); while (IsTruthy(stack.Pop()));
        }

        private void DoIf(Stack<dynamic> stack, Stack<dynamic> side) {
            bool condition = IsTruthy(stack.Pop());

            var then = stack.Pop();
            if (condition) {
                stack.Pop();
                stack.Push(then);
            }

            if (IsBlock(stack.Peek())) Run(stack.Pop().Program, stack, side);
        }

        private object ToNumber(dynamic arg) {
            if (IsArray(arg)) {
                return double.Parse(A2S(arg));
            }
            throw new Exception("Bad type for ToNumber");
        }

        private object ToString(dynamic arg) {
            if (IsFloat(arg)) {
                return S2A(arg.ToString());
            }
            throw new Exception("Bad type for ToString");
        }

        private void Print(object arg, bool newline = true) {
            if (IsArray(arg)) {
                Print(A2S((List<object>)arg), newline);
                return;
            }

            if (newline) Output.WriteLine(arg);
            else Output.Write(arg);
        }

        private void DoFilter(Stack<dynamic> stack, Stack<dynamic> side) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(a) && IsBlock(b)) {
                var initial = (_, Index);
                long i = 0;
                var result = new List<object>();
                foreach (var e in a) {
                    stack.Push(e);
                    _ = e;
                    Index = i++;
                    Run(b.Program, stack, side);
                    if (IsTruthy(stack.Pop())) result.Add(e);
                }
                stack.Push(result);
                (_, Index) = initial;
            }
            else {
                throw new Exception("Bad types for filter");
            }
        }

        private void DoFor(Stack<dynamic> stack, Stack<dynamic> side) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(a) && IsBlock(b)) {
                var initial = (_, Index);
                long i = 0;
                foreach (var e in a) {
                    stack.Push(e);
                    _ = e;
                    Index = i++;
                    Run(b.Program, stack, side);
                }
                (_, Index) = initial;
            }
            else {
                throw new Exception("Bad types for for");
            }
        }

        private void DoTranspose(Stack<dynamic> stack) {
            var list = stack.Pop();
            var result = new List<object>();

            int? count = null;
            foreach (var series in list) count = Math.Min(count ?? int.MaxValue, series.Count);

            for (int i = 0; i < (count ?? 0); i++) {
                var tuple = new List<object>();
                foreach (var series in list) tuple.Add(series[i]);
                result.Add(tuple);
            }

            stack.Push(result);
        }

        private void DoMap(Stack<dynamic> stack, Stack<dynamic> side) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(b)) (a, b) = (b, a);

            if (IsArray(a) && IsBlock(b)) {
                var initial = (_, Index);
                long i = 0;
                var result = new List<object>();
                foreach (var e in a) {
                    stack.Push(e);
                    _ = e;
                    Index = i++;
                    Run(b.Program, stack, side);
                    result.Add(stack.Pop());
                }
                stack.Push(result);
                (_, Index) = initial;
            }
            else {
                throw new Exception("bad type for map");
            }
        }

        private void DoPlus(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsFloat(a) && IsFloat(b)) {
                stack.Push(a + b);
            }
            else if (IsArray(a) && IsArray(b)) {
                var result = new List<object>(a);
                result.AddRange(b);
                stack.Push(result);
            }
            else if (IsArray(a)) {
                var result = new List<object>(a);
                result.Add(b);
                stack.Push(result);
            }
            else if (IsArray(b)) {
                var result = new List<object> { a };
                result.AddRange(b);
                stack.Push(result);
            }
            else {
                throw new Exception("Bad types for +");
            }
        }

        private void DoMinus(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsArray(a) && IsArray(b)) {
                a.RemoveAll((Predicate<object>)(e => b.Contains(e)));
                stack.Push(a);
            }
            else if (IsFloat(a) && IsFloat(b)) {
                stack.Push(a - b);
            }
            else {
                throw new Exception("Bad types for -");
            }
        }

        private void DoSlash(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsFloat(a) && IsFloat(b)) {
                stack.Push(Math.Floor(a / b));
            }
            else if (IsArray(a) && IsFloat(b)) {
                var result = new List<object>();
                for (int i = 0; i < a.Count; i += (int)b) {
                    result.Add(((IEnumerable<object>)a).Skip(i).Take((int)b).ToList());
                }
                stack.Push(result);
            }
            else if (IsArray(a) && IsArray(b)) {
                string[] strings = A2S(a).Split(new string[] { A2S(b) }, 0);
                stack.Push(strings.Select(s => S2A(s) as object).ToList());
            }
            else {
                throw new Exception("Bad types for /");
            }
        }

        private void DoPercent(Stack<dynamic> stack) {
            var b = stack.Pop();
            if (IsArray(b)) {
                stack.Push((double)b.Count);
                return;
            }

            var a = stack.Pop();

            if (IsFloat(a) && IsFloat(b)) {
                stack.Push(a % b);
            }
            else {
                throw new Exception("Bad types for %");
            }
        }

        private void DoStar(Stack<dynamic> stack, Stack<dynamic> side) {
            var b = stack.Pop();
            var a = stack.Pop();

            if (IsFloat(a)) (a, b) = (b, a);

            if (IsFloat(b)) {
                if (IsArray(a)) {
                    var result = new List<object>();
                    for (int i = 0; i < b; i++) result.AddRange(a);
                    stack.Push(result);
                    return;
                }
                else if (IsBlock(a)) {
                    var initial = Index;
                    for (Index = 0; Index < b; Index++) Run(a.Program, stack, side);
                    Index = initial;
                    return;
                }
            }

            if (IsArray(a) && IsArray(b)) {
                string result = "";
                string joiner = A2S(b);
                foreach (var e in a) {
                    if (result != "") result += joiner;
                    result += IsFloat(e) ? e : A2S(e);
                }
                stack.Push(S2A(result));
                return;
            }

            if (IsFloat(a) && IsFloat(b)) {
                stack.Push(a * b);
                return;
            }

            throw new Exception("Bad types for *");
        }

        private void DoEqual(Stack<dynamic> stack) {
            var b = stack.Pop();
            var a = stack.Pop();
            stack.Push(AreEqual(a, b) ? 1.0 : 0.0);
        }

        #region support
        private bool AreEqual(dynamic a, dynamic b) {
            if (IsFloat(a) && IsFloat(b)) return a == b;
            if (IsArray(a) && IsArray(b)) return Enumerable.SequenceEqual(a, b);
            throw new Exception("Bad types for =");
        }

        private bool IsFloat(object b) => b is double;
        private bool IsArray(object b) => b is List<object>;
        private bool IsBlock(object b) => b is Block;
        private bool IsTruthy(dynamic b) => (IsFloat(b) && b != 0) || (IsArray(b) && b.Count != 0);

        private List<object> S2A(string arg) => arg.ToCharArray().Select(c => (double)(int)c as object).ToList();
        private string A2S(List<object> arg) {
            return string.Concat(arg.Select(e => IsFloat(e)
                ? ((char)(double)e).ToString()
                : A2S((List<object>)e)));
        }
        private object Clone(dynamic o) => IsArray(o) ? new List<object>(o) : o;

        private double ParseNumber(string program, ref int ip) {
            long value = 0;

            while (ip < program.Length && char.IsDigit(program[ip]))
                value = value * 10 + program[ip++] - '0';

            if (ip < program.Length && program[ip] == '.') {
                ++ip;
                return double.Parse(value + "." + ParseNumber(program, ref ip));
            }
            return value;
        }

        private List<object> ParseString(string program, ref int ip) {
            string result = "";
            while (ip < program.Length - 1 && program[++ip] != '"') {
                if (program[ip] == '`') ++ip;
                result += program[ip];
            }
            if (ip < program.Length) ++ip; // final quote
            return S2A(result);
        }

        private Block ParseBlock(string program, ref int ip) {
            int depth = 0;
            int start = ip + 1;
            do {
                if (program[ip] == '|' || program[ip] == '\'') ip += 2; // extended and char

                if (program[ip] == '"') ParseString(program, ref ip);

                if (program[ip] == '{') ++depth;
                if (program[ip] == '}' && --depth == 0) return new Block(program.Substring(start, ip++ - start));

                // shortcut block terminators
                if ("wmfFO".Contains(program[ip]) && --depth == 0) return new Block(program.Substring(start, ip - start));
            } while (++ip < program.Length);
            return new Block(program.Substring(start));
        }
        #endregion

        #region extended
        private List<object> PrimeFactors(double n) {
            var result = new List<object>();
            double d = 2;
            n = Math.Floor(n);
            while (n > 1) {
                while (n % d == 0) {
                    result.Add(d);
                    n /= d;
                }
                ++d;
            }
            return result;
        }

        private void DoSum(Stack<dynamic> stack) {
            var list = stack.Pop();
            double result = 0;

            foreach (var e in list) result += e;

            stack.Push(result);
        }

        private void DoGCD(Stack<dynamic> stack) {
            var b = stack.Pop();

            if (IsArray(b)) {
                double result = 0;
                foreach (double e in b) result = GCD(result, e);
                stack.Push(result);
                return;
            }

            var a = stack.Pop();
            if (IsFloat(a) && IsFloat(b)) {
                stack.Push(GCD(a, b));
                return;
            }

            throw new Exception("Bad types for GCD");
        }

        private double GCD(double a, double b) {
            if (a == 0 || b == 0) return a + b;
            return GCD(b, a % b);
        }
        #endregion
    }
}