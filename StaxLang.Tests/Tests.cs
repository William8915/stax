﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;

namespace StaxLang.Tests {
    [TestClass]
    public class Tests {
        internal static string[] MultiLineStrip(string arg) {
            var result = arg.Trim().Split('\n')
                .Select(line => line.TrimStart())
                .ToArray();

            if (result[0] == "") return Array.Empty<string>();
            return result;
        }

        internal void RunProgram(string source, string input = "", params string[] expected) {
            var writer = new StringWriter();

            new Executor(writer).Run(source, MultiLineStrip(input));

            string actual = writer.ToString();
            var expectedJoined = string.Concat(expected.Select(e => e + Environment.NewLine));
            Assert.AreEqual(expectedJoined, actual);
        }

        [TestMethod]
        public void IntLiteral() {
            RunProgram("123", "", "123");
        }

        [TestMethod]
        public void DoubleLiteral() {
            RunProgram("1.23", "", "1.23");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TooManyDecimals() {
            RunProgram("1.23.45", "");
        }

        [TestMethod]
        public void StringLiteral() {
            RunProgram("\"hello\"", "", "hello");
        }

        [TestMethod]
        public void MultipleOutputs() {
            RunProgram("123\"hello\"", "", "123", "hello");
        }

        [TestMethod]
        public void UnterminatedString() {
            RunProgram("\"asdf", "", "asdf");
        }

        [TestMethod]
        public void EscapedString() {
            RunProgram("\"x`\"x\"", "", "x\"x");
        }

        [TestMethod]
        public void AdditionTest() {
            RunProgram("2 3+", "", "5");
        }

        [TestMethod]
        public void ConcatTest() {
            RunProgram("\"hello\" \"world\"+", "", "helloworld");
        }

        [TestMethod]
        public void RangeTest() {
            RunProgram("5rE", "", "0", "1", "2", "3", "4");
            RunProgram("5RE", "", "1", "2", "3", "4", "5");
        }

        [TestMethod]
        public void StarOverloadsTest() {
            RunProgram("2r3*E", "", "0", "1", "0", "1", "0", "1");
            RunProgram("4\"abc\"*", "", "abcabcabcabc");
            RunProgram("{1}3*", "", "1", "1", "1");
        }

        [TestMethod]
        public void CopyTest() {
            RunProgram("1c", "", "1", "1");
        }

        [TestMethod]
        public void InputTest() {
            RunProgram("#2*", "3", "6");
        }

        [TestMethod]
        public void SquaresTest() {
            RunProgram("5R{c*mE", "", "1", "4", "9", "16", "25");
            RunProgram("5R{c*PF", "", "1", "4", "9", "16", "25");
        }

        [TestMethod]
        public void WhileTest() {
            RunProgram("3{cvcw", "", "3", "2", "1", "0");
        }

        [TestMethod]
        public void CollatzTest() {
            RunProgram("#{ch1C3*^2C2%?cv}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("#{cXhx3*^x2%?cv}w", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("#{cXhx3*^x2%?cvw", "3", "3", "10", "5", "16", "8", "4", "2", "1");
            RunProgram("#{cXhx3*^2lx@cvw", "3", "3", "10", "5", "16", "8", "4", "2", "1");
        }

        [TestMethod]
        public void DropFirstTest() {
            RunProgram("4R~n*", "", "2", "3", "4");
        }

        [TestMethod]
        public void DivisorsTest() {
            RunProgram("#XvR{xs%!fE", "12", "1", "2", "3", "4", "6");
            RunProgram("#XvR{xs%!f%v", "12", "4");
        }

        [TestMethod]
        public void PrimeTest() {
            RunProgram("2Xd{xvR~{xs%!f%!{x}*x^X20-w", "", "2", "3", "5", "7", "11", "13", "17", "19");
            RunProgram("d2Zd{zvR~{zs%!f%!{z}*z^Zx-w", "15", "2", "3", "5", "7", "11", "13");
            RunProgram("#R~{|f%1=fE", "15", "2", "3", "5", "7", "11", "13");
        }

        [TestMethod]
        public void TriangleTest() {
            RunProgram("#R{'**mE", "4", "*", "**", "***", "****");
        }

        [TestMethod]
        public void FactorialTest() {
            RunProgram("1s#R{*cmd", "5", "120");
            RunProgram("1s#R{*F", "5", "120");
            RunProgram("1s#X{*xvXxwd", "5", "120");
            RunProgram("d1xR{*F", "5", "120");
        }

        [TestMethod]
        public void ListifyTest() {
            RunProgram("1 2 3 LrE", "", "1", "2", "3");
        }

        [TestMethod]
        public void ReverseTest() {
            RunProgram("r", "asdf", "fdsa");
        }

        [TestMethod]
        public void EqualTest() {
            RunProgram("1 2=", "", "0");
            RunProgram("1 1=", "", "1");
        }

        [TestMethod]
        public void DiagonalTest() {
            RunProgram(@"#R{'\)mE", "3", @"\", @" \", @"  \");
            RunProgram(@"#R{'\)PF", "3", @"\", @" \", @"  \");
        }

        [TestMethod]
        public void BigVTest() {
            RunProgram(@"#XR{c'\)sxs-H^'/)+PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{c'\)pxs-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{'\)px_-H^'/)PF", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#XR{'\)x_-H^'/)+mE", "3", @"\    /", @" \  /", @"  \/");
            RunProgram(@"#R{' xH*i'\&sN'/&TmE", "3", @"\    /", @" \  /", @"  \/");
        }

        [TestMethod]
        public void IndexAssignMethod() {
            RunProgram("3'x&", "12345", "123x5");
        }

        [TestMethod]
        public void BigXTest() {
            RunProgram(@"#XR{'\)x_v-H'/)+mEx^'X)xRr{'/)x_v-H'\)+mE", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"#H^Xrr{d' x*i'\&_""/X""i_=@&TmE", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"#H^Xrr{dSx*i'\&_""/X""i_=@&TmE", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
            RunProgram(@"#{xH^S*i'\&iNv'/&c[TP}*'Xx^)P{,rTP}x*", "2", @"\   /", @" \ /", @"  X", @" / \", @"/   \");
        }

        [TestMethod]
        public void SplitTest() {
            RunProgram("{]mE", "asdf\nxxx", "xxx", "a", "s", "d", "f");
            RunProgram("1/E", "asdf\nxxx", "xxx", "a", "s", "d", "f");
        }

        [TestMethod]
        public void PairSpacingTest() {
            RunProgram("2/' *", "sequencespacingtest", "se qu en ce sp ac in gt es t");
            RunProgram("2/S*", "sequencespacingtest", "se qu en ce sp ac in gt es t");
        }

        [TestMethod]
        public void PairSpacing2Test() {
            RunProgram("2/{t' s+m\"\"*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{t' s+me*t", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tm' *", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
            RunProgram("2/{tmS*", "Sequence spacing sample", "Se qu en ce s pa ci ng s am pl e");
        }

        [TestMethod]
        public void PairSpacing3Test() {
            RunProgram("' /{2/' *m' *", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("' Z/{2/z*mz*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
            RunProgram("S/{2/S*mS*", "Sequence spacing demonstration", "Se qu en ce sp ac in g de mo ns tr at io n");
        }

        [TestMethod]
        public void DigitTallyTest() {
            RunProgram("d10r{$ys/%v$me*", "176093677603", "2102003301");
            RunProgram("L{Xd10r{$xs/%v$me*F", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Xd10r{$xs/%v$me*P}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%v$me*Pd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%v$pFePd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$1Cs/%vpFePd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
            RunProgram("Ar{$:/%vpFePd}", "27204322879364\n82330228112748", "1042201211", "1242100130");
        }

        [TestMethod]
        public void SmileyTest() {
            RunProgram("':\":-\"{c')+}3*", "", ":", ":-", ":-)", ":-))", ":-)))");
            RunProgram("':c'-+{c')+}3*", "", ":", ":-", ":-)", ":-))", ":-)))");
        }

        [TestMethod]
        public void DeleteBlanksTest() {
            RunProgram("L{fE", "1\n\n2\n\n\n3", "1", "2", "3");
        }

        [TestMethod]
        public void ScopeTest() {
            RunProgram("2R{d 9R{dF _F", "", "1", "2");
            RunProgram("2R{d 9R{dF iF", "", "0", "1");
        }

        [TestMethod]
        public void AllDigitsTest() {
            RunProgram("Ar{$m'A{ch^1l}25*26l+e*", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("Are*'A{ch^1l}25*26le*+", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("Ar'A{ch^1l}25*26l+e*", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("ArE'A{ch^1l}25*Lre*", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("Ar26r{65+1lm+e*", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            RunProgram("Are*a^+", "", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        [TestMethod]
        public void FindIndexTest() {
            RunProgram("Sa+sIP}", "d\nz\n_", "4", "26", "-1");
        }

        [TestMethod]
        public void BaseConversionTest() {
            RunProgram("#16|bc4|b", "291", "123", "27");
        }

        [TestMethod]
        public void ShiftingDigitsTest() {
            https://codegolf.stackexchange.com/questions/141225/shifting-digits

            RunProgram("s#Xd{Are*a+YsI^x%ys@]pFe", "5f69\n16", "607a");
            RunProgram("s#Xd{]x|b^x%x|bpFe", "5f69\n16", "607a");
        }

        [TestMethod]
        public void FibTest() {
            RunProgram("#vv1s1s{c2C+}*", "7", "1", "1", "2", "3", "5", "8", "13");
            RunProgram("d1cxvv{c2C+}*", "7", "1", "1", "2", "3", "5", "8", "13");
        }

        [TestMethod]
        public void EvenLinesTest() {
            http://golf.shinh.org/p.rb?even+lines
            RunProgram("dP}", "qw\nas\nzx\nwe", "as", "we");
        }

        [TestMethod]
        public void SortCharsTest() {
            http://golf.shinh.org/p.rb?sort+characters
            RunProgram("O", "Hello, world!", " !,Hdellloorw");
        }

        [TestMethod]
        public void RegularTest() {
            http://golf.shinh.org/p.rb?Hamming+Numbers
            RunProgram("20R{5R~{*{_/c_%!wF1=fE", "", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12", "15", "16", "18", "20");
            RunProgram("#c*R{5R~{*{_/c_%!wF1=fx(E", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{H|fQ6/!fx(E", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{|fQ6<fx(E", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{H|fH6<fx(E", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
            RunProgram("#c*R{|f5R-!fx(E", "10", "1", "2", "3", "4", "5", "6", "8", "9", "10", "12");
        }

        [TestMethod]
        public void DeleteLastLineTest() {
            http://golf.shinh.org/p.rb?delete+last+line
            RunProgram("Lc%v(E", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("Lr~rE", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("L1N(E", "foo\nbar\nbaz", "foo", "bar");
            RunProgram("LEd", "foo\nbar\nbaz", "foo", "bar");
        }

        [TestMethod]
        public void RotateLinesTest() {
            http://golf.shinh.org/p.rb?rotate+lines
            RunProgram("[LE,", "foo\nbar\nbaz", "bar", "baz", "foo");
        }

        [TestMethod]
        public void FizzBuzzTest() {
            RunProgram("#R{3%!\"Fizz\"*_5%!\"Buzz\"*+c!_$*+mE", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
            RunProgram("#R{_3%!\"Fizz\"*_5%!\"Buzz\"*+c?mE", "15", "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz");
        }

        [TestMethod]
        public void GCDTest() {
            http://golf.shinh.org/p.rb?Greatest+Common+Divisor
            RunProgram("#s#|g", "42\n56", "14");
        }

        [TestMethod]
        public void LCMTest() {
            http://golf.shinh.org/p.rb?Least+Common+Multiple
            RunProgram("' /{#mXE*xE|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("' /E#Xs#Y*xy|g/P}", "195 548\n965 981", "106860", "946665");
            RunProgram("S/{#mXE*xE|g/P}", "195 548\n965 981", "106860", "946665");
        }

        [TestMethod]
        public void DeleteDupesTest() {
            http://golf.shinh.org/p.rb?delete+duplicate+lines
            RunProgram("LuE", "a\nb\na\nc", "a", "b", "c");
        }

        [TestMethod]
        public void PalindromizeTest() {
            http://golf.shinh.org/p.rb?palindromize
            RunProgram("c{dci(r:+m{cr=fhPd}", "test\nNISIOISIN", "testset", "NISIOISIN");
            RunProgram("X{dxxi(r+m{cr=fhP}", "test\nNISIOISIN", "testset", "NISIOISIN");
        }

        [TestMethod]
        public void BracketMatching() {
            http://golf.shinh.org/p.rb?Bracket+Matching
            // x - bracket type
            // y - input
            // z - temp storage for outer i
            RunProgram("0[{{\"_)}]\",@=!{\"failed at: \"pyPeh}*}{dx[}\"({[\"3CI^X?y~YdF\"yes", ")", "failed at: )");
            RunProgram("0[{{\"_)}]\",@=!{\"failed at: \"pyPeh}*}{dx[}\"({[\"3CI^X?y~YdF\"yes", "()", "yes");
            RunProgram("0[{{\"_)}]\",@=!{\"failed at: \"pyPeh}*}{dx[}\"({[\"3CI^X?y~YdF\"yes", "{()[]}", "yes");
            RunProgram("0[{{\"_)}]\",@=!{\"failed at: \"pyPeh}*}{dx[}\"({[\"3CI^X?y~YdF\"yes", "()}()", "failed at: }()");
        }

        [TestMethod]
        public void GoogleTest() {
            RunProgram("#'o*'gs+\"gle\"+", "2", "google");
            RunProgram("#'o*'gs+\"gle\"+", "10", "goooooooooogle");
            RunProgram("'gp#'o*p\"gle", "10", "goooooooooogle");
        }

        [TestMethod]
        public void SummationTest() {
            http://golf.shinh.org/p.rb?Summation
            RunProgram(":#c^*hPd}", "1\n2\n3\n0", "1", "3", "6");
        }

        [TestMethod]
        public void Rule30Test() {
            http://golf.shinh.org/p.rb?Rule+30
            RunProgram("SS+s+X{2%Hxi^@2%xi^^@2%++\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("SS++{cSs+}2*cLM{0s{2%+F\" ## \"s@m", "##  #   #", "## #### ###");
            RunProgram("SS++{cSs+}2*cLM{\" ## \"s{2%m|s@m", "##  #   #", "## #### ###");
        }

        [TestMethod]
        public void TransposeTest() {
            RunProgram("LME", "abc\ndef\nghi", "adg", "beh", "cfi");
        }

        [TestMethod]
        public void NegateTest() {
            RunProgram("#N", "13", "-13");
            RunProgram("#N", "-14", "14");
        }

        [TestMethod]
        public void NegativePadTest() {
            RunProgram("1N(P}", "abc\nzxcvb", "ab", "zxcv");
        }

        [TestMethod]
        public void OverlappingTriplesTest() {
            RunProgram("c%R~~{:(3)msdE", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("c%R~~{:(3)PFd", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("[;%R~~{;(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("X%R~~{x(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("%R~~{y(3)PF", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("2(y2N){+cP~Fd", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("es{+3)cm2N)sdn*", "abcdefg", "abc", "bcd", "cde", "def", "efg");
            RunProgram("{zs+3)Zm2N)n*", "abcdefg", "abc", "bcd", "cde", "def", "efg");
        }

        [TestMethod]
        public void TwinPrimesTest() {
            http://golf.shinh.org/p.rb?Twin+primes
            RunProgram("#R~{|f%_2+|f%*1=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R~{c2+*|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R~{^c*v|f%2=f{p',p_2+PF", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R~{^c*v|f%2={_p',p_2+P}*F", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
            RunProgram("#R~{c2+*|f%2=f{c2+2l',*mE", "100", "3,5", "5,7", "11,13", "17,19", "29,31", "41,43", "59,61", "71,73");
        }

        [TestMethod]
        public void RegexTest() {
            RunProgram("\"x+\"{%$}|r", "axbxxcxxxd", "a1b2c3d");
        }

        [TestMethod]
        public void BronspeakTest() {
            http://golf.shinh.org/p.rb?Bronspeak
            RunProgram("a\"aeeiioouua\"c^+X-{]2*me*~'b+c^+x+[\"\\w+\"{1(;r|t_1N(1N)x|t_1);|t++}|r", "The quick brown fox jumped over the lazy dogs!", "Shi paocl zruwp duy hampif ivis shi kezz cugt!");
        }

        [TestMethod]
        public void CheckersPatternTest() {
            http://golf.shinh.org/p.rb?checkers+pattern

            RunProgram("S/E#[#{;{R}{r}i2%?{2%mS*P}*", "3 19",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1");
            RunProgram("S/E#[#{i2%{!cpSp};v*!P}*", "3 19",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1");
            RunProgram("S/E#R{2%mS*s#{cP1001$|t}*d", "3 19",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1",
                "0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0",
                "1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1");
        }

        [TestMethod]
        public void IsFibTest() {
            https://codegolf.stackexchange.com/questions/126373/am-i-a-fibonacci-number
            RunProgram("#1N[1{c;+s[cx<w=", "0", "1");
            RunProgram("#1N[1{c;+s[cx<w=", "13", "1");
            RunProgram("#1N[1{c;+s[cx<w=", "14", "0");
        }
    }
}