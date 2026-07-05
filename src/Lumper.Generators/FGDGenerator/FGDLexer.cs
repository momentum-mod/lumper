namespace Lumper.Generators.FGDGenerator;

using System.Collections.Generic;

public enum TokenType { Word, _String, Symbol, EOF }
public record Token(TokenType Type, string Value);

public static class FGDLexer
{
    public static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < input.Length && input[i + 1] == '/')
            {
                while (i < input.Length && input[i] != '\n') i++;
                continue;
            }

            if (c == '"')
            {
                i++;
                int start = i;
                while (i < input.Length && input[i] != '"') i++;
                tokens.Add(new Token(TokenType._String, input[start..i]));
                i++; // Consume end quote
                continue;
            }

            if ("@=:[](),".IndexOf(c) != -1)
            {
                tokens.Add(new Token(TokenType.Symbol, c.ToString()));
                i++;
                continue;
            }

            int wStart = i;
            while (i < input.Length && !char.IsWhiteSpace(input[i]) && "@=:[](),\"".IndexOf(input[i]) == -1)
            {
                i++;
            }
            tokens.Add(new Token(TokenType.Word, input[wStart..i]));
        }

        tokens.Add(new Token(TokenType.EOF, ""));
        return tokens;
    }
}