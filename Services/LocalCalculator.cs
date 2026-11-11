using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class LocalCalculator
{
    private static readonly Regex Question = new(@"^(?:what is|what's|whats|calculate)\s+([0-9.\s()+*/×÷−-]+)\??$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool TryAnswer(string input, out string answer)
    {
        answer = "";
        var text = input.Trim().Replace('’', '\'');
        var command = text.Equals("/calc", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("/calc ", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("/calc\t", StringComparison.OrdinalIgnoreCase);
        var match = command ? null : Question.Match(text);
        if (!command && !match!.Success) return false;
        var expression = command ? text[5..].Trim() : match!.Groups[1].Value.Trim();
        try
        {
            if (expression.Length is 0 or > 256) throw new FormatException("Use an expression of 1–256 characters.");
            answer = new Parser(expression.Replace('×', '*').Replace('÷', '/').Replace('−', '-')).Parse().ToString();
        }
        catch (DivideByZeroException) { answer = "Cannot divide by zero."; }
        catch (FormatException ex) { answer = "Calculation error: " + ex.Message + " Use numbers, +, -, *, /, and parentheses."; }
        return true;
    }

    private readonly struct Fraction
    {
        private readonly BigInteger numerator;
        private readonly BigInteger denominator;
        public Fraction(BigInteger numerator, BigInteger denominator)
        {
            if (denominator.IsZero) throw new DivideByZeroException();
            if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
            var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
            this.numerator = numerator / gcd;
            this.denominator = denominator / gcd;
        }
        public static Fraction operator +(Fraction a, Fraction b) => new(a.numerator * b.denominator + b.numerator * a.denominator, a.denominator * b.denominator);
        public static Fraction operator -(Fraction a, Fraction b) => new(a.numerator * b.denominator - b.numerator * a.denominator, a.denominator * b.denominator);
        public static Fraction operator *(Fraction a, Fraction b) => new(a.numerator * b.numerator, a.denominator * b.denominator);
        public static Fraction operator /(Fraction a, Fraction b) => new(a.numerator * b.denominator, a.denominator * b.numerator);
        public override string ToString()
        {
            if (denominator.IsOne) return numerator.ToString(CultureInfo.InvariantCulture);
            var remainder = denominator;
            int twos = 0, fives = 0;
            while (remainder % 2 == 0) { remainder /= 2; twos++; }
            while (remainder % 5 == 0) { remainder /= 5; fives++; }
            // Repeating decimals stay exact fractions rather than an invented rounded value.
            if (!remainder.IsOne) return $"{numerator.ToString(CultureInfo.InvariantCulture)}/{denominator.ToString(CultureInfo.InvariantCulture)}";
            var scale = Math.Max(twos, fives);
            var digits = (BigInteger.Abs(numerator) * BigInteger.Pow(10, scale) / denominator)
                .ToString(CultureInfo.InvariantCulture).PadLeft(scale + 1, '0');
            return (numerator.Sign < 0 ? "-" : "") + digits.Insert(digits.Length - scale, ".");
        }
    }

    private sealed class Parser(string text)
    {
        private int position;
        public Fraction Parse()
        {
            var value = Expression(0);
            SkipSpace();
            if (position != text.Length) throw new FormatException("Unexpected character or missing operator.");
            return value;
        }
        private Fraction Expression(int depth)
        {
            var value = Term(depth);
            while (true)
            {
                if (Take('+')) value += Term(depth);
                else if (Take('-')) value -= Term(depth);
                else return value;
            }
        }
        private Fraction Term(int depth)
        {
            var value = Value(depth);
            while (true)
            {
                if (Take('*')) value *= Value(depth);
                else if (Take('/')) value /= Value(depth);
                else return value;
            }
        }
        private Fraction Value(int depth)
        {
            if (depth > 32) throw new FormatException("Too many nested parentheses or signs.");
            if (Take('+')) return Value(depth + 1);
            if (Take('-')) return new Fraction(-1, 1) * Value(depth + 1);
            if (Take('('))
            {
                var value = Expression(depth + 1);
                if (!Take(')')) throw new FormatException("Missing closing parenthesis.");
                return value;
            }
            SkipSpace();
            var start = position;
            while (position < text.Length && char.IsAsciiDigit(text[position])) position++;
            int scale = 0;
            if (position < text.Length && text[position] == '.')
            {
                position++;
                var decimals = position;
                while (position < text.Length && char.IsAsciiDigit(text[position])) position++;
                scale = position - decimals;
            }
            var digits = text[start..position].Replace(".", "");
            if (digits.Length == 0) throw new FormatException("Expected a number.");
            return new Fraction(BigInteger.Parse(digits, CultureInfo.InvariantCulture), BigInteger.Pow(10, scale));
        }
        private bool Take(char expected)
        {
            SkipSpace();
            if (position >= text.Length || text[position] != expected) return false;
            position++;
            return true;
        }
        private void SkipSpace() { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
    }
}
